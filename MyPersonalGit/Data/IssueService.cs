using System.Text.RegularExpressions;
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
    Task<bool> EditCommentAsync(int commentId, string body, string username);
    Task<bool> DeleteCommentAsync(int commentId, string username);
    Task<bool> CloseIssueAsync(string repoName, int number, string? closedBy = null);
    Task<bool> ReopenIssueAsync(string repoName, int number);
    Task<bool> TogglePinAsync(string repoName, int number);
    Task<bool> ToggleLockAsync(string repoName, int number, string? reason = null);
    Task<bool> SetAssigneesAsync(string repoName, int number, List<string> assignees);
    Task<bool> SetDueDateAsync(string repoName, int number, DateTime? dueDate);
    Task<(bool Success, string? Error, int? NewNumber)> TransferIssueAsync(string fromRepoName, int number, string toRepoName, string transferredBy);
    Task<(bool Success, string? Error)> SetParentIssueAsync(string repoName, int number, int? parentNumber);
    Task<List<Issue>> GetSubIssuesAsync(string repoName, int number);
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

        await _notificationService.NotifyWatchersAsync(
            repoName, author, participants: Enumerable.Empty<string>(),
            NotificationType.IssueCreated,
            $"New issue #{issue.Number} in {repoName}",
            $"{author} created issue: {title}",
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

        // Detect @mentions first — mentioned users get a dedicated notification and are
        // excluded from the general fan-out below so they aren't notified twice.
        var mentions = Regex.Matches(body, @"@([a-zA-Z0-9_-]+)")
            .Select(m => m.Groups[1].Value)
            .Where(u => !u.Equals(author, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Notify watchers and participants (issue author, assignees, prior commenters)
        var commenters = await db.IssueComments
            .Where(c => c.IssueId == issue.Id)
            .Select(c => c.Author)
            .Distinct()
            .ToListAsync();
        var participants = commenters.Append(issue.Author).Concat(issue.Assignees);

        await _notificationService.NotifyWatchersAsync(
            repoName, author, participants,
            NotificationType.IssueComment,
            $"New comment on issue #{number} in {repoName}",
            $"{author} commented: {body[..Math.Min(100, body.Length)]}...",
            $"/repo/{repoName}/issues/{number}",
            exclude: mentions
        );

        foreach (var mentioned in mentions)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == mentioned);
            if (user != null)
            {
                await _notificationService.CreateNotificationAsync(
                    mentioned, NotificationType.Mention,
                    $"Mentioned in issue #{number}",
                    $"{author} mentioned you: {body[..Math.Min(100, body.Length)]}...",
                    repoName, $"/repo/{repoName}/issues/{number}");
            }
        }

        return true;
    }

    public async Task<bool> CloseIssueAsync(string repoName, int number, string? closedBy = null)
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

        var actor = closedBy ?? issue.Author;
        await _activityService.RecordActivityAsync(actor, "closed_issue", repoName, $"Issue #{number} closed: {issue.Title}", $"/repo/{repoName}/issues/{number}");

        var commenters = await db.IssueComments
            .Where(c => c.IssueId == issue.Id)
            .Select(c => c.Author)
            .Distinct()
            .ToListAsync();

        await _notificationService.NotifyWatchersAsync(
            repoName, actor, commenters.Append(issue.Author).Concat(issue.Assignees),
            NotificationType.IssueClosed,
            $"Issue #{number} closed in {repoName}",
            $"{actor} closed issue: {issue.Title}",
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

    public async Task<bool> EditCommentAsync(int commentId, string body, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        var comment = await db.IssueComments.FindAsync(commentId);
        if (comment == null) return false;
        if (!comment.Author.Equals(username, StringComparison.OrdinalIgnoreCase)) return false;

        comment.Body = body;
        comment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("Comment {Id} edited by {User}", commentId, username);
        return true;
    }

    public async Task<bool> DeleteCommentAsync(int commentId, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        var comment = await db.IssueComments.FindAsync(commentId);
        if (comment == null) return false;
        if (!comment.Author.Equals(username, StringComparison.OrdinalIgnoreCase)) return false;

        db.IssueComments.Remove(comment);
        await db.SaveChangesAsync();

        _logger.LogInformation("Comment {Id} deleted by {User}", commentId, username);
        return true;
    }

    public async Task<bool> TogglePinAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        var issue = await db.Issues.FirstOrDefaultAsync(i => i.RepoName == repoName && i.Number == number);
        if (issue == null) return false;

        issue.IsPinned = !issue.IsPinned;
        await db.SaveChangesAsync();

        _logger.LogInformation("Issue #{Number} in {RepoName} pin toggled to {IsPinned}", number, repoName, issue.IsPinned);
        return true;
    }

    public async Task<bool> ToggleLockAsync(string repoName, int number, string? reason = null)
    {
        using var db = _dbFactory.CreateDbContext();
        var issue = await db.Issues.FirstOrDefaultAsync(i => i.RepoName == repoName && i.Number == number);
        if (issue == null) return false;

        issue.IsLocked = !issue.IsLocked;
        issue.LockReason = issue.IsLocked ? reason : null;
        await db.SaveChangesAsync();

        _logger.LogInformation("Issue #{Number} in {RepoName} lock toggled to {IsLocked}", number, repoName, issue.IsLocked);
        return true;
    }

    public async Task<bool> SetAssigneesAsync(string repoName, int number, List<string> assignees)
    {
        using var db = _dbFactory.CreateDbContext();
        var issue = await db.Issues.FirstOrDefaultAsync(i => i.RepoName == repoName && i.Number == number);
        if (issue == null) return false;

        issue.Assignees = assignees;
        issue.Assignee = assignees.FirstOrDefault();
        await db.SaveChangesAsync();

        _logger.LogInformation("Issue #{Number} in {RepoName} assignees set to [{Assignees}]", number, repoName, string.Join(", ", assignees));
        return true;
    }

    public async Task<bool> SetDueDateAsync(string repoName, int number, DateTime? dueDate)
    {
        using var db = _dbFactory.CreateDbContext();
        var issue = await db.Issues.FirstOrDefaultAsync(i => i.RepoName == repoName && i.Number == number);
        if (issue == null) return false;

        issue.DueDate = dueDate;
        await db.SaveChangesAsync();

        _logger.LogInformation("Issue #{Number} in {RepoName} due date set to {DueDate}", number, repoName, dueDate);
        return true;
    }

    /// <summary>
    /// Makes an issue a sub-issue of another issue in the same repository
    /// (or detaches it when <paramref name="parentNumber"/> is null).
    /// </summary>
    public async Task<(bool Success, string? Error)> SetParentIssueAsync(string repoName, int number, int? parentNumber)
    {
        using var db = _dbFactory.CreateDbContext();

        var issue = await db.Issues.FirstOrDefaultAsync(i => i.RepoName == repoName && i.Number == number);
        if (issue == null) return (false, "Issue not found");

        if (parentNumber == null)
        {
            issue.ParentIssueNumber = null;
            await db.SaveChangesAsync();
            return (true, null);
        }

        if (parentNumber == number) return (false, "An issue cannot be its own parent");

        var parent = await db.Issues.FirstOrDefaultAsync(i => i.RepoName == repoName && i.Number == parentNumber);
        if (parent == null) return (false, $"Issue #{parentNumber} not found");

        // Walk up the ancestor chain to reject cycles (and runaway depth)
        var ancestor = parent;
        for (var depth = 0; ancestor?.ParentIssueNumber != null; depth++)
        {
            if (depth >= 20) return (false, "Sub-issue hierarchy is too deep");
            if (ancestor.ParentIssueNumber == number)
                return (false, $"Cannot add: #{parentNumber} is already a sub-issue of #{number}");
            var parentNum = ancestor.ParentIssueNumber;
            ancestor = await db.Issues.FirstOrDefaultAsync(i => i.RepoName == repoName && i.Number == parentNum);
        }

        issue.ParentIssueNumber = parentNumber;
        await db.SaveChangesAsync();

        _logger.LogInformation("Issue #{Number} in {RepoName} set as sub-issue of #{Parent}", number, repoName, parentNumber);
        return (true, null);
    }

    public async Task<List<Issue>> GetSubIssuesAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Issues
            .Where(i => i.RepoName == repoName && i.ParentIssueNumber == number)
            .OrderBy(i => i.Number)
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error, int? NewNumber)> TransferIssueAsync(string fromRepoName, int number, string toRepoName, string transferredBy)
    {
        if (fromRepoName == toRepoName) return (false, "Cannot transfer issue to the same repository", null);

        using var db = _dbFactory.CreateDbContext();

        var issue = await db.Issues
            .Include(i => i.Comments)
            .FirstOrDefaultAsync(i => i.RepoName == fromRepoName && i.Number == number);
        if (issue == null) return (false, "Issue not found", null);

        // Verify target repo exists
        var targetRepo = await db.Repositories.FirstOrDefaultAsync(r => r.Name == toRepoName);
        if (targetRepo == null) return (false, $"Repository '{toRepoName}' not found", null);

        // Get next issue number in target repo
        var maxNumber = await db.Issues
            .Where(i => i.RepoName == toRepoName)
            .MaxAsync(i => (int?)i.Number) ?? 0;
        var newNumber = maxNumber + 1;

        // Create new issue in target repo
        var newIssue = new Issue
        {
            Number = newNumber,
            RepoName = toRepoName,
            Title = issue.Title,
            Body = issue.Body,
            Author = issue.Author,
            State = issue.State,
            Labels = new List<string>(), // Labels may not exist in target repo
            Assignees = new List<string>(),
            DueDate = issue.DueDate,
            CreatedAt = issue.CreatedAt
        };

        // Copy labels that exist in target repo
        if (issue.Labels.Any())
        {
            var targetLabels = await db.RepositoryLabels
                .Where(l => l.RepoName == toRepoName)
                .Select(l => l.Name)
                .ToListAsync();
            newIssue.Labels = issue.Labels.Where(l => targetLabels.Contains(l)).ToList();
        }

        db.Issues.Add(newIssue);
        await db.SaveChangesAsync();

        // Copy comments
        foreach (var comment in issue.Comments ?? new())
        {
            db.IssueComments.Add(new IssueComment
            {
                IssueId = newIssue.Id,
                Author = comment.Author,
                Body = comment.Body,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            });
        }

        // Add transfer note to new issue
        db.IssueComments.Add(new IssueComment
        {
            IssueId = newIssue.Id,
            Author = transferredBy,
            Body = $"*This issue was transferred from {fromRepoName}#{number} by @{transferredBy}*",
            CreatedAt = DateTime.UtcNow
        });

        // Close original issue with transfer note
        issue.State = IssueState.Closed;
        issue.ClosedAt = DateTime.UtcNow;
        db.IssueComments.Add(new IssueComment
        {
            IssueId = issue.Id,
            Author = transferredBy,
            Body = $"*This issue was transferred to {toRepoName}#{newNumber} by @{transferredBy}*",
            CreatedAt = DateTime.UtcNow
        });

        // Create transfer record
        db.IssueTransfers.Add(new IssueTransfer
        {
            FromRepoName = fromRepoName,
            FromIssueNumber = number,
            ToRepoName = toRepoName,
            ToIssueNumber = newNumber,
            TransferredBy = transferredBy,
            TransferredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        _logger.LogInformation("Issue #{Number} transferred from {From} to {To}#{NewNumber} by {User}",
            number, fromRepoName, toRepoName, newNumber, transferredBy);

        await _activityService.RecordActivityAsync(transferredBy, "transferred_issue", toRepoName,
            $"{transferredBy} transferred issue #{number} from {fromRepoName} to {toRepoName}#{newNumber}",
            $"/repo/{toRepoName}/issues/{newNumber}");

        return (true, null, newNumber);
    }
}
