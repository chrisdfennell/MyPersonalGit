using System.Text;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;
using GitRepository = LibGit2Sharp.Repository;

namespace MyPersonalGit.Data;

public interface IConflictResolutionService
{
    Task<List<MergeConflict>> GetConflictsAsync(string repoName, string sourceBranch, string targetBranch);
    Task<(bool Success, string? Error)> ApplyResolutionsAsync(string repoName, int prNumber, string resolvedBy, Dictionary<string, string> resolvedFiles);
}

public class ConflictResolutionService : IConflictResolutionService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAdminService _adminService;
    private readonly IActivityService _activityService;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _config;
    private readonly ILogger<ConflictResolutionService> _logger;

    public ConflictResolutionService(
        IDbContextFactory<AppDbContext> dbFactory,
        IAdminService adminService,
        IActivityService activityService,
        INotificationService notificationService,
        IConfiguration config,
        ILogger<ConflictResolutionService> logger)
    {
        _dbFactory = dbFactory;
        _adminService = adminService;
        _activityService = activityService;
        _notificationService = notificationService;
        _config = config;
        _logger = logger;
    }

    public async Task<List<MergeConflict>> GetConflictsAsync(string repoName, string sourceBranch, string targetBranch)
    {
        var repoPath = await GetRepoPath(repoName);
        if (repoPath == null || !GitRepository.IsValid(repoPath))
            return new();

        var conflicts = new List<MergeConflict>();

        using var repo = new GitRepository(repoPath);
        var source = repo.Branches[sourceBranch];
        var target = repo.Branches[targetBranch];

        if (source?.Tip == null || target?.Tip == null)
            return conflicts;

        var mergeResult = repo.ObjectDatabase.MergeCommits(source.Tip, target.Tip, null);
        if (mergeResult.Status != MergeTreeStatus.Conflicts)
            return conflicts;

        // Find the merge base for ancestor content
        var mergeBase = repo.ObjectDatabase.FindMergeBase(source.Tip, target.Tip);

        foreach (var conflict in mergeResult.Conflicts)
        {
            var filePath = conflict.Ours?.Path ?? conflict.Theirs?.Path ?? conflict.Ancestor?.Path ?? "unknown";

            string? baseContent = null;
            string oursContent = "";
            string theirsContent = "";

            if (conflict.Ancestor != null)
            {
                var ancestorBlob = repo.Lookup<Blob>(conflict.Ancestor.Id);
                if (ancestorBlob != null)
                    baseContent = ancestorBlob.GetContentText();
            }

            if (conflict.Ours != null)
            {
                var oursBlob = repo.Lookup<Blob>(conflict.Ours.Id);
                if (oursBlob != null)
                    oursContent = oursBlob.GetContentText();
            }

            if (conflict.Theirs != null)
            {
                var theirsBlob = repo.Lookup<Blob>(conflict.Theirs.Id);
                if (theirsBlob != null)
                    theirsContent = theirsBlob.GetContentText();
            }

            // Build conflict marker content
            var markerContent = BuildConflictMarkers(filePath, oursContent, theirsContent, baseContent, targetBranch, sourceBranch);

            conflicts.Add(new MergeConflict
            {
                FilePath = filePath,
                BaseContent = baseContent,
                OursContent = oursContent,
                TheirsContent = theirsContent,
                ConflictMarkerContent = markerContent
            });
        }

        return conflicts;
    }

    public async Task<(bool Success, string? Error)> ApplyResolutionsAsync(string repoName, int prNumber, string resolvedBy, Dictionary<string, string> resolvedFiles)
    {
        var repoPath = await GetRepoPath(repoName);
        if (repoPath == null || !GitRepository.IsValid(repoPath))
            return (false, "Repository not found on disk");

        using var db = _dbFactory.CreateDbContext();
        var pr = await db.PullRequests
            .FirstOrDefaultAsync(p => p.RepoName == repoName && p.Number == prNumber);

        if (pr == null)
            return (false, "Pull request not found");

        if (pr.State != PullRequestState.Open)
            return (false, "Pull request is not open");

        try
        {
            using var repo = new GitRepository(repoPath);
            var sourceBranch = repo.Branches[pr.SourceBranch];
            var targetBranch = repo.Branches[pr.TargetBranch];

            if (sourceBranch?.Tip == null)
                return (false, $"Branch '{pr.SourceBranch}' not found");
            if (targetBranch?.Tip == null)
                return (false, $"Branch '{pr.TargetBranch}' not found");

            // Perform the merge to get the base tree
            var mergeResult = repo.ObjectDatabase.MergeCommits(sourceBranch.Tip, targetBranch.Tip, null);

            // Build a new tree with resolved files
            var treeDefinition = TreeDefinition.From(mergeResult.Tree);

            foreach (var (filePath, resolvedContent) in resolvedFiles)
            {
                var blob = repo.ObjectDatabase.CreateBlob(new MemoryStream(Encoding.UTF8.GetBytes(resolvedContent)));
                treeDefinition.Add(filePath, blob, Mode.NonExecutableFile);
            }

            var resolvedTree = repo.ObjectDatabase.CreateTree(treeDefinition);

            // Create the merge commit
            var author = new Signature(resolvedBy, $"{resolvedBy}@localhost", DateTimeOffset.Now);
            var mergeCommit = repo.ObjectDatabase.CreateCommit(
                author, author,
                $"Merge pull request #{prNumber} from {pr.SourceBranch}\n\n{pr.Title}\n\nConflicts resolved by {resolvedBy}",
                resolvedTree,
                new[] { targetBranch.Tip, sourceBranch.Tip },
                prettifyMessage: true);

            // Update the target branch reference
            repo.Refs.UpdateTarget(repo.Refs[targetBranch.CanonicalName], mergeCommit.Id);

            // Update PR state
            pr.State = PullRequestState.Merged;
            pr.MergedAt = DateTime.UtcNow;
            pr.MergedBy = resolvedBy;
            await db.SaveChangesAsync();

            _logger.LogInformation("PR #{Number} merged with conflict resolution in {RepoName} by {User}", prNumber, repoName, resolvedBy);

            await _activityService.RecordActivityAsync(resolvedBy, "merged_pr", repoName,
                $"{resolvedBy} merged PR #{prNumber} (with conflict resolution): {pr.Title}",
                $"/repo/{repoName}/pulls/{prNumber}");

            await _notificationService.CreateNotificationAsync(
                "current-user",
                NotificationType.PullRequestMerged,
                $"Pull request #{prNumber} merged",
                $"{resolvedBy} merged PR with conflict resolution: {pr.Title}",
                repoName,
                $"/repo/{repoName}/pulls/{prNumber}");

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply conflict resolutions for PR #{Number} in {RepoName}", prNumber, repoName);
            return (false, $"Failed to apply resolutions: {ex.Message}");
        }
    }

    private static string BuildConflictMarkers(string filePath, string oursContent, string theirsContent, string? baseContent, string oursLabel, string theirsLabel)
    {
        // Simple line-by-line diff to produce conflict markers
        var oursLines = oursContent.Split('\n');
        var theirsLines = theirsContent.Split('\n');

        // If we have a base, do a 3-way comparison; otherwise just show both sides
        var sb = new StringBuilder();
        sb.AppendLine($"<<<<<<< {oursLabel} (target)");
        sb.Append(oursContent);
        if (!oursContent.EndsWith('\n')) sb.AppendLine();
        sb.AppendLine("=======");
        sb.Append(theirsContent);
        if (!theirsContent.EndsWith('\n')) sb.AppendLine();
        sb.AppendLine($">>>>>>> {theirsLabel} (source)");

        return sb.ToString();
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
