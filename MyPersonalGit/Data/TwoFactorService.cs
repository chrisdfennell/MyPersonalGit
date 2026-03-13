using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface ITwoFactorService
{
    string GenerateSecretKey();
    string GenerateTotpCode(string base32Secret);
    bool ValidateTotpCode(string base32Secret, string code);
    Task<TwoFactorSetupInfo> EnableTwoFactor(int userId);
    Task<bool> VerifyAndActivate(int userId, string code);
    Task<bool> DisableTwoFactor(int userId, string code);
    Task<bool> ValidateLogin(int userId, string code);
    Task<string[]> GenerateRecoveryCodes(int userId);
    Task<bool> UseRecoveryCode(int userId, string code);
    string GetTotpUri(string base32Secret, string userEmail);
    Task<bool> HasTwoFactorEnabled(int userId);
}

public class TwoFactorSetupInfo
{
    public string Secret { get; set; } = string.Empty;
    public string TotpUri { get; set; } = string.Empty;
}

public class TwoFactorService : ITwoFactorService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<TwoFactorService> _logger;

    private const int TimeStep = 30;
    private const int CodeDigits = 6;
    private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public TwoFactorService(IDbContextFactory<AppDbContext> dbFactory, ILogger<TwoFactorService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public string GenerateSecretKey()
    {
        var bytes = new byte[20]; // 160-bit secret
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base32Encode(bytes);
    }

    public string GenerateTotpCode(string base32Secret)
    {
        var secretBytes = Base32Decode(base32Secret);
        var timeCounter = GetCurrentTimeCounter();
        return ComputeTotp(secretBytes, timeCounter);
    }

    public bool ValidateTotpCode(string base32Secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != CodeDigits)
            return false;

        var secretBytes = Base32Decode(base32Secret);
        var timeCounter = GetCurrentTimeCounter();

        // Check current time step and +/- 1 window
        for (long offset = -1; offset <= 1; offset++)
        {
            var computed = ComputeTotp(secretBytes, timeCounter + offset);
            if (string.Equals(computed, code, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public async Task<TwoFactorSetupInfo> EnableTwoFactor(int userId)
    {
        using var db = _dbFactory.CreateDbContext();

        var user = await db.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        // Remove any existing (pending or active) 2FA record
        var existing = await db.TwoFactorAuths.FirstOrDefaultAsync(t => t.Username == user.Username);
        if (existing != null)
            db.TwoFactorAuths.Remove(existing);

        var secret = GenerateSecretKey();
        var totpUri = GetTotpUri(secret, user.Email);

        // Create 2FA record in pending state (IsEnabled = false until verified)
        var twoFa = new TwoFactorAuth
        {
            Username = user.Username,
            IsEnabled = false,
            Secret = secret,
            BackupCodes = Array.Empty<string>(),
            EnabledAt = null
        };

        db.TwoFactorAuths.Add(twoFa);
        await db.SaveChangesAsync();

        _logger.LogInformation("2FA setup initiated for user {UserId}", userId);

        return new TwoFactorSetupInfo
        {
            Secret = secret,
            TotpUri = totpUri
        };
    }

    public async Task<bool> VerifyAndActivate(int userId, string code)
    {
        using var db = _dbFactory.CreateDbContext();

        var user = await db.Users.FindAsync(userId);
        if (user == null)
            return false;

        var twoFa = await db.TwoFactorAuths.FirstOrDefaultAsync(t => t.Username == user.Username);
        if (twoFa == null)
            return false;

        if (!ValidateTotpCode(twoFa.Secret, code))
            return false;

        twoFa.IsEnabled = true;
        twoFa.EnabledAt = DateTime.UtcNow;
        twoFa.BackupCodes = GenerateBackupCodesInternal();

        await db.SaveChangesAsync();

        _logger.LogInformation("2FA activated for user {UserId}", userId);
        return true;
    }

    public async Task<bool> DisableTwoFactor(int userId, string code)
    {
        using var db = _dbFactory.CreateDbContext();

        var user = await db.Users.FindAsync(userId);
        if (user == null)
            return false;

        var twoFa = await db.TwoFactorAuths.FirstOrDefaultAsync(t => t.Username == user.Username && t.IsEnabled);
        if (twoFa == null)
            return false;

        if (!ValidateTotpCode(twoFa.Secret, code))
            return false;

        db.TwoFactorAuths.Remove(twoFa);
        await db.SaveChangesAsync();

        _logger.LogInformation("2FA disabled for user {UserId}", userId);
        return true;
    }

    public async Task<bool> ValidateLogin(int userId, string code)
    {
        using var db = _dbFactory.CreateDbContext();

        var user = await db.Users.FindAsync(userId);
        if (user == null)
            return false;

        var twoFa = await db.TwoFactorAuths.FirstOrDefaultAsync(t => t.Username == user.Username && t.IsEnabled);
        if (twoFa == null)
            return false;

        return ValidateTotpCode(twoFa.Secret, code);
    }

    public async Task<string[]> GenerateRecoveryCodes(int userId)
    {
        using var db = _dbFactory.CreateDbContext();

        var user = await db.Users.FindAsync(userId);
        if (user == null)
            return Array.Empty<string>();

        var twoFa = await db.TwoFactorAuths.FirstOrDefaultAsync(t => t.Username == user.Username && t.IsEnabled);
        if (twoFa == null)
            return Array.Empty<string>();

        twoFa.BackupCodes = GenerateBackupCodesInternal();
        await db.SaveChangesAsync();

        _logger.LogInformation("Recovery codes regenerated for user {UserId}", userId);
        return twoFa.BackupCodes;
    }

    public async Task<bool> UseRecoveryCode(int userId, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        using var db = _dbFactory.CreateDbContext();

        var user = await db.Users.FindAsync(userId);
        if (user == null)
            return false;

        var twoFa = await db.TwoFactorAuths.FirstOrDefaultAsync(t => t.Username == user.Username && t.IsEnabled);
        if (twoFa == null)
            return false;

        var normalizedCode = code.Trim().Replace("-", "").Replace(" ", "");
        var index = Array.FindIndex(twoFa.BackupCodes, c => string.Equals(c, normalizedCode, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
            return false;

        // Remove the used code
        var codes = twoFa.BackupCodes.ToList();
        codes.RemoveAt(index);
        twoFa.BackupCodes = codes.ToArray();

        await db.SaveChangesAsync();

        _logger.LogInformation("Recovery code used for user {UserId}, {Remaining} codes remaining", userId, codes.Count);
        return true;
    }

    public string GetTotpUri(string base32Secret, string userEmail)
    {
        var issuer = "MyPersonalGit";
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(userEmail);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={base32Secret}&issuer={encodedIssuer}&algorithm=SHA1&digits={CodeDigits}&period={TimeStep}";
    }

    public async Task<bool> HasTwoFactorEnabled(int userId)
    {
        using var db = _dbFactory.CreateDbContext();

        var user = await db.Users.FindAsync(userId);
        if (user == null)
            return false;

        return await db.TwoFactorAuths.AnyAsync(t => t.Username == user.Username && t.IsEnabled);
    }

    // --- TOTP Algorithm (RFC 6238) ---

    private static long GetCurrentTimeCounter()
    {
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return unixTime / TimeStep;
    }

    private static string ComputeTotp(byte[] secretBytes, long timeCounter)
    {
        // Convert time counter to big-endian 8-byte array
        var timeBytes = new byte[8];
        for (int i = 7; i >= 0; i--)
        {
            timeBytes[i] = (byte)(timeCounter & 0xFF);
            timeCounter >>= 8;
        }

        // HMAC-SHA1
        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(timeBytes);

        // Dynamic truncation
        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        var otp = binary % (int)Math.Pow(10, CodeDigits);
        return otp.ToString().PadLeft(CodeDigits, '0');
    }

    // --- Base32 Encoding/Decoding ---

    private static string Base32Encode(byte[] data)
    {
        var result = new char[(data.Length * 8 + 4) / 5];
        int buffer = 0, bitsLeft = 0, index = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                bitsLeft -= 5;
                result[index++] = Base32Chars[(buffer >> bitsLeft) & 0x1F];
            }
        }

        if (bitsLeft > 0)
        {
            result[index++] = Base32Chars[(buffer << (5 - bitsLeft)) & 0x1F];
        }

        return new string(result, 0, index);
    }

    private static byte[] Base32Decode(string base32)
    {
        var cleanInput = base32.Trim().Replace("-", "").Replace(" ", "").ToUpperInvariant();
        var output = new List<byte>();
        int buffer = 0, bitsLeft = 0;

        foreach (var c in cleanInput)
        {
            var val = Base32Chars.IndexOf(c);
            if (val < 0) continue;

            buffer = (buffer << 5) | val;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                output.Add((byte)((buffer >> bitsLeft) & 0xFF));
            }
        }

        return output.ToArray();
    }

    private static string[] GenerateBackupCodesInternal()
    {
        var codes = new string[10];
        using var rng = RandomNumberGenerator.Create();
        for (int i = 0; i < 10; i++)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var num = BitConverter.ToUInt32(bytes) % 100000000;
            codes[i] = num.ToString("D8");
        }
        return codes;
    }
}
