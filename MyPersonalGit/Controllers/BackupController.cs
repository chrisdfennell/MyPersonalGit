using System.Formats.Tar;
using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MyPersonalGit.Controllers;

/// <summary>
/// Admin-only backup/restore endpoints.
/// Backup creates a tar.gz of all bare repos + SQLite database.
/// Restore accepts a tar.gz and replaces both.
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[EnableRateLimiting("api")]
public class BackupController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<BackupController> _logger;

    public BackupController(IConfiguration config, ILogger<BackupController> logger)
    {
        _config = config;
        _logger = logger;
    }

    [HttpPost("backup")]
    public async Task<IActionResult> CreateBackup()
    {
        // Auth check: only admins (set by ApiAuth middleware)
        if (HttpContext.Items["IsAdmin"] is not true)
            return Forbid();

        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        var dbPath = GetDbPath();

        var tempFile = Path.GetTempFileName();
        try
        {
            await using (var fileStream = new FileStream(tempFile, FileMode.Create))
            await using (var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal))
            await using (var tarWriter = new TarWriter(gzipStream))
            {
                // Add SQLite database
                if (System.IO.File.Exists(dbPath))
                {
                    await tarWriter.WriteEntryAsync(dbPath, "database/mypersonalgit.db");
                }

                // Add all repo directories
                if (Directory.Exists(projectRoot))
                {
                    foreach (var dir in Directory.GetDirectories(projectRoot))
                    {
                        var repoName = Path.GetFileName(dir);
                        await AddDirectoryToTar(tarWriter, dir, $"repos/{repoName}");
                    }
                }

                // Add LFS storage if present
                var lfsRoot = Path.Combine(projectRoot, ".lfs");
                if (Directory.Exists(lfsRoot))
                {
                    await AddDirectoryToTar(tarWriter, lfsRoot, "lfs");
                }
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(tempFile);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            _logger.LogInformation("Backup created: {Size} bytes", bytes.Length);
            return File(bytes, "application/gzip", $"mypersonalgit-backup-{timestamp}.tar.gz");
        }
        finally
        {
            if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile);
        }
    }

    [HttpPost("restore")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> RestoreBackup(IFormFile file)
    {
        if (HttpContext.Items["IsAdmin"] is not true)
            return Forbid();

        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        var dbPath = GetDbPath();

        var tempDir = Path.Combine(Path.GetTempPath(), $"mpg-restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Extract tar.gz to temp dir
            var tempFile = Path.Combine(tempDir, "upload.tar.gz");
            await using (var fs = new FileStream(tempFile, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }

            await using (var fileStream = new FileStream(tempFile, FileMode.Open))
            await using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
            await using (var tarReader = new TarReader(gzipStream))
            {
                while (await tarReader.GetNextEntryAsync() is { } entry)
                {
                    if (entry.EntryType == TarEntryType.Directory)
                        continue;

                    var entryName = entry.Name.Replace('\\', '/');
                    string targetPath;

                    if (entryName.StartsWith("database/"))
                    {
                        targetPath = dbPath;
                    }
                    else if (entryName.StartsWith("repos/"))
                    {
                        var relativePath = entryName.Substring("repos/".Length);
                        targetPath = Path.Combine(projectRoot, relativePath);
                    }
                    else if (entryName.StartsWith("lfs/"))
                    {
                        var relativePath = entryName.Substring("lfs/".Length);
                        targetPath = Path.Combine(projectRoot, ".lfs", relativePath);
                    }
                    else
                    {
                        continue;
                    }

                    var targetDir = Path.GetDirectoryName(targetPath)!;
                    Directory.CreateDirectory(targetDir);
                    await entry.ExtractToFileAsync(targetPath, overwrite: true);
                }
            }

            _logger.LogInformation("Backup restored from uploaded file ({Size} bytes)", file.Length);
            return Ok(new { message = "Backup restored successfully. Restart the application to apply database changes." });
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }
    }

    private string GetDbPath()
    {
        var connString = _config.GetConnectionString("Default") ?? "Data Source=/data/mypersonalgit.db";
        // Extract path from "Data Source=..." connection string
        var parts = connString.Split('=', 2);
        return parts.Length > 1 ? parts[1].Trim() : "/data/mypersonalgit.db";
    }

    private static async Task AddDirectoryToTar(TarWriter writer, string sourceDir, string tarPrefix)
    {
        foreach (var filePath in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, filePath).Replace('\\', '/');
            var entryName = $"{tarPrefix}/{relativePath}";
            await writer.WriteEntryAsync(filePath, entryName);
        }
    }
}
