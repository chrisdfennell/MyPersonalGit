using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IRepositoryService
{
    Task<List<Repository>> GetRepositoriesAsync();
    Task<Repository?> GetRepositoryAsync(string name);
    Task<bool> StarRepositoryAsync(string repoName, string username);
    Task<bool> UnstarRepositoryAsync(string repoName, string username);
    Task<bool> IsStarredAsync(string repoName, string username);
}

public class RepositoryService : IRepositoryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<RepositoryService> _logger;
    private readonly INotificationService _notificationService;

    public RepositoryService(IDbContextFactory<AppDbContext> dbFactory, ILogger<RepositoryService> logger, INotificationService notificationService)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _notificationService = notificationService;
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
}
