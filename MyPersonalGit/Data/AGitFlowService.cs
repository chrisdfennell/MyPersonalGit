using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;

namespace MyPersonalGit.Data;

/// <summary>
/// AGit Flow: push to refs/for/{targetBranch} to create/update a pull request
/// without needing to fork or create a remote branch.
/// Usage: git push origin HEAD:refs/for/main
/// </summary>
public interface IAGitFlowService
{
    /// <summary>
    /// Process a push to refs/for/{targetBranch}. Creates a server-side branch
    /// (agit/{username}/{targetBranch}/{n}) and opens a pull request.
    /// Returns the PR number, or null if this is not an AGit push.
    /// </summary>
    Task<int?> ProcessAGitPushAsync(string repoDir, string repoName, string targetRef, string username);
}

public class AGitFlowService : IAGitFlowService
{
    private readonly IPullRequestService _prService;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<AGitFlowService> _logger;

    public AGitFlowService(IPullRequestService prService, IDbContextFactory<AppDbContext> dbFactory,
        ILogger<AGitFlowService> logger)
    {
        _prService = prService;
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<int?> ProcessAGitPushAsync(string repoDir, string repoName, string targetRef, string username)
    {
        // Only handle refs/for/{branch} pattern
        if (!targetRef.StartsWith("refs/for/"))
            return null;

        var targetBranch = targetRef["refs/for/".Length..];
        if (string.IsNullOrEmpty(targetBranch))
            return null;

        if (!Repository.IsValid(repoDir))
            return null;

        using var repo = new Repository(repoDir);

        // The push deposited commits at the target ref — find the tip
        var pushedRef = repo.Refs[targetRef];
        if (pushedRef == null)
        {
            _logger.LogWarning("AGit: pushed ref {Ref} not found in repo", targetRef);
            return null;
        }

        var tipSha = pushedRef.TargetIdentifier;

        // Create a server-side branch: agit/{username}/{targetBranch}/{counter}
        using var db = _dbFactory.CreateDbContext();
        var existingPrCount = await db.PullRequests
            .CountAsync(p => p.RepoName == repoName && p.Author == username && p.SourceBranch.StartsWith($"agit/{username}/{targetBranch}/"));
        var agitBranch = $"agit/{username}/{targetBranch}/{existingPrCount + 1}";

        // Create the branch pointing at the pushed commit
        var commit = repo.Lookup<Commit>(tipSha);
        if (commit == null)
        {
            _logger.LogWarning("AGit: commit {Sha} not found", tipSha);
            return null;
        }

        repo.Branches.Add(agitBranch, commit);
        _logger.LogInformation("AGit: created branch {Branch} at {Sha} for {User}", agitBranch, tipSha, username);

        // Remove the refs/for/ ref — it served its purpose
        repo.Refs.Remove(targetRef);

        // Check if there's already an open PR from this user for this target branch via AGit
        var existingPr = await db.PullRequests
            .FirstOrDefaultAsync(p => p.RepoName == repoName
                && p.Author == username
                && p.TargetBranch == targetBranch
                && p.SourceBranch.StartsWith($"agit/{username}/{targetBranch}/")
                && p.State == Models.PullRequestState.Open);

        if (existingPr != null)
        {
            // Update existing PR by pointing its source branch to the new commits
            var existingBranchRef = repo.Branches[existingPr.SourceBranch];
            if (existingBranchRef != null)
            {
                // Force-update the existing agit branch
                repo.Refs.UpdateTarget(repo.Refs[$"refs/heads/{existingPr.SourceBranch}"], tipSha);
                // Clean up the new branch we just created
                repo.Branches.Remove(agitBranch);
                _logger.LogInformation("AGit: updated existing PR #{Number} source branch {Branch}", existingPr.Number, existingPr.SourceBranch);
                return existingPr.Number;
            }
        }

        // Create a new PR
        var title = commit.MessageShort;
        var body = commit.Message.Length > commit.MessageShort.Length
            ? commit.Message[commit.MessageShort.Length..].Trim()
            : null;

        var pr = await _prService.CreatePullRequestAsync(
            repoName, title, body, username, agitBranch, targetBranch);

        _logger.LogInformation("AGit: created PR #{Number} from {Source} to {Target} for {User}",
            pr.Number, agitBranch, targetBranch, username);

        return pr.Number;
    }
}
