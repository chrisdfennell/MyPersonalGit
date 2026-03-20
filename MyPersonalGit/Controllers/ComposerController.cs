using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/composer")]
public class ComposerController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<ComposerController> _logger;

    public ComposerController(IPackageService packageService, IConfiguration config, ILogger<ComposerController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "composer");

    // Upload package — JSON body with dist URL or base64-encoded archive
    [HttpPost("")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload()
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        JsonDocument body;
        try
        {
            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            ms.Position = 0;
            body = await JsonDocument.ParseAsync(ms);
        }
        catch
        {
            return BadRequest("Invalid JSON body");
        }

        var root = body.RootElement;
        var name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
        var version = root.TryGetProperty("version", out var verProp) ? verProp.GetString() : null;
        var description = root.TryGetProperty("description", out var descProp) ? descProp.GetString() : "";

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
            return BadRequest("Missing required fields: name, version");

        // Composer package names are vendor/name
        var parts = name.Split('/', 2);
        if (parts.Length != 2)
            return BadRequest("Package name must be in vendor/name format");

        var vendor = parts[0].ToLowerInvariant();
        var packageName = parts[1].ToLowerInvariant();

        byte[]? archiveBytes = null;
        string? archiveFilename = null;

        // Check for base64-encoded archive in body
        if (root.TryGetProperty("archive", out var archiveProp))
        {
            var base64 = archiveProp.GetString();
            if (!string.IsNullOrEmpty(base64))
            {
                archiveBytes = Convert.FromBase64String(base64);
                archiveFilename = root.TryGetProperty("archive_filename", out var fnProp)
                    ? fnProp.GetString() ?? $"{packageName}-{version}.zip"
                    : $"{packageName}-{version}.zip";
            }
        }

        // Check for multipart form upload as fallback
        if (archiveBytes == null && Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            var file = form.Files.FirstOrDefault();
            if (file != null)
            {
                using var fileMs = new MemoryStream();
                await file.CopyToAsync(fileMs);
                archiveBytes = fileMs.ToArray();
                archiveFilename = file.FileName;
            }
        }

        string? sha = null;

        if (archiveBytes != null && archiveFilename != null)
        {
            // Store the archive file
            var pkgDir = Path.Combine(StorePath, vendor, packageName, version);
            Directory.CreateDirectory(pkgDir);
            var destPath = Path.Combine(pkgDir, archiveFilename);
            await System.IO.File.WriteAllBytesAsync(destPath, archiveBytes);

            sha = BitConverter.ToString(SHA256.HashData(archiveBytes))
                .Replace("-", "").ToLowerInvariant();
        }

        var metadataJson = root.GetRawText();

        var pkg = await _packageService.CreateOrUpdatePackageAsync(
            name, "composer", username, version, description, metadata: metadataJson);

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
        if (ver != null && archiveFilename != null && archiveBytes != null)
            await _packageService.AddPackageFileAsync(ver.Id, archiveFilename, archiveBytes.Length, sha);

        _logger.LogInformation("Composer package uploaded: {Name}@{Version} by {User}", name, version, username);

        return Ok(new { status = "ok", name, version });
    }

    // Root packages index (packages.json)
    [HttpGet("packages.json")]
    public async Task<IActionResult> PackagesIndex()
    {
        var packages = await _packageService.GetPackagesAsync("composer");
        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/composer";

        var providers = new Dictionary<string, object>();
        foreach (var pkg in packages)
        {
            providers[pkg.Name] = new { sha256 = pkg.Versions.LastOrDefault()?.Files.FirstOrDefault()?.Sha256 ?? "" };
        }

        return Ok(new
        {
            packages = new { },
            metadata_url = $"{baseUrl}/p2/%package%.json",
            provider_includes = providers.Count > 0 ? providers : null,
            available_packages = packages.Select(p => p.Name)
        });
    }

    // Composer v2 API — package metadata
    [HttpGet("p2/{vendor}/{name}.json")]
    public async Task<IActionResult> PackageMetadata(string vendor, string name)
    {
        var fullName = $"{vendor}/{name}";
        var pkg = await _packageService.GetPackageAsync(fullName, "composer");
        if (pkg == null)
        {
            var all = await _packageService.GetPackagesAsync("composer");
            pkg = all.FirstOrDefault(p => p.Name.Equals(fullName, StringComparison.OrdinalIgnoreCase));
        }
        if (pkg == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/composer";

        var versions = new Dictionary<string, object>();
        foreach (var ver in pkg.Versions)
        {
            var distFile = ver.Files.FirstOrDefault();
            var distInfo = distFile != null
                ? new
                {
                    type = distFile.Filename.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? "zip" : "tar",
                    url = $"{baseUrl}/files/{vendor}/{name}/{ver.Version}/{distFile.Filename}",
                    shasum = distFile.Sha256 ?? ""
                }
                : (object?)null;

            // Try to parse extra metadata
            var require = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(ver.Metadata))
            {
                try
                {
                    var meta = JsonDocument.Parse(ver.Metadata);
                    if (meta.RootElement.TryGetProperty("require", out var reqObj))
                    {
                        foreach (var dep in reqObj.EnumerateObject())
                        {
                            require[dep.Name] = dep.Value.GetString() ?? "*";
                        }
                    }
                }
                catch { /* metadata not parseable */ }
            }

            versions[ver.Version] = new
            {
                name = pkg.Name,
                version = ver.Version,
                description = pkg.Description ?? "",
                dist = distInfo,
                require = require.Count > 0 ? require : null,
            };
        }

        return Ok(new
        {
            packages = new Dictionary<string, object> { [pkg.Name] = versions }
        });
    }

    // Download archive file
    [HttpGet("files/{vendor}/{name}/{version}/{filename}")]
    public async Task<IActionResult> Download(string vendor, string name, string version, string filename)
    {
        var filePath = Path.Combine(StorePath, vendor.ToLowerInvariant(), name.ToLowerInvariant(), version, filename);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var fullName = $"{vendor}/{name}";
        await _packageService.IncrementDownloadAsync(fullName, "composer", version);

        return PhysicalFile(filePath, "application/octet-stream", filename);
    }
}
