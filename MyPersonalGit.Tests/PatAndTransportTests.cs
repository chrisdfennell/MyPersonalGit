using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Tests;

/// <summary>
/// End-to-end coverage for the hashed-PAT auth paths and the git transport:
/// PAT-as-password pushes over smart HTTP, bearer-token API auth, private repo
/// read authorization, and stacked-PR retargeting after a merge. Uses the same
/// Kestrel + real-git-CLI harness as GitHttpSmokeTests.
/// </summary>
public class PatAndTransportTests : IClassFixture<GitSmokeFactory>
{
    private readonly GitSmokeFactory _factory;
    private readonly string _serverAddress;

    private const string GitUser = "patuser";
    private const string GitPass = "patpass123";

    public PatAndTransportTests(GitSmokeFactory factory)
    {
        _factory = factory;
        _serverAddress = _factory.EnsureStarted();
        Seed();
    }

    private void Seed()
    {
        var dbFactory = _factory.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = dbFactory.CreateDbContext();

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
            auth.RegisterAsync(GitUser, "patuser@test.local", GitPass).GetAwaiter().GetResult();
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
        psi.Environment["GIT_TERMINAL_PROMPT"] = "0";
        psi.Environment["GCM_INTERACTIVE"] = "never";
        // Disable credential helpers: the system Git Credential Manager stores the
        // credential from an authenticated push and would silently reuse it for
        // "anonymous" requests, making auth-rejection tests pass vacuously.
        psi.Environment["GIT_CONFIG_COUNT"] = "1";
        psi.Environment["GIT_CONFIG_KEY_0"] = "credential.helper";
        psi.Environment["GIT_CONFIG_VALUE_0"] = "";

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
        RunGit("config user.email pat@test.local", dir);
        RunGit("config user.name pat", dir);
        File.WriteAllText(Path.Combine(dir, "file.txt"), "pat test content\n");
        Assert.Equal(0, RunGit("add .", dir).ExitCode);
        Assert.Equal(0, RunGit("commit -m initial", dir).ExitCode);
        return dir;
    }

    private async Task<string> CreateRepoOnDiskAsync(string name, bool isPrivate = false)
    {
        var repos = _factory.Services.GetRequiredService<IRepositoryService>();
        await repos.CreateRepositoryAsync(name, GitUser);
        var path = Path.Combine(_factory.ProjectRoot, name + ".git");
        LibGit2Sharp.Repository.Init(path, isBare: true);

        if (isPrivate)
        {
            var dbFactory = _factory.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = dbFactory.CreateDbContext();
            var repo = db.Repositories.First(r => r.Name.ToLower() == name.ToLower());
            repo.IsPrivate = true;
            db.SaveChanges();
        }
        return path;
    }

    private Task<string> CreatePatAsync(string[] scopes, DateTime? expiresAt = null)
    {
        var profiles = _factory.Services.GetRequiredService<IUserProfileService>();
        return profiles.CreateTokenAsync(GitUser, "test token", scopes, expiresAt);
    }

    private string UrlWith(string user, string pass, string repoSegment) =>
        _serverAddress.Replace("://", $"://{user}:{pass}@") + $"/git/{repoSegment}";

    // ---------------------------------------------------------------
    // PAT as git password (smart HTTP)
    // ---------------------------------------------------------------

    [Fact]
    public async Task Push_With_Pat_As_Password_Succeeds()
    {
        await CreateRepoOnDiskAsync("pat-push");
        var token = await CreatePatAsync(new[] { "repo" });
        var work = CreateLocalCommit(Path.Combine(_factory.ProjectRoot, "work-pat-push"));

        var push = RunGit($"push {UrlWith(GitUser, token, "pat-push.git")} main", work);
        Assert.True(push.ExitCode == 0, $"push with PAT failed:\n{push.Output}");
    }

