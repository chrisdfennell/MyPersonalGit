using System.Text.Json;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public class AdminService
{
    private readonly string _dataPath;
    private readonly IConfiguration _configuration;

    public AdminService(IConfiguration configuration)
    {
        _configuration = configuration;
        var projectRoot = configuration["ProjectRoot"] ?? throw new InvalidOperationException("ProjectRoot not configured");
        _dataPath = Path.Combine(projectRoot, ".mypersonalgit");
        Directory.CreateDirectory(_dataPath);
    }

    private string GetSettingsFilePath() => Path.Combine(_dataPath, "system_settings.json");
    private string GetAuditLogFilePath() => Path.Combine(_dataPath, "audit_log.json");
    private string GetUsersFilePath() => Path.Combine(_dataPath, "users.json");

    public async Task<SystemSettings> GetSystemSettingsAsync()
    {
        var filePath = GetSettingsFilePath();
        if (!File.Exists(filePath))
        {
            var defaultSettings = new SystemSettings
            {
                ProjectRoot = _configuration["ProjectRoot"] ?? "",
                RequireAuth = _configuration.GetValue<bool>("Git:RequireAuth", true)
            };
            await SaveSystemSettingsAsync(defaultSettings);
            return defaultSettings;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<SystemSettings>(json) ?? new SystemSettings();
    }

    public async Task SaveSystemSettingsAsync(SystemSettings settings)
    {
        var filePath = GetSettingsFilePath();
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<SystemStatistics> GetSystemStatisticsAsync()
    {
        var stats = new SystemStatistics();
        
        var users = await GetAllUsersAsync();
        stats.TotalUsers = users.Count;

        var projectRoot = _configuration["ProjectRoot"] ?? "";
        if (Directory.Exists(projectRoot))
        {
            var repoDirs = Directory.GetDirectories(projectRoot);
            stats.TotalRepositories = repoDirs.Length;

            long totalSize = 0;
            foreach (var dir in repoDirs)
            {
                totalSize += GetDirectorySize(dir);
            }
            stats.TotalStorageUsed = totalSize;
        }

        return stats;
    }

    private long GetDirectorySize(string path)
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
        var filePath = GetAuditLogFilePath();
        if (!File.Exists(filePath))
            return new List<AuditLog>();

        var json = await File.ReadAllTextAsync(filePath);
        var logs = JsonSerializer.Deserialize<List<AuditLog>>(json) ?? new List<AuditLog>();
        return logs.OrderByDescending(l => l.Timestamp).Take(limit).ToList();
    }

    public async Task AddAuditLogAsync(string username, string action, string details, string ipAddress = "")
    {
        var filePath = GetAuditLogFilePath();
        var logs = new List<AuditLog>();
        
        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath);
            logs = JsonSerializer.Deserialize<List<AuditLog>>(json) ?? new List<AuditLog>();
        }

        var log = new AuditLog
        {
            Id = logs.Count > 0 ? logs.Max(l => l.Id) + 1 : 1,
            Timestamp = DateTime.UtcNow,
            Username = username,
            Action = action,
            Details = details,
            IpAddress = ipAddress
        };

        logs.Add(log);

        var logsJson = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, logsJson);
    }

    public async Task<List<UserManagement>> GetAllUsersAsync()
    {
        var filePath = GetUsersFilePath();
        if (!File.Exists(filePath))
        {
            var gitUsers = _configuration.GetSection("Git:Users").GetChildren();
            var users = gitUsers.Select(u => new UserManagement
            {
                Username = u.Key,
                Email = $"{u.Key}@localhost",
                IsAdmin = true, // First-run: all config users are admins
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow
            }).ToList();

            await SaveUsersAsync(users);
            return users;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<List<UserManagement>>(json) ?? new List<UserManagement>();
    }

    private async Task SaveUsersAsync(List<UserManagement> users)
    {
        var filePath = GetUsersFilePath();
        var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<bool> AddUserAsync(string username, string password, string email, bool isAdmin = false)
    {
        var users = await GetAllUsersAsync();
        if (users.Any(u => u.Username == username))
            return false;

        var newUser = new UserManagement
        {
            Username = username,
            Email = email,
            IsAdmin = isAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow
        };

        users.Add(newUser);
        await SaveUsersAsync(users);

        return true;
    }

    public async Task<bool> DeleteUserAsync(string username)
    {
        var users = await GetAllUsersAsync();
        var user = users.FirstOrDefault(u => u.Username == username);
        if (user == null)
            return false;

        users.Remove(user);
        await SaveUsersAsync(users);

        return true;
    }

    public async Task<bool> UpdateUserAsync(UserManagement user)
    {
        var users = await GetAllUsersAsync();
        var existingUser = users.FirstOrDefault(u => u.Username == user.Username);
        if (existingUser == null)
            return false;

        existingUser.Email = user.Email;
        existingUser.IsAdmin = user.IsAdmin;
        existingUser.IsActive = user.IsActive;

        await SaveUsersAsync(users);
        return true;
    }
}
