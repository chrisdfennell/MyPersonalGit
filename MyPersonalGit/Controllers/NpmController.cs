using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/npm")]
public class NpmController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<NpmController> _logger;

    public NpmController(IPackageService packageService, IConfiguration config, ILogger<NpmController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "npm");

    // Publish npm package (npm sends JSON with base64 tarball embedded)
    [HttpPut("{*name}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Publish(string name)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // npm publish sends: { name, versions: { "1.0.0": { ... } }, _attachments: { "name-1.0.0.tgz": { data: "base64..." } } }
        if (!root.TryGetProperty("versions", out var versionsEl))
            return BadRequest("Missing versions field");

        string? version = null;
        string? description = null;
        foreach (var verProp in versionsEl.EnumerateObject())
        {
            version = verProp.Name;
            if (verProp.Value.TryGetProperty("description", out var descEl))
                description = descEl.GetString();
        }

        if (string.IsNullOrEmpty(version))
            return BadRequest("No version found");

        // Extract tarball from _attachments
        if (!root.TryGetProperty("_attachments", out var attachments))
            return BadRequest("Missing _attachments");

        foreach (var att in attachments.EnumerateObject())
        {
            if (!att.Value.TryGetProperty("data", out var dataEl))
                continue;

            var base64 = dataEl.GetString();
            if (string.IsNullOrEmpty(base64)) continue;

            var tarBytes = Convert.FromBase64String(base64);
            var pkgDir = Path.Combine(StorePath, name.Replace("/", "__"), version);
            Directory.CreateDirectory(pkgDir);
            var filePath = Path.Combine(pkgDir, $"{name.Replace("/", "-")}-{version}.tgz");
            await System.IO.File.WriteAllBytesAsync(filePath, tarBytes);

            var sha = BitConverter.ToString(SHA256.HashData(tarBytes)).Replace("-", "").ToLowerInvariant();
            var pkg = await _packageService.CreateOrUpdatePackageAsync(
                name, "npm", username, version, description, metadata: body);

            var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
            if (ver != null)
                await _packageService.AddPackageFileAsync(ver.Id, Path.GetFileName(filePath), tarBytes.Length, sha);

            _logger.LogInformation("npm package published: {Name}@{Version} by {User}", name, version, username);
            break;
        }

        return Ok(new { ok = true });
    }

    // Get package metadata
    [HttpGet("{*name}")]
    public async Task<IActionResult> GetMetadata(string name)
    {
        // Check if this is a tarball download: {name}/-/{tarball}
        if (name.Contains("/-/"))
        {
            var parts = name.Split("/-/", 2);
            return await DownloadTarball(parts[0], parts[1]);
        }

        var pkg = await _packageService.GetPackageAsync(name, "npm");
        if (pkg == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/npm";
        var versions = new Dictionary<string, object>();
        var distTags = new Dictionary<string, string>();
        string? latestVersion = null;

        foreach (var ver in pkg.Versions.OrderBy(v => v.CreatedAt))
        {
            latestVersion = ver.Version;
            var tarball = $"{baseUrl}/{name}/-/{name.Replace("/", "-")}-{ver.Version}.tgz";
            versions[ver.Version] = new
            {
                name = pkg.Name,
                version = ver.Version,
                description = ver.Description ?? pkg.Description ?? "",
                dist = new { tarball, shasum = ver.Files.FirstOrDefault()?.Sha256 ?? "" }
            };
        }

        if (latestVersion != null)
            distTags["latest"] = latestVersion;

        return Ok(new
        {
            name = pkg.Name,
            description = pkg.Description ?? "",
            versions,
            dist_tags = distTags,
        });
    }

    private async Task<IActionResult> DownloadTarball(string name, string tarball)
    {
        // Find the version from tarball name: {name}-{version}.tgz
        var pkg = await _packageService.GetPackageAsync(name, "npm");
        if (pkg == null) return NotFound();

        foreach (var ver in pkg.Versions)
        {
            var expectedTarball = $"{name.Replace("/", "-")}-{ver.Version}.tgz";
            if (tarball == expectedTarball)
            {
                var filePath = Path.Combine(StorePath, name.Replace("/", "__"), ver.Version, tarball);
                if (!System.IO.File.Exists(filePath)) return NotFound();
                await _packageService.IncrementDownloadAsync(name, "npm", ver.Version);
                return PhysicalFile(filePath, "application/gzip", tarball);
            }
        }

        return NotFound();
    }
}
