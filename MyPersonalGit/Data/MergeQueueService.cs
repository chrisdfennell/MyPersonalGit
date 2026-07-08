using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;
using GitRepository = LibGit2Sharp.Repository;

namespace MyPersonalGit.Data;

public interface IMergeQueueService
{
    Task<(bool Success, string? Error)> EnqueueAsync(string repoName, int prNumber, string enqueuedBy, MergeStrategy strategy = MergeStrategy.MergeCommit);
    Task<bool> DequeueAsync(string repoName, int prNumber);
    Task<List<MergeQueueEntry>> GetQueueAsync(string repoName, string? targetBranch = null);
    Task<MergeQueueEntry?> GetActiveEntryAsync(string repoName, int prNumber);
}

public class MergeQueueService : IMergeQueueService
{
    private static readonly MergeQueueState[] ActiveStates = { MergeQueueState.Queued, MergeQueueState.Validating };

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<MergeQueueService> _logger;

    public MergeQueueService(IDbContextFactory<AppDbContext> dbFactory, ILogger<MergeQueueService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error)> EnqueueAsync(string repoName, int prNumber, string enqueuedBy, MergeStrategy strategy = MergeStrategy.MergeCommit)
    {
        using var db = _dbFactory.CreateDbContext();

        var pr = await db.PullRequests.FirstOrDefaultAsync(p => p.RepoName == repoName && p.Number == prNumber);
        if (pr == null) return (false, "Pull request not found");
        if (pr.State != PullRequestState.Open) return (false, "Pull request is not open");
        if (pr.IsDraft) return (false, "Draft pull requests cannot be queued");

        if (await db.MergeQueueEntries.AnyAsync(e =>
                e.RepoName == repoName && e.PullRequestNumber == prNumber && ActiveStates.Contains(e.State)))
            return (false, "Pull request is already in the merge queue");

        db.MergeQueueEntries.Add(new MergeQueueEntry
        {
            RepoName = repoName,
            PullRequestNumber = prNumber,
            TargetBranch = pr.TargetBranch,
            EnqueuedBy = enqueuedBy,
            MergeStrategy = strategy.ToString(),
            State = MergeQueueState.Queued,
            EnqueuedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        _logger.LogInformation("PR #{Number} in {RepoName} added to merge queue by {User}", prNumber, repoName, enqueuedBy);
        return (true, null);
    }

    public async Task<bool> DequeueAsync(string repoName, int prNumber)
    {
        using var db = _dbFactory.CreateDbContext();
        var entry = await db.MergeQueueEntries.FirstOrDefaultAsync(e =>
            e.RepoName == repoName && e.PullRequestNumber == prNumber && ActiveStates.Contains(e.State));
        if (entry == null) return false;

        entry.State = MergeQueueState.Cancelled;
        entry.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("PR #{Number} in {RepoName} removed from merge queue", prNumber, repoName);
        return true;
    }

    public async Task<List<MergeQueueEntry>> GetQueueAsync(string repoName, string? targetBranch = null)
    {
        using var db = _dbFactory.CreateDbContext();
        var query = db.MergeQueueEntries.Where(e => e.RepoName == repoName && ActiveStates.Contains(e.State));
        if (targetBranch != null)
            query = query.Where(e => e.TargetBranch == targetBranch);
        return await query.OrderBy(e => e.EnqueuedAt).ToListAsync();
    }

    public async Task<MergeQueueEntry?> GetActiveEntryAsync(string repoName, int prNumber)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.MergeQueueEntries.FirstOrDefaultAsync(e =>
            e.RepoName == repoName && e.PullRequestNumber == prNumber && ActiveStates.Contains(e.State));
    }
}

/// <summary>
/// Background worker that drives the merge queue. Per repository + target branch, exactly
/// one entry validates at a time: the PR branch is brought up to date with the target head
/// (triggering CI on the result), the entry waits for the branch protection checks to pass,
/// and then merges. A failed entry drops out; the next queued entry starts on the next tick.
/// </summary>
public class MergeQueueProcessorService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ValidationTimeout = TimeSpan.FromMinutes(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MergeQueueProcessorService> _logger;
    private readonly IConfiguration _config;

    public MergeQueueProcessorService(IServiceScopeFactory scopeFactory, ILogger<MergeQueueProcessorService> logger, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Merge queue processor started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueuesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing merge queues");
            }
            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessQueuesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        List<(string RepoName, string TargetBranch)> queues;
        using (var db = dbFactory.CreateDbContext())
        {
            queues = (await db.MergeQueueEntries
                    .Where(e => e.State == MergeQueueState.Queued || e.State == MergeQueueState.Validating)
                    .Select(e => new { e.RepoName, e.TargetBranch })
                    .Distinct()
                    .ToListAsync(ct))
                .Select(x => (x.RepoName, x.TargetBranch))
                .ToList();
        }

        foreach (var (repoName, targetBranch) in queues)
        {
            try
            {
                await ProcessQueueAsync(scope.ServiceProvider, repoName, targetBranch, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process merge queue for {RepoName}/{Branch}", repoName, targetBranch);
            }
        }
    }

    private async Task ProcessQueueAsync(IServiceProvider services, string repoName, string targetBranch, CancellationToken ct)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = dbFactory.CreateDbContext();

        var active = await db.MergeQueueEntries
            .Where(e => e.RepoName == repoName && e.TargetBranch == targetBranch &&
                        (e.State == MergeQueueState.Queued || e.State == MergeQueueState.Validating))
            .OrderBy(e => e.EnqueuedAt)
            .ToListAsync(ct);
        if (active.Count == 0) return;

        var current = active.FirstOrDefault(e => e.State == MergeQueueState.Validating) ?? active.First();
        var prService = services.GetRequiredService<IPullRequestService>();
        var pr = await prService.GetPullRequestAsync(repoName, current.PullRequestNumber);

        // PR merged or closed outside the queue — drop the entry
        if (pr == null || pr.State != PullRequestState.Open)
        {
            current.State = pr?.State == PullRequestState.Merged ? MergeQueueState.Merged : MergeQueueState.Cancelled;
            current.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return;
        }

        if (current.State == MergeQueueState.Queued)
        {
            await StartValidationAsync(services, db, current, pr, ct);
            return;
        }

        // Validating: check for timeout, then re-evaluate mergeability
        if (current.StartedAt.HasValue && DateTime.UtcNow - current.StartedAt.Value > ValidationTimeout)
        {
            await FailEntryAsync(services, db, current, pr, "Timed out waiting for status checks", ct);
            return;
        }

        var (canMerge, reason) = await prService.CanMergeAsync(repoName, current.PullRequestNumber);
        if (canMerge)
        {
            var strategy = Enum.TryParse<MergeStrategy>(current.MergeStrategy, out var s) ? s : MergeStrategy.MergeCommit;
            var (merged, mergeError) = await prService.MergePullRequestAsync(repoName, current.PullRequestNumber, current.EnqueuedBy, strategy);
            if (merged)
            {
                current.State = MergeQueueState.Merged;
                current.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Merge queue merged PR #{Number} into {Branch} in {RepoName}", current.PullRequestNumber, targetBranch, repoName);
            }
            else
            {
                await FailEntryAsync(services, db, current, pr, $"Merge failed: {mergeError}", ct);
            }
            return;
        }

        if (IsRetryableReason(reason))
            return; // checks still running — wait for the next tick

        await FailEntryAsync(services, db, current, pr, reason ?? "Not mergeable", ct);
    }

