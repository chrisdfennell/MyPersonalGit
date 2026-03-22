using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;
using Repository = MyPersonalGit.Models.Repository;

namespace MyPersonalGit.Data;

public interface IRepositoryService
{
    Task<List<Repository>> GetRepositoriesAsync();
    Task<Repository?> GetRepositoryAsync(string name);
    Task<Repository> CreateRepositoryAsync(string name, string owner, string? description = null, bool isPrivate = false);
    Task<Repository> EnsureRepositoryRecordAsync(string name, string owner);
    Task<bool> UpdateRepositoryAsync(string name, Action<Repository> updateAction);
    Task<bool> StarRepositoryAsync(string repoName, string username);
    Task<bool> UnstarRepositoryAsync(string repoName, string username);
    Task<bool> IsStarredAsync(string repoName, string username);
    Task<RepositoryFork?> ForkRepositoryAsync(string sourceRepoName, string newOwner, string projectRoot);
    Task<List<RepositoryFork>> GetForksAsync(string repoName);
    Task<bool> IsForkedAsync(string repoName, string username);
    Task<(bool Success, string Message)> SyncForkWithUpstreamAsync(string forkedRepoName, string projectRoot);
    Task<Repository?> GetUpstreamRepositoryAsync(string repoName);
    Task<bool> DeleteRepositoryAsync(string name);
    Task<bool> ArchiveRepositoryAsync(string name);
    Task<bool> UnarchiveRepositoryAsync(string name);
    Task<bool> WatchRepositoryAsync(string repoName, string username);
    Task<bool> UnwatchRepositoryAsync(string repoName, string username);
    Task<bool> IsWatchingAsync(string repoName, string username);
    Task<List<ContributorInfo>> GetContributorsAsync(string repoPath, int maxCount = 20);
    Task<bool> TransferRepositoryAsync(string repoName, string newOwner, string projectRoot);
    Task<bool> SetDefaultBranchAsync(string repoName, string branchName, string projectRoot);
    Task<(bool Success, string Message)> RenameRepositoryAsync(string oldName, string newName, string projectRoot);
}

public class ContributorInfo
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Commits { get; set; }
}