    [Fact]
    public async Task Push_With_Pat_For_Wrong_Username_Is_Rejected()
    {
        await CreateRepoOnDiskAsync("pat-wrong-user");
        var token = await CreatePatAsync(new[] { "repo" });
        var work = CreateLocalCommit(Path.Combine(_factory.ProjectRoot, "work-pat-wrong"));

        // Token belongs to patuser but is presented as admin.
        var push = RunGit($"push {UrlWith("admin", token, "pat-wrong-user.git")} main", work);
        Assert.NotEqual(0, push.ExitCode);
    }

    [Fact]
    public async Task Push_With_Expired_Pat_Is_Rejected()
    {
        await CreateRepoOnDiskAsync("pat-expired");
        var token = await CreatePatAsync(new[] { "repo" }, DateTime.UtcNow.AddDays(-1));
        var work = CreateLocalCommit(Path.Combine(_factory.ProjectRoot, "work-pat-expired"));

        var push = RunGit($"push {UrlWith(GitUser, token, "pat-expired.git")} main", work);
        Assert.NotEqual(0, push.ExitCode);
    }

    // ---------------------------------------------------------------
    // Bearer token API auth (hashed lookup)
    // ---------------------------------------------------------------

    [Fact]
    public async Task Api_Request_With_Valid_Token_Is_Authenticated()
    {
        var token = await CreatePatAsync(new[] { "repo" });

        using var http = new HttpClient { BaseAddress = new Uri(_serverAddress) };
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/repos");
        req.Headers.Add("Authorization", $"Bearer {token}");
        var resp = await http.SendAsync(req);

        Assert.NotEqual(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Api_Request_With_Valid_Token_Updates_LastUsed()
    {
        var token = await CreatePatAsync(new[] { "repo" });

        using var http = new HttpClient { BaseAddress = new Uri(_serverAddress) };
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/repos");
        req.Headers.Add("Authorization", $"Bearer {token}");
        await http.SendAsync(req);

        var dbFactory = _factory.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = dbFactory.CreateDbContext();
        var hash = PatTokenService.HashToken(token);
        var row = db.PersonalAccessTokens.First(t => t.TokenHash == hash);
        Assert.NotNull(row.LastUsed);
        Assert.Equal(string.Empty, row.Token); // plaintext never persisted
    }

    [Fact]
    public async Task Api_Request_With_Expired_Token_Returns401()
    {
        var token = await CreatePatAsync(new[] { "repo" }, DateTime.UtcNow.AddDays(-1));

        using var http = new HttpClient { BaseAddress = new Uri(_serverAddress) };
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/repos");
        req.Headers.Add("Authorization", $"Bearer {token}");
        var resp = await http.SendAsync(req);

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        Assert.Contains("expired", await resp.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Api_Create_Repo_Then_Push_With_Pat_Round_Trips()
    {
        var token = await CreatePatAsync(new[] { "repo" });
        using var http = new HttpClient { BaseAddress = new Uri(_serverAddress) };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/repos")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { name = "api-created", description = "made via API" }),
                System.Text.Encoding.UTF8, "application/json")
        };
        req.Headers.Add("Authorization", $"Bearer {token}");
        var resp = await http.SendAsync(req);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("api-created", body);

        // Duplicate create conflicts rather than clobbering.
        var dup = new HttpRequestMessage(HttpMethod.Post, "/api/v1/repos")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { name = "api-created" }),
                System.Text.Encoding.UTF8, "application/json")
        };
        dup.Headers.Add("Authorization", $"Bearer {token}");
        Assert.Equal(HttpStatusCode.Conflict, (await http.SendAsync(dup)).StatusCode);

