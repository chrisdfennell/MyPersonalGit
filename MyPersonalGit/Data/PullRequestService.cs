using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;
using GitRepository = LibGit2Sharp.Repository;

namespace MyPersonalGit.Data;

public enum MergeStrategy { MergeCommit, Squash, Rebase }

public interface IPullRequestService
{
    Task<List<PullRequest>> GetPullRequestsAsync(string repoName);
    Task<PullRequest?> GetPullRequestAsync(string repoName, int number);
    Task<PullRequest> CreatePullRequestAsync(string repoName, string title, string? body, string author, string sourceBranch, string targetBranch, bool isDraft = false);
    Task<(bool Success, string? Error)> MergePullRequestAsync(string repoName, int number, string mergedBy, MergeStrategy strategy = MergeStrategy.MergeCommit);
    Task<bool> ClosePullRequestAsync(string repoName, int number);
    Task<bool> AddReviewAsync(string repoName, int number, string author, ReviewState state, string? body = null);
    Task<(bool CanMerge, string? Reason)> CanMergeAsync(string repoName, int number);
}

public class PullRequestService : IPullRequestService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<PullRequestService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IActivityService _activityService;
    private readonly IAdminService _adminService;
    private readonly IConfiguration _config;

    public PullRequestService(
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<PullRequestService> logger,
        INotificationService notificationService,
        IActivityService activityService,
        IAdminService adminService,
        IConfiguration config)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _notificationService = notificationService;
        _activityService = activityService;
        _adminService = adminService;
        _config = config;
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

        await _activityService.RecordActivityAsync(author, "opened_pr", repoName, $"{author} opened PR #{pr.Number}: {title}", $"/repo/{repoName}/pulls/{pr.Number}");

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

    public async Task<(bool CanMerge, string? Reason)> CanMergeAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        var pr = await db.PullRequests.FirstOrDefaultAsync(p => p.RepoName == repoName && p.Number == number);
        if (pr == null) return (false, "Pull request not found");
        if (pr.State != PullRequestState.Open) return (false, "Pull request is not open");

        var repoPath = await GetRepoPath(repoName);
        if (repoPath == null || !GitRepository.IsValid(repoPath)) return (false, "Repository not found on disk");

        try
        {
            using var repo = new GitRepository(repoPath);
            var sourceBranch = repo.Branches[pr.SourceBranch];
            var targetBranch = repo.Branches[pr.TargetBranch];
            if (sourceBranch?.Tip == null) return (false, $"Branch '{pr.SourceBranch}' not found");
            if (targetBranch?.Tip == null) return (false, $"Branch '{pr.TargetBranch}' not found");

            var mergeResult = repo.ObjectDatabase.MergeCommits(sourceBranch.Tip, targetBranch.Tip, null);
            if (mergeResult.Status == MergeTreeStatus.Conflicts)
                return (false, "Merge conflicts detected — resolve conflicts before merging");

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Error checking merge: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> MergePullRequestAsync(string repoName, int number, string mergedBy, MergeStrategy strategy = MergeStrategy.MergeCommit)
    {
        using var db = _dbFactory.CreateDbContext();

        var pr = await db.PullRequests
            .FirstOrDefaultAsync(p => p.RepoName == repoName && p.Number == number);

        if (pr == null)
            return (false, "Pull request not found");

        if (pr.State != PullRequestState.Open)
            return (false, "Pull request is not open");

        var repoPath = await GetRepoPath(repoName);
        if (repoPath == null || !GitRepository.IsValid(repoPath))
            return (false, "Repository not found on disk");

        try
        {
            using var repo = new GitRepository(repoPath);
            var sourceBranch = repo.Branches[pr.SourceBranch];
            var targetBranch = repo.Branches[pr.TargetBranch];

            if (sourceBranch?.Tip == null)
                return (false, $"Branch '{pr.SourceBranch}' not found");
            if (targetBranch?.Tip == null)
                return (false, $"Branch '{pr.TargetBranch}' not found");

            var author = new Signature(mergedBy, $"{mergedBy}@localhost", DateTimeOffset.Now);

            switch (strategy)
            {
                case MergeStrategy.MergeCommit:
                {
                    var mergeResult = repo.ObjectDatabase.MergeCommits(sourceBranch.Tip, targetBranch.Tip, null);
                    if (mergeResult.Status == MergeTreeStatus.Conflicts)
                        return (false, "Merge conflicts detected — resolve conflicts before merging");

                    var mergeCommit = repo.ObjectDatabase.CreateCommit(
                        author, author,
                        $"Merge pull request #{pr.Number} from {pr.SourceBranch}\n\n{pr.Title}",
                        mergeResult.Tree,
                        new[] { targetBranch.Tip, sourceBranch.Tip },
                        prettifyMessage: true);

                    repo.Refs.UpdateTarget(repo.Refs[targetBranch.CanonicalName], mergeCommit.Id);
                    break;
                }

                case MergeStrategy.Squash:
                {
                    var mergeResult = repo.ObjectDatabase.MergeCommits(sourceBranch.Tip, targetBranch.Tip, null);
                    if (mergeResult.Status == MergeTreeStatus.Conflicts)
                        return (false, "Merge conflicts detected — resolve conflicts before merging");

                    // Collect commit messages for squash message
                    var filter = new CommitFilter
                    {
                        IncludeReachableFrom = sourceBranch.Tip,
                        ExcludeReachableFrom = targetBranch.Tip,
                        SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
                    };
                    var commits = repo.Commits.QueryBy(filter).ToList();
                    var messages = string.Join("\n", commits.Select(c => $"* {c.MessageShort}"));

                    var squashCommit = repo.ObjectDatabase.CreateCommit(
                        author, author,
                        $"{pr.Title} (#{pr.Number})\n\n{messages}",
                        mergeResult.Tree,
                        new[] { targetBranch.Tip },
                        prettifyMessage: true);

                    repo.Refs.UpdateTarget(repo.Refs[targetBranch.CanonicalName], squashCommit.Id);
                    break;
                }

                case MergeStrategy.Rebase:
                {
                    // Walk source commits in reverse topo order and cherry-pick each onto target tip
                    var filter = new CommitFilter
                    {
                        IncludeReachableFrom = sourceBranch.Tip,
                        ExcludeReachableFrom = targetBranch.Tip,
                        SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
                    };
                    var commits = repo.Commits.QueryBy(filter).Reverse().ToList();
                    if (!commits.Any())
                        return (false, "No commits to rebase");

                    var currentTip = targetBranch.Tip;

                    foreach (var commit in commits)
                    {
                        var parent = commit.Parents.FirstOrDefault();
                        if (parent == null)
                        {
                            // Root commit — just use the tree directly
                            var newCommit = repo.ObjectDatabase.CreateCommit(
                                commit.Author, author,
                                commit.Message,
                                commit.Tree,
                                new[] { currentTip },
                                prettifyMessage: true);
                            currentTip = newCommit;
                            continue;
                        }

                        var cherryResult = repo.ObjectDatabase.MergeCommits(currentTip, commit, null);
                        if (cherryResult.Status == MergeTreeStatus.Conflicts)
                            return (false, $"Conflict while rebasing commit {commit.Id.ToString(7)}: {commit.MessageShort}");

                        var rebasedCommit = repo.ObjectDatabase.CreateCommit(
                            commit.Author, author,
                            commit.Message,
                            cherryResult.Tree,
                            new[] { currentTip },
                            prettifyMessage: true);
                        currentTip = rebasedCommit;
                    }

                    repo.Refs.UpdateTarget(repo.Refs[targetBranch.CanonicalName], currentTip.Id);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Git merge failed for PR #{Number} in {RepoName}", number, repoName);
            return (false, $"Git merge failed: {ex.Message}");
        }

        // Update DB state
        pr.State = PullRequestState.Merged;
        pr.MergedAt = DateTime.UtcNow;
        pr.MergedBy = mergedBy;
        await db.SaveChangesAsync();

        _logger.LogInformation("PR #{Number} merged ({Strategy}) in {RepoName} by {MergedBy}", number, strategy, repoName, mergedBy);

        await _activityService.RecordActivityAsync(mergedBy, "merged_pr", repoName, $"{mergedBy} merged PR #{number}: {pr.Title}", $"/repo/{repoName}/pulls/{number}");

        await _notificationService.CreateNotificationAsync(
            "current-user",
            NotificationType.PullRequestMerged,
            $"Pull request #{number} merged",
            $"{mergedBy} merged PR: {pr.Title}",
            repoName,
            $"/repo/{repoName}/pulls/{number}"
        );

        return (true, null);
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

        await _activityService.RecordActivityAsync(pr.Author, "closed_pr", repoName, $"PR #{number} closed: {pr.Title}", $"/repo/{repoName}/pulls/{number}");

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
