using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IActivityService
{
    Task RecordActivityAsync(string username, string type, string repository, string description, string url);
    Task<List<UserActivity>> GetRecentActivitiesAsync(int limit = 30);
    Task<List<UserActivity>> GetUserActivitiesAsync(string username, int limit = 30);
}

public class ActivityService : IActivityService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<ActivityService> _logger;

    public ActivityService(IDbContextFactory<AppDbContext> dbFactory, ILogger<ActivityService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task RecordActivityAsync(string username, string type, string repository, string description, string url)
    {
        try
        {
            using var db = _dbFactory.CreateDbContext();
            db.UserActivities.Add(new UserActivity
            {
                Username = username,
                Timestamp = DateTime.UtcNow,
                ActivityType = type,
                Repository = repository,
                Description = description,
                Url = url
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record activity for {Username}", username);
        }
    }

    public async Task<List<UserActivity>> GetRecentActivitiesAsync(int limit = 30)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.UserActivities
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<UserActivity>> GetUserActivitiesAsync(string username, int limit = 30)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.UserActivities
            .Where(a => a.Username == username)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
}