    private async Task StartValidationAsync(IServiceProvider services, AppDbContext db, MergeQueueEntry entry, PullRequest pr, CancellationToken ct)
    {
        entry.State = MergeQueueState.Validating;
        entry.StartedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var repoPath = await GetRepoPathAsync(services);
        repoPath = ResolveRepoPath(repoPath, entry.RepoName);
        if (repoPath == null)
        {
            await FailEntryAsync(services, db, entry, pr, "Repository not found on disk", ct);
            return;
        }

        try
        {
            using var repo = new GitRepository(repoPath);
            var source = repo.Branches[pr.SourceBranch];
            var target = repo.Branches[pr.TargetBranch];
            if (source?.Tip == null || target?.Tip == null)
            {
                await FailEntryAsync(services, db, entry, pr, "Source or target branch not found", ct);
                return;
            }

            var divergence = repo.ObjectDatabase.CalculateHistoryDivergence(source.Tip, target.Tip);
            if ((divergence.BehindBy ?? 0) > 0)
            {
                // Bring the PR branch up to date with the target so CI validates the
                // exact tree that will land on the target branch
                var mergeResult = repo.ObjectDatabase.MergeCommits(source.Tip, target.Tip, null);
                if (mergeResult.Status == MergeTreeStatus.Conflicts)
                {
                    await FailEntryAsync(services, db, entry, pr, $"Merge conflict with {pr.TargetBranch} — resolve conflicts and re-queue", ct);
                    return;
                }

                var sig = new Signature("MyPersonalGit Merge Queue", "merge-queue@localhost", DateTimeOffset.UtcNow);
                var mergeCommit = repo.ObjectDatabase.CreateCommit(
                    sig, sig,
                    $"Merge branch '{pr.TargetBranch}' into {pr.SourceBranch} (merge queue)",
                    mergeResult.Tree,
                    new[] { source.Tip, target.Tip },
                    prettifyMessage: true);
                repo.Refs.UpdateTarget(repo.Refs[source.CanonicalName], mergeCommit.Id);

                _logger.LogInformation("Merge queue updated {Source} with {Target} in {RepoName} ({Sha})",
                    pr.SourceBranch, pr.TargetBranch, entry.RepoName, mergeCommit.Sha[..8]);

                // Kick off CI against the updated branch head
                try
                {
                    var workflowService = services.GetRequiredService<IWorkflowService>();
                    await workflowService.TriggerPullRequestWorkflowsAsync(
                        entry.RepoName, repoPath, pr.SourceBranch, pr.TargetBranch, mergeCommit.Sha,
                        pr.Title, "merge-queue");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to trigger merge-queue CI for PR #{Number} in {RepoName}", pr.Number, entry.RepoName);
                }
            }
            // Already up to date: fall through — the next tick evaluates CanMergeAsync
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Merge queue branch update failed for PR #{Number} in {RepoName}", pr.Number, entry.RepoName);
            await FailEntryAsync(services, db, entry, pr, $"Branch update failed: {ex.Message}", ct);
        }
    }

