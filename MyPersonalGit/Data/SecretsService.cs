using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface ISecretsService
{
    Task<List<RepositorySecret>> GetSecretsAsync(string repoName);
    Task<bool> SetSecretAsync(string repoName, string name, string value);
    Task<bool> DeleteSecretAsync(string repoName, string name);
    Task<Dictionary<string, string>> GetDecryptedSecretsAsync(string repoName);
    Task<List<GlobalSecret>> GetGlobalSecretsAsync();
    Task<bool> SetGlobalSecretAsync(string name, string value);
    Task<bool> DeleteGlobalSecretAsync(string name);
    Task<Dictionary<string, string>> GetAllSecretsForRunAsync(string repoName);
    Task<List<UserSecret>> GetUserSecretsAsync(string username);
    Task<bool> SetUserSecretAsync(string username, string name, string value);
    Task<bool> DeleteUserSecretAsync(string username, string name);
    Task<List<OrganizationSecret>> GetOrgSecretsAsync(string orgName);
    Task<bool> SetOrgSecretAsync(string orgName, string name, string value);
    Task<bool> DeleteOrgSecretAsync(string orgName, string name);
}

public class SecretsService : ISecretsService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<SecretsService> _logger;
    private readonly byte[] _encryptionKey;

    public SecretsService(IDbContextFactory<AppDbContext> dbFactory, ILogger<SecretsService> logger, IConfiguration config)
    {
        _dbFactory = dbFactory;
        _logger = logger;

        // Derive encryption key from a configured secret or a default based on the DB connection string
        var keySource = config["Secrets:EncryptionKey"] ?? config.GetConnectionString("Default") ?? "MyPersonalGit-Default-Key";
        _encryptionKey = DeriveKey(keySource);
    }

    public async Task<List<RepositorySecret>> GetSecretsAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.RepositorySecrets
            .Where(s => s.RepoName == repoName)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<bool> SetSecretAsync(string repoName, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
            return false;

        // Validate secret name: alphanumeric + underscores, must start with letter
        var trimmedName = name.Trim().ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(trimmedName, @"^[A-Z_][A-Z0-9_]*$"))
            return false;

        using var db = _dbFactory.CreateDbContext();

        var encrypted = Encrypt(value);

        var existing = await db.RepositorySecrets
            .FirstOrDefaultAsync(s => s.RepoName == repoName && s.Name == trimmedName);

        if (existing != null)
        {
            existing.EncryptedValue = encrypted;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.RepositorySecrets.Add(new RepositorySecret
            {
                RepoName = repoName,
                Name = trimmedName,
                EncryptedValue = encrypted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("Secret '{Name}' set for repository {RepoName}", trimmedName, repoName);
        return true;
    }

    public async Task<bool> DeleteSecretAsync(string repoName, string name)
    {
        using var db = _dbFactory.CreateDbContext();

        var secret = await db.RepositorySecrets
            .FirstOrDefaultAsync(s => s.RepoName == repoName && s.Name == name);

        if (secret == null)
            return false;

        db.RepositorySecrets.Remove(secret);
        await db.SaveChangesAsync();

        _logger.LogInformation("Secret '{Name}' deleted from repository {RepoName}", name, repoName);
        return true;
    }

    public async Task<Dictionary<string, string>> GetDecryptedSecretsAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();

        var secrets = await db.RepositorySecrets
            .Where(s => s.RepoName == repoName)
            .ToListAsync();

        var result = new Dictionary<string, string>();
        foreach (var secret in secrets)
        {
            try
            {
                result[secret.Name] = Decrypt(secret.EncryptedValue);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt secret '{Name}' for {RepoName}", secret.Name, repoName);
            }
        }

        return result;
    }

    private string Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        // Prepend IV to ciphertext
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    private string Decrypt(string ciphertext)
    {
        var allBytes = Convert.FromBase64String(ciphertext);

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;

        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[allBytes.Length - iv.Length];
        Buffer.BlockCopy(allBytes, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(allBytes, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var plaintextBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private static byte[] DeriveKey(string source)
    {
        // Use PBKDF2 to derive a 256-bit key
        var salt = Encoding.UTF8.GetBytes("MyPersonalGit.Secrets.v1");
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(source),
            salt,
            iterations: 100_000,
            HashAlgorithmName.SHA256,
            outputLength: 32);
    }

    // --- Global secrets ---

    public async Task<List<GlobalSecret>> GetGlobalSecretsAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.GlobalSecrets.OrderBy(s => s.Name).ToListAsync();
    }

    public async Task<bool> SetGlobalSecretAsync(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
            return false;

        var trimmedName = name.Trim().ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(trimmedName, @"^[A-Z_][A-Z0-9_]*$"))
            return false;

        using var db = _dbFactory.CreateDbContext();
        var encrypted = Encrypt(value);

        var existing = await db.GlobalSecrets.FirstOrDefaultAsync(s => s.Name == trimmedName);
        if (existing != null)
        {
            existing.EncryptedValue = encrypted;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.GlobalSecrets.Add(new GlobalSecret
            {
                Name = trimmedName,
                EncryptedValue = encrypted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("Global secret '{Name}' set", trimmedName);
        return true;
    }

    public async Task<bool> DeleteGlobalSecretAsync(string name)
    {
        using var db = _dbFactory.CreateDbContext();
        var secret = await db.GlobalSecrets.FirstOrDefaultAsync(s => s.Name == name);
        if (secret == null) return false;

        db.GlobalSecrets.Remove(secret);
        await db.SaveChangesAsync();
        _logger.LogInformation("Global secret '{Name}' deleted", name);
        return true;
    }

    /// <summary>
    /// Get all secrets for a workflow run: Global -> Org -> User -> Repo (each level overrides the previous).
    /// </summary>
    public async Task<Dictionary<string, string>> GetAllSecretsForRunAsync(string repoName)
    {
        var result = new Dictionary<string, string>();

        using var db = _dbFactory.CreateDbContext();

        // 1. Global secrets (lowest priority)
        var globalSecrets = await db.GlobalSecrets.ToListAsync();
        foreach (var secret in globalSecrets)
        {
            try { result[secret.Name] = Decrypt(secret.EncryptedValue); }
            catch { }
        }

        // Look up the repo owner
        var repoNameAlt = repoName.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? repoName[..^4] : repoName + ".git";
        var repo = await db.Repositories
            .FirstOrDefaultAsync(r => r.Name == repoName || r.Name == repoNameAlt);

        if (repo != null)
        {
            var owner = repo.Owner;

            // 2. Org secrets — check if owner is an organization
            var isOrg = await db.Organizations.AnyAsync(o => o.Name == owner);
            if (isOrg)
            {
                var orgSecrets = await db.OrganizationSecrets
                    .Where(s => s.OrganizationName == owner)
                    .ToListAsync();
                foreach (var secret in orgSecrets)
                {
                    try { result[secret.Name] = Decrypt(secret.EncryptedValue); }
                    catch { }
                }
            }

            // 3. User secrets — if owner is a user (or org owner)
            var userSecrets = await db.UserSecrets
                .Where(s => s.Username == owner)
                .ToListAsync();
            foreach (var secret in userSecrets)
            {
                try { result[secret.Name] = Decrypt(secret.EncryptedValue); }
                catch { }
            }
        }

        // 4. Repo secrets (highest priority) — check both with and without .git suffix
        var repoSecrets = await db.RepositorySecrets
            .Where(s => s.RepoName == repoName || s.RepoName == repoNameAlt)
            .ToListAsync();
        foreach (var secret in repoSecrets)
        {
            try { result[secret.Name] = Decrypt(secret.EncryptedValue); }
            catch { }
        }

        return result;
    }

    // --- User secrets ---

    public async Task<List<UserSecret>> GetUserSecretsAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.UserSecrets
            .Where(s => s.Username == username)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<bool> SetUserSecretAsync(string username, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
            return false;

        var trimmedName = name.Trim().ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(trimmedName, @"^[A-Z_][A-Z0-9_]*$"))
            return false;

        using var db = _dbFactory.CreateDbContext();
        var encrypted = Encrypt(value);

        var existing = await db.UserSecrets
            .FirstOrDefaultAsync(s => s.Username == username && s.Name == trimmedName);

        if (existing != null)
        {
            existing.EncryptedValue = encrypted;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.UserSecrets.Add(new UserSecret
            {
                Username = username,
                Name = trimmedName,
                EncryptedValue = encrypted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("User secret '{Name}' set for user {Username}", trimmedName, username);
        return true;
    }

    public async Task<bool> DeleteUserSecretAsync(string username, string name)
    {
        using var db = _dbFactory.CreateDbContext();
        var secret = await db.UserSecrets
            .FirstOrDefaultAsync(s => s.Username == username && s.Name == name);
        if (secret == null) return false;

        db.UserSecrets.Remove(secret);
        await db.SaveChangesAsync();
        _logger.LogInformation("User secret '{Name}' deleted for user {Username}", name, username);
        return true;
    }

    // --- Organization secrets ---

    public async Task<List<OrganizationSecret>> GetOrgSecretsAsync(string orgName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.OrganizationSecrets
            .Where(s => s.OrganizationName == orgName)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<bool> SetOrgSecretAsync(string orgName, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
            return false;

        var trimmedName = name.Trim().ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(trimmedName, @"^[A-Z_][A-Z0-9_]*$"))
            return false;

        using var db = _dbFactory.CreateDbContext();
        var encrypted = Encrypt(value);

        var existing = await db.OrganizationSecrets
            .FirstOrDefaultAsync(s => s.OrganizationName == orgName && s.Name == trimmedName);

        if (existing != null)
        {
            existing.EncryptedValue = encrypted;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.OrganizationSecrets.Add(new OrganizationSecret
            {
                OrganizationName = orgName,
                Name = trimmedName,
                EncryptedValue = encrypted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("Org secret '{Name}' set for org {OrgName}", trimmedName, orgName);
        return true;
    }

    public async Task<bool> DeleteOrgSecretAsync(string orgName, string name)
    {
        using var db = _dbFactory.CreateDbContext();
        var secret = await db.OrganizationSecrets
            .FirstOrDefaultAsync(s => s.OrganizationName == orgName && s.Name == name);
        if (secret == null) return false;

        db.OrganizationSecrets.Remove(secret);
        await db.SaveChangesAsync();
        _logger.LogInformation("Org secret '{Name}' deleted for org {OrgName}", name, orgName);
        return true;
    }
}
