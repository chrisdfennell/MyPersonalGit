using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/alpine")]
public class AlpineController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<AlpineController> _logger;

    public AlpineController(IPackageService packageService, IConfiguration config, ILogger<AlpineController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "alpine");

    // Upload .apk package
    [HttpPut("{repository}/{arch}/{filename}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(string repository, string arch, string filename)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        if (!filename.EndsWith(".apk"))
            return BadRequest("Only .apk packages are supported");

        // Store the file on disk
        var diskDir = Path.Combine(StorePath, repository, arch);
        Directory.CreateDirectory(diskDir);
        var destPath = Path.Combine(diskDir, filename);

        await using (var fs = new FileStream(destPath, FileMode.Create))
            await Request.Body.CopyToAsync(fs);

        var fi = new FileInfo(destPath);
        var sha = BitConverter.ToString(SHA256.HashData(await System.IO.File.ReadAllBytesAsync(destPath)))
            .Replace("-", "").ToLowerInvariant();

        // Derive package name and version from filename (name-version-rN.apk)
        var stem = Path.GetFileNameWithoutExtension(filename);
        var lastDash = stem.LastIndexOf('-');
        var packageName = lastDash > 0 ? stem[..lastDash] : stem;
        // Try to extract version: second-to-last segment
        var namePart = packageName;
        var versionPart = "0.0.0";
        var verDash = namePart.LastIndexOf('-');
        if (verDash > 0)
        {
            var candidate = namePart[(verDash + 1)..];
            if (candidate.Length > 0 && char.IsDigit(candidate[0]))
            {
                versionPart = candidate;
                packageName = namePart[..verDash];
            }
        }

        var metadata = System.Text.Json.JsonSerializer.Serialize(new
        {
            repository,
            arch,
            filename
        });

        var pkg = await _packageService.CreateOrUpdatePackageAsync(
            packageName, "alpine", username, versionPart, metadata: metadata);

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == versionPart);
        if (ver != null)
            await _packageService.AddPackageFileAsync(ver.Id, filename, fi.Length, sha);

        _logger.LogInformation("Alpine package uploaded: {Name}={Version} ({Repo}/{Arch}) by {User}",
            packageName, versionPart, repository, arch, username);

        return StatusCode(201);
    }

    // Generate APK index (simplified text representation from DB)
    [HttpGet("{repository}/{arch}/APKINDEX.tar.gz")]
    public async Task<IActionResult> ApkIndex(string repository, string arch)
    {
        var packages = await _packageService.GetPackagesAsync("alpine");
        var lines = new List<string>();

        foreach (var pkg in packages)
        {
            foreach (var ver in pkg.Versions)
            {
                string? repo = null;
                string? pkgArch = null;
                if (!string.IsNullOrEmpty(ver.Metadata))
                {
                    try
                    {
                        var meta = System.Text.Json.JsonDocument.Parse(ver.Metadata);
                        repo = meta.RootElement.TryGetProperty("repository", out var r) ? r.GetString() : null;
                        pkgArch = meta.RootElement.TryGetProperty("arch", out var a) ? a.GetString() : null;
                    }
                    catch { /* skip invalid metadata */ }
                }
                if (!string.Equals(repo, repository, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(pkgArch, arch, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var file in ver.Files)
                {
                    lines.Add($"C:{file.Sha256 ?? ""}");
                    lines.Add($"P:{pkg.Name}");
                    lines.Add($"V:{ver.Version}");
                    lines.Add($"A:{arch}");
                    lines.Add($"S:{file.Size}");
                    lines.Add($"T:{pkg.Description ?? pkg.Name}");
                    lines.Add(""); // blank line separates entries
                }
            }
        }

        return Content(string.Join("\n", lines), "text/plain");
    }

    // Download .apk file
    [HttpGet("{repository}/{arch}/{filename}")]
    public async Task<IActionResult> Download(string repository, string arch, string filename)
    {
        var filePath = Path.Combine(StorePath, repository, arch, filename);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        // Derive package name from filename
        var stem = Path.GetFileNameWithoutExtension(filename);
        var lastDash = stem.LastIndexOf('-');
        var packageName = lastDash > 0 ? stem[..lastDash] : stem;
        var namePart = packageName;
        var verDash = namePart.LastIndexOf('-');
        string? version = null;
        if (verDash > 0)
        {
            var candidate = namePart[(verDash + 1)..];
            if (candidate.Length > 0 && char.IsDigit(candidate[0]))
            {
                version = candidate;
                packageName = namePart[..verDash];
            }
        }

        if (version != null)
            await _packageService.IncrementDownloadAsync(packageName, "alpine", version);

        return PhysicalFile(filePath, "application/octet-stream", filename);
    }
}
