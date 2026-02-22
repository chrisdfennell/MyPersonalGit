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
    Task<bool> DeleteRepositoryAsync(string name);
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
}
