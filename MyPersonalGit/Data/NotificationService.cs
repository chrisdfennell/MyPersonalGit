using System.Text.Json;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public class NotificationService
{
    private readonly string _dataPath;

    public NotificationService(IConfiguration configuration)
    {
        var projectRoot = configuration["ProjectRoot"] ?? throw new InvalidOperationException("ProjectRoot not configured");
        _dataPath = Path.Combine(projectRoot, ".mypersonalgit");
        Directory.CreateDirectory(_dataPath);
    }

    private string GetNotificationsFilePath(string username) => Path.Combine(_dataPath, $"{username}_notifications.json");

    public async Task<List<Notification>> GetNotificationsAsync(string username, bool unreadOnly = false)
    {
        var filePath = GetNotificationsFilePath(username);
        if (!File.Exists(filePath))
            return new List<Notification>();

        var json = await File.ReadAllTextAsync(filePath);
        var notifications = JsonSerializer.Deserialize<List<Notification>>(json) ?? new List<Notification>();
        
        if (unreadOnly)
            notifications = notifications.Where(n => !n.IsRead).ToList();
            
        return notifications.OrderByDescending(n => n.CreatedAt).ToList();
    }

    public async Task<int> GetUnreadCountAsync(string username)
    {
        var notifications = await GetNotificationsAsync(username, unreadOnly: true);
        return notifications.Count;
    }

    public async Task CreateNotificationAsync(string username, string type, string title, string message, string repoName, string? url = null)
    {
        var notifications = await GetNotificationsAsync(username);
        
        var notification = new Notification
        {
            Id = notifications.Count > 0 ? notifications.Max(n => n.Id) + 1 : 1,
            Username = username,
            Type = type,
            Title = title,
            Message = message,
            RepoName = repoName,
            Url = url,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        notifications.Add(notification);
        await SaveNotificationsAsync(username, notifications);
    }

    public async Task MarkAsReadAsync(string username, int notificationId)
    {
        var notifications = await GetNotificationsAsync(username);
        var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
        
        if (notification != null)
        {
            notification.IsRead = true;
            await SaveNotificationsAsync(username, notifications);
        }
    }

    public async Task MarkAllAsReadAsync(string username)
    {
        var notifications = await GetNotificationsAsync(username);
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }
        await SaveNotificationsAsync(username, notifications);
    }

    public async Task DeleteNotificationAsync(string username, int notificationId)
    {
        var notifications = await GetNotificationsAsync(username);
        var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
        
        if (notification != null)
        {
            notifications.Remove(notification);
            await SaveNotificationsAsync(username, notifications);
        }
    }

    private async Task SaveNotificationsAsync(string username, List<Notification> notifications)
    {
        var filePath = GetNotificationsFilePath(username);
        var json = JsonSerializer.Serialize(notifications, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }
}
