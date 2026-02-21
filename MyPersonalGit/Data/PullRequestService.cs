using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IPullRequestService
{
    Task<List<PullRequest>> GetPullRequestsAsync(string repoName);
    Task<PullRequest?> GetPullRequestAsync(string repoName, int number);
    Task<PullRequest> CreatePullRequestAsync(string repoName, string title, string? body, string author, string sourceBranch, string targetBranch, bool isDraft = false);
    Task<bool> MergePullRequestAsync(string repoName, int number, string mergedBy);
    Task<bool> ClosePullRequestAsync(string repoName, int number);
    Task<bool> AddReviewAsync(string repoName, int number, string author, ReviewState state, string? body = null);
}

public class PullRequestService : IPullRequestService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<PullRequestService> _logger;
    private readonly INotificationService _notificationService;

    public PullRequestService(IDbContextFactory<AppDbContext> dbFactory, ILogger<PullRequestService> logger, INotificationService notificationService)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<List<PullRequest>> GetPullRequestsAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.PullRequests
            .Include(p => p.Reviews)
            .Include(p => p.Comments)
            .Where(p => p.RepoName == repoName)
            .ToListAsync();
    }

    public async Task<PullRequest?> GetPullRequestAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.PullRequests
            .Include(p => p.Reviews)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.RepoName == repoName && p.Number == number);
    }

    public async Task<PullRequest> CreatePullRequestAsync(
        string repoName, string title, string? body, string author,
        string sourceBranch, string targetBranch, bool isDraft = false)
    {
        using var db = _dbFactory.CreateDbContext();

        var maxNumber = await db.PullRequests
            .Where(p => p.RepoName == repoName)
            .MaxAsync(p => (int?)p.Number) ?? 0;

        var pr = new PullRequest
        {
            Number = maxNumber + 1,
            RepoName = repoName,
            Title = title,
            Body = body,
            Author = author,
            SourceBranch = sourceBranch,
            TargetBranch = targetBranch,
            IsDraft = isDraft,
            CreatedAt = DateTime.UtcNow
        };

        db.PullRequests.Add(pr);
        await db.SaveChangesAsync();

        _logger.LogInformation("PR #{Number} created in {RepoName} by {Author}", pr.Number, repoName, author);

        await _notificationService.CreateNotificationAsync(
            "current-user",
            NotificationType.PullRequestCreated,
            $"New pull request #{pr.Number}",
            $"{author} created PR: {title}",
            repoName,
            $"/repo/{repoName}/pulls/{pr.Number}"
        );

        return pr;
    }

    public async Task<bool> MergePullRequestAsync(string repoName, int number, string mergedBy)
    {
        using var db = _dbFactory.CreateDbContext();

        var pr = await db.PullRequests
            .FirstOrDefaultAsync(p => p.RepoName == repoName && p.Number == number);

        if (pr == null)
            return false;

        pr.State = PullRequestState.Merged;
        pr.MergedAt = DateTime.UtcNow;
        pr.MergedBy = mergedBy;
        await db.SaveChangesAsync();

        _logger.LogInformation("PR #{Number} merged in {RepoName} by {MergedBy}", number, repoName, mergedBy);

        await _notificationService.CreateNotificationAsync(
            "current-user",
            NotificationType.PullRequestMerged,
            $"Pull request #{number} merged",
            $"{mergedBy} merged PR: {pr.Title}",
            repoName,
            $"/repo/{repoName}/pulls/{number}"
        );

        return true;
    }

    public async Task<bool> ClosePullRequestAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();

        var pr = await db.PullRequests
            .FirstOrDefaultAsync(p => p.RepoName == repoName && p.Number == number);

        if (pr == null)
            return false;

        pr.State = PullRequestState.Closed;
        pr.ClosedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("PR #{Number} closed in {RepoName}", number, repoName);
        return true;
    }

    public async Task<bool> AddReviewAsync(string repoName, int number, string author, ReviewState state, string? body = null)
    {
        using var db = _dbFactory.CreateDbContext();

        var pr = await db.PullRequests
            .FirstOrDefaultAsync(p => p.RepoName == repoName && p.Number == number);

        if (pr == null)
            return false;

        db.PullRequestReviews.Add(new PullRequestReview
        {
            PullRequestId = pr.Id,
            Author = author,
            State = state,
            Body = body,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        _logger.LogInformation("Review ({State}) added to PR #{Number} in {RepoName} by {Author}", state, number, repoName, author);

        await _notificationService.CreateNotificationAsync(
            "current-user",
            NotificationType.PullRequestReview,
            $"New review on PR #{number}",
            $"{author} reviewed PR: {state}",
            repoName,
            $"/repo/{repoName}/pulls/{number}"
        );

        return true;
    }
}
