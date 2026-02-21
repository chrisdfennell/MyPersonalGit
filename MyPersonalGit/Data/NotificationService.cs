using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface INotificationService
{
    Task<List<Notification>> GetNotificationsAsync(string username, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(string username);
    Task CreateNotificationAsync(string username, string type, string title, string message, string repoName, string? url = null);
    Task MarkAsReadAsync(string username, int notificationId);
    Task MarkAllAsReadAsync(string username);
    Task DeleteNotificationAsync(string username, int notificationId);
}

public class NotificationService : INotificationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IDbContextFactory<AppDbContext> dbFactory, ILogger<NotificationService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<Notification>> GetNotificationsAsync(string username, bool unreadOnly = false)
    {
        using var db = _dbFactory.CreateDbContext();

        var query = db.Notifications.Where(n => n.Username == username);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Notifications.CountAsync(n => n.Username == username && !n.IsRead);
    }

    public async Task CreateNotificationAsync(string username, string type, string title, string message, string repoName, string? url = null)
    {
        using var db = _dbFactory.CreateDbContext();

        db.Notifications.Add(new Notification
        {
            Username = username,
            Type = type,
            Title = title,
            Message = message,
            RepoName = repoName,
            Url = url,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        _logger.LogDebug("Notification created for {Username}: {Title}", username, title);
    }

    public async Task MarkAsReadAsync(string username, int notificationId)
    {
        using var db = _dbFactory.CreateDbContext();

        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.Username == username);

        if (notification != null)
        {
            notification.IsRead = true;
            await db.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();

        var unread = await db.Notifications
            .Where(n => n.Username == username && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unread)
            notification.IsRead = true;

        await db.SaveChangesAsync();
        _logger.LogDebug("All notifications marked as read for {Username}", username);
    }

    public async Task DeleteNotificationAsync(string username, int notificationId)
    {
        using var db = _dbFactory.CreateDbContext();

        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.Username == username);

        if (notification != null)
        {
            db.Notifications.Remove(notification);
            await db.SaveChangesAsync();
        }
    }
}
