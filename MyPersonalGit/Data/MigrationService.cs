using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IMigrationService
{
    Task<MigrationTask> CreateMigrationTaskAsync(string sourceUrl, MigrationSource source, string targetRepoName,
        string owner, bool importIssues, bool importPullRequests, bool makePrivate, string? authToken);
    Task<List<MigrationTask>> GetMigrationTasksAsync(string owner);
    Task<MigrationTask?> GetMigrationTaskAsync(int id);
    Task CancelMigrationTaskAsync(int id);
    Task ExecuteMigrationAsync(int taskId);
}

public class MigrationChannel
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();
    public ChannelWriter<int> Writer => _channel.Writer;
    public ChannelReader<int> Reader => _channel.Reader;
}

public class MigrationService : IMigrationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IRepositoryService _repoService;
    private readonly IIssueService _issueService;
    private readonly IAdminService _adminService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly MigrationChannel _channel;
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(
        IDbContextFactory<AppDbContext> dbFactory,
        IRepositoryService repoService,
        IIssueService issueService,
        IAdminService adminService,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        MigrationChannel channel,
        ILogger<MigrationService> logger)
    {
        _dbFactory = dbFactory;
        _repoService = repoService;
        _issueService = issueService;
        _adminService = adminService;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _channel = channel;
        _logger = logger;
    }

    public async Task<MigrationTask> CreateMigrationTaskAsync(string sourceUrl, MigrationSource source,
        string targetRepoName, string owner, bool importIssues, bool importPullRequests, bool makePrivate, string? authToken)
    {
        using var db = _dbFactory.CreateDbContext();

        var task = new MigrationTask
        {
            SourceUrl = sourceUrl,
            Source = source,
            TargetRepoName = targetRepoName,
            Owner = owner,
            ImportIssues = importIssues,
            ImportPullRequests = importPullRequests,
            MakePrivate = makePrivate,
            AuthToken = authToken,
            Status = MigrationStatus.Pending,
            StatusMessage = "Queued for processing"
        };

        db.MigrationTasks.Add(task);
        await db.SaveChangesAsync();

        await _channel.Writer.WriteAsync(task.Id);
        _logger.LogInformation("Migration task {Id} created for {SourceUrl} -> {TargetRepo}", task.Id, sourceUrl, targetRepoName);

        return task;
    }

    public async Task<List<MigrationTask>> GetMigrationTasksAsync(string owner)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.MigrationTasks
            .Where(t => t.Owner == owner)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<MigrationTask?> GetMigrationTaskAsync(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.MigrationTasks.FindAsync(id);
    }

    public async Task CancelMigrationTaskAsync(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        var task = await db.MigrationTasks.FindAsync(id);
        if (task != null && task.Status == MigrationStatus.Pending)
        {
            task.Status = MigrationStatus.Failed;
            task.ErrorMessage = "Cancelled by user";
            task.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task ExecuteMigrationAsync(int taskId)
    {
        using var db = _dbFactory.CreateDbContext();
        var task = await db.MigrationTasks.FindAsync(taskId);
        if (task == null || task.Status != MigrationStatus.Pending) return;

        try
        {
            // Step 1: Clone the repository
            task.Status = MigrationStatus.Cloning;
            task.StatusMessage = "Cloning repository...";
            task.ProgressPercent = 10;
            await db.SaveChangesAsync();

            var systemSettings = await _adminService.GetSystemSettingsAsync();
            var projectRoot = !string.IsNullOrEmpty(systemSettings.ProjectRoot)
                ? systemSettings.ProjectRoot
                : _config["Git:ProjectRoot"] ?? "/repos";

            var repoPath = Path.Combine(projectRoot, task.TargetRepoName);

            // Build clone URL with auth token if provided
            var cloneUrl = task.SourceUrl;
            if (!string.IsNullOrEmpty(task.AuthToken) && cloneUrl.StartsWith("https://"))
            {
                var uri = new Uri(cloneUrl);
                cloneUrl = $"https://oauth2:{task.AuthToken}@{uri.Host}{uri.PathAndQuery}";
            }

            // Clone bare repository using git CLI
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"clone --bare \"{cloneUrl}\" \"{repoPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Git clone failed: {stderr}");
            }

            task.ProgressPercent = 40;
            task.StatusMessage = "Creating repository record...";
            await db.SaveChangesAsync();

            // Create the repository record in the database
            await _repoService.CreateRepositoryAsync(task.TargetRepoName, task.Owner,
                $"Imported from {task.SourceUrl}", task.MakePrivate);

            task.ProgressPercent = 50;
            await db.SaveChangesAsync();

            // Step 2: Import issues if requested
            if (task.ImportIssues && task.Source != MigrationSource.GitUrl)
            {
                task.Status = MigrationStatus.ImportingIssues;
                task.StatusMessage = "Importing issues...";
                await db.SaveChangesAsync();

                await ImportIssuesAsync(task, db);
                task.ProgressPercent = 75;
                await db.SaveChangesAsync();
            }

            // Step 3: Import PRs if requested
            if (task.ImportPullRequests && task.Source != MigrationSource.GitUrl)
            {
                task.Status = MigrationStatus.ImportingPullRequests;
                task.StatusMessage = "Importing pull requests...";
                await db.SaveChangesAsync();

                await ImportPullRequestsAsync(task, db);
                task.ProgressPercent = 90;
                await db.SaveChangesAsync();
            }

            // Done
            task.Status = MigrationStatus.Completed;
            task.StatusMessage = "Migration completed successfully";
            task.ProgressPercent = 100;
            task.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            _logger.LogInformation("Migration task {Id} completed: {SourceUrl} -> {TargetRepo}", taskId, task.SourceUrl, task.TargetRepoName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration task {Id} failed", taskId);
            task.Status = MigrationStatus.Failed;
            task.ErrorMessage = ex.Message;
            task.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    private async Task ImportIssuesAsync(MigrationTask task, AppDbContext db)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(task.AuthToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", task.AuthToken);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyPersonalGit/1.0");

            var (apiUrl, _) = GetApiUrls(task);
            if (apiUrl == null) return;

            var issuesUrl = task.Source switch
            {
                MigrationSource.GitHub => $"{apiUrl}/issues?state=all&per_page=100",
                MigrationSource.GitLab => $"{apiUrl}/issues?per_page=100",
                MigrationSource.Bitbucket => $"{apiUrl}/issues?pagelen=50",
                _ => null
            };

            if (issuesUrl == null) return;

            var response = await client.GetAsync(issuesUrl);
            if (!response.IsSuccessStatusCode) return;

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);

            var issuesArray = task.Source == MigrationSource.Bitbucket
                ? (doc.RootElement.TryGetProperty("values", out var vals) ? vals : doc.RootElement)
                : doc.RootElement;

            foreach (var issue in issuesArray.EnumerateArray())
            {
                // Skip pull requests from GitHub (they come mixed in with issues)
                if (task.Source == MigrationSource.GitHub &&
                    issue.TryGetProperty("pull_request", out _))
                    continue;

                var title = issue.TryGetProperty("title", out var t) ? t.GetString() ?? "Untitled" : "Untitled";
                var issueBody = issue.TryGetProperty("body", out var b) ? b.GetString() :
                                issue.TryGetProperty("description", out var d) ? d.GetString() : null;

                var state = GetIssueState(issue, task.Source);

                await _issueService.CreateIssueAsync(task.TargetRepoName, title,
                    $"[Imported from {task.Source}]\n\n{issueBody}", task.Owner);

                if (state == "closed")
                {
                    // Get the latest issue number and close it
                    var issues = await _issueService.GetIssuesAsync(task.TargetRepoName);
                    var latest = issues.OrderByDescending(i => i.Number).FirstOrDefault();
                    if (latest != null)
                        await _issueService.CloseIssueAsync(task.TargetRepoName, latest.Number);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to import issues for migration task {Id}", task.Id);
        }
    }

    private async Task ImportPullRequestsAsync(MigrationTask task, AppDbContext db)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(task.AuthToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", task.AuthToken);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyPersonalGit/1.0");

            var (apiUrl, _) = GetApiUrls(task);
            if (apiUrl == null) return;

            var prsUrl = task.Source switch
            {
                MigrationSource.GitHub => $"{apiUrl}/pulls?state=all&per_page=100",
                MigrationSource.GitLab => $"{apiUrl}/merge_requests?per_page=100",
                _ => null
            };

            if (prsUrl == null) return;

            var response = await client.GetAsync(prsUrl);
            if (!response.IsSuccessStatusCode) return;

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);

            foreach (var pr in doc.RootElement.EnumerateArray())
            {
                var title = pr.TryGetProperty("title", out var t) ? t.GetString() ?? "Untitled PR" : "Untitled PR";
                var prBody = pr.TryGetProperty("body", out var b) ? b.GetString() :
                             pr.TryGetProperty("description", out var d) ? d.GetString() : null;
                var prState = task.Source == MigrationSource.GitHub
                    ? (pr.TryGetProperty("state", out var s) ? s.GetString() : "closed")
                    : (pr.TryGetProperty("state", out var gs) ? gs.GetString() : "closed");

                var prNumber = pr.TryGetProperty("number", out var n) ? n.GetInt32() :
                               pr.TryGetProperty("iid", out var iid) ? iid.GetInt32() : 0;

                // Create as an issue since the branches likely don't exist locally
                await _issueService.CreateIssueAsync(task.TargetRepoName,
                    $"[PR #{prNumber}] {title}",
                    $"[Imported {task.Source} Pull Request]\n\n{prBody}", task.Owner);

                if (prState == "closed" || prState == "merged")
                {
                    var issues = await _issueService.GetIssuesAsync(task.TargetRepoName);
                    var latest = issues.OrderByDescending(i => i.Number).FirstOrDefault();
                    if (latest != null)
                        await _issueService.CloseIssueAsync(task.TargetRepoName, latest.Number);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to import pull requests for migration task {Id}", task.Id);
        }
    }

    private static (string? apiUrl, string? owner) GetApiUrls(MigrationTask task)
    {
        try
        {
            var uri = new Uri(task.SourceUrl.TrimEnd('/').Replace(".git", ""));
            var segments = uri.AbsolutePath.Trim('/').Split('/');

            return task.Source switch
            {
                MigrationSource.GitHub when segments.Length >= 2 =>
                    ($"https://api.github.com/repos/{segments[0]}/{segments[1]}", segments[0]),
                MigrationSource.GitLab when segments.Length >= 2 =>
                    ($"https://gitlab.com/api/v4/projects/{Uri.EscapeDataString($"{segments[0]}/{segments[1]}")}", segments[0]),
                MigrationSource.Bitbucket when segments.Length >= 2 =>
                    ($"https://api.bitbucket.org/2.0/repositories/{segments[0]}/{segments[1]}", segments[0]),
                _ => (null, null)
            };
        }
        catch
        {
            return (null, null);
        }
    }

    private static string GetIssueState(JsonElement issue, MigrationSource source)
    {
        if (source == MigrationSource.GitHub || source == MigrationSource.GitLab)
        {
            return issue.TryGetProperty("state", out var s) ? s.GetString() ?? "open" : "open";
        }
        if (source == MigrationSource.Bitbucket)
        {
            var state = issue.TryGetProperty("state", out var s) ? s.GetString() ?? "" : "";
            return state is "resolved" or "closed" or "invalid" or "duplicate" or "wontfix" ? "closed" : "open";
        }
        return "open";
    }
}

public class MigrationWorkerService : BackgroundService
{
    private readonly MigrationChannel _channel;
    private readonly IMigrationService _migrationService;
    private readonly ILogger<MigrationWorkerService> _logger;

    public MigrationWorkerService(MigrationChannel channel, IMigrationService migrationService, ILogger<MigrationWorkerService> logger)
    {
        _channel = channel;
        _migrationService = migrationService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Migration worker started");

        await foreach (var taskId in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing migration task {Id}", taskId);
                await _migrationService.ExecuteMigrationAsync(taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing migration task {Id}", taskId);
            }
        }
    }
}
