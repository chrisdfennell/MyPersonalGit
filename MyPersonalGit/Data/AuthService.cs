using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IAuthService
{
    Task<User?> RegisterAsync(string username, string email, string password, string? fullName = null);
    Task<UserSession?> LoginAsync(string usernameOrEmail, string password);
    Task<bool> LogoutAsync(string sessionId);
    Task<User?> GetUserBySessionAsync(string sessionId);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<bool> UpdateUserProfileAsync(string username, Action<User> updateAction);
    Task<bool> ChangePasswordAsync(string username, string oldPassword, string newPassword);
}

public class AuthService : IAuthService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IDbContextFactory<AppDbContext> dbFactory, ILogger<AuthService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<User?> RegisterAsync(string username, string email, string password, string? fullName = null)
    {
        using var db = _dbFactory.CreateDbContext();

        if (await db.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
        {
            _logger.LogWarning("Registration failed: Username {Username} already exists", username);
            return null;
        }

        if (await db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
        {
            _logger.LogWarning("Registration failed: Email {Email} already exists", email);
            return null;
        }

        var newUser = new User
        {
            Username = username,
            Email = email,
            PasswordHash = HashPassword(password),
            FullName = fullName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        _logger.LogInformation("User {Username} registered successfully", username);
        return newUser;
    }

    public async Task<UserSession?> LoginAsync(string usernameOrEmail, string password)
    {
        using var db = _dbFactory.CreateDbContext();

        var user = await db.Users.FirstOrDefaultAsync(u =>
            (u.Username.ToLower() == usernameOrEmail.ToLower() ||
             u.Email.ToLower() == usernameOrEmail.ToLower()) &&
            u.IsActive);

        if (user == null || !VerifyPassword(password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for {UsernameOrEmail}", usernameOrEmail);
            return null;
        }

        if (!user.PasswordHash.StartsWith("$2"))
        {
            user.PasswordHash = HashPassword(password);
            _logger.LogInformation("Migrated password hash to bcrypt for {Username}", user.Username);
        }

        user.LastLoginAt = DateTime.UtcNow;

        var session = new UserSession
        {
            SessionId = Guid.NewGuid().ToString(),
            Username = user.Username,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        db.UserSessions.Add(session);
        await db.SaveChangesAsync();

        _logger.LogInformation("User {Username} logged in successfully", user.Username);
        return session;
    }

    public async Task<bool> LogoutAsync(string sessionId)
    {
        using var db = _dbFactory.CreateDbContext();

        var session = await db.UserSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
        if (session == null)
            return false;

        db.UserSessions.Remove(session);
        await db.SaveChangesAsync();

        _logger.LogInformation("User {Username} logged out", session.Username);
        return true;
    }

    public async Task<User?> GetUserBySessionAsync(string sessionId)
    {
        using var db = _dbFactory.CreateDbContext();

        var session = await db.UserSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.ExpiresAt > DateTime.UtcNow);

        if (session == null)
            return null;

        return await db.Users.FirstOrDefaultAsync(u => u.Username == session.Username);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
    }

    public async Task<bool> UpdateUserProfileAsync(string username, Action<User> updateAction)
    {
        using var db = _dbFactory.CreateDbContext();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null)
            return false;

        updateAction(user);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(string username, string oldPassword, string newPassword)
    {
        using var db = _dbFactory.CreateDbContext();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null || !VerifyPassword(oldPassword, user.PasswordHash))
            return false;

        user.PasswordHash = HashPassword(newPassword);
        await db.SaveChangesAsync();

        _logger.LogInformation("Password changed for {Username}", username);
        return true;
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        if (!passwordHash.StartsWith("$2"))
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes) == passwordHash;
        }

        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
