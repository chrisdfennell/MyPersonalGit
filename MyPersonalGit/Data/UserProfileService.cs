using System.Text.Json;
using MyPersonalGit.Models;
using System.Security.Cryptography;
using System.Text;

namespace MyPersonalGit.Data;

public class UserProfileService
{
    private readonly string _dataPath;

    public UserProfileService(IConfiguration configuration)
    {
        var projectRoot = configuration["ProjectRoot"] ?? throw new InvalidOperationException("ProjectRoot not configured");
        _dataPath = Path.Combine(projectRoot, ".mypersonalgit");
        Directory.CreateDirectory(_dataPath);
    }

    private string GetProfileFilePath(string username) => Path.Combine(_dataPath, $"{username}_profile.json");
    private string GetActivityFilePath(string username) => Path.Combine(_dataPath, $"{username}_activity.json");
    private string GetSshKeysFilePath(string username) => Path.Combine(_dataPath, $"{username}_ssh_keys.json");
    private string GetTokensFilePath(string username) => Path.Combine(_dataPath, $"{username}_tokens.json");
    private string GetSessionsFilePath(string username) => Path.Combine(_dataPath, $"{username}_sessions.json");
    private string Get2FAFilePath(string username) => Path.Combine(_dataPath, $"{username}_2fa.json");

