using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IReleaseService
{
    Task<List<Release>> GetReleasesAsync(string repoName);
    Task<Release?> GetReleaseAsync(string repoName, int releaseId);
    Task<Release> CreateReleaseAsync(string repoName, string tagName, string title, string? body, string author, bool isDraft, bool isPrerelease);
    Task<bool> DeleteReleaseAsync(string repoName, int releaseId);
    Task<ReleaseAsset> AddAssetAsync(int releaseId, string fileName, long size, string contentType, byte[] data);
    Task<(ReleaseAsset? asset, byte[]? data)> GetAssetAsync(int assetId);
    Task<bool> DeleteAssetAsync(int assetId);
}

public class ReleaseService : IReleaseService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<ReleaseService> _logger;
    private readonly IActivityService _activityService;
    private readonly IArchiveService _archiveService;
    private readonly IConfiguration _config;
    private const string AssetStoragePath = "/data/releases";

    public ReleaseService(IDbContextFactory<AppDbContext> dbFactory, ILogger<ReleaseService> logger, IActivityService activityService, IArchiveService archiveService, IConfiguration config)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _activityService = activityService;
        _archiveService = archiveService;
        _config = config;
    }

    public async Task<List<Release>> GetReleasesAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Releases
            .Include(r => r.Assets)
            .Where(r => r.RepoName.ToLower() == repoName.ToLower())
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Release?> GetReleaseAsync(string repoName, int releaseId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Releases
            .Include(r => r.Assets)
            .FirstOrDefaultAsync(r => r.RepoName.ToLower() == repoName.ToLower() && r.Id == releaseId);
    }

    public async Task<Release> CreateReleaseAsync(string repoName, string tagName, string title, string? body, string author, bool isDraft, bool isPrerelease)
    {
        using var db = _dbFactory.CreateDbContext();

        var release = new Release
        {
            RepoName = repoName,
            TagName = tagName,
            Title = title,
            Body = body,
            Author = author,
            IsDraft = isDraft,
            IsPrerelease = isPrerelease,
            CreatedAt = DateTime.UtcNow,
            PublishedAt = isDraft ? null : DateTime.UtcNow
        };

        db.Releases.Add(release);
        await db.SaveChangesAsync();
        _logger.LogInformation("Release {Title} created for {RepoName}", title, repoName);

        // Auto-attach source code archives
        await AttachSourceArchivesAsync(release, repoName, tagName);

        await _activityService.RecordActivityAsync(author, "created_release", repoName, $"{author} released {title} ({tagName})", $"/repo/{repoName}");

        return release;
    }

    public async Task<bool> DeleteReleaseAsync(string repoName, int releaseId)
    {
        using var db = _dbFactory.CreateDbContext();
        var release = await db.Releases
            .Include(r => r.Assets)
            .FirstOrDefaultAsync(r => r.RepoName.ToLower() == repoName.ToLower() && r.Id == releaseId);

        if (release == null) return false;

        // Delete asset files
        foreach (var asset in release.Assets)
        {
            var filePath = GetAssetPath(releaseId, asset.FileName);
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        var dirPath = Path.Combine(AssetStoragePath, releaseId.ToString());
        if (Directory.Exists(dirPath)) try { Directory.Delete(dirPath, true); } catch { }

        db.Releases.Remove(release);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<ReleaseAsset> AddAssetAsync(int releaseId, string fileName, long size, string contentType, byte[] data)
    {
        using var db = _dbFactory.CreateDbContext();
        var release = await db.Releases.FindAsync(releaseId);
        if (release == null) throw new InvalidOperationException("Release not found");

        var asset = new ReleaseAsset
        {
            ReleaseId = releaseId,
            FileName = fileName,
            Size = size,
            ContentType = contentType,
            CreatedAt = DateTime.UtcNow
        };

        db.ReleaseAssets.Add(asset);
        await db.SaveChangesAsync();

        // Save file to disk
        var filePath = GetAssetPath(releaseId, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, data);

        return asset;
    }

    public async Task<(ReleaseAsset? asset, byte[]? data)> GetAssetAsync(int assetId)
    {
        using var db = _dbFactory.CreateDbContext();
        var asset = await db.ReleaseAssets.FindAsync(assetId);
        if (asset == null) return (null, null);

        var release = await db.Releases.FindAsync(asset.ReleaseId);
        if (release == null) return (null, null);

        var filePath = GetAssetPath(asset.ReleaseId, asset.FileName);
        if (!File.Exists(filePath)) return (asset, null);

        asset.DownloadCount++;
        await db.SaveChangesAsync();

        return (asset, await File.ReadAllBytesAsync(filePath));
    }

    public async Task<bool> DeleteAssetAsync(int assetId)
    {
        using var db = _dbFactory.CreateDbContext();
        var asset = await db.ReleaseAssets.FindAsync(assetId);
        if (asset == null) return false;

        var filePath = GetAssetPath(asset.ReleaseId, asset.FileName);
        if (File.Exists(filePath)) File.Delete(filePath);

        db.ReleaseAssets.Remove(asset);
        await db.SaveChangesAsync();
        return true;
    }

    private async Task AttachSourceArchivesAsync(Release release, string repoName, string tagName)
    {
        try
        {
            var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
            var repoPath = ResolveRepoPath(projectRoot, repoName);
            if (repoPath == null)
            {
                _logger.LogWarning("Could not resolve repo path for {RepoName}, skipping source archives", repoName);
                return;
            }

            var baseName = repoName.EndsWith(".git") ? repoName[..^4] : repoName;

            // Try the tag first, fall back to HEAD if the tag doesn't exist in the local repo
            string? gitRef = tagName;
            var probe = _archiveService.CreateArchive(repoPath, tagName, ArchiveFormat.Zip, out _);
            if (probe == null)
            {
                _logger.LogInformation("Tag {TagName} not found in repo, falling back to HEAD for source archives", tagName);
                gitRef = null; // null = HEAD in ArchiveService
            }
            probe?.Dispose();

            // Create zip archive
            using var zipStream = _archiveService.CreateArchive(repoPath, gitRef, ArchiveFormat.Zip, out _);
            if (zipStream != null)
            {
                var zipBytes = zipStream.ToArray();
                await AddAssetAsync(release.Id, $"{baseName}-{tagName}.zip", zipBytes.Length, "application/zip", zipBytes);
            }

            // Create tar.gz archive
            using var tarStream = _archiveService.CreateArchive(repoPath, gitRef, ArchiveFormat.TarGz, out _);
            if (tarStream != null)
            {
                var tarBytes = tarStream.ToArray();
                await AddAssetAsync(release.Id, $"{baseName}-{tagName}.tar.gz", tarBytes.Length, "application/gzip", tarBytes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to attach source archives for release {ReleaseId}", release.Id);
        }
    }

    private static string? ResolveRepoPath(string projectRoot, string repoName)
    {
        var path = Path.Combine(projectRoot, repoName);
        if (LibGit2Sharp.Repository.IsValid(path)) return path;
        if (LibGit2Sharp.Repository.IsValid(path + ".git")) return path + ".git";
        var nested = Path.Combine(path, repoName + ".git");
        if (LibGit2Sharp.Repository.IsValid(nested)) return nested;
        return null;
    }

    private static string GetAssetPath(int releaseId, string fileName)
    {
        return Path.Combine(AssetStoragePath, releaseId.ToString(), fileName);
    }
}
