using System.IO.Compression;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/pypi")]
public class PyPIController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<PyPIController> _logger;

    public PyPIController(IPackageService packageService, IConfiguration config, ILogger<PyPIController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "pypi");

    // PEP 503 Simple API — root index listing all packages
    [HttpGet("simple/")]
    [HttpGet("simple")]
    public async Task<IActionResult> SimpleIndex()
    {
        var packages = await _packageService.GetPackagesAsync("pypi");
        var links = string.Join("\n", packages.Select(p =>
            $"    <a href=\"/api/packages/pypi/simple/{Normalize(p.Name)}/\">{p.Name}</a>"));

        return Content($@"<!DOCTYPE html>
<html>
<head><title>Simple Index</title></head>
<body>
<h1>Simple Index</h1>
{links}
</body>
</html>", "text/html");
    }

    // PEP 503 Simple API — per-package page listing all versions/files
    [HttpGet("simple/{name}/")]
    [HttpGet("simple/{name}")]
    public async Task<IActionResult> SimplePackage(string name)
    {
        var pkg = await _packageService.GetPackageAsync(name, "pypi");
        if (pkg == null)
        {
            // Try normalized name
            var all = await _packageService.GetPackagesAsync("pypi");
            pkg = all.FirstOrDefault(p => Normalize(p.Name) == Normalize(name));
        }
        if (pkg == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/pypi";
        var links = new List<string>();
        foreach (var ver in pkg.Versions)
        {
            foreach (var file in ver.Files)
            {
                var sha = file.Sha256 ?? "";
                var fragment = !string.IsNullOrEmpty(sha) ? $"#sha256={sha}" : "";
                links.Add($"    <a href=\"{baseUrl}/files/{Normalize(pkg.Name)}/{ver.Version}/{file.Filename}{fragment}\">{file.Filename}</a>");
            }
        }

        return Content($@"<!DOCTYPE html>
<html>
<head><title>Links for {pkg.Name}</title></head>
<body>
<h1>Links for {pkg.Name}</h1>
{string.Join("\n", links)}
</body>
</html>", "text/html");
    }

    // Upload package (twine upload compatible — multipart form)
    [HttpPost("upload/")]
    [HttpPost("upload")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload()
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        // twine sends multipart form with :action=file_upload
        var form = await Request.ReadFormAsync();
        var name = form["name"].FirstOrDefault();
        var version = form["version"].FirstOrDefault();
        var summary = form["summary"].FirstOrDefault();
        var file = form.Files.GetFile("content");

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version) || file == null)
            return BadRequest("Missing required fields: name, version, content");

        // Store the file
        var pkgDir = Path.Combine(StorePath, Normalize(name), version);
        Directory.CreateDirectory(pkgDir);
        var destPath = Path.Combine(pkgDir, file.FileName);

        await using (var fs = new FileStream(destPath, FileMode.Create))
            await file.CopyToAsync(fs);

        var sha = BitConverter.ToString(SHA256.HashData(await System.IO.File.ReadAllBytesAsync(destPath)))
            .Replace("-", "").ToLowerInvariant();

        // Build metadata JSON from form fields
        var metadata = System.Text.Json.JsonSerializer.Serialize(new
        {
            name,
            version,
            summary = summary ?? "",
            author = form["author"].FirstOrDefault() ?? "",
            author_email = form["author_email"].FirstOrDefault() ?? "",
            license = form["license"].FirstOrDefault() ?? "",
            requires_python = form["requires_python"].FirstOrDefault() ?? "",
            home_page = form["home_page"].FirstOrDefault() ?? "",
        });

        var pkg = await _packageService.CreateOrUpdatePackageAsync(
            name, "pypi", username, version, summary, metadata: metadata);

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
        if (ver != null)
            await _packageService.AddPackageFileAsync(ver.Id, file.FileName, file.Length, sha);

        _logger.LogInformation("PyPI package uploaded: {Name}=={Version} by {User}", name, version, username);
        return Ok();
    }

    // Download package file
    [HttpGet("files/{name}/{version}/{filename}")]
    public async Task<IActionResult> Download(string name, string version, string filename)
    {
        var filePath = Path.Combine(StorePath, Normalize(name), version, filename);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        // Find the actual package name (may differ in casing)
        var all = await _packageService.GetPackagesAsync("pypi");
        var pkg = all.FirstOrDefault(p => Normalize(p.Name) == Normalize(name));
        if (pkg != null)
            await _packageService.IncrementDownloadAsync(pkg.Name, "pypi", version);

        return PhysicalFile(filePath, "application/octet-stream", filename);
    }

    // JSON API — package metadata (used by pip for dependency resolution)
    [HttpGet("json/{name}")]
    public async Task<IActionResult> JsonMetadata(string name)
    {
        var pkg = await _packageService.GetPackageAsync(name, "pypi");
        if (pkg == null)
        {
            var all = await _packageService.GetPackagesAsync("pypi");
            pkg = all.FirstOrDefault(p => Normalize(p.Name) == Normalize(name));
        }
        if (pkg == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/pypi";
        var releases = new Dictionary<string, object[]>();
        foreach (var ver in pkg.Versions)
        {
            releases[ver.Version] = ver.Files.Select(f => new
            {
                filename = f.Filename,
                url = $"{baseUrl}/files/{Normalize(pkg.Name)}/{ver.Version}/{f.Filename}",
                size = f.Size,
                digests = new { sha256 = f.Sha256 ?? "" }
            }).ToArray<object>();
        }

        return Ok(new
        {
            info = new
            {
                name = pkg.Name,
                version = pkg.Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault()?.Version ?? "0.0.0",
                summary = pkg.Description ?? "",
                author = pkg.Owner,
            },
            releases
        });
    }

    // PEP 503 name normalization: lowercase, replace [-_.] with -
    private static string Normalize(string name) =>
        System.Text.RegularExpressions.Regex.Replace(name.ToLowerInvariant(), @"[-_.]+", "-");
}
