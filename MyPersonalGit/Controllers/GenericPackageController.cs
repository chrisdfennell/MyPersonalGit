using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/generic")]
public class GenericPackageController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<GenericPackageController> _logger;

    public GenericPackageController(IPackageService packageService, IConfiguration config, ILogger<GenericPackageController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "generic");

    // Upload a generic package file
    [HttpPut("{name}/{version}/{filename}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(string name, string version, string filename)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        var dir = Path.Combine(StorePath, name, version);
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, filename);

        await using (var fs = new FileStream(filePath, FileMode.Create))
            await Request.Body.CopyToAsync(fs);

        var fi = new FileInfo(filePath);
        var sha = BitConverter.ToString(SHA256.HashData(await System.IO.File.ReadAllBytesAsync(filePath))).Replace("-", "").ToLowerInvariant();

        var pkg = await _packageService.CreateOrUpdatePackageAsync(name, "generic", username, version);
        var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
        if (ver != null)
            await _packageService.AddPackageFileAsync(ver.Id, filename, fi.Length, sha);

        _logger.LogInformation("Generic package uploaded: {Name}/{Version}/{File} by {User}", name, version, filename, username);
        return StatusCode(201, new { message = "Package uploaded successfully" });
    }

    // Download
    [HttpGet("{name}/{version}/{filename}")]
    public async Task<IActionResult> Download(string name, string version, string filename)
    {
        var filePath = Path.Combine(StorePath, name, version, filename);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        await _packageService.IncrementDownloadAsync(name, "generic", version);
        return PhysicalFile(filePath, "application/octet-stream", filename);
    }

    // List versions
    [HttpGet("{name}")]
    public async Task<IActionResult> ListVersions(string name)
    {
        var pkg = await _packageService.GetPackageAsync(name, "generic");
        if (pkg == null) return NotFound();

        return Ok(new
        {
            name = pkg.Name,
            owner = pkg.Owner,
            downloads = pkg.Downloads,
            versions = pkg.Versions.OrderByDescending(v => v.CreatedAt).Select(v => new
            {
                version = v.Version,
                size = v.Size,
                downloads = v.Downloads,
                createdAt = v.CreatedAt,
                files = v.Files.Select(f => new { f.Filename, f.Size, f.Sha256 })
            })
        });
    }

    // Delete
    [HttpDelete("{name}/{version}/{filename}")]
    public async Task<IActionResult> Delete(string name, string version, string filename)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        var filePath = Path.Combine(StorePath, name, version, filename);
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        await _packageService.DeletePackageVersionAsync(name, "generic", version);
        return Ok(new { message = "Deleted" });
    }
}
