using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/chef")]
public class ChefController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<ChefController> _logger;

    public ChefController(IPackageService packageService, IConfiguration config, ILogger<ChefController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "chef");

    // Upload cookbook
    [HttpPost("api/v1/cookbooks/{name}/{version}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(string name, string version)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        // Store the file on disk
        var diskDir = Path.Combine(StorePath, name, version);
        Directory.CreateDirectory(diskDir);
        var filename = $"{name}-{version}.tar.gz";
        var destPath = Path.Combine(diskDir, filename);

        await using (var fs = new FileStream(destPath, FileMode.Create))
            await Request.Body.CopyToAsync(fs);

        var fi = new FileInfo(destPath);
        var sha = BitConverter.ToString(SHA256.HashData(await System.IO.File.ReadAllBytesAsync(destPath)))
            .Replace("-", "").ToLowerInvariant();

        var metadata = System.Text.Json.JsonSerializer.Serialize(new
        {
            name,
            version,
            filename
        });

        var pkg = await _packageService.CreateOrUpdatePackageAsync(
            name, "chef", username, version, metadata: metadata);

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
        if (ver != null)
            await _packageService.AddPackageFileAsync(ver.Id, filename, fi.Length, sha);

        _logger.LogInformation("Chef cookbook uploaded: {Name}={Version} by {User}",
            name, version, username);

        return StatusCode(201, new { name, version, uri = $"api/packages/chef/api/v1/cookbooks/{name}/{version}" });
    }

    // Cookbook metadata with all versions
    [HttpGet("api/v1/cookbooks/{name}")]
    public async Task<IActionResult> CookbookVersions(string name)
    {
        var pkg = await _packageService.GetPackageAsync(name, "chef");
        if (pkg == null)
            return NotFound(new { error = "Cookbook not found" });

        var versions = pkg.Versions.OrderByDescending(v => v.CreatedAt).Select(v => new
        {
            version = v.Version,
            created_at = v.CreatedAt.ToString("o"),
            download_url = $"api/packages/chef/api/v1/cookbooks/{name}/{v.Version}/download"
        });

        return Ok(new
        {
            name = pkg.Name,
            owner = pkg.Owner,
            description = pkg.Description,
            downloads = pkg.Downloads,
            versions
        });
    }

    // Specific version metadata
    [HttpGet("api/v1/cookbooks/{name}/{version}")]
    public async Task<IActionResult> CookbookVersion(string name, string version)
    {
        var pkg = await _packageService.GetPackageAsync(name, "chef");
        if (pkg == null)
            return NotFound(new { error = "Cookbook not found" });

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
        if (ver == null)
            return NotFound(new { error = $"Version {version} not found" });

        var files = ver.Files.Select(f => new
        {
            filename = f.Filename,
            size = f.Size,
            sha256 = f.Sha256
        });

        return Ok(new
        {
            name = pkg.Name,
            version = ver.Version,
            created_at = ver.CreatedAt.ToString("o"),
            download_url = $"api/packages/chef/api/v1/cookbooks/{name}/{version}/download",
            files
        });
    }

    // Download cookbook archive
    [HttpGet("api/v1/cookbooks/{name}/{version}/download")]
    public async Task<IActionResult> Download(string name, string version)
    {
        var filename = $"{name}-{version}.tar.gz";
        var filePath = Path.Combine(StorePath, name, version, filename);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        await _packageService.IncrementDownloadAsync(name, "chef", version);

        return PhysicalFile(filePath, "application/gzip", filename);
    }
}
