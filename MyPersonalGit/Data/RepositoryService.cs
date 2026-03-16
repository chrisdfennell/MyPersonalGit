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
            return null;

        // Determine paths
        var sourcePath = Path.Combine(projectRoot, sourceRepoName);
        if (!LibGit2Sharp.Repository.IsValid(sourcePath))
        {
            sourcePath = Path.Combine(projectRoot, sourceRepoName + ".git");
            if (!LibGit2Sharp.Repository.IsValid(sourcePath))
                return null;
        }

        var forkedRepoName = $"{newOwner}-{sourceRepoName}";
        if (!forkedRepoName.EndsWith(".git"))
            forkedRepoName += ".git";
        var forkedPath = Path.Combine(projectRoot, forkedRepoName);

        if (Directory.Exists(forkedPath))
            return null;

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

            var authorCounts = new Dictionary<string, (string Name, string Email, int Count)>(StringComparer.OrdinalIgnoreCase);
            foreach (var commit in repo.Commits.Take(5000))
            {
                var key = commit.Author.Email?.ToLowerInvariant() ?? commit.Author.Name;
                if (authorCounts.TryGetValue(key, out var existing))
                    authorCounts[key] = (existing.Name, existing.Email, existing.Count + 1);
                else
                    authorCounts[key] = (commit.Author.Name, commit.Author.Email ?? "", 1);
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
}
