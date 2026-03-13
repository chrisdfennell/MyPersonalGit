using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IDeployKeyService
{
    Task<DeployKey?> AddDeployKeyAsync(int repoId, string title, string publicKey, bool readOnly);
    Task<List<DeployKey>> GetDeployKeysAsync(int repoId);
    Task<bool> DeleteDeployKeyAsync(int id);
    Task<DeployKeyValidationResult?> ValidateDeployKeyAsync(string fingerprint, string repoPath);
}

public class DeployKeyValidationResult
{
    public int DeployKeyId { get; set; }
    public int RepositoryId { get; set; }
    public string RepositoryName { get; set; } = string.Empty;
    public string RepositoryOwner { get; set; } = string.Empty;
    public bool ReadOnly { get; set; }
}

public class DeployKeyService : IDeployKeyService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<DeployKeyService> _logger;

    public DeployKeyService(IDbContextFactory<AppDbContext> dbFactory, ILogger<DeployKeyService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<DeployKey?> AddDeployKeyAsync(int repoId, string title, string publicKey, bool readOnly)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(publicKey))
            return null;

        var fingerprint = GenerateFingerprint(publicKey.Trim());

        using var db = _dbFactory.CreateDbContext();

        // Check for duplicate fingerprint on the same repository
        var exists = await db.DeployKeys
            .AnyAsync(k => k.RepositoryId == repoId && k.KeyFingerprint == fingerprint);
        if (exists)
        {
            _logger.LogWarning("Deploy key with fingerprint {Fingerprint} already exists for repository {RepoId}", fingerprint, repoId);
            return null;
        }

        var deployKey = new DeployKey
        {
            RepositoryId = repoId,
            Title = title.Trim(),
            PublicKey = publicKey.Trim(),
            KeyFingerprint = fingerprint,
            ReadOnly = readOnly,
            CreatedAt = DateTime.UtcNow
        };

        db.DeployKeys.Add(deployKey);
        await db.SaveChangesAsync();

        _logger.LogInformation("Deploy key '{Title}' added to repository {RepoId} (fingerprint: {Fingerprint})",
            title, repoId, fingerprint);

        return deployKey;
    }

    public async Task<List<DeployKey>> GetDeployKeysAsync(int repoId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.DeployKeys
            .Where(k => k.RepositoryId == repoId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteDeployKeyAsync(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        var key = await db.DeployKeys.FindAsync(id);
        if (key == null)
            return false;

        db.DeployKeys.Remove(key);
        await db.SaveChangesAsync();

        _logger.LogInformation("Deploy key {Id} ('{Title}') deleted from repository {RepoId}",
            id, key.Title, key.RepositoryId);

        return true;
    }

    /// <summary>
    /// Validates a deploy key by fingerprint against a specific repository path.
    /// Returns validation result if the key is valid for the given repo, null otherwise.
    /// Updates the LastUsedAt timestamp on successful validation.
    /// </summary>
    public async Task<DeployKeyValidationResult?> ValidateDeployKeyAsync(string fingerprint, string repoPath)
    {
        if (string.IsNullOrWhiteSpace(fingerprint) || string.IsNullOrWhiteSpace(repoPath))
            return null;

        using var db = _dbFactory.CreateDbContext();

        // Find deploy key by fingerprint, including its repository
        var deployKey = await db.DeployKeys
            .Include(k => k.Repository)
            .FirstOrDefaultAsync(k => k.KeyFingerprint == fingerprint);

        if (deployKey?.Repository == null)
            return null;

        // Check that the deploy key's repository matches the requested repo path
        var repoName = Path.GetFileName(repoPath.TrimEnd('/', '\\'));
        // Strip .git suffix if present
        if (repoName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            repoName = repoName[..^4];

        var deployKeyRepoName = deployKey.Repository.Name;
        if (deployKeyRepoName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            deployKeyRepoName = deployKeyRepoName[..^4];

        if (!repoName.Equals(deployKeyRepoName, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Deploy key {Id} is for repo '{DeployKeyRepo}', but was used against '{RequestedRepo}'",
                deployKey.Id, deployKey.Repository.Name, repoName);
            return null;
        }

        // Update last used timestamp
        deployKey.LastUsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("Deploy key {Id} validated for repository '{RepoName}' (read-only: {ReadOnly})",
            deployKey.Id, deployKey.Repository.Name, deployKey.ReadOnly);

        return new DeployKeyValidationResult
        {
            DeployKeyId = deployKey.Id,
            RepositoryId = deployKey.RepositoryId,
            RepositoryName = deployKey.Repository.Name,
            RepositoryOwner = deployKey.Repository.Owner,
            ReadOnly = deployKey.ReadOnly
        };
    }

    private static string GenerateFingerprint(string key)
    {
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
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(hash)[..16].ToLower();
        }
    }
}
