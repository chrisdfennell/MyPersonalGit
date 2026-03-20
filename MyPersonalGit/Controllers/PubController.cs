using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/pub")]
public class PubController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<PubController> _logger;

    public PubController(IPackageService packageService, IConfiguration config, ILogger<PubController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "pub");

    // Upload package (multipart: file field)
    [HttpPost("api/packages/versions/new")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload()
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        var form = await Request.ReadFormAsync();
        var file = form.Files.GetFile("file");
        if (file == null)
            return BadRequest(new { error = new { message = "Missing required file field" } });

        // Save to a temp location, extract pubspec.yaml to get name + version
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tar.gz");
        await using (var fs = new FileStream(tempPath, FileMode.Create))
            await file.CopyToAsync(fs);

        // Try to extract name/version from pubspec.yaml inside the archive
        string? name = null;
        string? version = null;
        string? description = null;
        try
        {
            using var archiveStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read);
            using var gzipStream = new System.IO.Compression.GZipStream(archiveStream, System.IO.Compression.CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream);
            var content = await reader.ReadToEndAsync();
            // Simple YAML parsing for name/version
            foreach (var line in content.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("name:") && name == null)
                    name = trimmed["name:".Length..].Trim().Trim('\'', '"');
                else if (trimmed.StartsWith("version:") && version == null)
                    version = trimmed["version:".Length..].Trim().Trim('\'', '"');
                else if (trimmed.StartsWith("description:") && description == null)
                    description = trimmed["description:".Length..].Trim().Trim('\'', '"');
            }
        }
        catch
        {
            // If extraction fails, try form fields or filename
        }

        name ??= form["name"].FirstOrDefault();
        version ??= form["version"].FirstOrDefault();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
        {
            System.IO.File.Delete(tempPath);
            return BadRequest(new { error = new { message = "Could not determine package name and version" } });
        }

        // Move to permanent storage
        var pkgDir = Path.Combine(StorePath, name.ToLowerInvariant(), version);
        Directory.CreateDirectory(pkgDir);
        var filename = $"{name}-{version}.tar.gz";
        var destPath = Path.Combine(pkgDir, filename);
        System.IO.File.Move(tempPath, destPath, overwrite: true);

        var fi = new FileInfo(destPath);
        var sha = BitConverter.ToString(SHA256.HashData(await System.IO.File.ReadAllBytesAsync(destPath)))
            .Replace("-", "").ToLowerInvariant();

        var metadata = System.Text.Json.JsonSerializer.Serialize(new
        {
            name,
            version,
            description = description ?? ""
        });

        var pkg = await _packageService.CreateOrUpdatePackageAsync(
            name, "pub", username, version, description, metadata: metadata);

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
        if (ver != null)
            await _packageService.AddPackageFileAsync(ver.Id, filename, fi.Length, sha);

        _logger.LogInformation("Pub package uploaded: {Name}@{Version} by {User}", name, version, username);

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/pub";
        return Ok(new
        {
            success = new
            {
                message = "Successfully uploaded package.",
                url = $"{baseUrl}/api/packages/{name}"
            }
        });
    }

    // Package metadata — pub.dev API format
    [HttpGet("api/packages/{name}")]
    public async Task<IActionResult> Metadata(string name)
    {
        var pkg = await _packageService.GetPackageAsync(name, "pub");
        if (pkg == null) return NotFound(new { error = new { message = "Package not found" } });

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/pub";
        var latest = pkg.Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();

        var versions = pkg.Versions.OrderByDescending(v => v.CreatedAt).Select(v => new
        {
            version = v.Version,
            archive_url = $"{baseUrl}/packages/{name}/versions/{v.Version}.tar.gz",
            archive_sha256 = v.Files.FirstOrDefault()?.Sha256,
            published = v.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
            pubspec = TryParseMetadata(v.Metadata)
        }).ToArray();

        return Ok(new
        {
            name = pkg.Name,
            latest = latest != null ? new
            {
                version = latest.Version,
                archive_url = $"{baseUrl}/packages/{name}/versions/{latest.Version}.tar.gz",
                archive_sha256 = latest.Files.FirstOrDefault()?.Sha256,
                published = latest.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
                pubspec = TryParseMetadata(latest.Metadata)
            } : null,
            versions
        });
    }

    // Download package archive
    [HttpGet("packages/{name}/versions/{version}.tar.gz")]
    public async Task<IActionResult> Download(string name, string version)
    {
        var filename = $"{name}-{version}.tar.gz";
        var filePath = Path.Combine(StorePath, name.ToLowerInvariant(), version, filename);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        await _packageService.IncrementDownloadAsync(name, "pub", version);
        return PhysicalFile(filePath, "application/gzip", filename);
    }

    private static object? TryParseMetadata(string? metadata)
    {
        if (string.IsNullOrEmpty(metadata)) return null;
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<object>(metadata);
        }
        catch
        {
            return null;
        }
    }
}
