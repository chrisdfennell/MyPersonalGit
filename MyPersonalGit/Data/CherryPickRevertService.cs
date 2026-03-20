using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;
using GitRepository = LibGit2Sharp.Repository;

namespace MyPersonalGit.Data;

public interface ICherryPickRevertService
{
    Task<(bool Success, string? Error, string? NewSha)> CherryPickAsync(string repoName, string commitSha, string targetBranch, string actor);
    Task<(bool Success, string? Error, string? NewSha)> RevertAsync(string repoName, string commitSha, string branch, string actor);
    Task<(bool Success, string? Error, int? PrNumber)> CherryPickAsPullRequestAsync(string repoName, string commitSha, string targetBranch, string actor);
    Task<(bool Success, string? Error, int? PrNumber)> RevertAsPullRequestAsync(string repoName, string commitSha, string branch, string actor);
}

public class CherryPickRevertService : ICherryPickRevertService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<CherryPickRevertService> _logger;
    private readonly IPullRequestService _prService;
    private readonly IAdminService _adminService;
    private readonly IConfiguration _config;

    public CherryPickRevertService(
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<CherryPickRevertService> logger,
        IPullRequestService prService,
        IAdminService adminService,
        IConfiguration config)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _prService = prService;
        _adminService = adminService;
        _config = config;
    }

    public async Task<(bool Success, string? Error, string? NewSha)> CherryPickAsync(string repoName, string commitSha, string targetBranch, string actor)
    {
        var repoPath = await GetRepoPath(repoName);
        if (repoPath == null) return (false, "Repository not found on disk", null);

        try
        {
            using var repo = new GitRepository(repoPath);
            var commit = repo.Lookup<Commit>(new ObjectId(commitSha));
            if (commit == null) return (false, $"Commit {commitSha} not found", null);

            var target = repo.Branches[targetBranch];
            if (target?.Tip == null) return (false, $"Branch '{targetBranch}' not found", null);

            var parent = commit.Parents.FirstOrDefault();
            if (parent == null) return (false, "Cannot cherry-pick a root commit", null);

            // Cherry-pick: merge the commit onto the target using the commit's parent as merge base
            var mergeResult = repo.ObjectDatabase.MergeCommits(target.Tip, commit, null);
            if (mergeResult.Status == MergeTreeStatus.Conflicts)
                return (false, "Cherry-pick resulted in merge conflicts", null);

            var author = new Signature(actor, $"{actor}@localhost", DateTimeOffset.Now);
            var message = $"{commit.Message.TrimEnd()}\n\n(cherry picked from commit {commitSha[..7]})";

            var newCommit = repo.ObjectDatabase.CreateCommit(
                commit.Author, author, message,
                mergeResult.Tree,
                new[] { target.Tip },
                prettifyMessage: true);

            repo.Refs.UpdateTarget(repo.Refs[target.CanonicalName], newCommit.Id);

            _logger.LogInformation("Cherry-picked {Sha} onto {Branch} in {Repo} by {Actor}", commitSha[..7], targetBranch, repoName, actor);
            return (true, null, newCommit.Sha);
        }
        catch (Exception ex)
        {
            return (false, $"Cherry-pick failed: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string? Error, string? NewSha)> RevertAsync(string repoName, string commitSha, string branch, string actor)
    {
        var repoPath = await GetRepoPath(repoName);
        if (repoPath == null) return (false, "Repository not found on disk", null);

        try
        {
            using var repo = new GitRepository(repoPath);
            var commit = repo.Lookup<Commit>(new ObjectId(commitSha));
            if (commit == null) return (false, $"Commit {commitSha} not found", null);

            var target = repo.Branches[branch];
            if (target?.Tip == null) return (false, $"Branch '{branch}' not found", null);

            var parent = commit.Parents.FirstOrDefault();
            if (parent == null) return (false, "Cannot revert a root commit", null);

            // Revert: apply the inverse of the commit's changes
            // Merge the parent onto the current tip, using the commit as the merge base
            var mergeResult = repo.ObjectDatabase.MergeCommits(target.Tip, parent, null);
            if (mergeResult.Status == MergeTreeStatus.Conflicts)
                return (false, "Revert resulted in merge conflicts", null);

            var author = new Signature(actor, $"{actor}@localhost", DateTimeOffset.Now);
            var message = $"Revert \"{commit.MessageShort}\"\n\nThis reverts commit {commitSha[..7]}.";

            var newCommit = repo.ObjectDatabase.CreateCommit(
                author, author, message,
                mergeResult.Tree,
                new[] { target.Tip },
                prettifyMessage: true);

            repo.Refs.UpdateTarget(repo.Refs[target.CanonicalName], newCommit.Id);

            _logger.LogInformation("Reverted {Sha} on {Branch} in {Repo} by {Actor}", commitSha[..7], branch, repoName, actor);
            return (true, null, newCommit.Sha);
        }
        catch (Exception ex)
        {
            return (false, $"Revert failed: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string? Error, int? PrNumber)> CherryPickAsPullRequestAsync(string repoName, string commitSha, string targetBranch, string actor)
    {
        var repoPath = await GetRepoPath(repoName);
        if (repoPath == null) return (false, "Repository not found on disk", null);

        var tempBranch = $"cherry-pick/{commitSha[..7]}-to-{targetBranch}";

        try
        {
            using var repo = new GitRepository(repoPath);
            var commit = repo.Lookup<Commit>(new ObjectId(commitSha));
            if (commit == null) return (false, $"Commit {commitSha} not found", null);

            var target = repo.Branches[targetBranch];
            if (target?.Tip == null) return (false, $"Branch '{targetBranch}' not found", null);

            // Create temp branch from target
            var branch = repo.CreateBranch(tempBranch, target.Tip);

            var parent = commit.Parents.FirstOrDefault();
            if (parent == null) return (false, "Cannot cherry-pick a root commit", null);

            var mergeResult = repo.ObjectDatabase.MergeCommits(target.Tip, commit, null);
            if (mergeResult.Status == MergeTreeStatus.Conflicts)
            {
                repo.Branches.Remove(tempBranch);
                return (false, "Cherry-pick resulted in merge conflicts", null);
            }

            var author = new Signature(actor, $"{actor}@localhost", DateTimeOffset.Now);
            var message = $"{commit.Message.TrimEnd()}\n\n(cherry picked from commit {commitSha[..7]})";

            var newCommit = repo.ObjectDatabase.CreateCommit(
                commit.Author, author, message,
                mergeResult.Tree,
                new[] { target.Tip },
                prettifyMessage: true);

            repo.Refs.UpdateTarget(repo.Refs[branch.CanonicalName], newCommit.Id);
        }
        catch (Exception ex)
        {
            return (false, $"Cherry-pick failed: {ex.Message}", null);
        }

        // Create PR
        var pr = await _prService.CreatePullRequestAsync(
            repoName,
            $"Cherry-pick: {commitSha[..7]} to {targetBranch}",
            $"Cherry-picking commit {commitSha[..7]} to `{targetBranch}`.\n\nRequested by @{actor}.",
            actor, tempBranch, targetBranch);

        return (true, null, pr.Number);
    }

    public async Task<(bool Success, string? Error, int? PrNumber)> RevertAsPullRequestAsync(string repoName, string commitSha, string branch, string actor)
    {
        var repoPath = await GetRepoPath(repoName);
        if (repoPath == null) return (false, "Repository not found on disk", null);

        var tempBranch = $"revert/{commitSha[..7]}";
        string? messageShort = null;

        try
        {
            using var repo = new GitRepository(repoPath);
            var commit = repo.Lookup<Commit>(new ObjectId(commitSha));
            if (commit == null) return (false, $"Commit {commitSha} not found", null);
            messageShort = commit.MessageShort;

            var target = repo.Branches[branch];
            if (target?.Tip == null) return (false, $"Branch '{branch}' not found", null);

            var parent = commit.Parents.FirstOrDefault();
            if (parent == null) return (false, "Cannot revert a root commit", null);

            var branchObj = repo.CreateBranch(tempBranch, target.Tip);

            var mergeResult = repo.ObjectDatabase.MergeCommits(target.Tip, parent, null);
            if (mergeResult.Status == MergeTreeStatus.Conflicts)
            {
                repo.Branches.Remove(tempBranch);
                return (false, "Revert resulted in merge conflicts", null);
            }

            var author = new Signature(actor, $"{actor}@localhost", DateTimeOffset.Now);
            var message = $"Revert \"{commit.MessageShort}\"\n\nThis reverts commit {commitSha[..7]}.";

            var newCommit = repo.ObjectDatabase.CreateCommit(
                author, author, message,
                mergeResult.Tree,
                new[] { target.Tip },
                prettifyMessage: true);

            repo.Refs.UpdateTarget(repo.Refs[branchObj.CanonicalName], newCommit.Id);
        }
        catch (Exception ex)
        {
            return (false, $"Revert failed: {ex.Message}", null);
        }

        var pr = await _prService.CreatePullRequestAsync(
            repoName,
            $"Revert \"{messageShort}\"",
            $"Reverts commit {commitSha[..7]}.\n\nRequested by @{actor}.",
            actor, tempBranch, branch);

        return (true, null, pr.Number);
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
