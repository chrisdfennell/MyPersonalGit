namespace MyPersonalGit.Models;

public class UserProfile
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string TwitterHandle { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsPublic { get; set; } = true;
}

public class UserActivity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class ContributionDay
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public int Level { get; set; }
}

public class UserStatistics
{
    public int TotalRepositories { get; set; }
    public int TotalCommits { get; set; }
    public int TotalPullRequests { get; set; }
    public int TotalIssues { get; set; }
    public int TotalStars { get; set; }
    public int TotalForks { get; set; }
    public int ContributionsThisYear { get; set; }
    public string LongestStreak { get; set; } = "0 days";
    public string CurrentStreak { get; set; } = "0 days";
}

public class SshKey
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsed { get; set; }
}

public class PersonalAccessToken
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsed { get; set; }
}

public class ActiveUserSession
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivity { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TwoFactorAuth
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string Secret { get; set; } = string.Empty;
    public string[] BackupCodes { get; set; } = Array.Empty<string>();
    public DateTime? EnabledAt { get; set; }
}