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
    Task<bool> ToggleDraftAsync(string repoName, int number);
    Task<bool> EnableAutoMergeAsync(string repoName, int number, MergeStrategy strategy);
    Task<bool> DisableAutoMergeAsync(string repoName, int number);
    Task<bool> ReopenPullRequestAsync(string repoName, int number);
    Task<List<ReviewComment>> GetReviewCommentsAsync(string repoName, int number);
    Task<ReviewComment> AddReviewCommentAsync(string repoName, int number, string author, string body, string filePath, int lineNumber, string side = "RIGHT", int? replyToId = null);
    Task<bool> DeleteReviewCommentAsync(int commentId);
    Task<bool> UpdateReviewCommentSuggestionAsync(int commentId, string suggestionBody);
}

public class PullRequestService : IPullRequestService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<PullRequestService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IActivityService _activityService;
    private readonly IAdminService _adminService;
    private readonly IBranchProtectionService _branchProtectionService;
    private readonly ICodeOwnersService _codeOwnersService;
    private readonly IIssueAutoCloseService _issueAutoCloseService;
    private readonly IWorkflowService _workflowService;
    private readonly IGpgKeyService _gpgKeyService;
    private readonly IAutoMergeService _autoMergeService;
    private readonly IConfiguration _config;

    public PullRequestService(
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<PullRequestService> logger,
        INotificationService notificationService,
        IActivityService activityService,
        IAdminService adminService,
        IBranchProtectionService branchProtectionService,
        ICodeOwnersService codeOwnersService,
        IIssueAutoCloseService issueAutoCloseService,
        IWorkflowService workflowService,
        IGpgKeyService gpgKeyService,
        IAutoMergeService autoMergeService,
        IConfiguration config)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _notificationService = notificationService;
        _activityService = activityService;
        _adminService = adminService;
        _branchProtectionService = branchProtectionService;
        _codeOwnersService = codeOwnersService;
        _issueAutoCloseService = issueAutoCloseService;
        _workflowService = workflowService;
        _gpgKeyService = gpgKeyService;
        _autoMergeService = autoMergeService;
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

        // Auto-assign reviewers from CODEOWNERS
        try
        {
            var repoPath = await GetRepoPath(repoName);
            if (repoPath != null && GitRepository.IsValid(repoPath))
            {
                using var repo = new GitRepository(repoPath);
                var source = repo.Branches[sourceBranch];
                var target = repo.Branches[targetBranch];

                if (source?.Tip != null && target?.Tip != null)
                {
                    var diff = repo.Diff.Compare<TreeChanges>(target.Tip.Tree, source.Tip.Tree);
                    var changedFiles = diff.Select(c => c.Path).ToList();

                    var ownersByFile = _codeOwnersService.GetCodeOwnersForPullRequest(repoPath, targetBranch, changedFiles);
                    var allOwners = ownersByFile.Values.SelectMany(o => o).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                    // Filter out the PR author and only include users that exist in the system
                    var validReviewers = new List<string>();
                    foreach (var owner in allOwners)
                    {
                        if (owner.Equals(author, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == owner);
                        if (user != null)
                            validReviewers.Add(owner);
                    }

                    if (validReviewers.Any())
                    {
                        pr.Reviewers = validReviewers;
                        _logger.LogInformation("CODEOWNERS auto-assigned reviewers [{Reviewers}] to PR #{Number} in {RepoName}",
                            string.Join(", ", validReviewers), pr.Number, repoName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-assign CODEOWNERS reviewers for PR in {RepoName}", repoName);
        }

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

        // Trigger pull_request workflows
        if (!isDraft)
        {
            try
            {
                var wfRepoPath = await GetRepoPath(repoName);
                if (wfRepoPath != null)
                {
                    using var wfRepo = new GitRepository(wfRepoPath);
                    var tip = wfRepo.Branches[sourceBranch]?.Tip;
                    var sha = tip?.Sha ?? "HEAD";
                    await _workflowService.TriggerPullRequestWorkflowsAsync(
                        repoName, wfRepoPath, sourceBranch, targetBranch, sha, title, author);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to trigger PR workflows for {RepoName}", repoName);
            }
        }

        return pr;
    }

    public async Task<(bool CanMerge, string? Reason)> CanMergeAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        var pr = await db.PullRequests
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.RepoName == repoName && p.Number == number);
        if (pr == null) return (false, "Pull request not found");
        if (pr.State != PullRequestState.Open) return (false, "Pull request is not open");
        if (pr.IsDraft) return (false, "Cannot merge a draft pull request");

        // Check branch protection rules
        var protectionRule = await _branchProtectionService.GetMatchingRuleAsync(repoName, pr.TargetBranch);
        if (protectionRule != null)
        {
            // Check required approvals
            if (protectionRule.RequirePullRequest && protectionRule.RequiredApprovals > 0)
            {
                var approvalCount = pr.Reviews
                    .Where(r => r.State == ReviewState.Approved)
                    .Select(r => r.Author)
                    .Distinct()
                    .Count();

                if (approvalCount < protectionRule.RequiredApprovals)
                    return (false, $"Requires {protectionRule.RequiredApprovals} approval(s), has {approvalCount}");
            }

            // Check for changes-requested reviews that haven't been resolved
            if (protectionRule.RequirePullRequest)
            {
                var latestReviewByAuthor = pr.Reviews
                    .GroupBy(r => r.Author)
                    .Select(g => g.OrderByDescending(r => r.CreatedAt).First())
                    .ToList();

                var hasChangesRequested = latestReviewByAuthor.Any(r => r.State == ReviewState.ChangesRequested);
                if (hasChangesRequested)
                    return (false, "Changes have been requested — address review feedback before merging");
            }

            // Check required status checks (workflow runs + commit statuses)
            if (protectionRule.RequireStatusChecks && protectionRule.RequiredStatusChecks.Any())
            {
                var workflowRuns = await db.WorkflowRuns
                    .Where(w => w.RepoName == repoName && w.Branch == pr.SourceBranch)
                    .OrderByDescending(w => w.CreatedAt)
                    .ToListAsync();

                foreach (var requiredCheck in protectionRule.RequiredStatusChecks)
                {
                    // Check workflow runs first
                    var latestRun = workflowRuns.FirstOrDefault(w => w.WorkflowName == requiredCheck);
                    if (latestRun != null)
                    {
                        if (latestRun.Status != WorkflowStatus.Success)
                            return (false, $"Required status check '{requiredCheck}' has not passed (status: {latestRun.Status})");
                        continue;
                    }

                    // Fall back to commit status API
                    var commitStatus = await db.CommitStatuses
                        .Where(s => s.RepoName == repoName && s.Context == requiredCheck)
                        .OrderByDescending(s => s.UpdatedAt)
                        .FirstOrDefaultAsync();

                    if (commitStatus == null)
                        return (false, $"Required status check '{requiredCheck}' has not run");
                    if (commitStatus.State != CommitStatusState.Success)
                        return (false, $"Required status check '{requiredCheck}' has not passed (status: {commitStatus.State})");
                }
            }

            // Check linear history requirement
            if (protectionRule.RequireLinearHistory)
            {
                // Linear history is enforced by only allowing squash or rebase merges
                // This is informational — actual enforcement is in MergePullRequestAsync
            }

            // Check CODEOWNERS approval requirement
            if (protectionRule.RequireCodeOwnersApproval)
            {
                var coRepoPath = await GetRepoPath(repoName);
                if (coRepoPath != null && GitRepository.IsValid(coRepoPath))
                {
                    try
                    {
                        using var coRepo = new GitRepository(coRepoPath);
                        var coSource = coRepo.Branches[pr.SourceBranch];
                        var coTarget = coRepo.Branches[pr.TargetBranch];

                        if (coSource?.Tip != null && coTarget?.Tip != null)
                        {
                            var diff = coRepo.Diff.Compare<TreeChanges>(coTarget.Tip.Tree, coSource.Tip.Tree);
                            var changedFiles = diff.Select(c => c.Path).ToList();
                            var ownersByFile = _codeOwnersService.GetCodeOwnersForPullRequest(coRepoPath, pr.TargetBranch, changedFiles);

                            var approvedAuthors = pr.Reviews
                                .Where(r => r.State == ReviewState.Approved)
                                .Select(r => r.Author)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToHashSet(StringComparer.OrdinalIgnoreCase);

                            var unapprovedFiles = new List<string>();
                            foreach (var (file, owners) in ownersByFile)
                            {
                                if (!owners.Any()) continue;
                                if (!owners.Any(o => approvedAuthors.Contains(o)))
                                    unapprovedFiles.Add(file);
                            }

                            if (unapprovedFiles.Any())
                            {
                                var fileList = string.Join(", ", unapprovedFiles.Take(3));
                                var more = unapprovedFiles.Count > 3 ? $" and {unapprovedFiles.Count - 3} more" : "";
                                return (false, $"CODEOWNERS approval required for: {fileList}{more}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to check CODEOWNERS approval for PR #{Number} in {RepoName}", number, repoName);
                    }
                }
            }
        }

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
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.RepoName == repoName && p.Number == number);

        if (pr == null)
            return (false, "Pull request not found");

        if (pr.State != PullRequestState.Open)
            return (false, "Pull request is not open");

        if (pr.IsDraft)
            return (false, "Cannot merge a draft pull request");

        // Enforce branch protection rules
        var protectionRule = await _branchProtectionService.GetMatchingRuleAsync(repoName, pr.TargetBranch);
        if (protectionRule != null)
        {
            if (protectionRule.RequirePullRequest && protectionRule.RequiredApprovals > 0)
            {
                var approvalCount = pr.Reviews
                    .Where(r => r.State == ReviewState.Approved)
                    .Select(r => r.Author)
                    .Distinct()
                    .Count();
                if (approvalCount < protectionRule.RequiredApprovals)
                    return (false, $"Branch protection: requires {protectionRule.RequiredApprovals} approval(s), has {approvalCount}");
            }

            if (protectionRule.RequirePullRequest)
            {
                var latestReviewByAuthor = pr.Reviews
                    .GroupBy(r => r.Author)
                    .Select(g => g.OrderByDescending(r => r.CreatedAt).First())
                    .ToList();
                if (latestReviewByAuthor.Any(r => r.State == ReviewState.ChangesRequested))
                    return (false, "Branch protection: changes requested — address feedback before merging");
            }

            if (protectionRule.RequireStatusChecks && protectionRule.RequiredStatusChecks.Any())
            {
                var workflowRuns = await db.WorkflowRuns
                    .Where(w => w.RepoName == repoName && w.Branch == pr.SourceBranch)
                    .OrderByDescending(w => w.CreatedAt)
                    .ToListAsync();
                foreach (var check in protectionRule.RequiredStatusChecks)
                {
                    var latestWf = workflowRuns.FirstOrDefault(w => w.WorkflowName == check);
                    if (latestWf != null)
                    {
                        if (latestWf.Status != WorkflowStatus.Success)
                            return (false, $"Branch protection: required status check '{check}' has not passed");
                        continue;
                    }
                    var commitStatus = await db.CommitStatuses
                        .Where(s => s.RepoName == repoName && s.Context == check)
                        .OrderByDescending(s => s.UpdatedAt)
                        .FirstOrDefaultAsync();
                    if (commitStatus == null || commitStatus.State != CommitStatusState.Success)
                        return (false, $"Branch protection: required status check '{check}' has not passed");
                }
            }

            if (protectionRule.RequireLinearHistory && strategy == MergeStrategy.MergeCommit)
                return (false, "Branch protection: linear history required — use squash or rebase merge strategy");

            // Enforce CODEOWNERS approval
            if (protectionRule.RequireCodeOwnersApproval)
            {
                var coRepoPath = await GetRepoPath(repoName);
                if (coRepoPath != null && GitRepository.IsValid(coRepoPath))
                {
                    try
                    {
                        using var coRepo = new GitRepository(coRepoPath);
                        var coSource = coRepo.Branches[pr.SourceBranch];
                        var coTarget = coRepo.Branches[pr.TargetBranch];

                        if (coSource?.Tip != null && coTarget?.Tip != null)
                        {
                            var diff = coRepo.Diff.Compare<TreeChanges>(coTarget.Tip.Tree, coSource.Tip.Tree);
                            var changedFiles = diff.Select(c => c.Path).ToList();
                            var ownersByFile = _codeOwnersService.GetCodeOwnersForPullRequest(coRepoPath, pr.TargetBranch, changedFiles);

                            var approvedAuthors = pr.Reviews
                                .Where(r => r.State == ReviewState.Approved)
                                .Select(r => r.Author)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToHashSet(StringComparer.OrdinalIgnoreCase);

                            foreach (var (file, owners) in ownersByFile)
                            {
                                if (!owners.Any()) continue;
                                if (!owners.Any(o => approvedAuthors.Contains(o)))
                                    return (false, $"Branch protection: CODEOWNERS approval required for '{file}'");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to check CODEOWNERS for merge of PR #{Number}", number);
                    }
                }
            }
        }

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

                    var msg = $"Merge pull request #{pr.Number} from {pr.SourceBranch}\n\n{pr.Title}";

                    // Try signed commit if enabled
                    var settings = await _adminService.GetSystemSettingsAsync();
                    string? signedSha = null;
                    if (settings.SignMergeCommits && !string.IsNullOrEmpty(settings.ServerGpgKeyId))
                    {
                        signedSha = await _gpgKeyService.CreateSignedCommitAsync(
                            repoPath, mergeResult.Tree.Sha,
                            new[] { targetBranch.Tip.Sha, sourceBranch.Tip.Sha },
                            msg, mergedBy, $"{mergedBy}@localhost", settings.ServerGpgKeyId);
                    }

                    if (signedSha != null)
                    {
                        var signedCommit = repo.Lookup<Commit>(new ObjectId(signedSha));
                        repo.Refs.UpdateTarget(repo.Refs[targetBranch.CanonicalName], signedCommit.Id);
                    }
                    else
                    {
                        var mergeCommit = repo.ObjectDatabase.CreateCommit(
                            author, author, msg,
                            mergeResult.Tree,
                            new[] { targetBranch.Tip, sourceBranch.Tip },
                            prettifyMessage: true);
                        repo.Refs.UpdateTarget(repo.Refs[targetBranch.CanonicalName], mergeCommit.Id);
                    }
                    break;
                }

                case MergeStrategy.Squash:
                {
                    var mergeResult = repo.ObjectDatabase.MergeCommits(sourceBranch.Tip, targetBranch.Tip, null);
                    if (mergeResult.Status == MergeTreeStatus.Conflicts)
                        return (false, "Merge conflicts detected — resolve conflicts before merging");

                    var filter = new CommitFilter
                    {
                        IncludeReachableFrom = sourceBranch.Tip,
                        ExcludeReachableFrom = targetBranch.Tip,
                        SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
                    };
                    var commits = repo.Commits.QueryBy(filter).ToList();
                    var messages = string.Join("\n", commits.Select(c => $"* {c.MessageShort}"));
                    var msg = $"{pr.Title} (#{pr.Number})\n\n{messages}";

                    var squashSettings = await _adminService.GetSystemSettingsAsync();
                    string? signedSha = null;
                    if (squashSettings.SignMergeCommits && !string.IsNullOrEmpty(squashSettings.ServerGpgKeyId))
                    {
                        signedSha = await _gpgKeyService.CreateSignedCommitAsync(
                            repoPath, mergeResult.Tree.Sha,
                            new[] { targetBranch.Tip.Sha },
                            msg, mergedBy, $"{mergedBy}@localhost", squashSettings.ServerGpgKeyId);
                    }

                    if (signedSha != null)
                    {
                        var signedCommit = repo.Lookup<Commit>(new ObjectId(signedSha));
                        repo.Refs.UpdateTarget(repo.Refs[targetBranch.CanonicalName], signedCommit.Id);
                    }
                    else
                    {
                        var squashCommit = repo.ObjectDatabase.CreateCommit(
                            author, author, msg,
                            mergeResult.Tree,
                            new[] { targetBranch.Tip },
                            prettifyMessage: true);
                        repo.Refs.UpdateTarget(repo.Refs[targetBranch.CanonicalName], squashCommit.Id);
                    }
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

                    var rebaseSettings = await _adminService.GetSystemSettingsAsync();
                    var currentTip = targetBranch.Tip;

                    foreach (var commit in commits)
                    {
                        Tree rebasedTree;
                        var parent = commit.Parents.FirstOrDefault();
                        if (parent == null)
                        {
                            rebasedTree = commit.Tree;
                        }
                        else
                        {
                            var cherryResult = repo.ObjectDatabase.MergeCommits(currentTip, commit, null);
                            if (cherryResult.Status == MergeTreeStatus.Conflicts)
                                return (false, $"Conflict while rebasing commit {commit.Id.ToString(7)}: {commit.MessageShort}");
                            rebasedTree = cherryResult.Tree;
                        }

                        // Try signed commit if enabled
                        string? signedSha = null;
                        if (rebaseSettings.SignMergeCommits && !string.IsNullOrEmpty(rebaseSettings.ServerGpgKeyId))
                        {
                            signedSha = await _gpgKeyService.CreateSignedCommitAsync(
                                repoPath, rebasedTree.Sha,
                                new[] { currentTip.Sha },
                                commit.Message, commit.Author.Name, commit.Author.Email, rebaseSettings.ServerGpgKeyId);
                        }

                        if (signedSha != null)
                        {
                            currentTip = repo.Lookup<Commit>(new ObjectId(signedSha));
                        }
                        else
                        {
                            var rebasedCommit = repo.ObjectDatabase.CreateCommit(
                                commit.Author, author,
                                commit.Message,
                                rebasedTree,
                                new[] { currentTip },
                                prettifyMessage: true);
                            currentTip = rebasedCommit;
                        }
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

        // Auto-close referenced issues from PR commit messages and title/body
        try
        {
            var commitMessages = new List<string>();

            // Include PR title and body as sources of closing keywords
            commitMessages.Add(pr.Title);
            if (!string.IsNullOrEmpty(pr.Body))
                commitMessages.Add(pr.Body);

            // Collect commit messages from the merged branch
            var mergeRepoPath = await GetRepoPath(repoName);
            if (mergeRepoPath != null && GitRepository.IsValid(mergeRepoPath))
            {
                using var mergeRepo = new GitRepository(mergeRepoPath);
                var mergeTargetBranch = mergeRepo.Branches[pr.TargetBranch];
                if (mergeTargetBranch?.Tip != null)
                {
                    // Get recent commits on the target branch (the merge just landed)
                    var recentFilter = new CommitFilter
                    {
                        IncludeReachableFrom = mergeTargetBranch.Tip,
                        SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
                    };
                    // Take enough commits to cover the PR's changes
                    foreach (var c in mergeRepo.Commits.QueryBy(recentFilter).Take(50))
                    {
                        commitMessages.Add(c.Message);
                    }
                }
            }

            await _issueAutoCloseService.ProcessPullRequestMerge(repoName, number, commitMessages, mergedBy);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process issue auto-close for PR #{Number} in {RepoName}", number, repoName);
        }

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

        // Trigger auto-merge check when a review is approved
        if (state == ReviewState.Approved)
        {
            try { await _autoMergeService.TryAutoMergeAsync(repoName); }
            catch (Exception ex) { _logger.LogWarning(ex, "Auto-merge check failed after review on PR #{Number}", number); }
        }

        return true;
    }

    public async Task<bool> ToggleDraftAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        var pr = await db.PullRequests.FirstOrDefaultAsync(p => p.RepoName == repoName && p.Number == number);
        if (pr == null || pr.State != PullRequestState.Open) return false;

        pr.IsDraft = !pr.IsDraft;
        await db.SaveChangesAsync();

        _logger.LogInformation("PR #{Number} in {RepoName} draft status toggled to {IsDraft}", number, repoName, pr.IsDraft);
        return true;
    }

    public async Task<bool> EnableAutoMergeAsync(string repoName, int number, MergeStrategy strategy)
    {
        using var db = _dbFactory.CreateDbContext();
        var pr = await db.PullRequests.FirstOrDefaultAsync(p => p.RepoName == repoName && p.Number == number);
        if (pr == null || pr.State != PullRequestState.Open) return false;

        pr.AutoMergeEnabled = true;
        pr.AutoMergeStrategy = strategy.ToString();
        await db.SaveChangesAsync();

        _logger.LogInformation("Auto-merge enabled for PR #{Number} in {RepoName} with strategy {Strategy}", number, repoName, strategy);
        return true;
    }

    public async Task<bool> DisableAutoMergeAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        var pr = await db.PullRequests.FirstOrDefaultAsync(p => p.RepoName == repoName && p.Number == number);
        if (pr == null) return false;

        pr.AutoMergeEnabled = false;
        pr.AutoMergeStrategy = null;
        await db.SaveChangesAsync();

        _logger.LogInformation("Auto-merge disabled for PR #{Number} in {RepoName}", number, repoName);
        return true;
    }

    public async Task<bool> ReopenPullRequestAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        var pr = await db.PullRequests.FirstOrDefaultAsync(p => p.RepoName == repoName && p.Number == number);
        if (pr == null || pr.State != PullRequestState.Closed) return false;

        pr.State = PullRequestState.Open;
        pr.ClosedAt = null;
        await db.SaveChangesAsync();

        _logger.LogInformation("PR #{Number} reopened in {RepoName}", number, repoName);
        return true;
    }

    public async Task<List<ReviewComment>> GetReviewCommentsAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        var pr = await db.PullRequests.FirstOrDefaultAsync(p => p.RepoName == repoName && p.Number == number);
        if (pr == null) return new();

        return await db.ReviewComments
            .Where(c => c.PullRequestId == pr.Id)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<ReviewComment> AddReviewCommentAsync(string repoName, int number, string author, string body, string filePath, int lineNumber, string side = "RIGHT", int? replyToId = null)
    {
        using var db = _dbFactory.CreateDbContext();
        var pr = await db.PullRequests.FirstOrDefaultAsync(p => p.RepoName == repoName && p.Number == number);
        if (pr == null) throw new InvalidOperationException("Pull request not found");

        var comment = new ReviewComment
        {
            PullRequestId = pr.Id,
            Author = author,
            Body = body,
            FilePath = filePath,
            LineNumber = lineNumber,
            Side = side,
            ReplyToId = replyToId,
            CreatedAt = DateTime.UtcNow
        };

        db.ReviewComments.Add(comment);
        await db.SaveChangesAsync();

        _logger.LogInformation("Review comment added to PR #{Number} in {RepoName} at {FilePath}:{LineNumber} by {Author}", number, repoName, filePath, lineNumber, author);
        return comment;
    }

    public async Task<bool> DeleteReviewCommentAsync(int commentId)
    {
        using var db = _dbFactory.CreateDbContext();
        var comment = await db.ReviewComments.FindAsync(commentId);
        if (comment == null) return false;

        db.ReviewComments.Remove(comment);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateReviewCommentSuggestionAsync(int commentId, string suggestionBody)
    {
        using var db = _dbFactory.CreateDbContext();
        var comment = await db.ReviewComments.FindAsync(commentId);
        if (comment == null) return false;

        comment.SuggestionBody = suggestionBody;
        await db.SaveChangesAsync();
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
