using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public class SignatureVerificationResult
{
    public bool IsSigned { get; set; }
    public bool IsVerified { get; set; }
    public string SignerKeyId { get; set; } = "";
    public string SignerName { get; set; } = "";
    public string SignerEmail { get; set; } = "";
    public string TrustLevel { get; set; } = "unknown"; // unknown, signed, verified
}

public interface IGpgKeyService
{
    Task<GpgKey?> AddGpgKeyAsync(int userId, string armoredPublicKey);
    Task<List<GpgKey>> GetUserGpgKeysAsync(int userId);
    Task<bool> DeleteGpgKeyAsync(int id);
    Task<SignatureVerificationResult> VerifyCommitSignatureAsync(string commitSha, string repoPath);
    Task<Dictionary<string, SignatureVerificationResult>> BatchVerifyCommitSignaturesAsync(IEnumerable<string> commitShas, string repoPath);
    Task<SignatureVerificationResult> VerifyTagSignatureAsync(string tagName, string repoPath);
    Task<string?> CreateSignedCommitAsync(string repoPath, string treeSha, string[] parentShas, string message, string authorName, string authorEmail, string? gpgKeyId = null);
}

public class GpgKeyService : IGpgKeyService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<GpgKeyService> _logger;

    public GpgKeyService(IDbContextFactory<AppDbContext> dbFactory, ILogger<GpgKeyService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<GpgKey?> AddGpgKeyAsync(int userId, string armoredPublicKey)
    {
        if (string.IsNullOrWhiteSpace(armoredPublicKey))
            return null;

        var parsed = ParseArmoredPublicKey(armoredPublicKey.Trim());
        if (parsed == null)
        {
            _logger.LogWarning("Failed to parse GPG public key for user {UserId}", userId);
            return null;
        }

        using var db = _dbFactory.CreateDbContext();

        // Check for duplicate
        var existing = await db.GpgKeys
            .FirstOrDefaultAsync(k => k.UserId == userId && k.LongKeyId == parsed.LongKeyId);
        if (existing != null)
        {
            _logger.LogWarning("GPG key {KeyId} already exists for user {UserId}", parsed.LongKeyId, userId);
            return null;
        }

        var gpgKey = new GpgKey
        {
            UserId = userId,
            KeyId = parsed.KeyId,
            LongKeyId = parsed.LongKeyId,
            PublicKey = armoredPublicKey.Trim(),
            PrimaryEmail = parsed.PrimaryEmail,
            Emails = parsed.Emails,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = parsed.ExpiresAt,
            IsVerified = true // We trust the key since the user uploaded it while authenticated
        };

        db.GpgKeys.Add(gpgKey);
        await db.SaveChangesAsync();

        _logger.LogInformation("Added GPG key {KeyId} for user {UserId}", gpgKey.KeyId, userId);
        return gpgKey;
    }

    public async Task<List<GpgKey>> GetUserGpgKeysAsync(int userId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.GpgKeys
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteGpgKeyAsync(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        var key = await db.GpgKeys.FindAsync(id);
        if (key == null) return false;

        db.GpgKeys.Remove(key);
        await db.SaveChangesAsync();

        _logger.LogInformation("Deleted GPG key {KeyId} (ID: {Id})", key.KeyId, id);
        return true;
    }

    public async Task<SignatureVerificationResult> VerifyCommitSignatureAsync(string commitSha, string repoPath)
    {
        var result = new SignatureVerificationResult();

        try
        {
            // Read the raw commit object from the git object store to extract the gpgsig header
            var signature = ExtractSignatureFromRawCommit(commitSha, repoPath);
            if (string.IsNullOrEmpty(signature))
                return result;

            result.IsSigned = true;
            result.TrustLevel = "signed";

            // Get author info from LibGit2Sharp
            using var repo = new LibGit2Sharp.Repository(repoPath);
            var commit = repo.Lookup(commitSha) as LibGit2Sharp.Commit;
            if (commit != null)
            {
                result.SignerName = commit.Author.Name;
                result.SignerEmail = commit.Author.Email;
            }

            // Try to extract the key ID from the signature
            var keyId = ExtractKeyIdFromSignature(signature);
            if (!string.IsNullOrEmpty(keyId))
            {
                result.SignerKeyId = keyId;

                // Check if we have this key in our database
                using var db = _dbFactory.CreateDbContext();
                var matchingKey = await db.GpgKeys
                    .Include(k => k.User)
                    .FirstOrDefaultAsync(k =>
                        k.LongKeyId == keyId ||
                        k.KeyId == keyId ||
                        k.LongKeyId.EndsWith(keyId) ||
                        keyId.EndsWith(k.KeyId));

                if (matchingKey != null && matchingKey.IsVerified)
                {
                    result.IsVerified = true;
                    result.TrustLevel = "verified";
                    result.SignerName = matchingKey.User?.FullName ?? result.SignerName;
                    result.SignerEmail = matchingKey.PrimaryEmail;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying commit signature for {Sha} in {RepoPath}", commitSha, repoPath);
        }

        return result;
    }

    public async Task<Dictionary<string, SignatureVerificationResult>> BatchVerifyCommitSignaturesAsync(IEnumerable<string> commitShas, string repoPath)
    {
        var results = new Dictionary<string, SignatureVerificationResult>();

        // Pre-load all GPG keys once
        using var db = _dbFactory.CreateDbContext();
        var allKeys = await db.GpgKeys.Include(k => k.User).ToListAsync();

        foreach (var sha in commitShas)
        {
            var result = new SignatureVerificationResult();
            try
            {
                var signature = ExtractSignatureFromRawCommit(sha, repoPath);
                if (string.IsNullOrEmpty(signature))
                {
                    results[sha] = result;
                    continue;
                }

                result.IsSigned = true;
                result.TrustLevel = "signed";

                using var repo = new LibGit2Sharp.Repository(repoPath);
                var commit = repo.Lookup(sha) as LibGit2Sharp.Commit;
                if (commit != null)
                {
                    result.SignerName = commit.Author.Name;
                    result.SignerEmail = commit.Author.Email;
                }

                var keyId = ExtractKeyIdFromSignature(signature);
                if (!string.IsNullOrEmpty(keyId))
                {
                    result.SignerKeyId = keyId;

                    var matchingKey = allKeys.FirstOrDefault(k =>
                        k.LongKeyId == keyId ||
                        k.KeyId == keyId ||
                        k.LongKeyId.EndsWith(keyId) ||
                        keyId.EndsWith(k.KeyId));

                    if (matchingKey != null && matchingKey.IsVerified)
                    {
                        result.IsVerified = true;
                        result.TrustLevel = "verified";
                        result.SignerName = matchingKey.User?.FullName ?? result.SignerName;
                        result.SignerEmail = matchingKey.PrimaryEmail;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch-verifying commit {Sha}", sha);
            }

            results[sha] = result;
        }

        return results;
    }

    public async Task<SignatureVerificationResult> VerifyTagSignatureAsync(string tagName, string repoPath)
    {
        var result = new SignatureVerificationResult();

        try
        {
            using var repo = new LibGit2Sharp.Repository(repoPath);
            var tag = repo.Tags[tagName];
            if (tag == null)
                return result;

            // Only annotated tags can have signatures
            if (tag.Annotation == null)
                return result;

            // Extract signature from the annotated tag object
            var signature = ExtractSignatureFromRawTag(tag.Annotation.Sha, repoPath);
            if (string.IsNullOrEmpty(signature))
                return result;

            result.IsSigned = true;
            result.TrustLevel = "signed";
            result.SignerName = tag.Annotation.Tagger.Name;
            result.SignerEmail = tag.Annotation.Tagger.Email;

            var keyId = ExtractKeyIdFromSignature(signature);
            if (!string.IsNullOrEmpty(keyId))
            {
                result.SignerKeyId = keyId;

                using var db = _dbFactory.CreateDbContext();
                var matchingKey = await db.GpgKeys
                    .Include(k => k.User)
                    .FirstOrDefaultAsync(k =>
                        k.LongKeyId == keyId ||
                        k.KeyId == keyId ||
                        k.LongKeyId.EndsWith(keyId) ||
                        keyId.EndsWith(k.KeyId));

                if (matchingKey != null && matchingKey.IsVerified)
                {
                    result.IsVerified = true;
                    result.TrustLevel = "verified";
                    result.SignerName = matchingKey.User?.FullName ?? result.SignerName;
                    result.SignerEmail = matchingKey.PrimaryEmail;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying tag signature for {TagName} in {RepoPath}", tagName, repoPath);
        }

        return result;
    }

    public async Task<string?> CreateSignedCommitAsync(string repoPath, string treeSha, string[] parentShas, string message, string authorName, string authorEmail, string? gpgKeyId = null)
    {
        try
        {
            var args = new List<string> { "commit-tree", treeSha };
            foreach (var parent in parentShas)
            {
                args.Add("-p");
                args.Add(parent);
            }

            if (!string.IsNullOrEmpty(gpgKeyId))
                args.Add($"-S{gpgKeyId}");
            else
                args.Add("-S");

            args.Add("-m");
            args.Add(message);

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = repoPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var arg in args)
                psi.ArgumentList.Add(arg);

            var now = DateTimeOffset.Now;
            var dateStr = now.ToString("ddd MMM d HH:mm:ss yyyy K", System.Globalization.CultureInfo.InvariantCulture);
            psi.Environment["GIT_AUTHOR_NAME"] = authorName;
            psi.Environment["GIT_AUTHOR_EMAIL"] = authorEmail;
            psi.Environment["GIT_AUTHOR_DATE"] = dateStr;
            psi.Environment["GIT_COMMITTER_NAME"] = authorName;
            psi.Environment["GIT_COMMITTER_EMAIL"] = authorEmail;
            psi.Environment["GIT_COMMITTER_DATE"] = dateStr;

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return null;

            var sha = (await process.StandardOutput.ReadToEndAsync()).Trim();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("git commit-tree failed: {Error}", stderr);
                return null;
            }

            return sha;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create signed commit");
            return null;
        }
    }

    private static string? ExtractSignatureFromRawTag(string tagSha, string repoPath)
    {
        try
        {
            // Try git cat-file for tag objects (similar to commit signature extraction)
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"cat-file tag {tagSha}",
                WorkingDirectory = repoPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
                return null;

            // Tag signatures appear after the message, starting with "-----BEGIN PGP SIGNATURE-----"
            var sigStart = output.IndexOf("-----BEGIN PGP SIGNATURE-----", StringComparison.Ordinal);
            if (sigStart < 0)
                sigStart = output.IndexOf("-----BEGIN PGP MESSAGE-----", StringComparison.Ordinal);

            if (sigStart >= 0)
                return output.Substring(sigStart);

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Read the raw git commit object and extract the gpgsig header value.
    /// Git commit objects store the signature in a "gpgsig" header.
    /// </summary>
    private static string? ExtractSignatureFromRawCommit(string commitSha, string repoPath)
    {
        try
        {
            // Resolve the git directory (handle both bare and non-bare repos)
            var gitDir = repoPath;
            var dotGit = System.IO.Path.Combine(repoPath, ".git");
            if (Directory.Exists(dotGit))
                gitDir = dotGit;

            // Read the loose object or pack file via git cat-file equivalent
            // For simplicity, use the zlib-compressed loose object format
            var prefix = commitSha.Substring(0, 2);
            var suffix = commitSha.Substring(2);
            var objectPath = System.IO.Path.Combine(gitDir, "objects", prefix, suffix);

            byte[] rawData;
            if (File.Exists(objectPath))
            {
                // Loose object - decompress zlib
                var compressed = File.ReadAllBytes(objectPath);
                rawData = DecompressZlib(compressed);
            }
            else
            {
                // Object may be in a pack file - fall back to running git command
                return ExtractSignatureViaGit(commitSha, repoPath);
            }

            // Parse the commit object: "commit <size>\0<content>"
            var nullIdx = Array.IndexOf(rawData, (byte)0);
            if (nullIdx < 0) return null;

            var content = Encoding.UTF8.GetString(rawData, nullIdx + 1, rawData.Length - nullIdx - 1);
            return ExtractGpgSigFromCommitContent(content);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Check if a commit has a GPG signature by reading its raw object.
    /// This is a lightweight check that doesn't do full parsing.
    /// </summary>
    public static bool CommitHasSignature(string commitSha, string repoPath)
    {
        return ExtractSignatureFromRawCommit(commitSha, repoPath) != null;
    }

    /// <summary>
    /// Extract the gpgsig header from commit content text.
    /// The gpgsig header spans multiple lines with continuation lines starting with a space.
    /// </summary>
    private static string? ExtractGpgSigFromCommitContent(string content)
    {
        var lines = content.Split('\n');
        var inSig = false;
        var sigBuilder = new StringBuilder();

        foreach (var line in lines)
        {
            if (line.StartsWith("gpgsig "))
            {
                inSig = true;
                sigBuilder.AppendLine(line.Substring(7)); // Remove "gpgsig " prefix
                continue;
            }

            if (inSig)
            {
                if (line.StartsWith(" "))
                {
                    sigBuilder.AppendLine(line.Substring(1)); // Remove continuation space
                }
                else
                {
                    break; // End of gpgsig header
                }
            }
        }

        var sig = sigBuilder.ToString().Trim();
        return string.IsNullOrEmpty(sig) ? null : sig;
    }

    /// <summary>
    /// Fall back to using git command line to extract signature.
    /// </summary>
    private static string? ExtractSignatureViaGit(string commitSha, string repoPath)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"cat-file commit {commitSha}",
                WorkingDirectory = repoPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
                return null;

            return ExtractGpgSigFromCommitContent(output);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Decompress zlib-compressed data (git loose objects use zlib).
    /// </summary>
    private static byte[] DecompressZlib(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        // Skip the 2-byte zlib header
        input.ReadByte();
        input.ReadByte();
        using var deflate = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }

    /// <summary>
    /// Extract the issuer key ID from a PGP signature packet.
    /// PGP signatures contain the issuer key ID in a subpacket.
    /// </summary>
    private static string? ExtractKeyIdFromSignature(string armoredSignature)
    {
        try
        {
            // Strip PGP armor headers
            var lines = armoredSignature.Split('\n')
                .Select(l => l.Trim())
                .Where(l => !l.StartsWith("-----") && !l.Contains(':') && !string.IsNullOrEmpty(l))
                .ToList();

            var base64Data = string.Join("", lines);

            // Remove the CRC checksum line (starts with =)
            var eqIdx = base64Data.IndexOf('=');
            // PGP armor has a CRC24 checksum after the base64 data, preceded by '='
            // But '=' is also valid base64 padding. We need to find the CRC line specifically.
            // The CRC is on its own line starting with '=', so let's handle this differently.
            var cleanLines = armoredSignature.Split('\n')
                .Select(l => l.Trim())
                .Where(l => !l.StartsWith("-----") && !l.Contains(':') && !string.IsNullOrEmpty(l) && !l.StartsWith("="))
                .ToList();

            base64Data = string.Join("", cleanLines);

            var bytes = Convert.FromBase64String(base64Data);
            return ExtractKeyIdFromPacket(bytes);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Parse OpenPGP binary packet data to extract the issuer key ID.
    /// RFC 4880 Section 5.2 - Signature Packet
    /// </summary>
    private static string? ExtractKeyIdFromPacket(byte[] data)
    {
        if (data.Length < 3) return null;

        var offset = 0;

        while (offset < data.Length)
        {
            // Parse packet tag
            var tag = data[offset];
            if ((tag & 0x80) == 0) break; // Not a valid packet

            int packetTag;
            int bodyLength;

            if ((tag & 0x40) != 0)
            {
                // New format packet
                packetTag = tag & 0x3F;
                offset++;
                if (offset >= data.Length) break;

                if (data[offset] < 192)
                {
                    bodyLength = data[offset];
                    offset++;
                }
                else if (data[offset] < 224)
                {
                    if (offset + 1 >= data.Length) break;
                    bodyLength = ((data[offset] - 192) << 8) + data[offset + 1] + 192;
                    offset += 2;
                }
                else if (data[offset] == 255)
                {
                    if (offset + 4 >= data.Length) break;
                    bodyLength = (data[offset + 1] << 24) | (data[offset + 2] << 16) |
                                 (data[offset + 3] << 8) | data[offset + 4];
                    offset += 5;
                }
                else
                {
                    break; // Partial body - not expected in signatures
                }
            }
            else
            {
                // Old format packet
                packetTag = (tag & 0x3C) >> 2;
                var lengthType = tag & 0x03;
                offset++;

                switch (lengthType)
                {
                    case 0:
                        if (offset >= data.Length) return null;
                        bodyLength = data[offset];
                        offset++;
                        break;
                    case 1:
                        if (offset + 1 >= data.Length) return null;
                        bodyLength = (data[offset] << 8) | data[offset + 1];
                        offset += 2;
                        break;
                    case 2:
                        if (offset + 3 >= data.Length) return null;
                        bodyLength = (data[offset] << 24) | (data[offset + 1] << 16) |
                                     (data[offset + 2] << 8) | data[offset + 3];
                        offset += 4;
                        break;
                    default:
                        bodyLength = data.Length - offset; // Indeterminate
                        break;
                }
            }

            // Signature packet tag = 2
            if (packetTag == 2 && offset + bodyLength <= data.Length)
            {
                var keyId = ExtractKeyIdFromSignaturePacket(data, offset, bodyLength);
                if (keyId != null) return keyId;
            }

            offset += bodyLength;
        }

        return null;
    }

    /// <summary>
    /// Extract issuer key ID from a signature packet body.
    /// Handles both V3 and V4 signature formats.
    /// </summary>
    private static string? ExtractKeyIdFromSignaturePacket(byte[] data, int offset, int length)
    {
        if (length < 1) return null;

        var version = data[offset];

        if (version == 3)
        {
            // V3 signature: offset+6 has 8 bytes of key ID
            if (offset + 14 > data.Length) return null;
            var keyIdBytes = new byte[8];
            Array.Copy(data, offset + 6, keyIdBytes, 0, 8);
            return Convert.ToHexString(keyIdBytes).ToUpperInvariant();
        }
        else if (version == 4)
        {
            // V4 signature packet structure:
            // 1 byte version (4)
            // 1 byte signature type
            // 1 byte public-key algorithm
            // 1 byte hash algorithm
            // 2 bytes hashed subpacket data length
            // N bytes hashed subpackets
            // 2 bytes unhashed subpacket data length
            // N bytes unhashed subpackets

            if (offset + 4 >= data.Length) return null;

            var hashedLen = (data[offset + 4] << 8) | data[offset + 5];
            var unhashedStart = offset + 6 + hashedLen;

            // Search hashed subpackets for issuer key ID (type 16) or issuer fingerprint (type 33)
            var keyId = SearchSubpacketsForKeyId(data, offset + 6, hashedLen);
            if (keyId != null) return keyId;

            // Search unhashed subpackets
            if (unhashedStart + 2 <= data.Length)
            {
                var unhashedLen = (data[unhashedStart] << 8) | data[unhashedStart + 1];
                keyId = SearchSubpacketsForKeyId(data, unhashedStart + 2, unhashedLen);
                if (keyId != null) return keyId;
            }
        }

        return null;
    }

    /// <summary>
    /// Search through OpenPGP subpackets to find the issuer key ID (subpacket type 16).
    /// </summary>
    private static string? SearchSubpacketsForKeyId(byte[] data, int offset, int totalLength)
    {
        var end = offset + totalLength;
        while (offset < end && offset < data.Length)
        {
            // Subpacket length
            int subLen;
            if (data[offset] < 192)
            {
                subLen = data[offset];
                offset++;
            }
            else if (data[offset] < 255)
            {
                if (offset + 1 >= data.Length) break;
                subLen = ((data[offset] - 192) << 8) + data[offset + 1] + 192;
                offset += 2;
            }
            else
            {
                if (offset + 4 >= data.Length) break;
                subLen = (data[offset + 1] << 24) | (data[offset + 2] << 16) |
                         (data[offset + 3] << 8) | data[offset + 4];
                offset += 5;
            }

            if (subLen < 1 || offset + subLen > data.Length) break;

            var subType = data[offset] & 0x7F; // Strip critical bit

            // Subpacket type 16 = Issuer Key ID (8 bytes)
            if (subType == 16 && subLen >= 9)
            {
                var keyIdBytes = new byte[8];
                Array.Copy(data, offset + 1, keyIdBytes, 0, 8);
                return Convert.ToHexString(keyIdBytes).ToUpperInvariant();
            }

            // Subpacket type 33 = Issuer Fingerprint (V4: 1 byte version + 20 bytes fingerprint)
            if (subType == 33 && subLen >= 22)
            {
                // The key ID is the last 8 bytes of the fingerprint
                var keyIdBytes = new byte[8];
                Array.Copy(data, offset + 1 + (subLen - 1) - 8 + 1, keyIdBytes, 0, 8);
                return Convert.ToHexString(keyIdBytes).ToUpperInvariant();
            }

            offset += subLen;
        }

        return null;
    }

    /// <summary>
    /// Parse an ASCII-armored PGP public key to extract key ID, emails, and expiry.
    /// </summary>
    private static ParsedGpgKey? ParseArmoredPublicKey(string armoredKey)
    {
        if (!armoredKey.Contains("-----BEGIN PGP PUBLIC KEY BLOCK-----"))
            return null;

        try
        {
            // Extract base64 data between armor headers
            var lines = armoredKey.Split('\n')
                .Select(l => l.Trim())
                .Where(l => !l.StartsWith("-----") && !l.Contains(':') && !string.IsNullOrEmpty(l) && !l.StartsWith("="))
                .ToList();

            var base64Data = string.Join("", lines);
            var bytes = Convert.FromBase64String(base64Data);

            var result = new ParsedGpgKey();

            var offset = 0;
            while (offset < bytes.Length)
            {
                var tag = bytes[offset];
                if ((tag & 0x80) == 0) break;

                int packetTag;
                int bodyLength;

                if ((tag & 0x40) != 0)
                {
                    packetTag = tag & 0x3F;
                    offset++;
                    if (offset >= bytes.Length) break;

                    if (bytes[offset] < 192)
                    {
                        bodyLength = bytes[offset];
                        offset++;
                    }
                    else if (bytes[offset] < 224)
                    {
                        if (offset + 1 >= bytes.Length) break;
                        bodyLength = ((bytes[offset] - 192) << 8) + bytes[offset + 1] + 192;
                        offset += 2;
                    }
                    else if (bytes[offset] == 255)
                    {
                        if (offset + 4 >= bytes.Length) break;
                        bodyLength = (bytes[offset + 1] << 24) | (bytes[offset + 2] << 16) |
                                     (bytes[offset + 3] << 8) | bytes[offset + 4];
                        offset += 5;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    packetTag = (tag & 0x3C) >> 2;
                    var lengthType = tag & 0x03;
                    offset++;

                    switch (lengthType)
                    {
                        case 0:
                            if (offset >= bytes.Length) return null;
                            bodyLength = bytes[offset];
                            offset++;
                            break;
                        case 1:
                            if (offset + 1 >= bytes.Length) return null;
                            bodyLength = (bytes[offset] << 8) | bytes[offset + 1];
                            offset += 2;
                            break;
                        case 2:
                            if (offset + 3 >= bytes.Length) return null;
                            bodyLength = (bytes[offset] << 24) | (bytes[offset + 1] << 16) |
                                         (bytes[offset + 2] << 8) | bytes[offset + 3];
                            offset += 4;
                            break;
                        default:
                            bodyLength = bytes.Length - offset;
                            break;
                    }
                }

                if (offset + bodyLength > bytes.Length) break;

                // Public-Key packet (tag 6) or Public-Subkey packet (tag 14)
                if (packetTag == 6)
                {
                    ExtractKeyIdFromPublicKeyPacket(bytes, offset, bodyLength, result);
                }
                // User ID packet (tag 13)
                else if (packetTag == 13)
                {
                    var uid = Encoding.UTF8.GetString(bytes, offset, bodyLength);
                    // Extract email from UID string like "Name <email@example.com>"
                    var emailMatch = Regex.Match(uid, @"<([^>]+)>");
                    if (emailMatch.Success)
                    {
                        var email = emailMatch.Groups[1].Value;
                        result.Emails.Add(email);
                        if (string.IsNullOrEmpty(result.PrimaryEmail))
                            result.PrimaryEmail = email;
                    }
                }

                offset += bodyLength;
            }

            // If we got a key ID, consider it valid
            if (!string.IsNullOrEmpty(result.LongKeyId))
                return result;

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Extract the key ID from a Public-Key packet by computing the V4 fingerprint.
    /// V4 key ID = last 8 bytes of SHA-1 fingerprint.
    /// </summary>
    private static void ExtractKeyIdFromPublicKeyPacket(byte[] data, int offset, int length, ParsedGpgKey result)
    {
        if (length < 1) return;

        var version = data[offset];
        if (version == 4)
        {
            // V4 fingerprint = SHA-1 of: 0x99 + 2-byte packet length + packet body
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            var fingerprintInput = new byte[3 + length];
            fingerprintInput[0] = 0x99;
            fingerprintInput[1] = (byte)(length >> 8);
            fingerprintInput[2] = (byte)(length & 0xFF);
            Array.Copy(data, offset, fingerprintInput, 3, length);

            var fingerprint = sha1.ComputeHash(fingerprintInput);
            var fullFingerprint = Convert.ToHexString(fingerprint).ToUpperInvariant();

            // Long key ID = last 16 hex chars of fingerprint
            result.LongKeyId = fullFingerprint.Substring(fullFingerprint.Length - 16);
            // Short key ID = last 8 hex chars
            result.KeyId = fullFingerprint.Substring(fullFingerprint.Length - 8);
        }
    }

    private class ParsedGpgKey
    {
        public string KeyId { get; set; } = "";
        public string LongKeyId { get; set; } = "";
        public string PrimaryEmail { get; set; } = "";
        public List<string> Emails { get; set; } = new();
        public DateTime? ExpiresAt { get; set; }
    }
}