        // The repo the API just created accepts a real push with the same PAT.
        var work = CreateLocalCommit(Path.Combine(_factory.ProjectRoot, "work-api-created"));
        var push = RunGit($"push {UrlWith(GitUser, token, "api-created.git")} main", work);
        Assert.True(push.ExitCode == 0, $"push to API-created repo failed:\n{push.Output}");
    }

    // ---------------------------------------------------------------
    // Private repo reads over smart HTTP
    // ---------------------------------------------------------------

    [Fact]
    public async Task Private_Repo_Anonymous_Clone_Is_Rejected_But_Authed_Clone_Works()
    {
        var bare = await CreateRepoOnDiskAsync("private-read", isPrivate: true);
        var work = CreateLocalCommit(Path.Combine(_factory.ProjectRoot, "work-private"));
        Assert.Equal(0, RunGit($"push {UrlWith(GitUser, GitPass, "private-read.git")} main", work).ExitCode);

        var anonDir = Path.Combine(_factory.ProjectRoot, "clone-private-anon");
        var anon = RunGit($"clone {_serverAddress}/git/private-read.git \"{anonDir}\"", _factory.ProjectRoot);
        Assert.NotEqual(0, anon.ExitCode);

        var authedDir = Path.Combine(_factory.ProjectRoot, "clone-private-authed");
        var authed = RunGit($"clone {UrlWith(GitUser, GitPass, "private-read.git")} \"{authedDir}\"", _factory.ProjectRoot);
        Assert.True(authed.ExitCode == 0, $"authed clone failed:\n{authed.Output}");
    }

    // ---------------------------------------------------------------
    // Stacked pull requests
    // ---------------------------------------------------------------

    [Fact]
    public async Task Merging_Stack_Base_Retargets_Child_And_Computes_Stack()
    {
        await CreateRepoOnDiskAsync("stacked");
        var work = CreateLocalCommit(Path.Combine(_factory.ProjectRoot, "work-stacked"));
        var url = UrlWith(GitUser, GitPass, "stacked.git");

        Assert.Equal(0, RunGit($"push {url} main", work).ExitCode);

        Assert.Equal(0, RunGit("checkout -b feat-a", work).ExitCode);
        File.WriteAllText(Path.Combine(work, "a.txt"), "feature a\n");
        RunGit("add .", work);
        Assert.Equal(0, RunGit("commit -m feat-a", work).ExitCode);
        Assert.Equal(0, RunGit($"push {url} feat-a", work).ExitCode);

        Assert.Equal(0, RunGit("checkout -b feat-b", work).ExitCode);
        File.WriteAllText(Path.Combine(work, "b.txt"), "feature b\n");
        RunGit("add .", work);
        Assert.Equal(0, RunGit("commit -m feat-b", work).ExitCode);
        Assert.Equal(0, RunGit($"push {url} feat-b", work).ExitCode);

        var prService = _factory.Services.GetRequiredService<IPullRequestService>();
        var prA = await prService.CreatePullRequestAsync("stacked", "Feature A", null, GitUser, "feat-a", "main");
        var prB = await prService.CreatePullRequestAsync("stacked", "Feature B", null, GitUser, "feat-b", "feat-a");

        // Stack is visible from the child: A (depth 0) then B (depth 1, current).
        var stack = await prService.GetStackAsync("stacked", prB.Number);
        Assert.Equal(2, stack.Count);
        Assert.Equal(prA.Number, stack[0].PullRequest.Number);
        Assert.False(stack[0].IsCurrent);
        Assert.Equal(prB.Number, stack[1].PullRequest.Number);
        Assert.True(stack[1].IsCurrent);

        // Merge the base PR — the child must retarget from feat-a to main.
        var (ok, err) = await prService.MergePullRequestAsync("stacked", prA.Number, GitUser);
        Assert.True(ok, $"merge failed: {err}");

        var reloadedB = await prService.GetPullRequestAsync("stacked", prB.Number);
        Assert.NotNull(reloadedB);
        Assert.Equal("main", reloadedB!.TargetBranch);
        Assert.Contains(reloadedB.Comments, c => c.Body.Contains("Base automatically changed"));
    }

    [Fact]
    public async Task Lone_Pull_Request_Has_No_Stack()
    {
        await CreateRepoOnDiskAsync("unstacked");
        var work = CreateLocalCommit(Path.Combine(_factory.ProjectRoot, "work-unstacked"));
        var url = UrlWith(GitUser, GitPass, "unstacked.git");
        Assert.Equal(0, RunGit($"push {url} main", work).ExitCode);
        Assert.Equal(0, RunGit("checkout -b solo", work).ExitCode);
        File.WriteAllText(Path.Combine(work, "s.txt"), "solo\n");
        RunGit("add .", work);
        Assert.Equal(0, RunGit("commit -m solo", work).ExitCode);
        Assert.Equal(0, RunGit($"push {url} solo", work).ExitCode);

        var prService = _factory.Services.GetRequiredService<IPullRequestService>();
        var pr = await prService.CreatePullRequestAsync("unstacked", "Solo", null, GitUser, "solo", "main");

        var stack = await prService.GetStackAsync("unstacked", pr.Number);
        Assert.Empty(stack);
    }
}

