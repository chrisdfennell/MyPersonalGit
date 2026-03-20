using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/conda")]
public class CondaController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<CondaController> _logger;

    public CondaController(IPackageService packageService, IConfiguration config, ILogger<CondaController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "conda");

    // Upload a conda package (.tar.bz2 or .conda)
    [HttpPut("{channel}/{filename}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(string channel, string filename)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        if (!filename.EndsWith(".tar.bz2") && !filename.EndsWith(".conda"))
            return BadRequest("Package must be a .tar.bz2 or .conda file.");

        // Parse name and version from filename: {name}-{version}-{build}.tar.bz2 or .conda
        var baseName = filename.EndsWith(".tar.bz2")
            ? filename[..^8]   // strip .tar.bz2
            : filename[..^6];  // strip .conda

        var parts = baseName.Split('-');
        if (parts.Length < 2)
            return BadRequest("Invalid package filename. Expected format: {name}-{version}-{build}.tar.bz2");

        var packageName = parts[0];
        var packageVersion = parts[1];

        // Store the file on disk
        var diskDir = Path.Combine(StorePath, channel);
        Directory.CreateDirectory(diskDir);
        var destPath = Path.Combine(diskDir, filename);

        await using (var fs = new FileStream(destPath, FileMode.Create))
            await Request.Body.CopyToAsync(fs);

        var fi = new FileInfo(destPath);
        var sha = BitConverter.ToString(SHA256.HashData(await System.IO.File.ReadAllBytesAsync(destPath)))
            .Replace("-", "").ToLowerInvariant();

        var coordinate = $"{channel}/{packageName}";
        var metadata = JsonSerializer.Serialize(new
        {
            channel,
            name = packageName,
            version = packageVersion,
            filename
        });

        var pkg = await _packageService.CreateOrUpdatePackageAsync(
            coordinate, "conda", username, packageVersion, metadata: metadata);

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == packageVersion);
        if (ver != null)
            await _packageService.AddPackageFileAsync(ver.Id, filename, fi.Length, sha);

        _logger.LogInformation("Conda package uploaded: {Channel}/{Package}:{Version} by {User}",
            channel, packageName, packageVersion, username);

        return StatusCode(201);
    }

    // Channel repo metadata (repodata.json)
    [HttpGet("{channel}/repodata.json")]
    public async Task<IActionResult> RepoData(string channel)
    {
        var allPackages = await _packageService.GetPackagesAsync(type: "conda");
        var channelPackages = allPackages
            .Where(p => p.Name.StartsWith($"{channel}/"))
            .ToList();

        var packages = new Dictionary<string, object>();

        foreach (var pkg in channelPackages)
        {
            foreach (var ver in pkg.Versions)
            {
                foreach (var file in ver.Files)
                {
                    packages[file.Filename] = new
                    {
                        name = pkg.Name.Split('/').Last(),
                        version = ver.Version,
                        sha256 = file.Sha256 ?? "",
                        size = file.Size
                    };
                }
            }
        }

        var repodata = new
        {
            info = new { subdir = channel },
            packages
        };

        return new JsonResult(repodata);
    }

    // Download a conda package
    [HttpGet("{channel}/{filename}")]
    public async Task<IActionResult> Download(string channel, string filename)
    {
        // Don't match repodata.json
        if (filename == "repodata.json")
            return NotFound();

        var filePath = Path.Combine(StorePath, channel, filename);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        // Parse name/version for download tracking
        var baseName = filename.EndsWith(".tar.bz2")
            ? filename[..^8]
            : filename.EndsWith(".conda")
                ? filename[..^6]
                : filename;

        var parts = baseName.Split('-');
        if (parts.Length >= 2)
        {
            var coordinate = $"{channel}/{parts[0]}";
            await _packageService.IncrementDownloadAsync(coordinate, "conda", parts[1]);
        }

        var contentType = filename.EndsWith(".tar.bz2")
            ? "application/x-bzip2"
            : "application/octet-stream";

        return PhysicalFile(filePath, contentType, filename);
    }
}
