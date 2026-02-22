using System.Security.Cryptography;
using System.Text;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;
using GitRepository = LibGit2Sharp.Repository;

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
    private readonly IAdminService _adminService;
    private readonly IConfiguration _config;

    public UserProfileService(IDbContextFactory<AppDbContext> dbFactory, ILogger<UserProfileService> logger,
        IAdminService adminService, IConfiguration config)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _adminService = adminService;
        _config = config;
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
        var dbActivities = await db.UserActivities
            .Where(a => a.Username == username)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();

        // Also include git commits as synthetic activities
        var commitActivities = await Task.Run(() => ScanGitCommitActivities(username, limit));

        return dbActivities
            .Concat(commitActivities)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToList();
    }

    private List<UserActivity> ScanGitCommitActivities(string username, int limit)
    {
        var activities = new List<UserActivity>();
        var repoPath = GetReposRoot();
        if (repoPath == null || !Directory.Exists(repoPath)) return activities;

        try
        {
            foreach (var dir in Directory.GetDirectories(repoPath))
            {
                if (!GitRepository.IsValid(dir)) continue;
                try
                {
                    using var repo = new GitRepository(dir);
                    var repoName = Path.GetFileName(dir);
                    var displayName = repoName.EndsWith(".git") ? repoName[..^4] : repoName;
                    var seen = new HashSet<string>();

                    foreach (var branch in repo.Branches.Where(b => !b.IsRemote))
                    {
                        foreach (var commit in branch.Commits)
                        {
                            if (!seen.Add(commit.Sha)) continue;

                            var authorName = commit.Author.Name;
                            var authorEmail = commit.Author.Email;
                            if (authorName.Equals(username, StringComparison.OrdinalIgnoreCase) ||
                                authorEmail.StartsWith(username + "@", StringComparison.OrdinalIgnoreCase) ||
                                authorEmail.Equals(username, StringComparison.OrdinalIgnoreCase))
                            {
                                activities.Add(new UserActivity
                                {
                                    Username = username,
                                    Timestamp = commit.Author.When.UtcDateTime,
                                    ActivityType = "commit",
                                    Repository = displayName,
                                    Description = commit.MessageShort,
                                    Url = $"/repo/{displayName}"
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error scanning commit activities in {Dir}", dir);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scanning repos for activity feed");
        }

        return activities
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToList();
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

        // Count DB-recorded activities per day
        var activities = await db.UserActivities
            .Where(a => a.Username == username && a.Timestamp >= startDate)
            .ToListAsync();

        var countsByDate = new Dictionary<DateTime, int>();
        foreach (var a in activities)
        {
            var d = a.Timestamp.Date;
            countsByDate[d] = countsByDate.GetValueOrDefault(d) + 1;
        }

        // Also scan git commits across all repos for this user
        var commitCounts = await Task.Run(() => ScanGitCommits(username, startDate));
        foreach (var (date, count) in commitCounts)
        {
            countsByDate[date] = countsByDate.GetValueOrDefault(date) + count;
        }

        // Build contribution day list
        var contributions = new List<ContributionDay>();
        for (int i = 0; i <= days; i++)
        {
            var date = startDate.AddDays(i);
            if (date > DateTime.UtcNow.Date) break;
            var count = countsByDate.GetValueOrDefault(date);
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

    private Dictionary<DateTime, int> ScanGitCommits(string username, DateTime since)
    {
        var counts = new Dictionary<DateTime, int>();
        var repoPath = GetReposRoot();
        if (repoPath == null || !Directory.Exists(repoPath)) return counts;

        try
        {
            foreach (var dir in Directory.GetDirectories(repoPath))
            {
                if (!GitRepository.IsValid(dir)) continue;
                try
                {
                    using var repo = new GitRepository(dir);
                    var seen = new HashSet<string>();
                    foreach (var branch in repo.Branches.Where(b => !b.IsRemote))
                    {
                        foreach (var commit in branch.Commits)
                        {
                            if (commit.Author.When.UtcDateTime < since) break;
                            if (!seen.Add(commit.Sha)) continue;

                            var authorName = commit.Author.Name;
                            var authorEmail = commit.Author.Email;
                            if (authorName.Equals(username, StringComparison.OrdinalIgnoreCase) ||
                                authorEmail.StartsWith(username + "@", StringComparison.OrdinalIgnoreCase) ||
                                authorEmail.Equals(username, StringComparison.OrdinalIgnoreCase))
                            {
                                var date = commit.Author.When.UtcDateTime.Date;
                                counts[date] = counts.GetValueOrDefault(date) + 1;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error scanning commits in {Dir}", dir);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scanning repos for contribution graph");
        }

        return counts;
    }

    private string? GetReposRoot()
    {
        try
        {
            var settings = _adminService.GetSystemSettingsAsync().GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(settings.ProjectRoot) && Directory.Exists(settings.ProjectRoot))
                return settings.ProjectRoot;
        }
        catch { }

        var configured = _config["Git:ProjectRoot"];
        if (!string.IsNullOrEmpty(configured) && Directory.Exists(configured))
            return configured;

        return Directory.Exists("/repos") ? "/repos" : null;
    }

    public async Task<UserStatistics> GetStatisticsAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();

        var activities = await db.UserActivities
            .Where(a => a.Username == username)
            .ToListAsync();

        var prTypes = new[] { "opened_pr", "merged_pr", "closed_pr" };
        var issueTypes = new[] { "opened_issue", "closed_issue" };

        // Count actual git commits across all repos
        var oneYearAgo = DateTime.UtcNow.AddYears(-1);
        var commitCounts = await Task.Run(() => ScanGitCommits(username, DateTime.MinValue));
        var totalCommits = commitCounts.Values.Sum();
        var yearCommitCounts = await Task.Run(() => ScanGitCommits(username, oneYearAgo));
        var yearCommits = yearCommitCounts.Values.Sum();

        // Calculate contribution counts per day for the last year (for streak calc)
        var contributions = await GetContributionsAsync(username, 365);
        var (longest, current) = CalculateStreaks(contributions);

        return new UserStatistics
        {
            TotalCommits = totalCommits,
            TotalPullRequests = activities.Count(a => prTypes.Contains(a.ActivityType)),
            TotalIssues = activities.Count(a => issueTypes.Contains(a.ActivityType)),
            ContributionsThisYear = yearCommits + activities.Count(a => a.Timestamp >= oneYearAgo),
            LongestStreak = FormatStreak(longest),
            CurrentStreak = FormatStreak(current)
        };
    }

    private static (int longest, int current) CalculateStreaks(List<ContributionDay> contributions)
    {
        int longest = 0, current = 0, streak = 0;
        foreach (var day in contributions.OrderBy(c => c.Date))
        {
            if (day.Count > 0)
            {
                streak++;
                if (streak > longest) longest = streak;
            }
            else
            {
                streak = 0;
            }
        }

        // Current streak: count backwards from today
        current = 0;
        foreach (var day in contributions.OrderByDescending(c => c.Date))
        {
            if (day.Count > 0) current++;
            else break;
        }

        return (longest, current);
    }

    private static string FormatStreak(int days)
    {
        return days == 1 ? "1 day" : $"{days} days";
    }

    public async Task<List<SshKey>> GetSshKeysAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.SshKeys.Where(k => k.Username == username).ToListAsync();
    }

    public async Task<bool> AddSshKeyAsync(string username, string title, string key)
    {
        // Validate SSH key format
        if (string.IsNullOrWhiteSpace(key)) return false;
        var trimmedKey = key.Trim();
        var validPrefixes = new[] { "ssh-rsa", "ssh-ed25519", "ssh-dss", "ecdsa-sha2-nistp256", "ecdsa-sha2-nistp384", "ecdsa-sha2-nistp521" };
        if (!validPrefixes.Any(p => trimmedKey.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return false;

        using var db = _dbFactory.CreateDbContext();

        var fingerprint = GenerateFingerprint(trimmedKey);

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
        // Extract the base64 data portion of the SSH key for fingerprinting
        var parts = key.Trim().Split(' ');
        var keyData = parts.Length >= 2 ? parts[1] : key;
        using var sha256 = SHA256.Create();
        try
        {
            var bytes = Convert.FromBase64String(keyData);
            var hash = sha256.ComputeHash(bytes);
            return "SHA256:" + Convert.ToBase64String(hash).TrimEnd('=');
        }
        catch
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(hash)[..16].ToLower();
        }
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