public class RepositoryService : IRepositoryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<RepositoryService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IActivityService _activityService;

    public RepositoryService(IDbContextFactory<AppDbContext> dbFactory, ILogger<RepositoryService> logger, INotificationService notificationService, IActivityService activityService)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _notificationService = notificationService;
        _activityService = activityService;
    }

    public async Task<List<Repository>> GetRepositoriesAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Repositories.ToListAsync();
    }

    public async Task<Repository?> GetRepositoryAsync(string name)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Repositories.FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());
    }

    public async Task<Repository> CreateRepositoryAsync(string name, string owner, string? description = null, bool isPrivate = false)
    {
        using var db = _dbFactory.CreateDbContext();

        var existing = await db.Repositories.FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());
        if (existing != null)
        {
            existing.Owner = owner;
            if (description != null) existing.Description = description;
            existing.IsPrivate = isPrivate;
            existing.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return existing;
        }

        var repo = new Repository
        {
            Name = name,
            Owner = owner,
            Description = description,
            IsPrivate = isPrivate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Repositories.Add(repo);
        await db.SaveChangesAsync();
        _logger.LogInformation("Repository record created: {Name} by {Owner}", name, owner);

        await _activityService.RecordActivityAsync(owner, "created_repo", name, $"{owner} created repository {name}", $"/repo/{name}");

        return repo;
    }

    public async Task<Repository> EnsureRepositoryRecordAsync(string name, string owner)
    {
        using var db = _dbFactory.CreateDbContext();

        var existing = await db.Repositories.FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());
        if (existing != null)
            return existing;

        var repo = new Repository
        {
            Name = name,
            Owner = owner,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Repositories.Add(repo);
        await db.SaveChangesAsync();
        return repo;
    }

    public async Task<bool> UpdateRepositoryAsync(string name, Action<Repository> updateAction)
    {
        using var db = _dbFactory.CreateDbContext();

        var repo = await db.Repositories.FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());
        if (repo == null) return false;

        updateAction(repo);
        repo.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> StarRepositoryAsync(string repoName, string username)
    {
        using var db = _dbFactory.CreateDbContext();

        if (await db.RepositoryStars.AnyAsync(s => s.RepoName == repoName && s.Username == username))
            return false;

        db.RepositoryStars.Add(new RepositoryStar
        {
            RepoName = repoName,
            Username = username,
            StarredAt = DateTime.UtcNow
        });

        var repo = await db.Repositories.FirstOrDefaultAsync(r => r.Name == repoName);
        if (repo != null)
            repo.Stars++;

        await db.SaveChangesAsync();

        _logger.LogInformation("{Username} starred {RepoName}", username, repoName);

        await _activityService.RecordActivityAsync(username, "starred_repo", repoName, $"{username} starred {repoName}", $"/repo/{repoName}");

        await _notificationService.CreateNotificationAsync(
            "current-user",
            NotificationType.RepositoryStarred,
            "Repository starred",
            $"{username} starred {repoName}",
            repoName,
            $"/repo/{repoName}"
        );

        return true;
    }

    public async Task<bool> UnstarRepositoryAsync(string repoName, string username)
    {
        using var db = _dbFactory.CreateDbContext();

        var star = await db.RepositoryStars
            .FirstOrDefaultAsync(s => s.RepoName == repoName && s.Username == username);

        if (star == null)
            return false;

        db.RepositoryStars.Remove(star);

        var repo = await db.Repositories.FirstOrDefaultAsync(r => r.Name == repoName);
        if (repo != null && repo.Stars > 0)
            repo.Stars--;

        await db.SaveChangesAsync();

        _logger.LogInformation("{Username} unstarred {RepoName}", username, repoName);
        return true;
    }

    public async Task<bool> IsStarredAsync(string repoName, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.RepositoryStars.AnyAsync(s => s.RepoName == repoName && s.Username == username);
    }

    public async Task<RepositoryFork?> ForkRepositoryAsync(string sourceRepoName, string newOwner, string projectRoot)
    {
        using var db = _dbFactory.CreateDbContext();

        // Check if already forked by this user
        if (await db.RepositoryForks.AnyAsync(f => f.OriginalRepo == sourceRepoName && f.Owner == newOwner))
        {
            _logger.LogWarning("Fork already exists for {Owner} from {Source}", newOwner, sourceRepoName);
            return null;
        }

        // Determine paths
        var sourcePath = Path.Combine(projectRoot, sourceRepoName);
        if (!LibGit2Sharp.Repository.IsValid(sourcePath))
        {
            sourcePath = Path.Combine(projectRoot, sourceRepoName + ".git");
            if (!LibGit2Sharp.Repository.IsValid(sourcePath))
            {
                _logger.LogWarning("Source repo not found for fork: {Source}", sourceRepoName);
                return null;
            }
        }

        var forkedRepoName = $"{newOwner}-{sourceRepoName}";
        if (!forkedRepoName.EndsWith(".git"))
            forkedRepoName += ".git";
        var forkedPath = Path.Combine(projectRoot, forkedRepoName);

        // If a stale directory exists from a previously deleted fork, clean it up
        if (Directory.Exists(forkedPath))
        {
            var hasDbRecord = await db.RepositoryForks.AnyAsync(f => f.ForkedRepo == forkedRepoName);
            if (!hasDbRecord)
            {
                _logger.LogInformation("Cleaning up stale fork directory: {Path}", forkedPath);
                try { Directory.Delete(forkedPath, true); }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to clean up stale fork directory: {Path}", forkedPath);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        // Clone the bare repo
        LibGit2Sharp.Repository.Clone(sourcePath, forkedPath, new CloneOptions { IsBare = true });

        // Create DB records
        var fork = new RepositoryFork
        {
            OriginalRepo = sourceRepoName,
            ForkedRepo = forkedRepoName,
            Owner = newOwner,
            ForkedAt = DateTime.UtcNow
        };
        db.RepositoryForks.Add(fork);

        // Create the forked repo DB record
        var sourceDisplayName = sourceRepoName.EndsWith(".git") ? sourceRepoName[..^4] : sourceRepoName;
        var sourceMeta = await db.Repositories.FirstOrDefaultAsync(r => r.Name.ToLower() == sourceRepoName.ToLower());
        db.Repositories.Add(new Repository
        {
            Name = forkedRepoName,
            Owner = newOwner,
            Description = sourceMeta?.Description,
            ForkedFrom = sourceDisplayName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Increment fork count on source
        if (sourceMeta != null)
            sourceMeta.Forks++;

        await db.SaveChangesAsync();
        _logger.LogInformation("{Owner} forked {Source} as {Forked}", newOwner, sourceRepoName, forkedRepoName);

        await _activityService.RecordActivityAsync(newOwner, "forked_repo", sourceRepoName, $"{newOwner} forked {sourceRepoName}", $"/repo/{forkedRepoName}");

        return fork;
    }

    public async Task<List<RepositoryFork>> GetForksAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.RepositoryForks.Where(f => f.OriginalRepo == repoName).ToListAsync();
    }

    public async Task<bool> IsForkedAsync(string repoName, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.RepositoryForks.AnyAsync(f => f.OriginalRepo == repoName && f.Owner == username);
    }

    public async Task<bool> DeleteRepositoryAsync(string name)
    {
        using var db = _dbFactory.CreateDbContext();
        var repo = await db.Repositories.FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());
        if (repo == null) return false;

        // If this repo is a fork, decrement the source repo's fork count and remove the fork record
        var forkRecord = await db.RepositoryForks.FirstOrDefaultAsync(f => f.ForkedRepo == name || f.ForkedRepo == repo.Name);
        if (forkRecord != null)
        {
            var sourceMeta = await db.Repositories.FirstOrDefaultAsync(r => r.Name == forkRecord.OriginalRepo);
            if (sourceMeta != null && sourceMeta.Forks > 0)
                sourceMeta.Forks--;
            db.RepositoryForks.Remove(forkRecord);
        }

        // Also remove any fork records where this repo is the source
        var childForks = await db.RepositoryForks.Where(f => f.OriginalRepo == name || f.OriginalRepo == repo.Name).ToListAsync();
        db.RepositoryForks.RemoveRange(childForks);

        db.Repositories.Remove(repo);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ArchiveRepositoryAsync(string name)
    {
        using var db = _dbFactory.CreateDbContext();
        var repo = await db.Repositories.FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());
        if (repo == null) return false;
        repo.IsArchived = true;
        repo.ArchivedAt = DateTime.UtcNow;
        repo.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        _logger.LogInformation("Repository archived: {Name}", name);
        return true;
    }

    public async Task<bool> UnarchiveRepositoryAsync(string name)
    {
        using var db = _dbFactory.CreateDbContext();
        var repo = await db.Repositories.FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());
        if (repo == null) return false;
        repo.IsArchived = false;
        repo.ArchivedAt = null;
        repo.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        _logger.LogInformation("Repository unarchived: {Name}", name);
        return true;
    }

    public async Task<Repository?> GetUpstreamRepositoryAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        var repo = await db.Repositories.FirstOrDefaultAsync(r => r.Name.ToLower() == repoName.ToLower());
        if (repo?.ForkedFrom == null) return null;

        return await db.Repositories.FirstOrDefaultAsync(r =>
            r.Name.ToLower() == repo.ForkedFrom.ToLower() ||
            r.Name.ToLower() == (repo.ForkedFrom + ".git").ToLower());
    }

    public async Task<(bool Success, string Message)> SyncForkWithUpstreamAsync(string forkedRepoName, string projectRoot)
    {
        using var db = _dbFactory.CreateDbContext();

        var forkedMeta = await db.Repositories.FirstOrDefaultAsync(r => r.Name.ToLower() == forkedRepoName.ToLower());
        if (forkedMeta == null)
            return (false, "Forked repository not found");

        if (string.IsNullOrEmpty(forkedMeta.ForkedFrom))
            return (false, "Repository is not a fork");

        // Find the forked repo path on disk
        var forkedPath = Path.Combine(projectRoot, forkedRepoName);
        if (!LibGit2Sharp.Repository.IsValid(forkedPath))
        {
            forkedPath = Path.Combine(projectRoot, forkedRepoName + ".git");
            if (!LibGit2Sharp.Repository.IsValid(forkedPath))
                return (false, "Forked repository not found on disk");
        }

        // Find the upstream repo path
        var upstreamName = forkedMeta.ForkedFrom;
        var upstreamPath = Path.Combine(projectRoot, upstreamName);
        if (!LibGit2Sharp.Repository.IsValid(upstreamPath))
        {
            upstreamPath = Path.Combine(projectRoot, upstreamName + ".git");
            if (!LibGit2Sharp.Repository.IsValid(upstreamPath))
                return (false, "Upstream repository not found on disk");
        }

        try
        {
            using var forkedRepo = new LibGit2Sharp.Repository(forkedPath);

            // Add or update the "upstream" remote
            var upstream = forkedRepo.Network.Remotes["upstream"];
            if (upstream == null)
            {
                forkedRepo.Network.Remotes.Add("upstream", upstreamPath);
            }
            else if (upstream.Url != upstreamPath)
            {
                forkedRepo.Network.Remotes.Update("upstream", r => r.Url = upstreamPath);
            }

            // Fetch from upstream
            var remote = forkedRepo.Network.Remotes["upstream"];
            var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
            Commands.Fetch(forkedRepo, "upstream", refSpecs, new FetchOptions(), "Sync with upstream");

            // For bare repos, update refs directly
            // Copy upstream's HEAD ref branches to the fork
            int updatedBranches = 0;
            foreach (var reference in forkedRepo.Refs.Where(r => r.CanonicalName.StartsWith("refs/remotes/upstream/")))
            {
                var branchName = reference.CanonicalName.Replace("refs/remotes/upstream/", "");
                var localRef = $"refs/heads/{branchName}";

                if (reference is DirectReference directRef)
                {
                    // Only update if the local branch exists (don't create new branches)
                    var existingRef = forkedRepo.Refs[localRef];
                    if (existingRef != null)
                    {
                        forkedRepo.Refs.UpdateTarget(existingRef, directRef.Target.Id, "Sync with upstream");
                        updatedBranches++;
                    }
                }
            }

            forkedMeta.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            _logger.LogInformation("Fork {ForkedRepo} synced with upstream {Upstream}, {Count} branches updated",
                forkedRepoName, upstreamName, updatedBranches);

            return (true, $"Successfully synced with upstream. {updatedBranches} branch(es) updated.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync fork {ForkedRepo} with upstream", forkedRepoName);
            return (false, $"Sync failed: {ex.Message}");
        }
    }

    public async Task<bool> WatchRepositoryAsync(string repoName, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        if (await db.Set<RepositoryWatch>().AnyAsync(w => w.RepoName == repoName && w.Username == username))
            return false;

        db.Set<RepositoryWatch>().Add(new RepositoryWatch { RepoName = repoName, Username = username });
        var repo = await db.Repositories.FirstOrDefaultAsync(r => r.Name == repoName);
        if (repo != null) repo.Watchers++;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnwatchRepositoryAsync(string repoName, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        var watch = await db.Set<RepositoryWatch>().FirstOrDefaultAsync(w => w.RepoName == repoName && w.Username == username);
        if (watch == null) return false;

        db.Set<RepositoryWatch>().Remove(watch);
        var repo = await db.Repositories.FirstOrDefaultAsync(r => r.Name == repoName);
        if (repo != null && repo.Watchers > 0) repo.Watchers--;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsWatchingAsync(string repoName, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Set<RepositoryWatch>().AnyAsync(w => w.RepoName == repoName && w.Username == username);
    }

    public Task<List<ContributorInfo>> GetContributorsAsync(string repoPath, int maxCount = 20)
    {
        var contributors = new List<ContributorInfo>();
        try
        {
            if (!LibGit2Sharp.Repository.IsValid(repoPath)) return Task.FromResult(contributors);
            using var repo = new LibGit2Sharp.Repository(repoPath);
            var head = repo.Head;
            if (head?.Tip == null) return Task.FromResult(contributors);

            // Load .mailmap from repo root if it exists
            var mailmap = new Dictionary<string, (string Name, string Email)>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var mailmapBlob = head.Tip.Tree[".mailmap"]?.Target as LibGit2Sharp.Blob;
                if (mailmapBlob != null)
                {
                    using var reader = new StreamReader(mailmapBlob.GetContentStream());
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (string.IsNullOrEmpty(line) || line.StartsWith('#')) continue;
                        // Format: "Proper Name <proper@email> old name <old@email>"
                        var match = System.Text.RegularExpressions.Regex.Match(line,
                            @"^(.+?)\s+<([^>]+)>\s+.+?\s+<([^>]+)>$");
                        if (match.Success)
                        {
                            mailmap[match.Groups[3].Value.ToLowerInvariant()] =
                                (match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());
                        }
                    }
                }
            }
            catch { }

            var authorCounts = new Dictionary<string, (string Name, string Email, int Count)>(StringComparer.OrdinalIgnoreCase);
            foreach (var commit in repo.Commits.Take(5000))
            {
                var email = commit.Author.Email?.ToLowerInvariant() ?? "";
                var name = commit.Author.Name;

                // Apply mailmap
                if (mailmap.TryGetValue(email, out var mapped))
                {
                    name = mapped.Name;
                    email = mapped.Email.ToLowerInvariant();
                }

                var key = email != "" ? email : name;
                if (authorCounts.TryGetValue(key, out var existing))
                    authorCounts[key] = (existing.Name, existing.Email, existing.Count + 1);
                else
                    authorCounts[key] = (name, email, 1);
            }

            contributors = authorCounts.Values
                .OrderByDescending(a => a.Count)
                .Take(maxCount)
                .Select(a => new ContributorInfo { Name = a.Name, Email = a.Email, Commits = a.Count })
                .ToList();
        }
        catch { }
        return Task.FromResult(contributors);
    }

    public async Task<bool> TransferRepositoryAsync(string repoName, string newOwner, string projectRoot)
    {
        using var db = _dbFactory.CreateDbContext();
        var repo = await db.Repositories.FirstOrDefaultAsync(r => r.Name == repoName);
        if (repo == null) return false;

        // Verify new owner exists
        var targetUser = await db.Users.FirstOrDefaultAsync(u => u.Username == newOwner);
        if (targetUser == null)
        {
            // Check if it's an organization
            var org = await db.Organizations.FirstOrDefaultAsync(o => o.Name == newOwner);
            if (org == null) return false;
        }

        repo.Owner = newOwner;
        await db.SaveChangesAsync();
        _logger.LogInformation("Repository {Repo} transferred to {NewOwner}", repoName, newOwner);
        return true;
    }

    public async Task<bool> SetDefaultBranchAsync(string repoName, string branchName, string projectRoot)
    {
        var repoPath = Path.Combine(projectRoot, repoName);
        if (!Directory.Exists(repoPath))
            repoPath = Path.Combine(projectRoot, repoName + ".git");
        if (!Directory.Exists(repoPath) || !LibGit2Sharp.Repository.IsValid(repoPath))
            return false;

        try
        {
            using var repo = new LibGit2Sharp.Repository(repoPath);
            var branch = repo.Branches[branchName];
            if (branch == null) return false;

            repo.Refs.UpdateTarget("HEAD", $"refs/heads/{branchName}");

            // Also update the DB record
            using var db = _dbFactory.CreateDbContext();
            var dbRepo = await db.Repositories.FirstOrDefaultAsync(r => r.Name == repoName);
            if (dbRepo != null)
            {
                dbRepo.DefaultBranch = branchName;
                await db.SaveChangesAsync();
            }

            _logger.LogInformation("Default branch for {Repo} set to {Branch}", repoName, branchName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set default branch for {Repo}", repoName);
            return false;
        }
    }

    public async Task<(bool Success, string Message)> RenameRepositoryAsync(string oldName, string newName, string projectRoot)
    {
        // Validate new name
        if (string.IsNullOrWhiteSpace(newName))
            return (false, "New name cannot be empty.");

        newName = newName.Trim();

        // Allow alphanumeric, hyphens, underscores, dots, and .git suffix
        if (!System.Text.RegularExpressions.Regex.IsMatch(newName, @"^[a-zA-Z0-9._-]+$"))
            return (false, "Repository name can only contain alphanumeric characters, hyphens, underscores, and dots.");

        if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
            return (false, "New name is the same as the current name.");

        using var db = _dbFactory.CreateDbContext();

        // Check DB for existing repo with new name
        if (await db.Repositories.AnyAsync(r => r.Name.ToLower() == newName.ToLower()))
            return (false, $"A repository named '{newName}' already exists.");

        // Check filesystem
        var newPath = Path.Combine(projectRoot, newName);
        if (Directory.Exists(newPath))
            return (false, $"A directory named '{newName}' already exists on disk.");

        // Find the old repo record
        var repo = await db.Repositories.FirstOrDefaultAsync(r => r.Name.ToLower() == oldName.ToLower());
        if (repo == null)
            return (false, "Repository not found.");

        // Rename directory on disk
        var oldPath = Path.Combine(projectRoot, oldName);
        if (!Directory.Exists(oldPath))
        {
            // Try with .git suffix
            oldPath = Path.Combine(projectRoot, oldName + ".git");
            if (!Directory.Exists(oldPath))
                return (false, "Repository directory not found on disk.");
        }

        try
        {
            Directory.Move(oldPath, newPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rename repository directory from {Old} to {New}", oldPath, newPath);
            return (false, $"Failed to rename directory: {ex.Message}");
        }

        // Update the main Repository record
        var actualOldName = repo.Name;
        repo.Name = newName;
        repo.UpdatedAt = DateTime.UtcNow;

        // Update all related records that reference the old RepoName
        // Stars
        var stars = await db.RepositoryStars.Where(s => s.RepoName == actualOldName).ToListAsync();
        foreach (var s in stars) s.RepoName = newName;

        // Watches
        var watches = await db.RepositoryWatches.Where(w => w.RepoName == actualOldName).ToListAsync();
        foreach (var w in watches) w.RepoName = newName;

        // Forks (update both OriginalRepo and ForkedRepo references)
        var forksAsOriginal = await db.RepositoryForks.Where(f => f.OriginalRepo == actualOldName).ToListAsync();
        foreach (var f in forksAsOriginal) f.OriginalRepo = newName;
        var forksAsForked = await db.RepositoryForks.Where(f => f.ForkedRepo == actualOldName).ToListAsync();
        foreach (var f in forksAsForked) f.ForkedRepo = newName;

        // Issues
        var issues = await db.Issues.Where(i => i.RepoName == actualOldName).ToListAsync();
        foreach (var i in issues) i.RepoName = newName;

        // Pull Requests
        var prs = await db.PullRequests.Where(p => p.RepoName == actualOldName).ToListAsync();
        foreach (var p in prs) p.RepoName = newName;

        // Labels
        var labels = await db.RepositoryLabels.Where(l => l.RepoName == actualOldName).ToListAsync();
        foreach (var l in labels) l.RepoName = newName;

        // Secrets
        var secrets = await db.RepositorySecrets.Where(s => s.RepoName == actualOldName).ToListAsync();
        foreach (var s in secrets) s.RepoName = newName;

        // Collaborators
        var collabs = await db.RepositoryCollaborators.Where(c => c.RepoName == actualOldName).ToListAsync();
        foreach (var c in collabs) c.RepoName = newName;

        // Wiki pages
        var wikiPages = await db.WikiPages.Where(w => w.RepoName == actualOldName).ToListAsync();
        foreach (var w in wikiPages) w.RepoName = newName;

        // Workflow runs
        var workflowRuns = await db.WorkflowRuns.Where(w => w.RepoName == actualOldName).ToListAsync();
        foreach (var w in workflowRuns) w.RepoName = newName;

        // Workflow schedules
        var schedules = await db.WorkflowSchedules.Where(s => s.RepoName == actualOldName).ToListAsync();
        foreach (var s in schedules) s.RepoName = newName;

        // Webhooks
        var hooks = await db.Webhooks.Where(h => h.RepoName == actualOldName).ToListAsync();
        foreach (var h in hooks) h.RepoName = newName;

        // Branch protection rules
        var branchRules = await db.BranchProtectionRules.Where(b => b.RepoName == actualOldName).ToListAsync();
        foreach (var b in branchRules) b.RepoName = newName;

        // Tag protection rules
        var tagRules = await db.TagProtectionRules.Where(t => t.RepoName == actualOldName).ToListAsync();
        foreach (var t in tagRules) t.RepoName = newName;

        // Releases
        var releases = await db.Releases.Where(r => r.RepoName == actualOldName).ToListAsync();
        foreach (var r in releases) r.RepoName = newName;

        // Commit statuses
        var statuses = await db.CommitStatuses.Where(s => s.RepoName == actualOldName).ToListAsync();
        foreach (var s in statuses) s.RepoName = newName;

        // Projects
        var projects = await db.Projects.Where(p => p.RepoName == actualOldName).ToListAsync();
        foreach (var p in projects) p.RepoName = newName;

        // LFS objects
        var lfs = await db.LfsObjects.Where(l => l.RepoName == actualOldName).ToListAsync();
        foreach (var l in lfs) l.RepoName = newName;

        // Milestones
        var milestones = await db.Milestones.Where(m => m.RepoName == actualOldName).ToListAsync();
        foreach (var m in milestones) m.RepoName = newName;

        // Discussions
        var discussions = await db.Discussions.Where(d => d.RepoName == actualOldName).ToListAsync();
        foreach (var d in discussions) d.RepoName = newName;

        // Commit comments
        var commitComments = await db.CommitComments.Where(c => c.RepoName == actualOldName).ToListAsync();
        foreach (var c in commitComments) c.RepoName = newName;

        // Time entries
        var timeEntries = await db.TimeEntries.Where(t => t.RepoName == actualOldName).ToListAsync();
        foreach (var t in timeEntries) t.RepoName = newName;

        // Issue templates
        var issueTemplates = await db.IssueTemplates.Where(t => t.RepoName == actualOldName).ToListAsync();
        foreach (var t in issueTemplates) t.RepoName = newName;

        // Issue dependencies
        var issueDeps = await db.IssueDependencies.Where(d => d.RepoName == actualOldName).ToListAsync();
        foreach (var d in issueDeps) d.RepoName = newName;

        // Autolink patterns
        var autolinks = await db.AutolinkPatterns.Where(a => a.RepoName == actualOldName).ToListAsync();
        foreach (var a in autolinks) a.RepoName = newName;

        // Mirrors
        var mirrors = await db.RepositoryMirrors.Where(m => m.RepoName == actualOldName).ToListAsync();
        foreach (var m in mirrors) m.RepoName = newName;

        // Pinned repositories
        var pinned = await db.PinnedRepositories.Where(p => p.RepoName == actualOldName).ToListAsync();
        foreach (var p in pinned) p.RepoName = newName;

        // Security scans
        var scans = await db.SecurityScans.Where(s => s.RepoName == actualOldName).ToListAsync();
        foreach (var s in scans) s.RepoName = newName;

        // Security advisories
        var advisories = await db.SecurityAdvisories.Where(a => a.RepoName == actualOldName).ToListAsync();
        foreach (var a in advisories) a.RepoName = newName;

        await db.SaveChangesAsync();

        _logger.LogInformation("Repository renamed from {OldName} to {NewName}", actualOldName, newName);

        await _activityService.RecordActivityAsync(
            repo.Owner, "renamed_repo", newName,
            $"Repository renamed from {actualOldName} to {newName}",
            $"/repo/{newName}");

        return (true, $"Repository renamed from '{actualOldName}' to '{newName}'.");
    }
}
