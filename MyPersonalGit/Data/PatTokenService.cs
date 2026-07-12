using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IPatTokenService
{
    /// <summary>
    /// Look up a personal access token by its plaintext value (hash comparison).
    /// Returns the token record even if expired — callers decide how to report expiry.
    /// Updates LastUsed (throttled) on a successful match.
    /// </summary>
    Task<PersonalAccessToken?> ValidateAsync(string plaintextToken);
}

public class PatTokenService : IPatTokenService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<PatTokenService> _logger;

    // LastUsed is informational; avoid a DB write on every API call.
    private static readonly TimeSpan LastUsedResolution = TimeSpan.FromMinutes(5);

    public PatTokenService(IDbContextFactory<AppDbContext> dbFactory, ILogger<PatTokenService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    /// <summary>First characters of the token shown in the UI so users can tell tokens apart.</summary>
    public static string TokenPrefix(string token)
        => token.Length <= 12 ? token : token[..12];

    public async Task<PersonalAccessToken?> ValidateAsync(string plaintextToken)
    {
        if (string.IsNullOrEmpty(plaintextToken)) return null;

        var hash = HashToken(plaintextToken);
        try
        {
            using var db = _dbFactory.CreateDbContext();
            var matched = await db.PersonalAccessTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
            if (matched == null) return null;

            if (matched.LastUsed == null || DateTime.UtcNow - matched.LastUsed.Value > LastUsedResolution)
            {
                try
                {
                    matched.LastUsed = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
                catch (Exception ex) { _logger.LogDebug(ex, "Failed to update token LastUsed"); }
            }

            return matched;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    /// <summary>
    /// One-time startup migration to hashed token storage. Idempotent.
    /// 1. Hashes any DB rows still carrying a plaintext Token and blanks the plaintext.
    /// 2. Imports tokens from legacy {user}_tokens.json files into the DB (hashed),
    ///    then renames the files to *.migrated so plaintext secrets stop being read.
    /// </summary>
    public static void MigrateToHashedStorage(AppDbContext db, string dataPath, Action<string> log)
    {
        // 1. Hash plaintext tokens already in the database.
        var plaintextRows = db.PersonalAccessTokens
            .Where(t => t.TokenHash == "" && t.Token != "")
            .ToList();
        foreach (var row in plaintextRows)
        {
            row.TokenHash = HashToken(row.Token);
            row.TokenPrefix = TokenPrefix(row.Token);
            row.Token = string.Empty;
        }
        if (plaintextRows.Count > 0)
        {
            db.SaveChanges();
            log($"==> Hashed {plaintextRows.Count} personal access token(s) at rest");
        }

        // 2. Import legacy JSON token files.
        if (!Directory.Exists(dataPath)) return;
        foreach (var file in Directory.GetFiles(dataPath, "*_tokens.json"))
        {
            try
            {
                var tokens = JsonSerializer.Deserialize<List<PersonalAccessToken>>(File.ReadAllText(file));
                var imported = 0;
                foreach (var t in tokens ?? new())
                {
                    if (string.IsNullOrEmpty(t.Token) || string.IsNullOrEmpty(t.Username)) continue;
                    var hash = HashToken(t.Token);
                    if (db.PersonalAccessTokens.Any(x => x.TokenHash == hash)) continue;

                    db.PersonalAccessTokens.Add(new PersonalAccessToken
                    {
                        Username = t.Username,
                        Name = string.IsNullOrEmpty(t.Name) ? "Imported token" : t.Name,
                        Token = string.Empty,
                        TokenHash = hash,
                        TokenPrefix = TokenPrefix(t.Token),
                        Scopes = t.Scopes ?? Array.Empty<string>(),
                        AllowedRoutes = t.AllowedRoutes ?? Array.Empty<string>(),
                        CreatedAt = t.CreatedAt == default ? DateTime.UtcNow : t.CreatedAt,
                        ExpiresAt = t.ExpiresAt,
                        LastUsed = t.LastUsed
                    });
                    imported++;
                }
                if (imported > 0) db.SaveChanges();

                // Stop serving auth from the plaintext file; keep a copy so the
                // operator can verify before deleting it.
                File.Move(file, file + ".migrated", overwrite: true);
                log($"==> Imported {imported} token(s) from {Path.GetFileName(file)}; " +
                    $"renamed to .migrated — delete it once verified");
            }
            catch (Exception ex)
            {
                log($"Warning: token file migration failed for {Path.GetFileName(file)}: {ex.Message}");
            }
        }
    }
}
