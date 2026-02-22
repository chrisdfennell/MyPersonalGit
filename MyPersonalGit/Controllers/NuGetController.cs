using System.IO.Compression;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/nuget/v3")]
public class NuGetController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<NuGetController> _logger;

    public NuGetController(IPackageService packageService, IConfiguration config, ILogger<NuGetController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "nuget");

    // NuGet v3 Service Index
    [HttpGet("index.json")]
    public IActionResult ServiceIndex()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/nuget/v3";
        return Ok(new
        {
            version = "3.0.0",
            resources = new object[]
            {
                new { @id = $"{baseUrl}/query", @type = "SearchQueryService/3.5.0" },
                new { @id = $"{baseUrl}/registration", @type = "RegistrationsBaseUrl/3.6.0" },
                new { @id = $"{baseUrl}/flatcontainer", @type = "PackageBaseAddress/3.0.0" },
                new { @id = $"{baseUrl}/package", @type = "PackagePublish/2.0.0" }
            }
        });
    }

    // Push .nupkg
    [HttpPut("package")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Push()
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        var file = Request.Form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
            return BadRequest("No .nupkg file uploaded");

        var tempPath = Path.GetTempFileName();
        try
        {
            await using (var fs = new FileStream(tempPath, FileMode.Create))
                await file.CopyToAsync(fs);

            // Read .nuspec from .nupkg (it's a zip)
            string? packageId = null, version = null, description = null;
            using (var zip = ZipFile.OpenRead(tempPath))
            {
                var nuspecEntry = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
                if (nuspecEntry != null)
                {
                    using var stream = nuspecEntry.Open();
                    var doc = XDocument.Load(stream);
                    var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
                    var metadata = doc.Root?.Element(ns + "metadata");
                    packageId = metadata?.Element(ns + "id")?.Value;
                    version = metadata?.Element(ns + "version")?.Value;
                    description = metadata?.Element(ns + "description")?.Value;
                }
            }

            if (string.IsNullOrEmpty(packageId) || string.IsNullOrEmpty(version))
                return BadRequest("Invalid .nupkg: missing package id or version in .nuspec");

            var pkg = await _packageService.CreateOrUpdatePackageAsync(
                packageId, "nuget", username, version, description);

            // Store the .nupkg file
            var pkgDir = Path.Combine(StorePath, packageId.ToLower(), version.ToLower());
            Directory.CreateDirectory(pkgDir);
            var destPath = Path.Combine(pkgDir, $"{packageId.ToLower()}.{version.ToLower()}.nupkg");
            System.IO.File.Copy(tempPath, destPath, overwrite: true);

            var fi = new FileInfo(destPath);
            var sha = BitConverter.ToString(SHA256.HashData(await System.IO.File.ReadAllBytesAsync(destPath))).Replace("-", "").ToLowerInvariant();

            var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
            if (ver != null)
                await _packageService.AddPackageFileAsync(ver.Id, fi.Name, fi.Length, sha);

            _logger.LogInformation("NuGet package pushed: {Id}@{Version} by {User}", packageId, version, username);
            return StatusCode(201);
        }
        finally
        {
            if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath);
        }
    }

    // Download .nupkg
    [HttpGet("flatcontainer/{id}/{version}/{filename}")]
    public async Task<IActionResult> Download(string id, string version, string filename)
    {
        var filePath = Path.Combine(StorePath, id.ToLower(), version.ToLower(), filename.ToLower());
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        await _packageService.IncrementDownloadAsync(id, "nuget", version);
        return PhysicalFile(filePath, "application/octet-stream", filename);
    }

    // Search
    [HttpGet("query")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var packages = await _packageService.GetPackagesAsync("nuget");
        if (!string.IsNullOrEmpty(q))
            packages = packages.Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/nuget/v3";
        return Ok(new
        {
            totalHits = packages.Count,
            data = packages.Skip(skip).Take(take).Select(p => new
            {
                id = p.Name,
                version = p.Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault()?.Version ?? "0.0.0",
                description = p.Description ?? "",
                totalDownloads = p.Downloads,
                registration = $"{baseUrl}/registration/{p.Name.ToLower()}/index.json",
                versions = p.Versions.Select(v => new
                {
                    version = v.Version,
                    downloads = v.Downloads,
                    @id = $"{baseUrl}/registration/{p.Name.ToLower()}/{v.Version}.json"
                })
            })
        });
    }

    // Package Registration
    [HttpGet("registration/{id}/index.json")]
    public async Task<IActionResult> Registration(string id)
    {
        var pkg = await _packageService.GetPackageAsync(id, "nuget");
        if (pkg == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/nuget/v3";
        return Ok(new
        {
            count = 1,
            items = new[]
            {
                new
                {
                    count = pkg.Versions.Count,
                    items = pkg.Versions.Select(v => new
                    {
                        catalogEntry = new
                        {
                            id = pkg.Name,
                            version = v.Version,
                            description = v.Description ?? pkg.Description ?? "",
                            listed = true,
                            published = v.CreatedAt
                        },
                        packageContent = $"{baseUrl}/flatcontainer/{pkg.Name.ToLower()}/{v.Version.ToLower()}/{pkg.Name.ToLower()}.{v.Version.ToLower()}.nupkg"
                    })
                }
            }
        });
    }
}
