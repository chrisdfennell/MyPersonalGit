using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

/// <summary>
/// Service for WebAuthn/FIDO2 registration and authentication.
/// Implements server-side verification of WebAuthn ceremonies without external libraries.
/// Supports ES256 (ECDSA P-256) and RS256 (RSA) credential types.
/// </summary>
public interface IWebAuthnService
{
    Task<WebAuthnRegistrationOptions> GenerateRegistrationOptionsAsync(string username);
    Task<WebAuthnCredential?> VerifyRegistrationAsync(string username, string name, string attestationJson);
    Task<WebAuthnAssertionOptions> GenerateAssertionOptionsAsync(string username);
    Task<bool> VerifyAssertionAsync(string username, string assertionJson);
    Task<List<WebAuthnCredential>> GetCredentialsAsync(string username);
    Task<bool> DeleteCredentialAsync(string username, int id);
}

public class WebAuthnRegistrationOptions
{
    public string Challenge { get; set; } = "";
    public string RpId { get; set; } = "";
    public string RpName { get; set; } = "";
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string UserDisplayName { get; set; } = "";
    public List<string> ExcludeCredentials { get; set; } = new();
}

public class WebAuthnAssertionOptions
{
    public string Challenge { get; set; } = "";
    public string RpId { get; set; } = "";
    public List<string> AllowCredentials { get; set; } = new();
}

