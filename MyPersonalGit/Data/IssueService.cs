using System.Text.Json;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public class IssueService
{
    private readonly string _dataPath;
    private readonly ILogger<IssueService> _logger;
    private readonly NotificationService _notificationService;

    public IssueService(IConfiguration config, ILogger<IssueService> logger, NotificationService notificationService)
    {
        var projectRoot = config["Git:ProjectRoot"] ?? "/repos";
        _dataPath = Path.Combine(projectRoot, ".data");
        _logger = logger;
        _notificationService = notificationService;
        Directory.CreateDirectory(_dataPath);
    }

    private string GetIssuesFilePath(string repoName) =>
        Path.Combine(_dataPath, $"{repoName}_issues.json");

    public async Task<List<Issue>> GetIssuesAsync(string repoName)
    {
        var filePath = GetIssuesFilePath(repoName);
        if (!File.Exists(filePath))
            return new List<Issue>();

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<Issue>>(json) ?? new List<Issue>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load issues for {RepoName}", repoName);
            return new List<Issue>();
        }
    }

    public async Task<Issue?> GetIssueAsync(string repoName, int number)
    {
        var issues = await GetIssuesAsync(repoName);
        return issues.FirstOrDefault(i => i.Number == number);
    }

    public async Task<Issue> CreateIssueAsync(string repoName, string title, string? body, string author, List<string>? labels = null)
    {
        var issues = await GetIssuesAsync(repoName);
        var nextNumber = issues.Any() ? issues.Max(i => i.Number) + 1 : 1;

        var issue = new Issue
        {
            Number = nextNumber,
            RepoName = repoName,
            Title = title,
            Body = body,
            Author = author,
            Labels = labels ?? new List<string>(),
            CreatedAt = DateTime.UtcNow
        };

        issues.Add(issue);
        await SaveIssuesAsync(repoName, issues);

        await _notificationService.CreateNotificationAsync(
            "current-user",
            NotificationType.IssueCreated,
            $"New issue #{issue.Number}",
            $"{author} created issue: {title}",
            repoName,
            $"/repo/{repoName}/issues/{issue.Number}"
        );

        return issue;
    }

    public async Task<bool> UpdateIssueAsync(string repoName, int number, Action<Issue> updateAction)
    {
        var issues = await GetIssuesAsync(repoName);
        var issue = issues.FirstOrDefault(i => i.Number == number);
        if (issue == null)
            return false;

        updateAction(issue);
        await SaveIssuesAsync(repoName, issues);
        return true;
    }

    public async Task<bool> AddCommentAsync(string repoName, int number, string author, string body)
    {
        var result = await UpdateIssueAsync(repoName, number, issue =>
        {
            issue.Comments.Add(new IssueComment
            {
                Id = issue.Comments.Any() ? issue.Comments.Max(c => c.Id) + 1 : 1,
                Author = author,
                Body = body,
                CreatedAt = DateTime.UtcNow
            });
        });

        if (result)
        {
            var issue = await GetIssueAsync(repoName, number);
            if (issue != null)
            {
                await _notificationService.CreateNotificationAsync(
                    "current-user",
                    NotificationType.IssueComment,
                    $"New comment on issue #{number}",
                    $"{author} commented: {body.Substring(0, Math.Min(100, body.Length))}...",
                    repoName,
                    $"/repo/{repoName}/issues/{number}"
                );
            }
        }

        return result;
    }

    public async Task<bool> CloseIssueAsync(string repoName, int number)
    {
        var result = await UpdateIssueAsync(repoName, number, issue =>
        {
            issue.State = IssueState.Closed;
            issue.ClosedAt = DateTime.UtcNow;
        });

        if (result)
        {
            var issue = await GetIssueAsync(repoName, number);
            if (issue != null)
            {
                await _notificationService.CreateNotificationAsync(
                    "current-user",
                    NotificationType.IssueClosed,
                    $"Issue #{number} closed",
                    $"Issue closed: {issue.Title}",
                    repoName,
                    $"/repo/{repoName}/issues/{number}"
                );
            }
        }

        return result;
    }

    public async Task<bool> ReopenIssueAsync(string repoName, int number)
    {
        return await UpdateIssueAsync(repoName, number, issue =>
        {
            issue.State = IssueState.Open;
            issue.ClosedAt = null;
        });
    }

    private async Task SaveIssuesAsync(string repoName, List<Issue> issues)
    {
        var filePath = GetIssuesFilePath(repoName);
        var json = JsonSerializer.Serialize(issues, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }
}