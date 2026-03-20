using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/vagrant")]
public class VagrantController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<VagrantController> _logger;

    public VagrantController(IPackageService packageService, IConfiguration config, ILogger<VagrantController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "vagrant");

    // Upload .box file
    [HttpPut("{name}/{version}/{provider}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(string name, string version, string provider)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        // Store the .box file on disk
        var boxDir = Path.Combine(StorePath, name.ToLowerInvariant(), version, provider);
        Directory.CreateDirectory(boxDir);
        var filename = $"{name}_{version}_{provider}.box";
        var destPath = Path.Combine(boxDir, filename);

        await using (var fs = new FileStream(destPath, FileMode.Create))
            await Request.Body.CopyToAsync(fs);

        var fi = new FileInfo(destPath);
        var sha = BitConverter.ToString(SHA256.HashData(await System.IO.File.ReadAllBytesAsync(destPath)))
            .Replace("-", "").ToLowerInvariant();

        var metadata = System.Text.Json.JsonSerializer.Serialize(new
        {
            name,
            version,
            provider
        });

        var pkg = await _packageService.CreateOrUpdatePackageAsync(
            name, "vagrant", username, version, metadata: metadata);

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
        if (ver != null)
            await _packageService.AddPackageFileAsync(ver.Id, filename, fi.Length, sha);

        _logger.LogInformation("Vagrant box uploaded: {Name}/{Version}/{Provider} by {User}",
            name, version, provider, username);
        return StatusCode(201);
    }

    // Box metadata — Vagrant Cloud API format
    [HttpGet("{name}")]
    public async Task<IActionResult> Metadata(string name)
    {
        var pkg = await _packageService.GetPackageAsync(name, "vagrant");
        if (pkg == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/vagrant";

        var versions = pkg.Versions.OrderByDescending(v => v.CreatedAt).Select(v => new
        {
            version = v.Version,
            status = "active",
            providers = v.Files.Select(f =>
            {
                // Extract provider from filename: {name}_{version}_{provider}.box
                var providerName = ExtractProvider(f.Filename, name, v.Version);
                return new
                {
                    name = providerName,
                    url = $"{baseUrl}/{name}/{v.Version}/{providerName}",
                    checksum = f.Sha256 != null ? $"sha256:{f.Sha256}" : null,
                    checksum_type = f.Sha256 != null ? "sha256" : null
                };
            }).ToArray()
        }).ToArray();

        return Ok(new
        {
            name = pkg.Name,
            description = pkg.Description ?? "",
            versions
        });
    }

    // Download .box file
    [HttpGet("{name}/{version}/{provider}")]
    public async Task<IActionResult> Download(string name, string version, string provider)
    {
        var filename = $"{name}_{version}_{provider}.box";
        var filePath = Path.Combine(StorePath, name.ToLowerInvariant(), version, provider, filename);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        await _packageService.IncrementDownloadAsync(name, "vagrant", version);
        return PhysicalFile(filePath, "application/octet-stream", filename);
    }

    private static string ExtractProvider(string filename, string name, string version)
    {
        // Filename format: {name}_{version}_{provider}.box
        var prefix = $"{name}_{version}_";
        if (filename.StartsWith(prefix) && filename.EndsWith(".box"))
            return filename[prefix.Length..^4];
        return "virtualbox";
    }
}