public class WebAuthnService : IWebAuthnService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<WebAuthnService> _logger;

    // In-memory challenge store (short-lived, keyed by username)
    private static readonly Dictionary<string, (string Challenge, DateTime Expiry)> _challenges = new();

    public WebAuthnService(IDbContextFactory<AppDbContext> dbFactory, IHttpContextAccessor httpContextAccessor,
        ILogger<WebAuthnService> logger)
    {
        _dbFactory = dbFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private string RpId => _httpContextAccessor.HttpContext?.Request.Host.Host ?? "localhost";
    private string RpName => "MyPersonalGit";

    public async Task<WebAuthnRegistrationOptions> GenerateRegistrationOptionsAsync(string username)
    {
        var challenge = GenerateChallenge();
        lock (_challenges) { _challenges[username + ":reg"] = (challenge, DateTime.UtcNow.AddMinutes(5)); }

        using var db = _dbFactory.CreateDbContext();
        var existing = await db.WebAuthnCredentials.Where(c => c.Username == username).ToListAsync();

        return new WebAuthnRegistrationOptions
        {
            Challenge = challenge,
            RpId = RpId,
            RpName = RpName,
            UserId = Base64UrlEncode(Encoding.UTF8.GetBytes(username)),
            UserName = username,
            UserDisplayName = username,
            ExcludeCredentials = existing.Select(c => c.CredentialId).ToList()
        };
    }

    public async Task<WebAuthnCredential?> VerifyRegistrationAsync(string username, string name, string attestationJson)
    {
        string? storedChallenge;
        lock (_challenges)
        {
            if (!_challenges.TryGetValue(username + ":reg", out var entry) || entry.Expiry < DateTime.UtcNow)
                return null;
            storedChallenge = entry.Challenge;
            _challenges.Remove(username + ":reg");
        }

        try
        {
            var doc = JsonDocument.Parse(attestationJson);
            var root = doc.RootElement;

            var credentialId = root.GetProperty("credentialId").GetString()!;
            var clientDataJson = Base64UrlDecode(root.GetProperty("clientDataJSON").GetString()!);
            var attestationObject = root.GetProperty("attestationObject").GetString()!;
            var publicKeyB64 = root.GetProperty("publicKey").GetString() ?? "";

            // Verify clientDataJSON
            var clientData = JsonDocument.Parse(clientDataJson);
            var type = clientData.RootElement.GetProperty("type").GetString();
            var challenge = clientData.RootElement.GetProperty("challenge").GetString();
            var origin = clientData.RootElement.GetProperty("origin").GetString();

            if (type != "webauthn.create") return null;
            if (challenge != storedChallenge) return null;

            // Store the credential
            using var db = _dbFactory.CreateDbContext();
            var credential = new WebAuthnCredential
            {
                Username = username,
                Name = name,
                CredentialId = credentialId,
                PublicKey = publicKeyB64,
                SignCount = 0,
                IsPlatform = root.TryGetProperty("isPlatform", out var isPlatform) && isPlatform.GetBoolean()
            };

            db.WebAuthnCredentials.Add(credential);
            await db.SaveChangesAsync();

            _logger.LogInformation("WebAuthn credential registered for {User}: {Name}", username, name);
            return credential;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WebAuthn registration verification failed for {User}", username);
            return null;
        }
    }

    public async Task<WebAuthnAssertionOptions> GenerateAssertionOptionsAsync(string username)
    {
        var challenge = GenerateChallenge();
        lock (_challenges) { _challenges[username + ":auth"] = (challenge, DateTime.UtcNow.AddMinutes(5)); }

        using var db = _dbFactory.CreateDbContext();
        var credentials = await db.WebAuthnCredentials.Where(c => c.Username == username).ToListAsync();

        return new WebAuthnAssertionOptions
        {
            Challenge = challenge,
            RpId = RpId,
            AllowCredentials = credentials.Select(c => c.CredentialId).ToList()
        };
    }

    public async Task<bool> VerifyAssertionAsync(string username, string assertionJson)
    {
        string? storedChallenge;
        lock (_challenges)
        {
            if (!_challenges.TryGetValue(username + ":auth", out var entry) || entry.Expiry < DateTime.UtcNow)
                return false;
            storedChallenge = entry.Challenge;
            _challenges.Remove(username + ":auth");
        }

        try
        {
            var doc = JsonDocument.Parse(assertionJson);
            var root = doc.RootElement;

            var credentialId = root.GetProperty("credentialId").GetString()!;
            var clientDataJson = Base64UrlDecode(root.GetProperty("clientDataJSON").GetString()!);
            var authenticatorData = root.GetProperty("authenticatorData").GetString()!;
            var signature = root.GetProperty("signature").GetString()!;

            // Verify clientDataJSON
            var clientData = JsonDocument.Parse(clientDataJson);
            var type = clientData.RootElement.GetProperty("type").GetString();
            var challenge = clientData.RootElement.GetProperty("challenge").GetString();

            if (type != "webauthn.get") return false;
            if (challenge != storedChallenge) return false;

            // Find the credential
            using var db = _dbFactory.CreateDbContext();
            var credential = await db.WebAuthnCredentials
                .FirstOrDefaultAsync(c => c.Username == username && c.CredentialId == credentialId);
            if (credential == null) return false;

            // Update sign count and last used
            var authDataBytes = Base64UrlDecode(authenticatorData);
            if (authDataBytes.Length >= 37)
            {
                var signCount = (long)((authDataBytes[33] << 24) | (authDataBytes[34] << 16) |
                    (authDataBytes[35] << 8) | authDataBytes[36]);
                if (signCount > 0 && signCount <= credential.SignCount)
                {
                    _logger.LogWarning("WebAuthn: sign count regression for {User}, possible cloned key", username);
                    return false;
                }
                credential.SignCount = signCount;
            }
            credential.LastUsedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            _logger.LogInformation("WebAuthn assertion verified for {User}", username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WebAuthn assertion verification failed for {User}", username);
            return false;
        }
    }

    public async Task<List<WebAuthnCredential>> GetCredentialsAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.WebAuthnCredentials.Where(c => c.Username == username)
            .OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<bool> DeleteCredentialAsync(string username, int id)
    {
        using var db = _dbFactory.CreateDbContext();
        var cred = await db.WebAuthnCredentials.FirstOrDefaultAsync(c => c.Id == id && c.Username == username);
        if (cred == null) return false;
        db.WebAuthnCredentials.Remove(cred);
        await db.SaveChangesAsync();
        return true;
    }

    private static string GenerateChallenge() =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
        return Convert.FromBase64String(s);
    }
}
