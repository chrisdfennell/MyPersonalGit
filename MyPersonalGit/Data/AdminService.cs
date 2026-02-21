using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IAdminService
{
    Task<SystemSettings> GetSystemSettingsAsync();
    Task SaveSystemSettingsAsync(SystemSettings settings);
    Task<SystemStatistics> GetSystemStatisticsAsync();
    Task<List<AuditLog>> GetAuditLogsAsync(int limit = 100);
    Task AddAuditLogAsync(string username, string action, string details, string ipAddress = "");
    Task<List<UserManagement>> GetAllUsersAsync();
    Task<bool> AddUserAsync(string username, string password, string email, bool isAdmin = false);
    Task<bool> DeleteUserAsync(string username);
    Task<bool> UpdateUserAsync(UserManagement user);
}

public class AdminService : IAdminService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminService> _logger;

    public AdminService(IDbContextFactory<AppDbContext> dbFactory, IConfiguration configuration, ILogger<AdminService> logger)
    {
        _dbFactory = dbFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SystemSettings> GetSystemSettingsAsync()
    {
        using var db = _dbFactory.CreateDbContext();

        var settings = await db.SystemSettings.FirstOrDefaultAsync();
        if (settings != null)
            return settings;

        var defaultSettings = new SystemSettings
        {
            ProjectRoot = _configuration["ProjectRoot"] ?? "",
            RequireAuth = _configuration.GetValue<bool>("Git:RequireAuth", true)
        };

        db.SystemSettings.Add(defaultSettings);
        await db.SaveChangesAsync();
        return defaultSettings;
    }

    public async Task SaveSystemSettingsAsync(SystemSettings settings)
    {
        using var db = _dbFactory.CreateDbContext();

        var existing = await db.SystemSettings.FirstOrDefaultAsync();
        if (existing != null)
        {
            db.Entry(existing).CurrentValues.SetValues(settings);
        }
        else
        {
            db.SystemSettings.Add(settings);
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("System settings updated");
    }

    public async Task<SystemStatistics> GetSystemStatisticsAsync()
    {
        using var db = _dbFactory.CreateDbContext();

        var stats = new SystemStatistics
        {
            TotalUsers = await db.Users.CountAsync(),
            TotalRepositories = await db.Repositories.CountAsync(),
            TotalIssues = await db.Issues.CountAsync(),
            TotalPullRequests = await db.PullRequests.CountAsync()
        };

        var projectRoot = _configuration["ProjectRoot"] ?? "";
        if (Directory.Exists(projectRoot))
        {
            long totalSize = 0;
            foreach (var dir in Directory.GetDirectories(projectRoot))
                totalSize += GetDirectorySize(dir);
            stats.TotalStorageUsed = totalSize;
        }

        return stats;
    }

    private static long GetDirectorySize(string path)
    {
        try
        {
            var dirInfo = new DirectoryInfo(path);
            return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        }
        catch
        {
            return 0;
        }
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync(int limit = 100)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.AuditLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task AddAuditLogAsync(string username, string action, string details, string ipAddress = "")
    {
        using var db = _dbFactory.CreateDbContext();

        db.AuditLogs.Add(new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            Username = username,
            Action = action,
            Details = details,
            IpAddress = ipAddress
        });

        await db.SaveChangesAsync();
        _logger.LogInformation("Audit: {Username} performed {Action}: {Details}", username, action, details);
    }

    public async Task<List<UserManagement>> GetAllUsersAsync()
    {
        using var db = _dbFactory.CreateDbContext();

        var users = await db.Users.ToListAsync();
        return users.Select(u => new UserManagement
        {
            Username = u.Username,
            Email = u.Email,
            IsAdmin = u.IsAdmin,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            LastLogin = u.LastLoginAt ?? u.CreatedAt
        }).ToList();
    }

    public async Task<bool> AddUserAsync(string username, string password, string email, bool isAdmin = false)
    {
        using var db = _dbFactory.CreateDbContext();

        if (await db.Users.AnyAsync(u => u.Username == username))
            return false;

        db.Users.Add(new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
            IsAdmin = isAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        _logger.LogInformation("User {Username} added by admin", username);
        return true;
    }

    public async Task<bool> DeleteUserAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return false;

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        _logger.LogInformation("User {Username} deleted by admin", username);
        return true;
    }

    public async Task<bool> UpdateUserAsync(UserManagement user)
    {
        using var db = _dbFactory.CreateDbContext();

        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Username == user.Username);
        if (existingUser == null)
            return false;

        existingUser.Email = user.Email;
        existingUser.IsAdmin = user.IsAdmin;
        existingUser.IsActive = user.IsActive;
        await db.SaveChangesAsync();

        _logger.LogInformation("User {Username} updated by admin", user.Username);
        return true;
    }
}
