using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IIssueService
{
    Task<List<Issue>> GetIssuesAsync(string repoName);
    Task<Issue?> GetIssueAsync(string repoName, int number);
    Task<Issue> CreateIssueAsync(string repoName, string title, string? body, string author, List<string>? labels = null);
    Task<bool> UpdateIssueAsync(string repoName, int number, Action<Issue> updateAction);
    Task<bool> AddCommentAsync(string repoName, int number, string author, string body);
    Task<bool> CloseIssueAsync(string repoName, int number);
    Task<bool> ReopenIssueAsync(string repoName, int number);
}

public class IssueService : IIssueService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<IssueService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IActivityService _activityService;

    public IssueService(IDbContextFactory<AppDbContext> dbFactory, ILogger<IssueService> logger, INotificationService notificationService, IActivityService activityService)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _notificationService = notificationService;
        _activityService = activityService;
    }

    public async Task<List<Issue>> GetIssuesAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Issues
            .Include(i => i.Comments)
            .Where(i => i.RepoName == repoName)
            .ToListAsync();
    }

    public async Task<Issue?> GetIssueAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Issues
            .Include(i => i.Comments)
            .FirstOrDefaultAsync(i => i.RepoName == repoName && i.Number == number);
    }

    public async Task<Issue> CreateIssueAsync(string repoName, string title, string? body, string author, List<string>? labels = null)
    {
        using var db = _dbFactory.CreateDbContext();

        var maxNumber = await db.Issues
            .Where(i => i.RepoName == repoName)
            .MaxAsync(i => (int?)i.Number) ?? 0;

        var issue = new Issue
        {
            Number = maxNumber + 1,
            RepoName = repoName,
            Title = title,
            Body = body,
            Author = author,
            Labels = labels ?? new List<string>(),
            CreatedAt = DateTime.UtcNow
        };

        db.Issues.Add(issue);
        await db.SaveChangesAsync();

        _logger.LogInformation("Issue #{Number} created in {RepoName} by {Author}", issue.Number, repoName, author);

        await _activityService.RecordActivityAsync(author, "opened_issue", repoName, $"{author} opened issue #{issue.Number}: {title}", $"/repo/{repoName}/issues/{issue.Number}");

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
        using var db = _dbFactory.CreateDbContext();

        var issue = await db.Issues
            .Include(i => i.Comments)
            .FirstOrDefaultAsync(i => i.RepoName == repoName && i.Number == number);

        if (issue == null)
            return false;

        updateAction(issue);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddCommentAsync(string repoName, int number, string author, string body)
    {
        using var db = _dbFactory.CreateDbContext();

        var issue = await db.Issues
            .FirstOrDefaultAsync(i => i.RepoName == repoName && i.Number == number);

        if (issue == null)
            return false;

        db.IssueComments.Add(new IssueComment
        {
            IssueId = issue.Id,
            Author = author,
            Body = body,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        _logger.LogInformation("Comment added to issue #{Number} in {RepoName} by {Author}", number, repoName, author);

        await _notificationService.CreateNotificationAsync(
            "current-user",
            NotificationType.IssueComment,
            $"New comment on issue #{number}",
            $"{author} commented: {body[..Math.Min(100, body.Length)]}...",
            repoName,
            $"/repo/{repoName}/issues/{number}"
        );

        return true;
    }

    public async Task<bool> CloseIssueAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();

        var issue = await db.Issues
            .FirstOrDefaultAsync(i => i.RepoName == repoName && i.Number == number);

        if (issue == null)
            return false;

        issue.State = IssueState.Closed;
        issue.ClosedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("Issue #{Number} closed in {RepoName}", number, repoName);

        await _activityService.RecordActivityAsync(issue.Author, "closed_issue", repoName, $"Issue #{number} closed: {issue.Title}", $"/repo/{repoName}/issues/{number}");

        await _notificationService.CreateNotificationAsync(
            "current-user",
            NotificationType.IssueClosed,
            $"Issue #{number} closed",
            $"Issue closed: {issue.Title}",
            repoName,
            $"/repo/{repoName}/issues/{number}"
        );

        return true;
    }

    public async Task<bool> ReopenIssueAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();

        var issue = await db.Issues
            .FirstOrDefaultAsync(i => i.RepoName == repoName && i.Number == number);

        if (issue == null)
            return false;

        issue.State = IssueState.Open;
        issue.ClosedAt = null;
        await db.SaveChangesAsync();

        _logger.LogInformation("Issue #{Number} reopened in {RepoName}", number, repoName);
        return true;
    }
}
