using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IUserProfileService
{
    Task<UserProfile> GetProfileAsync(string username);
    Task SaveProfileAsync(UserProfile profile);
    Task<List<UserActivity>> GetActivityAsync(string username, int limit = 50);
    Task AddActivityAsync(string username, string activityType, string repository, string description, string url = "");
    Task<List<ContributionDay>> GetContributionsAsync(string username, int days = 365);
    Task<UserStatistics> GetStatisticsAsync(string username);
    Task<List<SshKey>> GetSshKeysAsync(string username);
    Task<bool> AddSshKeyAsync(string username, string title, string key);
    Task<bool> DeleteSshKeyAsync(string username, int keyId);
    Task<List<PersonalAccessToken>> GetTokensAsync(string username);
    Task<string> CreateTokenAsync(string username, string name, string[] scopes, DateTime? expiresAt = null);
    Task<bool> DeleteTokenAsync(string username, int tokenId);
    Task<List<ActiveUserSession>> GetSessionsAsync(string username);
    Task<bool> RevokeSessionAsync(string username, int sessionId);
    Task<TwoFactorAuth?> Get2FAAsync(string username);
    Task<TwoFactorAuth> Enable2FAAsync(string username);
    Task<bool> Disable2FAAsync(string username);
}

public class UserProfileService : IUserProfileService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(IDbContextFactory<AppDbContext> dbFactory, ILogger<UserProfileService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<UserProfile> GetProfileAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();

        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Username == username);
        if (profile != null)
            return profile;

        profile = new UserProfile
        {
            Username = username,
            Email = $"{username}@localhost",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile;
    }

    public async Task SaveProfileAsync(UserProfile profile)
    {
        using var db = _dbFactory.CreateDbContext();

        var existing = await db.UserProfiles.FirstOrDefaultAsync(p => p.Username == profile.Username);
        if (existing != null)
        {
            profile.UpdatedAt = DateTime.UtcNow;
            db.Entry(existing).CurrentValues.SetValues(profile);
        }
        else
        {
            profile.UpdatedAt = DateTime.UtcNow;
            db.UserProfiles.Add(profile);
        }

        await db.SaveChangesAsync();
        _logger.LogDebug("Profile saved for {Username}", profile.Username);
    }

    public async Task<List<UserActivity>> GetActivityAsync(string username, int limit = 50)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.UserActivities
            .Where(a => a.Username == username)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task AddActivityAsync(string username, string activityType, string repository, string description, string url = "")
    {
        using var db = _dbFactory.CreateDbContext();

        db.UserActivities.Add(new UserActivity
        {
            Username = username,
            Timestamp = DateTime.UtcNow,
            ActivityType = activityType,
            Repository = repository,
            Description = description,
            Url = url
        });

        await db.SaveChangesAsync();
    }

    public async Task<List<ContributionDay>> GetContributionsAsync(string username, int days = 365)
    {
        using var db = _dbFactory.CreateDbContext();

        var startDate = DateTime.UtcNow.AddDays(-days).Date;
        var activities = await db.UserActivities
            .Where(a => a.Username == username && a.Timestamp >= startDate)
            .ToListAsync();

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
        using var db = _dbFactory.CreateDbContext();

        var activities = await db.UserActivities
            .Where(a => a.Username == username)
            .ToListAsync();

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
        using var db = _dbFactory.CreateDbContext();
        return await db.SshKeys.Where(k => k.Username == username).ToListAsync();
    }

    public async Task<bool> AddSshKeyAsync(string username, string title, string key)
    {
        using var db = _dbFactory.CreateDbContext();

        var fingerprint = GenerateFingerprint(key);

        if (await db.SshKeys.AnyAsync(k => k.Username == username && k.Fingerprint == fingerprint))
            return false;

        db.SshKeys.Add(new SshKey
        {
            Username = username,
            Title = title,
            Key = key,
            Fingerprint = fingerprint,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        _logger.LogInformation("SSH key '{Title}' added for {Username}", title, username);
        return true;
    }

    public async Task<bool> DeleteSshKeyAsync(string username, int keyId)
    {
        using var db = _dbFactory.CreateDbContext();

        var key = await db.SshKeys.FirstOrDefaultAsync(k => k.Id == keyId && k.Username == username);
        if (key == null)
            return false;

        db.SshKeys.Remove(key);
        await db.SaveChangesAsync();

        _logger.LogInformation("SSH key {KeyId} deleted for {Username}", keyId, username);
        return true;
    }

    private static string GenerateFingerprint(string key)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash)[..16].ToLower();
    }

    public async Task<List<PersonalAccessToken>> GetTokensAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.PersonalAccessTokens.Where(t => t.Username == username).ToListAsync();
    }

    public async Task<string> CreateTokenAsync(string username, string name, string[] scopes, DateTime? expiresAt = null)
    {
        using var db = _dbFactory.CreateDbContext();

        var token = GenerateToken();

        db.PersonalAccessTokens.Add(new PersonalAccessToken
        {
            Username = username,
            Name = name,
            Token = token,
            Scopes = scopes,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        });

        await db.SaveChangesAsync();
        _logger.LogInformation("Personal access token '{Name}' created for {Username}", name, username);
        return token;
    }

    public async Task<bool> DeleteTokenAsync(string username, int tokenId)
    {
        using var db = _dbFactory.CreateDbContext();

        var token = await db.PersonalAccessTokens.FirstOrDefaultAsync(t => t.Id == tokenId && t.Username == username);
        if (token == null)
            return false;

        db.PersonalAccessTokens.Remove(token);
        await db.SaveChangesAsync();

        _logger.LogInformation("Token {TokenId} deleted for {Username}", tokenId, username);
        return true;
    }

    private static string GenerateToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return "mypg_" + Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..40];
    }

    public async Task<List<ActiveUserSession>> GetSessionsAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.ActiveUserSessions.Where(s => s.Username == username).ToListAsync();
    }

    public async Task<bool> RevokeSessionAsync(string username, int sessionId)
    {
        using var db = _dbFactory.CreateDbContext();

        var session = await db.ActiveUserSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.Username == username);
        if (session == null)
            return false;

        session.IsActive = false;
        await db.SaveChangesAsync();

        _logger.LogInformation("Session {SessionId} revoked for {Username}", sessionId, username);
        return true;
    }

    public async Task<TwoFactorAuth?> Get2FAAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.TwoFactorAuths.FirstOrDefaultAsync(t => t.Username == username);
    }

    public async Task<TwoFactorAuth> Enable2FAAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();

        var existing = await db.TwoFactorAuths.FirstOrDefaultAsync(t => t.Username == username);
        if (existing != null)
            db.TwoFactorAuths.Remove(existing);

        var twoFa = new TwoFactorAuth
        {
            Username = username,
            IsEnabled = true,
            Secret = GenerateSecret(),
            BackupCodes = GenerateBackupCodes(),
            EnabledAt = DateTime.UtcNow
        };

        db.TwoFactorAuths.Add(twoFa);
        await db.SaveChangesAsync();

        _logger.LogInformation("2FA enabled for {Username}", username);
        return twoFa;
    }

    public async Task<bool> Disable2FAAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();

        var twoFa = await db.TwoFactorAuths.FirstOrDefaultAsync(t => t.Username == username);
        if (twoFa == null)
            return false;

        db.TwoFactorAuths.Remove(twoFa);
        await db.SaveChangesAsync();

        _logger.LogInformation("2FA disabled for {Username}", username);
        return true;
    }

    private static string GenerateSecret()
    {
        var bytes = new byte[20];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string[] GenerateBackupCodes()
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
