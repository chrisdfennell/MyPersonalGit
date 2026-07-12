using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Tests;

/// <summary>
/// End-to-end git smoke tests: host the real app on Kestrel (a real TCP port, unlike
/// the in-memory TestServer) and drive it with the actual `git` CLI. This exercises
/// the full push/clone path — clone URL format, basic auth, permission lookups,
/// on-disk repo resolution, git http-backend CGI, and the pre-receive hook — which
/// unit tests cannot cover. Every bug fixed on 2026-07-11 (the .git-suffix 403, the
/// project-root 404, the suffix-less-directory 404, the sed-broken hook and its
/// PAT-blocked callback) would have been caught here.
/// </summary>
/// <summary>Captures app log output so smoke-test failures can show what the server saw.</summary>
public sealed class SmokeLogSink : ILoggerProvider
{
    public static readonly System.Collections.Concurrent.ConcurrentQueue<string> Lines = new();

    public ILogger CreateLogger(string categoryName) => new SinkLogger(categoryName);

    private sealed class SinkLogger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Lines.Enqueue($"[{logLevel}] {category}: {formatter(state, exception)}");
            while (Lines.Count > 200 && Lines.TryDequeue(out _)) { }
        }
    }

    public void Dispose() { }
}

public class GitSmokeFactory : WebApplicationFactory<Program>
{
    public string ProjectRoot { get; } =
        Path.Combine(Path.GetTempPath(), "mpg-git-smoke-" + Guid.NewGuid().ToString("N"));

    public GitSmokeFactory()
    {
        // Must be set before Program's top-level statements read it.
        Environment.SetEnvironmentVariable("MPG_TEST_HOST", "1");
        Directory.CreateDirectory(ProjectRoot);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Appended after appsettings*.json, so these win. RequireAuth must be true:
        // appsettings.Development.json turns it off, but the smoke tests exist to
        // exercise the production auth path.
        builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Git:ProjectRoot"] = ProjectRoot,
                ["Git:RequireAuth"] = "true"
            }));

        builder.ConfigureLogging(logging => logging.AddProvider(new SmokeLogSink()));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();

            services.RemoveAll(typeof(IDbContextFactory<AppDbContext>));
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            var efServices = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();
            // Unique DB per factory so runs don't share state with other fixtures.
            var dbName = "mpg-git-smoke-" + Guid.NewGuid().ToString("N");
            services.AddDbContextFactory<AppDbContext>(o =>
                o.UseInMemoryDatabase(dbName)
                 .UseInternalServiceProvider(efServices));
        });
    }

    private readonly object _startLock = new();
    private bool _started;

    /// <summary>Kestrel must be configured exactly once per factory, but xUnit
    /// constructs the test class (and thus calls this) once per test.</summary>
    public string EnsureStarted()
    {
        lock (_startLock)
        {
            if (!_started)
            {
                this.UseKestrel(0);
                StartServer();
                var feature = Services.GetRequiredService<IServer>()
                    .Features.Get<IServerAddressesFeature>();
                ServerAddress = feature!.Addresses.First().Replace("[::]", "127.0.0.1");
                _started = true;
            }
            return ServerAddress;
        }
    }

    public string ServerAddress { get; private set; } = "";

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { Directory.Delete(ProjectRoot, true); } catch { /* best effort */ }
    }
}

public class GitHttpSmokeTests : IClassFixture<GitSmokeFactory>
{
    private readonly GitSmokeFactory _factory;
    private readonly string _serverAddress;

    private const string GitUser = "gituser";
    private const string GitPass = "gitpass123";

    public GitHttpSmokeTests(GitSmokeFactory factory)
    {
        _factory = factory;
        _serverAddress = _factory.EnsureStarted();
        Seed();
    }