    private async Task FailEntryAsync(IServiceProvider services, AppDbContext db, MergeQueueEntry entry, PullRequest pr, string reason, CancellationToken ct)
    {
        entry.State = MergeQueueState.Failed;
        entry.FailureReason = reason;
        entry.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Merge queue entry for PR #{Number} in {RepoName} failed: {Reason}", entry.PullRequestNumber, entry.RepoName, reason);

        try
        {
            var notifications = services.GetRequiredService<INotificationService>();
            await notifications.NotifyWatchersAsync(
                entry.RepoName, actor: "merge-queue",
                participants: new[] { pr.Author, entry.EnqueuedBy },
                NotificationType.PullRequestReview,
                $"PR #{entry.PullRequestNumber} removed from merge queue",
                $"Merge queue: {reason}",
                $"/repo/{entry.RepoName}/pulls/{entry.PullRequestNumber}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send merge queue failure notification");
        }
    }

    /// <summary>Reasons that mean "checks are still running" rather than a hard failure.</summary>
    internal static bool IsRetryableReason(string? reason)
    {
        if (string.IsNullOrEmpty(reason)) return false;
        return reason.Contains("has not run", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("Queued", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("InProgress", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("Pending", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> GetRepoPathAsync(IServiceProvider services)
    {
        var adminService = services.GetRequiredService<IAdminService>();
        var settings = await adminService.GetSystemSettingsAsync();
        return !string.IsNullOrEmpty(settings.ProjectRoot)
            ? settings.ProjectRoot
            : _config["Git:ProjectRoot"] ?? "/repos";
    }

    private static string? ResolveRepoPath(string projectRoot, string repoName)
    {
        var path = Path.Combine(projectRoot, repoName);
        if (GitRepository.IsValid(path)) return path;
        if (GitRepository.IsValid(path + ".git")) return path + ".git";
        return null;
    }
}
