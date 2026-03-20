using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/cran")]
public class CranController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<CranController> _logger;

    public CranController(IPackageService packageService, IConfiguration config, ILogger<CranController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "cran");

    // Upload R source package (.tar.gz)
    [HttpPut("src/contrib/{filename}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(string filename)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        if (!filename.EndsWith(".tar.gz"))
            return BadRequest("Only .tar.gz source packages are supported");

        // Store the file on disk
        var diskDir = Path.Combine(StorePath, "src", "contrib");
        Directory.CreateDirectory(diskDir);
        var destPath = Path.Combine(diskDir, filename);

        await using (var fs = new FileStream(destPath, FileMode.Create))
            await Request.Body.CopyToAsync(fs);

        var fi = new FileInfo(destPath);
        var sha = BitConverter.ToString(SHA256.HashData(await System.IO.File.ReadAllBytesAsync(destPath)))
            .Replace("-", "").ToLowerInvariant();

        // Derive package name and version from filename (Package_Version.tar.gz)
        var stem = filename.Replace(".tar.gz", "");
        var underscoreIdx = stem.IndexOf('_');
        var packageName = underscoreIdx >= 0 ? stem[..underscoreIdx] : stem;
        var version = underscoreIdx >= 0 ? stem[(underscoreIdx + 1)..] : "0.0.0";

        var metadata = System.Text.Json.JsonSerializer.Serialize(new
        {
            package = packageName,
            version,
            filename
        });

        var pkg = await _packageService.CreateOrUpdatePackageAsync(
            packageName, "cran", username, version, metadata: metadata);

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
        if (ver != null)
            await _packageService.AddPackageFileAsync(ver.Id, filename, fi.Length, sha);

        _logger.LogInformation("CRAN package uploaded: {Name}_{Version} by {User}",
            packageName, version, username);

        return StatusCode(201);
    }

    // PACKAGES index file — generated dynamically from the database
    [HttpGet("src/contrib/PACKAGES")]
    public async Task<IActionResult> PackagesIndex()
    {
        var packages = await _packageService.GetPackagesAsync("cran");
        var lines = new List<string>();

        foreach (var pkg in packages)
        {
            // Use the latest version for each package
            var latest = pkg.Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
            if (latest == null) continue;

            lines.Add($"Package: {pkg.Name}");
            lines.Add($"Version: {latest.Version}");

            // Extract Depends from metadata if available
            string? depends = null;
            if (!string.IsNullOrEmpty(latest.Metadata))
            {
                try
                {
                    var meta = System.Text.Json.JsonDocument.Parse(latest.Metadata);
                    depends = meta.RootElement.TryGetProperty("depends", out var d) ? d.GetString() : null;
                }
                catch { /* skip invalid metadata */ }
            }
            if (!string.IsNullOrEmpty(depends))
                lines.Add($"Depends: {depends}");

            lines.Add(""); // blank line separates entries
        }

        return Content(string.Join("\n", lines), "text/plain");
    }

    // Download source package
    [HttpGet("src/contrib/{filename}")]
    public async Task<IActionResult> Download(string filename)
    {
        var filePath = Path.Combine(StorePath, "src", "contrib", filename);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        // Derive package name and version from filename
        var stem = filename.Replace(".tar.gz", "");
        var underscoreIdx = stem.IndexOf('_');
        var packageName = underscoreIdx >= 0 ? stem[..underscoreIdx] : stem;
        var version = underscoreIdx >= 0 ? stem[(underscoreIdx + 1)..] : null;

        if (version != null)
            await _packageService.IncrementDownloadAsync(packageName, "cran", version);

        return PhysicalFile(filePath, "application/gzip", filename);
    }
}
