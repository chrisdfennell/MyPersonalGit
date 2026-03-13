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
    private readonly IEmailService _emailService;

    public NotificationService(IDbContextFactory<AppDbContext> dbFactory, ILogger<NotificationService> logger,
        IHttpClientFactory httpClientFactory, IAdminService adminService, IEmailService emailService)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _adminService = adminService;
        _emailService = emailService;
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

        // Dispatch email notification (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try { await DispatchEmailNotificationAsync(username, title, message, url); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to dispatch email notification"); }
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

    private async Task DispatchEmailNotificationAsync(string username, string title, string message, string? url)
    {
        using var db = _dbFactory.CreateDbContext();
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Username == username);
        if (profile is { EmailNotificationsEnabled: false }) return;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null || string.IsNullOrEmpty(user.Email)) return;

        var htmlBody = $@"
<html>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; color: #24292e;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""border-bottom: 1px solid #e1e4e8; padding-bottom: 10px;"">{System.Net.WebUtility.HtmlEncode(title)}</h2>
        <p>{System.Net.WebUtility.HtmlEncode(message)}</p>
        {(string.IsNullOrEmpty(url) ? "" : $@"<p><a href=""{System.Net.WebUtility.HtmlEncode(url)}"" style=""color: #0366d6;"">View on MyPersonalGit</a></p>")}
        <hr style=""border: none; border-top: 1px solid #e1e4e8; margin-top: 20px;"" />
        <p style=""color: #6a737d; font-size: 12px;"">You received this because of your notification settings. Manage preferences in your account settings.</p>
    </div>
</body>
</html>";

        await _emailService.SendEmailAsync(user.Email, title, htmlBody);
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
