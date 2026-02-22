using System.Net.Http.Headers;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAdminService _adminService;

    public NotificationService(IDbContextFactory<AppDbContext> dbFactory, ILogger<NotificationService> logger,
        IHttpClientFactory httpClientFactory, IAdminService adminService)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _adminService = adminService;
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

        // Dispatch to external push services (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try { await DispatchPushNotificationAsync(username, title, message); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to dispatch push notification"); }
        });
    }

    private async Task DispatchPushNotificationAsync(string username, string title, string message)
    {
        var settings = await _adminService.GetSystemSettingsAsync();
        if (!settings.EnablePushNotifications) return;

        // Check user opt-in
        using var db = _dbFactory.CreateDbContext();
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Username == username);
        if (profile is { PushNotificationsEnabled: false }) return;

        var client = _httpClientFactory.CreateClient();

        // Ntfy
        if (!string.IsNullOrEmpty(settings.NtfyUrl) && !string.IsNullOrEmpty(settings.NtfyTopic))
        {
            try
            {
                var url = $"{settings.NtfyUrl.TrimEnd('/')}/{settings.NtfyTopic}";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Title", title);
                request.Content = new StringContent(message);
                if (!string.IsNullOrEmpty(settings.NtfyAccessToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.NtfyAccessToken);
                await client.SendAsync(request);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Ntfy push failed"); }
        }

        // Gotify
        if (!string.IsNullOrEmpty(settings.GotifyUrl) && !string.IsNullOrEmpty(settings.GotifyAppToken))
        {
            try
            {
                var url = $"{settings.GotifyUrl.TrimEnd('/')}/message?token={settings.GotifyAppToken}";
                await client.PostAsJsonAsync(url, new { title, message, priority = 5 });
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Gotify push failed"); }
        }
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
