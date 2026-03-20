using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/debian")]
public class DebianController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<DebianController> _logger;

    public DebianController(IPackageService packageService, IConfiguration config, ILogger<DebianController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "debian");

    // Upload .deb package
    [HttpPut("pool/{distribution}/{component}/{filename}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(string distribution, string component, string filename)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        if (!filename.EndsWith(".deb"))
            return BadRequest("Only .deb packages are supported");

        // Store the file on disk
        var diskDir = Path.Combine(StorePath, "pool", distribution, component);
        Directory.CreateDirectory(diskDir);
        var destPath = Path.Combine(diskDir, filename);

        await using (var fs = new FileStream(destPath, FileMode.Create))
            await Request.Body.CopyToAsync(fs);

        var fi = new FileInfo(destPath);
        var sha = BitConverter.ToString(SHA256.HashData(await System.IO.File.ReadAllBytesAsync(destPath)))
            .Replace("-", "").ToLowerInvariant();

        // Derive package name and version from filename (name_version_arch.deb)
        var stem = Path.GetFileNameWithoutExtension(filename);
        var parts = stem.Split('_');
        var packageName = parts.Length >= 1 ? parts[0] : stem;
        var version = parts.Length >= 2 ? parts[1] : "0.0.0";

        var metadata = System.Text.Json.JsonSerializer.Serialize(new
        {
            distribution,
            component,
            architecture = parts.Length >= 3 ? parts[2] : "amd64",
            filename
        });

        var pkg = await _packageService.CreateOrUpdatePackageAsync(
            packageName, "debian", username, version, metadata: metadata);

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
        if (ver != null)
            await _packageService.AddPackageFileAsync(ver.Id, filename, fi.Length, sha);

        _logger.LogInformation("Debian package uploaded: {Name}={Version} ({Dist}/{Comp}) by {User}",
            packageName, version, distribution, component, username);

        return StatusCode(201);
    }

    // APT Packages index — generated dynamically from the database
    [HttpGet("dists/{distribution}/{component}/binary-amd64/Packages")]
    public async Task<IActionResult> PackagesIndex(string distribution, string component)
    {
        var packages = await _packageService.GetPackagesAsync("debian");
        var lines = new List<string>();

        foreach (var pkg in packages)
        {
            foreach (var ver in pkg.Versions)
            {
                // Check if this version belongs to the requested distribution/component via metadata
                string? dist = null;
                string? comp = null;
                string? arch = null;
                if (!string.IsNullOrEmpty(ver.Metadata))
                {
                    try
                    {
                        var meta = System.Text.Json.JsonDocument.Parse(ver.Metadata);
                        dist = meta.RootElement.TryGetProperty("distribution", out var d) ? d.GetString() : null;
                        comp = meta.RootElement.TryGetProperty("component", out var c) ? c.GetString() : null;
                        arch = meta.RootElement.TryGetProperty("architecture", out var a) ? a.GetString() : null;
                    }
                    catch { /* skip invalid metadata */ }
                }
                if (!string.Equals(dist, distribution, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(comp, component, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var file in ver.Files)
                {
                    lines.Add($"Package: {pkg.Name}");
                    lines.Add($"Version: {ver.Version}");
                    lines.Add($"Architecture: {arch ?? "amd64"}");
                    lines.Add($"Filename: pool/{distribution}/{component}/{file.Filename}");
                    lines.Add($"Size: {file.Size}");
                    lines.Add($"SHA256: {file.Sha256 ?? ""}");
                    lines.Add(""); // blank line separates entries
                }
            }
        }

        return Content(string.Join("\n", lines), "text/plain");
    }

    // Distribution Release file
    [HttpGet("dists/{distribution}/Release")]
    public IActionResult Release(string distribution)
    {
        var release = $@"Origin: MyPersonalGit
Label: MyPersonalGit
Suite: {distribution}
Codename: {distribution}
Architectures: amd64
Components: main
Date: {DateTime.UtcNow:R}";

        return Content(release, "text/plain");
    }

    // Download .deb file
    [HttpGet("pool/{distribution}/{component}/{filename}")]
    public async Task<IActionResult> Download(string distribution, string component, string filename)
    {
        var filePath = Path.Combine(StorePath, "pool", distribution, component, filename);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        // Derive package name from filename
        var stem = Path.GetFileNameWithoutExtension(filename);
        var parts = stem.Split('_');
        var packageName = parts.Length >= 1 ? parts[0] : stem;
        var version = parts.Length >= 2 ? parts[1] : null;

        if (version != null)
            await _packageService.IncrementDownloadAsync(packageName, "debian", version);

        return PhysicalFile(filePath, "application/vnd.debian.binary-package", filename);
    }
}
