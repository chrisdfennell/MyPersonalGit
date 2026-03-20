using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/swift")]
public class SwiftController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<SwiftController> _logger;

    public SwiftController(IPackageService packageService, IConfiguration config, ILogger<SwiftController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "swift");

    // Upload Swift package zip
    [HttpPut("{scope}/{name}/{version}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(string scope, string name, string version)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        var coordinate = $"{scope}.{name}";
        var pkgDir = Path.Combine(StorePath, scope.ToLowerInvariant(), name.ToLowerInvariant(), version);
        Directory.CreateDirectory(pkgDir);
        var filename = $"{name}-{version}.zip";
        var destPath = Path.Combine(pkgDir, filename);

        await using (var fs = new FileStream(destPath, FileMode.Create))
            await Request.Body.CopyToAsync(fs);

        var fi = new FileInfo(destPath);
        var sha = BitConverter.ToString(SHA256.HashData(await System.IO.File.ReadAllBytesAsync(destPath)))
            .Replace("-", "").ToLowerInvariant();

        // Try to extract Package.swift from the uploaded zip
        string? manifest = null;
        try
        {
            using var archive = System.IO.Compression.ZipFile.OpenRead(destPath);
            var manifestEntry = archive.Entries.FirstOrDefault(e =>
                e.FullName.EndsWith("Package.swift", StringComparison.OrdinalIgnoreCase));
            if (manifestEntry != null)
            {
                using var reader = new StreamReader(manifestEntry.Open());
                manifest = await reader.ReadToEndAsync();
                // Save manifest to disk for later retrieval
                var manifestPath = Path.Combine(pkgDir, "Package.swift");
                await System.IO.File.WriteAllTextAsync(manifestPath, manifest);
            }
        }
        catch
        {
            // Not a valid zip or no manifest, that's OK
        }

        var metadata = System.Text.Json.JsonSerializer.Serialize(new
        {
            scope,
            name,
            version,
            repositoryURL = Request.Headers["X-Swift-Package-Repository-URL"].FirstOrDefault() ?? ""
        });

        var pkg = await _packageService.CreateOrUpdatePackageAsync(
            coordinate, "swift", username, version, metadata: metadata);

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
        if (ver != null)
            await _packageService.AddPackageFileAsync(ver.Id, filename, fi.Length, sha);

        _logger.LogInformation("Swift package uploaded: {Scope}.{Name}@{Version} by {User}",
            scope, name, version, username);
        return StatusCode(201);
    }

    // Package releases list
    [HttpGet("{scope}/{name}")]
    public async Task<IActionResult> Releases(string scope, string name)
    {
        var coordinate = $"{scope}.{name}";
        var pkg = await _packageService.GetPackageAsync(coordinate, "swift");
        if (pkg == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/swift";
        var releases = pkg.Versions.OrderByDescending(v => v.CreatedAt).Select(v => new
        {
            url = $"{baseUrl}/{scope}/{name}/{v.Version}",
            id = v.Version
        }).ToArray();

        return Ok(new
        {
            releases
        });
    }

    // Specific version metadata + manifest
    [HttpGet("{scope}/{name}/{version}")]
    public async Task<IActionResult> VersionMetadata(string scope, string name, string version)
    {
        var coordinate = $"{scope}.{name}";
        var pkg = await _packageService.GetPackageAsync(coordinate, "swift");
        if (pkg == null) return NotFound();

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
        if (ver == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/swift";
        var file = ver.Files.FirstOrDefault();

        // Try to read the manifest from disk
        string? manifest = null;
        var manifestPath = Path.Combine(StorePath, scope.ToLowerInvariant(), name.ToLowerInvariant(), version, "Package.swift");
        if (System.IO.File.Exists(manifestPath))
            manifest = await System.IO.File.ReadAllTextAsync(manifestPath);

        return Ok(new
        {
            id = ver.Version,
            version = ver.Version,
            resources = new[]
            {
                new
                {
                    name = "source-archive",
                    type = "application/zip",
                    checksum = file?.Sha256 ?? ""
                }
            },
            metadata = TryParseMetadata(ver.Metadata),
            manifests = manifest != null ? new Dictionary<string, object>
            {
                ["Package.swift"] = new { toolsVersion = "", content = manifest }
            } : null
        });
    }

    // Swift package manifest
    [HttpGet("{scope}/{name}/{version}/Package.swift")]
    public async Task<IActionResult> Manifest(string scope, string name, string version)
    {
        var manifestPath = Path.Combine(StorePath, scope.ToLowerInvariant(), name.ToLowerInvariant(), version, "Package.swift");
        if (!System.IO.File.Exists(manifestPath))
            return NotFound();

        var content = await System.IO.File.ReadAllTextAsync(manifestPath);
        return Content(content, "text/x-swift");
    }

    // Resolve package identifiers from repository URL
    [HttpGet("identifiers")]
    public async Task<IActionResult> LookupIdentifiers([FromQuery] string url)
    {
        if (string.IsNullOrEmpty(url))
            return BadRequest(new { problem = "url query parameter is required" });

        // Search all Swift packages for matching repository URL
        var packages = await _packageService.GetPackagesAsync("swift");
        var identifiers = new List<string>();

        foreach (var pkg in packages)
        {
            var latestVersion = pkg.Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
            if (latestVersion?.Metadata == null) continue;

            try
            {
                var meta = System.Text.Json.JsonDocument.Parse(latestVersion.Metadata);
                if (meta.RootElement.TryGetProperty("repositoryURL", out var repoUrl))
                {
                    var repoUrlStr = repoUrl.GetString() ?? "";
                    if (repoUrlStr.Equals(url, StringComparison.OrdinalIgnoreCase) ||
                        repoUrlStr.TrimEnd('/').Equals(url.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                    {
                        identifiers.Add(pkg.Name);
                    }
                }
            }
            catch
            {
                // Skip packages with invalid metadata
            }
        }

        return Ok(new { identifiers });
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
