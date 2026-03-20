using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/rpm")]
public class RpmController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<RpmController> _logger;

    public RpmController(IPackageService packageService, IConfiguration config, ILogger<RpmController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "rpm");

    // Upload .rpm package
    [HttpPut("{repo}/{filename}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(string repo, string filename)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        if (!filename.EndsWith(".rpm"))
            return BadRequest("Only .rpm packages are supported");

        // Store the file on disk
        var diskDir = Path.Combine(StorePath, repo);
        Directory.CreateDirectory(diskDir);
        var destPath = Path.Combine(diskDir, filename);

        await using (var fs = new FileStream(destPath, FileMode.Create))
            await Request.Body.CopyToAsync(fs);

        var fi = new FileInfo(destPath);
        var sha = BitConverter.ToString(SHA256.HashData(await System.IO.File.ReadAllBytesAsync(destPath)))
            .Replace("-", "").ToLowerInvariant();

        // Derive package name and version from filename (name-version-release.arch.rpm)
        var stem = Path.GetFileNameWithoutExtension(filename); // removes .rpm
        // Remove architecture suffix (e.g., .x86_64, .noarch)
        var dotIdx = stem.LastIndexOf('.');
        if (dotIdx > 0)
            stem = stem[..dotIdx];

        // Split name-version-release: last two dash-separated segments are version and release
        var parts = stem.Split('-');
        string packageName;
        string version;
        if (parts.Length >= 3)
        {
            packageName = string.Join("-", parts[..^2]);
            version = parts[^2] + "-" + parts[^1];
        }
        else if (parts.Length == 2)
        {
            packageName = parts[0];
            version = parts[1];
        }
        else
        {
            packageName = stem;
            version = "0.0.0";
        }

        var metadata = System.Text.Json.JsonSerializer.Serialize(new
        {
            repo,
            filename,
            architecture = Path.GetFileNameWithoutExtension(filename).Contains('.')
                ? Path.GetFileNameWithoutExtension(filename).Split('.').Last()
                : "x86_64"
        });

        var pkg = await _packageService.CreateOrUpdatePackageAsync(
            packageName, "rpm", username, version, metadata: metadata);

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
        if (ver != null)
            await _packageService.AddPackageFileAsync(ver.Id, filename, fi.Length, sha);

        _logger.LogInformation("RPM package uploaded: {Name}={Version} ({Repo}) by {User}",
            packageName, version, repo, username);

        return StatusCode(201);
    }

    // Generate repo metadata XML (repomd.xml)
    [HttpGet("{repo}/repodata/repomd.xml")]
    public async Task<IActionResult> RepoMetadata(string repo)
    {
        var packages = await _packageService.GetPackagesAsync("rpm");
        var entries = new List<string>();

        foreach (var pkg in packages)
        {
            foreach (var ver in pkg.Versions)
            {
                string? pkgRepo = null;
                if (!string.IsNullOrEmpty(ver.Metadata))
                {
                    try
                    {
                        var meta = System.Text.Json.JsonDocument.Parse(ver.Metadata);
                        pkgRepo = meta.RootElement.TryGetProperty("repo", out var r) ? r.GetString() : null;
                    }
                    catch { /* skip invalid metadata */ }
                }
                if (!string.Equals(pkgRepo, repo, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var file in ver.Files)
                {
                    entries.Add($@"    <package type=""rpm"">
      <name>{pkg.Name}</name>
      <version ver=""{ver.Version}"" />
      <checksum type=""sha256"">{file.Sha256 ?? ""}</checksum>
      <location href=""{repo}/{file.Filename}"" />
      <size package=""{file.Size}"" />
    </package>");
                }
            }
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<repomd xmlns=""http://linux.duke.edu/metadata/repo"">
  <revision>{timestamp}</revision>
  <data type=""primary"">
    <location href=""repodata/primary.xml"" />
    <timestamp>{timestamp}</timestamp>
  </data>
</repomd>
<!-- primary package list -->
<metadata xmlns=""http://linux.duke.edu/metadata/common"" packages=""{entries.Count}"">
{string.Join("\n", entries)}
</metadata>";

        return Content(xml, "application/xml");
    }

    // Download .rpm file
    [HttpGet("{repo}/{filename}")]
    public async Task<IActionResult> Download(string repo, string filename)
    {
        var filePath = Path.Combine(StorePath, repo, filename);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        // Derive package name from filename
        var stem = Path.GetFileNameWithoutExtension(filename);
        var dotIdx = stem.LastIndexOf('.');
        if (dotIdx > 0)
            stem = stem[..dotIdx];

        var parts = stem.Split('-');
        string packageName;
        string? version = null;
        if (parts.Length >= 3)
        {
            packageName = string.Join("-", parts[..^2]);
            version = parts[^2] + "-" + parts[^1];
        }
        else if (parts.Length == 2)
        {
            packageName = parts[0];
            version = parts[1];
        }
        else
        {
            packageName = stem;
        }

        if (version != null)
            await _packageService.IncrementDownloadAsync(packageName, "rpm", version);

        return PhysicalFile(filePath, "application/x-rpm", filename);
    }
}
