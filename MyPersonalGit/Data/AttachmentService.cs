using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IAttachmentService
{
    /// <summary>Saves an uploaded image and returns the attachment record, or null if rejected (bad type / too large).</summary>
    Task<CommentAttachment?> SaveAttachmentAsync(string fileName, string contentType, byte[] data, string uploadedBy, string? repoName = null);

    /// <summary>Returns the attachment record and its on-disk path, or null if not found.</summary>
    Task<(CommentAttachment Attachment, string FilePath)?> GetAttachmentAsync(string uuid);

    /// <summary>Public URL for an attachment.</summary>
    string GetAttachmentUrl(CommentAttachment attachment);
}

public class AttachmentService : IAttachmentService
{
    public const long MaxSizeBytes = 10 * 1024 * 1024; // 10 MB

    // Raster image formats only — SVG is excluded because it can carry scripts.
    private static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/gif"] = ".gif",
        ["image/webp"] = ".webp",
        ["image/bmp"] = ".bmp",
    };

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAdminService _adminService;
    private readonly IConfiguration _config;
    private readonly ILogger<AttachmentService> _logger;

    public AttachmentService(IDbContextFactory<AppDbContext> dbFactory, IAdminService adminService,
        IConfiguration config, ILogger<AttachmentService> logger)
    {
        _dbFactory = dbFactory;
        _adminService = adminService;
        _config = config;
        _logger = logger;
    }

    public async Task<CommentAttachment?> SaveAttachmentAsync(string fileName, string contentType, byte[] data, string uploadedBy, string? repoName = null)
    {
        if (data.Length == 0 || data.Length > MaxSizeBytes)
        {
            _logger.LogWarning("Attachment rejected: size {Size} bytes (max {Max})", data.Length, MaxSizeBytes);
            return null;
        }

        if (!AllowedContentTypes.TryGetValue(contentType, out var extension))
        {
            _logger.LogWarning("Attachment rejected: unsupported content type {ContentType}", contentType);
            return null;
        }

        var uuid = Guid.NewGuid().ToString("N");
        var dir = await GetAttachmentsDirAsync();
        Directory.CreateDirectory(dir);
        await File.WriteAllBytesAsync(Path.Combine(dir, uuid + extension), data);

        var safeName = SanitizeFileName(fileName, extension);
        var attachment = new CommentAttachment
        {
            Uuid = uuid,
            FileName = safeName,
            ContentType = contentType,
            SizeBytes = data.Length,
            UploadedBy = uploadedBy,
            RepoName = repoName,
            CreatedAt = DateTime.UtcNow
        };

        using var db = _dbFactory.CreateDbContext();
        db.CommentAttachments.Add(attachment);
        await db.SaveChangesAsync();

        _logger.LogInformation("Attachment {Uuid} ({FileName}, {Size} bytes) uploaded by {User}", uuid, safeName, data.Length, uploadedBy);
        return attachment;
    }

    public async Task<(CommentAttachment Attachment, string FilePath)?> GetAttachmentAsync(string uuid)
    {
        // The UUID is generated server-side as hex; reject anything else so it can never
        // be used for path traversal.
        if (uuid.Length != 32 || !uuid.All(Uri.IsHexDigit))
            return null;

        using var db = _dbFactory.CreateDbContext();
        var attachment = await db.CommentAttachments.FirstOrDefaultAsync(a => a.Uuid == uuid);
        if (attachment == null) return null;

        var extension = AllowedContentTypes.TryGetValue(attachment.ContentType, out var ext) ? ext : "";
        var path = Path.Combine(await GetAttachmentsDirAsync(), uuid + extension);
        if (!File.Exists(path)) return null;

        return (attachment, path);
    }

    public string GetAttachmentUrl(CommentAttachment attachment)
        => $"/attachments/{attachment.Uuid}/{Uri.EscapeDataString(attachment.FileName)}";

    private async Task<string> GetAttachmentsDirAsync()
    {
        var systemSettings = await _adminService.GetSystemSettingsAsync();
        var projectRoot = !string.IsNullOrEmpty(systemSettings.ProjectRoot)
            ? systemSettings.ProjectRoot
            : _config["Git:ProjectRoot"] ?? "/repos";
        return Path.Combine(projectRoot, ".mypersonalgit", "attachments");
    }

    private static string SanitizeFileName(string fileName, string fallbackExtension)
    {
        var name = Path.GetFileName(fileName ?? "").Trim();
        if (string.IsNullOrEmpty(name)) name = "image" + fallbackExtension;

        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return sanitized.Length > 200 ? sanitized[..200] : sanitized;
    }
}
