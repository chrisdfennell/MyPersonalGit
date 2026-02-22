namespace MyPersonalGit.Models;

public class SystemSettings
{
    public int Id { get; set; }
    public string ProjectRoot { get; set; } = string.Empty;
    public bool RequireAuth { get; set; } = true;
    public bool AllowUserRegistration { get; set; } = false;
    public bool RequireEmailVerification { get; set; } = false;
    public int MaxRepositoriesPerUser { get; set; } = 100;
    public long MaxRepositorySize { get; set; } = 1073741824;
    public bool EnableActions { get; set; } = true;
    public bool EnableWiki { get; set; } = true;
    public bool EnableIssues { get; set; } = true;
    public bool EnableProjects { get; set; } = true;
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool SmtpEnableSsl { get; set; } = true;

    // Push notifications
    public bool EnablePushNotifications { get; set; }
    public string NtfyUrl { get; set; } = string.Empty;
    public string NtfyTopic { get; set; } = string.Empty;
    public string NtfyAccessToken { get; set; } = string.Empty;
    public string GotifyUrl { get; set; } = string.Empty;
    public string GotifyAppToken { get; set; } = string.Empty;
}

public class SystemStatistics
{
    public int TotalUsers { get; set; }
    public int TotalRepositories { get; set; }
    public int TotalIssues { get; set; }
    public int TotalPullRequests { get; set; }
    public long TotalStorageUsed { get; set; }
    public int ActiveUsers24h { get; set; }
    public int ActiveUsers7d { get; set; }
    public int ActiveUsers30d { get; set; }
    public DateTime LastBackup { get; set; }
}

public class AuditLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

public class UserManagement
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime LastLogin { get; set; }
    public int RepositoryCount { get; set; }
}
