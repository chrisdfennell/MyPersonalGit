using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public class AuthService
{
    private readonly string _dataPath;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
    {
        var projectRoot = configuration["ProjectRoot"] ?? throw new InvalidOperationException("ProjectRoot not configured");
        _dataPath = Path.Combine(projectRoot, ".mypersonalgit");
        _logger = logger;
        Directory.CreateDirectory(_dataPath);
    }

    private string GetUsersFilePath() => Path.Combine(_dataPath, "users.json");
    private string GetSessionsFilePath() => Path.Combine(_dataPath, "sessions.json");

    public async Task<User?> RegisterAsync(string username, string email, string password, string? fullName = null)
    {
        var users = await GetUsersAsync();
        
        if (users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Registration failed: Username {Username} already exists", username);
            return null;
        }
        
        if (users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Registration failed: Email {Email} already exists", email);
            return null;
        }

        var user = new User
        {
            Id = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1,
            Username = username,
            Email = email,
            PasswordHash = HashPassword(password),
            FullName = fullName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        users.Add(user);
        await SaveUsersAsync(users);
        
        _logger.LogInformation("User {Username} registered successfully", username);
        return user;
    }

    public async Task<UserSession?> LoginAsync(string usernameOrEmail, string password)
    {
        var users = await GetUsersAsync();
        var user = users.FirstOrDefault(u => 
            (u.Username.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase) || 
             u.Email.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase)) &&
            u.IsActive);

        if (user == null || !VerifyPassword(password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for {UsernameOrEmail}", usernameOrEmail);
            return null;
        }

        // Transparent migration: re-hash legacy SHA256 passwords with bcrypt
        if (!user.PasswordHash.StartsWith("$2"))
        {
            user.PasswordHash = HashPassword(password);
            _logger.LogInformation("Migrated password hash to bcrypt for {Username}", user.Username);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await SaveUsersAsync(users);

        var session = new UserSession
        {
            SessionId = Guid.NewGuid().ToString(),
            Username = user.Username,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        var sessions = await GetSessionsAsync();
        sessions.Add(session);
        await SaveSessionsAsync(sessions);

        _logger.LogInformation("User {Username} logged in successfully", user.Username);
        return session;
    }

    public async Task<bool> LogoutAsync(string sessionId)
    {
        var sessions = await GetSessionsAsync();
        var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
        
        if (session != null)
        {
            sessions.Remove(session);
            await SaveSessionsAsync(sessions);
            _logger.LogInformation("User {Username} logged out", session.Username);
            return true;
        }
        
        return false;
    }

    public async Task<User?> GetUserBySessionAsync(string sessionId)
    {
        var sessions = await GetSessionsAsync();
        var session = sessions.FirstOrDefault(s => s.SessionId == sessionId && s.ExpiresAt > DateTime.UtcNow);
        
        if (session == null)
            return null;

        var users = await GetUsersAsync();
        return users.FirstOrDefault(u => u.Username == session.Username);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        var users = await GetUsersAsync();
        return users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> UpdateUserProfileAsync(string username, Action<User> updateAction)
    {
        var users = await GetUsersAsync();
        var user = users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        
        if (user == null)
            return false;

        updateAction(user);
        await SaveUsersAsync(users);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(string username, string oldPassword, string newPassword)
    {
        var users = await GetUsersAsync();
        var user = users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        
        if (user == null || !VerifyPassword(oldPassword, user.PasswordHash))
            return false;

        user.PasswordHash = HashPassword(newPassword);
        await SaveUsersAsync(users);
        return true;
    }

    private async Task<List<User>> GetUsersAsync()
    {
        var filePath = GetUsersFilePath();
        if (!File.Exists(filePath))
            return new List<User>();

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load users");
            return new List<User>();
        }
    }

    private async Task SaveUsersAsync(List<User> users)
    {
        var filePath = GetUsersFilePath();
        var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    private async Task<List<UserSession>> GetSessionsAsync()
    {
        var filePath = GetSessionsFilePath();
        if (!File.Exists(filePath))
            return new List<UserSession>();

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var sessions = JsonSerializer.Deserialize<List<UserSession>>(json) ?? new List<UserSession>();
            return sessions.Where(s => s.ExpiresAt > DateTime.UtcNow).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load sessions");
            return new List<UserSession>();
        }
    }

    private async Task SaveSessionsAsync(List<UserSession> sessions)
    {
        var filePath = GetSessionsFilePath();
        var json = JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    private bool VerifyPassword(string password, string passwordHash)
    {
        // Support legacy SHA256 hashes for migration
        if (!passwordHash.StartsWith("$2"))
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes) == passwordHash;
        }

        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