    public async Task<UserProfile> GetProfileAsync(string username)
    {
        var filePath = GetProfileFilePath(username);
        if (!File.Exists(filePath))
        {
            var profile = new UserProfile
            {
                Username = username,
                Email = $"{username}@localhost",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await SaveProfileAsync(profile);
            return profile;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<UserProfile>(json) ?? new UserProfile { Username = username };
    }

    public async Task SaveProfileAsync(UserProfile profile)
    {
        profile.UpdatedAt = DateTime.UtcNow;
        var filePath = GetProfileFilePath(profile.Username);
        var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<List<UserActivity>> GetActivityAsync(string username, int limit = 50)
    {
        var filePath = GetActivityFilePath(username);
        if (!File.Exists(filePath))
            return new List<UserActivity>();

        var json = await File.ReadAllTextAsync(filePath);
        var activities = JsonSerializer.Deserialize<List<UserActivity>>(json) ?? new List<UserActivity>();
        return activities.OrderByDescending(a => a.Timestamp).Take(limit).ToList();
    }

    public async Task AddActivityAsync(string username, string activityType, string repository, string description, string url = "")
    {
        var filePath = GetActivityFilePath(username);
        var activities = new List<UserActivity>();
        
        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath);
            activities = JsonSerializer.Deserialize<List<UserActivity>>(json) ?? new List<UserActivity>();
        }

        var activity = new UserActivity
        {
            Id = activities.Count > 0 ? activities.Max(a => a.Id) + 1 : 1,
            Username = username,
            Timestamp = DateTime.UtcNow,
            ActivityType = activityType,
            Repository = repository,
            Description = description,
            Url = url
        };

        activities.Add(activity);

        var activitiesJson = JsonSerializer.Serialize(activities, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, activitiesJson);
    }

    public async Task<List<ContributionDay>> GetContributionsAsync(string username, int days = 365)
    {
        var activities = await GetActivityAsync(username, int.MaxValue);
        var startDate = DateTime.UtcNow.AddDays(-days).Date;
        
        var contributions = new List<ContributionDay>();
        for (int i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            var count = activities.Count(a => a.Timestamp.Date == date);
            var level = count == 0 ? 0 : count < 3 ? 1 : count < 6 ? 2 : count < 10 ? 3 : 4;
            
            contributions.Add(new ContributionDay
            {
                Date = date,
                Count = count,
                Level = level
            });
        }

        return contributions;
    }

    public async Task<UserStatistics> GetStatisticsAsync(string username)
    {
        var activities = await GetActivityAsync(username, int.MaxValue);
        
        return new UserStatistics
        {
            TotalCommits = activities.Count(a => a.ActivityType == "commit"),
            TotalPullRequests = activities.Count(a => a.ActivityType == "pull_request"),
            TotalIssues = activities.Count(a => a.ActivityType == "issue"),
            ContributionsThisYear = activities.Count(a => a.Timestamp.Year == DateTime.UtcNow.Year)
        };
    }

    public async Task<List<SshKey>> GetSshKeysAsync(string username)
    {
        var filePath = GetSshKeysFilePath(username);
        if (!File.Exists(filePath))
            return new List<SshKey>();

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<List<SshKey>>(json) ?? new List<SshKey>();
    }

    public async Task<bool> AddSshKeyAsync(string username, string title, string key)
    {
        var keys = await GetSshKeysAsync(username);
        
        var fingerprint = GenerateFingerprint(key);
        if (keys.Any(k => k.Fingerprint == fingerprint))
            return false;

        var sshKey = new SshKey
        {
            Id = keys.Count > 0 ? keys.Max(k => k.Id) + 1 : 1,
            Username = username,
            Title = title,
            Key = key,
            Fingerprint = fingerprint,
            CreatedAt = DateTime.UtcNow
        };

        keys.Add(sshKey);

        var filePath = GetSshKeysFilePath(username);
        var json = JsonSerializer.Serialize(keys, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);

        return true;
    }

    public async Task<bool> DeleteSshKeyAsync(string username, int keyId)
    {
        var keys = await GetSshKeysAsync(username);
        var key = keys.FirstOrDefault(k => k.Id == keyId);
        if (key == null)
            return false;

        keys.Remove(key);

        var filePath = GetSshKeysFilePath(username);
        var json = JsonSerializer.Serialize(keys, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);

        return true;
    }

    private string GenerateFingerprint(string key)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash)[..16].ToLower();
    }

    public async Task<List<PersonalAccessToken>> GetTokensAsync(string username)
    {
        var filePath = GetTokensFilePath(username);
        if (!File.Exists(filePath))
            return new List<PersonalAccessToken>();

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<List<PersonalAccessToken>>(json) ?? new List<PersonalAccessToken>();
    }

    public async Task<string> CreateTokenAsync(string username, string name, string[] scopes, DateTime? expiresAt = null)
    {
        var tokens = await GetTokensAsync(username);
        
        var token = GenerateToken();
        var pat = new PersonalAccessToken
        {
            Id = tokens.Count > 0 ? tokens.Max(t => t.Id) + 1 : 1,
            Username = username,
            Name = name,
            Token = token,
            Scopes = scopes,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        tokens.Add(pat);

        var filePath = GetTokensFilePath(username);
        var json = JsonSerializer.Serialize(tokens, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);

        return token;
    }

    public async Task<bool> DeleteTokenAsync(string username, int tokenId)
    {
        var tokens = await GetTokensAsync(username);
        var token = tokens.FirstOrDefault(t => t.Id == tokenId);
        if (token == null)
            return false;

        tokens.Remove(token);

        var filePath = GetTokensFilePath(username);
        var json = JsonSerializer.Serialize(tokens, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);

        return true;
    }

    private string GenerateToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return "mypg_" + Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..40];
    }

    public async Task<List<ActiveUserSession>> GetSessionsAsync(string username)
    {
        var filePath = GetSessionsFilePath(username);
        if (!File.Exists(filePath))
            return new List<ActiveUserSession>();

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<List<ActiveUserSession>>(json) ?? new List<ActiveUserSession>();
    }

    public async Task<bool> RevokeSessionAsync(string username, int sessionId)
    {
        var sessions = await GetSessionsAsync(username);
        var session = sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session == null)
            return false;

        session.IsActive = false;

        var filePath = GetSessionsFilePath(username);
        var json = JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);

        return true;
    }

    public async Task<TwoFactorAuth?> Get2FAAsync(string username)
    {
        var filePath = Get2FAFilePath(username);
        if (!File.Exists(filePath))
            return null;

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<TwoFactorAuth>(json);
    }

    public async Task<TwoFactorAuth> Enable2FAAsync(string username)
    {
        var secret = GenerateSecret();
        var backupCodes = GenerateBackupCodes();

        var twoFa = new TwoFactorAuth
        {
            Username = username,
            IsEnabled = true,
            Secret = secret,
            BackupCodes = backupCodes,
            EnabledAt = DateTime.UtcNow
        };

        var filePath = Get2FAFilePath(username);
        var json = JsonSerializer.Serialize(twoFa, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);

        return twoFa;
    }

    public async Task<bool> Disable2FAAsync(string username)
    {
        var filePath = Get2FAFilePath(username);
        if (!File.Exists(filePath))
            return false;

        File.Delete(filePath);
        return true;
    }

    private string GenerateSecret()
    {
        var bytes = new byte[20];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private string[] GenerateBackupCodes()
    {
        var codes = new string[10];
        for (int i = 0; i < 10; i++)
        {
            var bytes = new byte[4];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            codes[i] = BitConverter.ToUInt32(bytes).ToString("D8");
        }
        return codes;
    }
}
