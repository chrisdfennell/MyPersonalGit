using System.Text.Json;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;
using GitRepository = LibGit2Sharp.Repository;

namespace MyPersonalGit.Data;

public interface IDependencyUpdateService
{
    Task<List<DependencyUpdateConfig>> GetConfigsAsync(string repoName);
    Task<DependencyUpdateConfig> EnableUpdatesAsync(string repoName, string ecosystem, string schedule);
    Task<bool> DisableUpdatesAsync(string repoName, int configId);
    Task<List<DependencyUpdateLog>> GetLogsAsync(string repoName, int limit = 50);
    Task RunUpdateCheckAsync(string repoName);
}

public class DependencyUpdateService : IDependencyUpdateService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<DependencyUpdateService> _logger;
    private readonly IPullRequestService _prService;
    private readonly IAdminService _adminService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public DependencyUpdateService(
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<DependencyUpdateService> logger,
        IPullRequestService prService,
        IAdminService adminService,
        IHttpClientFactory httpClientFactory,
        IConfiguration config)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _prService = prService;
        _adminService = adminService;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public async Task<List<DependencyUpdateConfig>> GetConfigsAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.DependencyUpdateConfigs.Where(c => c.RepoName == repoName).ToListAsync();
    }

    public async Task<DependencyUpdateConfig> EnableUpdatesAsync(string repoName, string ecosystem, string schedule)
    {
        using var db = _dbFactory.CreateDbContext();
        var existing = await db.DependencyUpdateConfigs.FirstOrDefaultAsync(c => c.RepoName == repoName && c.Ecosystem == ecosystem);

        if (existing != null)
        {
            existing.Schedule = schedule;
            existing.IsEnabled = true;
        }
        else
        {
            existing = new DependencyUpdateConfig
            {
                RepoName = repoName,
                Ecosystem = ecosystem,
                Schedule = schedule,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };
            db.DependencyUpdateConfigs.Add(existing);
        }

        await db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DisableUpdatesAsync(string repoName, int configId)
    {
        using var db = _dbFactory.CreateDbContext();
        var config = await db.DependencyUpdateConfigs.FindAsync(configId);
        if (config == null || config.RepoName != repoName) return false;
        config.IsEnabled = false;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<DependencyUpdateLog>> GetLogsAsync(string repoName, int limit = 50)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.DependencyUpdateLogs
            .Where(l => l.RepoName == repoName)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task RunUpdateCheckAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        var configs = await db.DependencyUpdateConfigs
            .Where(c => c.RepoName == repoName && c.IsEnabled)
            .ToListAsync();

        if (!configs.Any()) return;

        var repoPath = await GetRepoPath(repoName);
        if (repoPath == null || !GitRepository.IsValid(repoPath)) return;

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        foreach (var config in configs)
        {
            try
            {
                // Count open dependency-update PRs
                var openPRs = await db.PullRequests
                    .CountAsync(p => p.RepoName == repoName &&
                                     p.State == PullRequestState.Open &&
                                     p.SourceBranch.StartsWith("dependabot/"));
                if (openPRs >= config.OpenPRLimit) continue;

                var updates = await FindUpdatesAsync(repoPath, config.Ecosystem, httpClient);

                foreach (var (packageName, currentVersion, newVersion, filePath) in updates.Take(config.OpenPRLimit - openPRs))
                {
                    // Check if we already created a PR for this update
                    var branchName = $"dependabot/{config.Ecosystem.ToLowerInvariant()}/{packageName}-{newVersion}";
                    var existingPr = await db.PullRequests
                        .AnyAsync(p => p.RepoName == repoName && p.SourceBranch == branchName);
                    if (existingPr) continue;

                    var created = await CreateUpdatePRAsync(repoName, repoPath, config.Ecosystem, packageName, currentVersion, newVersion, filePath, branchName);
                    if (created)
                    {
                        db.DependencyUpdateLogs.Add(new DependencyUpdateLog
                        {
                            ConfigId = config.Id,
                            RepoName = repoName,
                            PackageName = packageName,
                            CurrentVersion = currentVersion,
                            NewVersion = newVersion,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                config.LastRunAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking updates for {Ecosystem} in {RepoName}", config.Ecosystem, repoName);
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task<List<(string PackageName, string CurrentVersion, string NewVersion, string FilePath)>> FindUpdatesAsync(
        string repoPath, string ecosystem, HttpClient httpClient)
    {
        var updates = new List<(string, string, string, string)>();

        using var repo = new GitRepository(repoPath);
        var head = repo.Head;
        if (head?.Tip == null) return updates;

        switch (ecosystem.ToLowerInvariant())
        {
            case "nuget":
                updates.AddRange(await FindNuGetUpdatesAsync(repo, httpClient));
                break;
            case "npm":
                updates.AddRange(await FindNpmUpdatesAsync(repo, httpClient));
                break;
            case "pip":
            case "pypi":
                updates.AddRange(await FindPyPIUpdatesAsync(repo, httpClient));
                break;
        }

        return updates;
    }

    private static async Task<List<(string, string, string, string)>> FindNuGetUpdatesAsync(GitRepository repo, HttpClient httpClient)
    {
        var updates = new List<(string, string, string, string)>();
        var tree = repo.Head.Tip.Tree;

        foreach (var (blob, filePath) in WalkTreeForFiles(tree, "", ".csproj"))
        {
            using var reader = new StreamReader(blob.GetContentStream());
            var content = await reader.ReadToEndAsync();

            var matches = Regex.Matches(content, @"<PackageReference\s+Include=""([^""]+)""\s+Version=""([^""]+)""", RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                var name = match.Groups[1].Value;
                var currentVersion = match.Groups[2].Value;

                try
                {
                    var response = await httpClient.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{name.ToLowerInvariant()}/index.json");
                    using var doc = JsonDocument.Parse(response);
                    var versions = doc.RootElement.GetProperty("versions");
                    var latest = versions.EnumerateArray().LastOrDefault().GetString();
                    if (latest != null && latest != currentVersion && !latest.Contains("-"))
                        updates.Add((name, currentVersion, latest, filePath));
                }
                catch { }
            }
        }

        return updates;
    }

    private static async Task<List<(string, string, string, string)>> FindNpmUpdatesAsync(GitRepository repo, HttpClient httpClient)
    {
        var updates = new List<(string, string, string, string)>();
        var tree = repo.Head.Tip.Tree;

        foreach (var (blob, filePath) in WalkTreeForFiles(tree, "", "package.json"))
        {
            if (filePath.Contains("node_modules")) continue;
            using var reader = new StreamReader(blob.GetContentStream());
            var content = await reader.ReadToEndAsync();

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                foreach (var section in new[] { "dependencies", "devDependencies" })
                {
                    if (!root.TryGetProperty(section, out var deps)) continue;
                    foreach (var dep in deps.EnumerateObject())
                    {
                        var version = Regex.Replace(dep.Value.GetString() ?? "", @"^[\^~>=<]*", "");
                        if (string.IsNullOrEmpty(version) || !char.IsDigit(version[0])) continue;

                        try
                        {
                            var response = await httpClient.GetStringAsync($"https://registry.npmjs.org/{dep.Name}/latest");
                            using var pkgDoc = JsonDocument.Parse(response);
                            var latest = pkgDoc.RootElement.GetProperty("version").GetString();
                            if (latest != null && latest != version)
                                updates.Add((dep.Name, version, latest, filePath));
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        return updates;
    }

    private static async Task<List<(string, string, string, string)>> FindPyPIUpdatesAsync(GitRepository repo, HttpClient httpClient)
    {
        var updates = new List<(string, string, string, string)>();
        var tree = repo.Head.Tip.Tree;

        foreach (var (blob, filePath) in WalkTreeForFiles(tree, "", "requirements.txt"))
        {
            using var reader = new StreamReader(blob.GetContentStream());
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith('#') || line.StartsWith('-')) continue;
                var match = Regex.Match(line, @"^([a-zA-Z0-9_.-]+)\s*==\s*([0-9][^\s,;]*)");
                if (!match.Success) continue;

                var name = match.Groups[1].Value;
                var currentVersion = match.Groups[2].Value;

                try
                {
                    var response = await httpClient.GetStringAsync($"https://pypi.org/pypi/{name}/json");
                    using var doc = JsonDocument.Parse(response);
                    var latest = doc.RootElement.GetProperty("info").GetProperty("version").GetString();
                    if (latest != null && latest != currentVersion)
                        updates.Add((name, currentVersion, latest, filePath));
                }
                catch { }
            }
        }

        return updates;
    }

    private async Task<bool> CreateUpdatePRAsync(string repoName, string repoPath, string ecosystem,
        string packageName, string currentVersion, string newVersion, string filePath, string branchName)
    {
        try
        {
            using var repo = new GitRepository(repoPath);
            var defaultBranch = repo.Head;
            if (defaultBranch?.Tip == null) return false;

            // Create branch from default
            var branch = repo.CreateBranch(branchName, defaultBranch.Tip);

            // Read and update the file
            var entry = defaultBranch.Tip[filePath];
            if (entry == null) return false;

            var blob = (Blob)entry.Target;
            using var reader = new StreamReader(blob.GetContentStream());
            var content = await reader.ReadToEndAsync();

            var updatedContent = ecosystem.ToLowerInvariant() switch
            {
                "nuget" => content.Replace($"Version=\"{currentVersion}\"", $"Version=\"{newVersion}\""),
                "npm" => content.Replace($"\"{packageName}\": \"{currentVersion}\"", $"\"{packageName}\": \"{newVersion}\"")
                                .Replace($"\"{packageName}\": \"^{currentVersion}\"", $"\"{packageName}\": \"^{newVersion}\"")
                                .Replace($"\"{packageName}\": \"~{currentVersion}\"", $"\"{packageName}\": \"~{newVersion}\""),
                "pip" or "pypi" => content.Replace($"{packageName}=={currentVersion}", $"{packageName}=={newVersion}"),
                _ => content
            };

            if (updatedContent == content) return false;

            // Create the commit on the new branch
            var updatedBlob = repo.ObjectDatabase.CreateBlob(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(updatedContent)));
            var treeDefinition = TreeDefinition.From(defaultBranch.Tip);
            treeDefinition.Add(filePath, updatedBlob, Mode.NonExecutableFile);
            var tree = repo.ObjectDatabase.CreateTree(treeDefinition);

            var author = new Signature("dependabot", "dependabot@localhost", DateTimeOffset.Now);
            var commit = repo.ObjectDatabase.CreateCommit(
                author, author,
                $"Bump {packageName} from {currentVersion} to {newVersion}",
                tree, new[] { defaultBranch.Tip },
                prettifyMessage: true);

            repo.Refs.UpdateTarget(repo.Refs[branch.CanonicalName], commit.Id);

            // Create PR
            await _prService.CreatePullRequestAsync(
                repoName,
                $"Bump {packageName} from {currentVersion} to {newVersion}",
                $"Bumps [{packageName}] from {currentVersion} to {newVersion}.\n\n---\n\nDependabot will resolve any conflicts with this PR as long as you don't alter it yourself.",
                "dependabot",
                branchName,
                defaultBranch.FriendlyName);

            _logger.LogInformation("Created dependency update PR for {Package} {Old} -> {New} in {Repo}",
                packageName, currentVersion, newVersion, repoName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create update PR for {Package} in {Repo}", packageName, repoName);
            return false;
        }
    }

    private static IEnumerable<(Blob Blob, string Path)> WalkTreeForFiles(Tree tree, string path, string fileNameOrExtension)
    {
        foreach (var entry in tree)
        {
            var fullPath = string.IsNullOrEmpty(path) ? entry.Name : $"{path}/{entry.Name}";

            if (entry.TargetType == TreeEntryTargetType.Tree)
            {
                foreach (var child in WalkTreeForFiles((Tree)entry.Target, fullPath, fileNameOrExtension))
                    yield return child;
            }
            else if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                if (fileNameOrExtension.StartsWith(".") ? entry.Name.EndsWith(fileNameOrExtension, StringComparison.OrdinalIgnoreCase)
                    : entry.Name.Equals(fileNameOrExtension, StringComparison.OrdinalIgnoreCase))
                    yield return ((Blob)entry.Target, fullPath);
            }
        }
    }

    private async Task<string?> GetRepoPath(string repoName)
    {
        var systemSettings = await _adminService.GetSystemSettingsAsync();
        var projectRoot = !string.IsNullOrEmpty(systemSettings.ProjectRoot)
            ? systemSettings.ProjectRoot
            : _config["Git:ProjectRoot"] ?? "/repos";

        var path = Path.Combine(projectRoot, repoName);
        if (GitRepository.IsValid(path)) return path;
        if (GitRepository.IsValid(path + ".git")) return path + ".git";
        return null;
    }
}

public class DependencyUpdateSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DependencyUpdateSchedulerService> _logger;

    public DependencyUpdateSchedulerService(IServiceScopeFactory scopeFactory, ILogger<DependencyUpdateSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit before first run
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using var db = dbFactory.CreateDbContext();
                var updateService = scope.ServiceProvider.GetRequiredService<IDependencyUpdateService>();

                var now = DateTime.UtcNow;
                var configs = await db.DependencyUpdateConfigs.Where(c => c.IsEnabled).ToListAsync(stoppingToken);

                foreach (var config in configs)
                {
                    var shouldRun = config.LastRunAt == null || config.Schedule switch
                    {
                        "daily" => config.LastRunAt.Value.AddDays(1) <= now,
                        "weekly" => config.LastRunAt.Value.AddDays(7) <= now,
                        "monthly" => config.LastRunAt.Value.AddDays(30) <= now,
                        _ => config.LastRunAt.Value.AddDays(7) <= now
                    };

                    if (shouldRun)
                    {
                        _logger.LogInformation("Running dependency update check for {Repo}/{Ecosystem}", config.RepoName, config.Ecosystem);
                        await updateService.RunUpdateCheckAsync(config.RepoName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in dependency update scheduler");
            }

            // Check every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