/// <summary>
/// Startup migration from plaintext token storage (DB rows and legacy
/// {user}_tokens.json files) to hashed-at-rest storage.
/// </summary>
public class PatMigrationTests : IDisposable
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly string _dataDir;

    public PatMigrationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);
        _dataDir = Path.Combine(Path.GetTempPath(), "mpg-pat-migration-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dataDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dataDir, true); } catch { }
    }

    [Fact]
    public void Migration_Hashes_Plaintext_Db_Rows()
    {
        using (var db = _dbFactory.CreateDbContext())
        {
            db.PersonalAccessTokens.Add(new PersonalAccessToken
            {
                Username = "alice",
                Name = "old token",
                Token = "mypg_plaintext_from_before_hashing",
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        using (var db = _dbFactory.CreateDbContext())
        {
            PatTokenService.MigrateToHashedStorage(db, _dataDir, _ => { });
        }

        using (var db = _dbFactory.CreateDbContext())
        {
            var row = db.PersonalAccessTokens.Single();
            Assert.Equal(string.Empty, row.Token);
            Assert.Equal(PatTokenService.HashToken("mypg_plaintext_from_before_hashing"), row.TokenHash);
            Assert.StartsWith("mypg_", row.TokenPrefix);
        }
    }

    [Fact]
    public void Migration_Imports_Legacy_Json_Files_And_Renames_Them()
    {
        var legacy = new[]
        {
            new PersonalAccessToken
            {
                Id = 1,
                Username = "bob",
                Name = "ci token",
                Token = "mypg_legacy_json_token_value",
                Scopes = new[] { "repo" },
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            }
        };
        var file = Path.Combine(_dataDir, "bob_tokens.json");
        File.WriteAllText(file, JsonSerializer.Serialize(legacy));

        using (var db = _dbFactory.CreateDbContext())
        {
            PatTokenService.MigrateToHashedStorage(db, _dataDir, _ => { });
        }

        Assert.False(File.Exists(file), "legacy plaintext file should have been renamed");
        Assert.True(File.Exists(file + ".migrated"));

        using (var db = _dbFactory.CreateDbContext())
        {
            var row = db.PersonalAccessTokens.Single(t => t.Username == "bob");
            Assert.Equal(string.Empty, row.Token);
            Assert.Equal(PatTokenService.HashToken("mypg_legacy_json_token_value"), row.TokenHash);
            Assert.Equal(new[] { "repo" }, row.Scopes);
        }
    }

    [Fact]
    public void Migration_Is_Idempotent()
    {
        var legacy = new[]
        {
            new PersonalAccessToken { Id = 1, Username = "carol", Name = "t", Token = "mypg_carol_token", CreatedAt = DateTime.UtcNow }
        };
        File.WriteAllText(Path.Combine(_dataDir, "carol_tokens.json"), JsonSerializer.Serialize(legacy));

        using (var db = _dbFactory.CreateDbContext())
        {
            PatTokenService.MigrateToHashedStorage(db, _dataDir, _ => { });
            PatTokenService.MigrateToHashedStorage(db, _dataDir, _ => { });
        }

        using (var db = _dbFactory.CreateDbContext())
        {
            Assert.Single(db.PersonalAccessTokens.Where(t => t.Username == "carol").ToList());
        }
    }

    [Fact]
    public async Task ValidateAsync_Matches_By_Hash_And_Rejects_Unknown()
    {
        string token;
        using (var db = _dbFactory.CreateDbContext())
        {
            token = "mypg_validate_me";
            db.PersonalAccessTokens.Add(new PersonalAccessToken
            {
                Username = "dave",
                Name = "t",
                Token = string.Empty,
                TokenHash = PatTokenService.HashToken(token),
                TokenPrefix = PatTokenService.TokenPrefix(token),
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        var svc = new PatTokenService(_dbFactory, NullLogger<PatTokenService>.Instance);

        var matched = await svc.ValidateAsync(token);
        Assert.NotNull(matched);
        Assert.Equal("dave", matched!.Username);
        Assert.NotNull(matched.LastUsed);

        Assert.Null(await svc.ValidateAsync("mypg_wrong_token"));
        Assert.Null(await svc.ValidateAsync(""));
    }
}

/// <summary>
/// FTS5 code search against a real SQLite database — the in-memory EF provider
/// can't exercise the virtual table, triggers, or MATCH queries.
/// </summary>
public class CodeSearchFtsTests : IDisposable
{
    private readonly string _dbPath;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CodeSearchFtsTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), "mpg-fts-" + Guid.NewGuid().ToString("N") + ".db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _dbFactory = new TestDbContextFactory(options);

        using var db = _dbFactory.CreateDbContext();
        db.Database.EnsureCreated();
        CodeSearchFts.EnsureCreated(db, _ => { });

        db.Repositories.Add(new Repository { Name = "searchrepo", Owner = "tester", IsPrivate = false, DefaultBranch = "main" });
        db.CodeSearchIndices.Add(new CodeSearchIndex
        {
            RepoName = "searchrepo",
            FilePath = "src/Program.cs",
            ContentHash = "h1",
            Content = "public static void Main(string[] args)\n{\n    Console.WriteLine(\"UniqueNeedleXyz\");\n}\n",
            IndexedAt = DateTime.UtcNow
        });
        db.CodeSearchIndices.Add(new CodeSearchIndex
        {
            RepoName = "searchrepo",
            FilePath = "README.md",
            ContentHash = "h2",
            Content = "# Search Repo\nNothing interesting here.\n",
            IndexedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch { }
    }

    private CodeSearchService CreateService()
    {
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var admin = NSubstitute.Substitute.For<IAdminService>();
        return new CodeSearchService(_dbFactory, config, admin, NullLogger<CodeSearchService>.Instance);
    }

    [Fact]
    public async Task Fts_Search_Finds_Substring_Case_Insensitively()
    {
        var results = await CreateService().SearchAsync("uniqueneedle");
        var hit = Assert.Single(results);
        Assert.Equal("src/Program.cs", hit.FilePath);
        Assert.Contains(hit.Matches, m => m.Line.Contains("UniqueNeedleXyz"));
    }

    [Fact]
    public async Task Fts_Search_Reflects_Updates_And_Deletes_Via_Triggers()
    {
        var svc = CreateService();

        using (var db = _dbFactory.CreateDbContext())
        {
            var row = db.CodeSearchIndices.First(i => i.FilePath == "README.md");
            row.Content = "# Search Repo\nNow contains FreshTriggerToken here.\n";
            db.SaveChanges();
        }
        var afterUpdate = await svc.SearchAsync("freshtriggertoken");
        Assert.Single(afterUpdate);

        using (var db = _dbFactory.CreateDbContext())
        {
            db.CodeSearchIndices.RemoveRange(db.CodeSearchIndices.Where(i => i.FilePath == "README.md"));
            db.SaveChanges();
        }
        var afterDelete = await svc.SearchAsync("freshtriggertoken");
        Assert.Empty(afterDelete);
    }

    [Fact]
    public async Task Short_Query_Falls_Back_To_Like_Scan()
    {
        // 2-char queries bypass FTS (trigram needs >= 3 chars) but must still work.
        var results = await CreateService().SearchAsync("Xy");
        Assert.Single(results);
    }

    [Fact]
    public async Task Fts_Operators_In_Query_Are_Treated_As_Literals()
    {
        // Must not throw an FTS syntax error or match everything.
        var results = await CreateService().SearchAsync("NEAR(a b)");
        Assert.Empty(results);
    }
}