    /// <summary>Admin (satisfies the first-run setup gate) plus a NON-admin repo owner —
    /// non-admin matters: admins bypass the permission checks we want covered.</summary>
    private void Seed()
    {
        var dbFactory = _factory.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = dbFactory.CreateDbContext();

        // The admin-configured project root must win over appsettings ("/repos") —
        // this is the exact resolution the NAS 404 bug was about.
        if (!db.SystemSettings.Any())
        {
            db.SystemSettings.Add(new SystemSettings { ProjectRoot = _factory.ProjectRoot });
            db.SaveChanges();
        }

        if (!db.Users.Any(u => u.IsAdmin))
        {
            db.Users.Add(new User
            {
                Username = "admin",
                Email = "admin@test.local",
                PasswordHash = "x",
                IsAdmin = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        if (!db.Users.Any(u => u.Username == GitUser))
        {
            var auth = _factory.Services.GetRequiredService<IAuthService>();
            auth.RegisterAsync(GitUser, "gituser@test.local", GitPass).GetAwaiter().GetResult();
        }
    }

    private static (int ExitCode, string Output) RunGit(string args, string workDir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        // Never prompt: fail fast instead of hanging the test on a credential dialog.
        psi.Environment["GIT_TERMINAL_PROMPT"] = "0";
        psi.Environment["GCM_INTERACTIVE"] = "never";

        using var p = Process.Start(psi)!;
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit(60_000);
        return (p.ExitCode, stdout + stderr);
    }

    private string CreateLocalCommit(string dir)
    {
        Directory.CreateDirectory(dir);
        Assert.Equal(0, RunGit("init -b main", dir).ExitCode);
        RunGit("config user.email smoke@test.local", dir);
        RunGit("config user.name smoke", dir);
        File.WriteAllText(Path.Combine(dir, "hello.txt"), "smoke test content\n");
        Assert.Equal(0, RunGit("add .", dir).ExitCode);
        Assert.Equal(0, RunGit("commit -m initial", dir).ExitCode);
        return dir;
    }

    private string AuthedUrl(string repoSegment) =>
        _serverAddress.Replace("://", $"://{GitUser}:{GitPass}@") + $"/git/{repoSegment}";

    [Fact]
    public async Task Push_Then_Clone_RoundTrips_Standard_Layout()
    {
        // Repo registered under a name WITH the suffix (like older UI code did) but
        // stored on disk as {name}.git — lookups must still match the clean name.
        var repos = _factory.Services.GetRequiredService<IRepositoryService>();
        await repos.CreateRepositoryAsync("smoke.git", GitUser);
        LibGit2Sharp.Repository.Init(Path.Combine(_factory.ProjectRoot, "smoke.git"), isBare: true);

        var work = CreateLocalCommit(Path.Combine(_factory.ProjectRoot, "work-standard"));

        var push = RunGit($"push {AuthedUrl("smoke.git")} main", work);
        Assert.True(push.ExitCode == 0, $"push failed:\n{push.Output}");

        // Anonymous clone (public repo read path — no credentials in the URL).
        var cloneDir = Path.Combine(_factory.ProjectRoot, "clone-standard");
        var clone = RunGit($"clone {_serverAddress}/git/smoke.git \"{cloneDir}\"", _factory.ProjectRoot);
        Assert.True(clone.ExitCode == 0, $"clone failed:\n{clone.Output}");
        Assert.Equal("smoke test content\n",
            File.ReadAllText(Path.Combine(cloneDir, "hello.txt")).Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task Push_Works_When_Repo_Stored_Without_Git_Suffix()
    {
        // Some repos live on disk as plain {name}; the URL still says {name}.git.
        var repos = _factory.Services.GetRequiredService<IRepositoryService>();
        await repos.CreateRepositoryAsync("plain", GitUser);
        LibGit2Sharp.Repository.Init(Path.Combine(_factory.ProjectRoot, "plain"), isBare: true);

        var work = CreateLocalCommit(Path.Combine(_factory.ProjectRoot, "work-plain"));

        var push = RunGit($"push {AuthedUrl("plain.git")} main", work);
        Assert.True(push.ExitCode == 0, $"push failed:\n{push.Output}");
    }

    [Fact]
    public async Task Push_Without_Credentials_Is_Rejected()
    {
        var repos = _factory.Services.GetRequiredService<IRepositoryService>();
        await repos.CreateRepositoryAsync("locked", GitUser);
        LibGit2Sharp.Repository.Init(Path.Combine(_factory.ProjectRoot, "locked.git"), isBare: true);

        var work = CreateLocalCommit(Path.Combine(_factory.ProjectRoot, "work-locked"));

        var push = RunGit($"push {_serverAddress}/git/locked.git main", work);
        Assert.NotEqual(0, push.ExitCode);
    }
}
