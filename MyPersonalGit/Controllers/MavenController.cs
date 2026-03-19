using System.Security.Claims;
using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/maven")]
public class MavenController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<MavenController> _logger;

    public MavenController(IPackageService packageService, IConfiguration config, ILogger<MavenController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "maven");

    // Deploy artifact (Maven deploy:deploy-file sends PUT)
    // Path format: /api/packages/maven/{groupId path}/{artifactId}/{version}/{filename}
    // e.g. /api/packages/maven/com/example/mylib/1.0.0/mylib-1.0.0.jar
    [HttpPut("{**path}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Deploy(string path)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        var segments = path.Split('/');
        if (segments.Length < 4)
            return BadRequest("Invalid Maven path. Expected: {groupId}/{artifactId}/{version}/{filename}");

        var filename = segments[^1];
        var version = segments[^2];
        var artifactId = segments[^3];
        var groupId = string.Join(".", segments[..^3]);
        var coordinate = $"{groupId}:{artifactId}";

        // Store the file on disk
        var diskDir = Path.Combine(StorePath, Path.Combine(segments[..^1]));
        Directory.CreateDirectory(diskDir);
        var destPath = Path.Combine(diskDir, filename);

        await using (var fs = new FileStream(destPath, FileMode.Create))
            await Request.Body.CopyToAsync(fs);

        var fi = new FileInfo(destPath);

        // Only register package metadata for actual artifacts (not checksums or metadata xml)
        if (filename.EndsWith(".jar") || filename.EndsWith(".pom") || filename.EndsWith(".aar")
            || filename.EndsWith(".war") || filename.EndsWith(".zip"))
        {
            var sha = BitConverter.ToString(SHA256.HashData(await System.IO.File.ReadAllBytesAsync(destPath)))
                .Replace("-", "").ToLowerInvariant();

            string? description = null;
            // Extract description from POM if this is a .pom file
            if (filename.EndsWith(".pom"))
            {
                try
                {
                    var doc = XDocument.Load(destPath);
                    var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
                    description = doc.Root?.Element(ns + "description")?.Value;
                }
                catch { /* not a valid XML, skip */ }
            }

            var metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                groupId,
                artifactId,
                version,
                packaging = Path.GetExtension(filename).TrimStart('.')
            });

            var pkg = await _packageService.CreateOrUpdatePackageAsync(
                coordinate, "maven", username, version, description, metadata: metadata);

            var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
            if (ver != null)
                await _packageService.AddPackageFileAsync(ver.Id, filename, fi.Length, sha);

            _logger.LogInformation("Maven artifact deployed: {Coordinate}:{Version}/{File} by {User}",
                coordinate, version, filename, username);
        }

        return StatusCode(201);
    }

    // Download artifact
    [HttpGet("{**path}")]
    public async Task<IActionResult> Download(string path)
    {
        // Serve maven-metadata.xml if requested
        if (path.EndsWith("maven-metadata.xml"))
            return await ServeMavenMetadata(path);

        var segments = path.Split('/');
        if (segments.Length < 4)
            return NotFound();

        var filePath = Path.Combine(StorePath, Path.Combine(segments));
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var filename = segments[^1];
        var version = segments[^2];
        var artifactId = segments[^3];
        var groupId = string.Join(".", segments[..^3]);
        var coordinate = $"{groupId}:{artifactId}";

        await _packageService.IncrementDownloadAsync(coordinate, "maven", version);

        var contentType = filename switch
        {
            _ when filename.EndsWith(".pom") => "application/xml",
            _ when filename.EndsWith(".jar") => "application/java-archive",
            _ when filename.EndsWith(".md5") => "text/plain",
            _ when filename.EndsWith(".sha1") => "text/plain",
            _ when filename.EndsWith(".sha256") => "text/plain",
            _ => "application/octet-stream"
        };

        return PhysicalFile(filePath, contentType, filename);
    }

    // Generate maven-metadata.xml dynamically
    private async Task<IActionResult> ServeMavenMetadata(string path)
    {
        var segments = path.Replace("/maven-metadata.xml", "").Split('/');
        if (segments.Length < 2)
            return NotFound();

        var artifactId = segments[^1];
        var groupId = string.Join(".", segments[..^1]);
        var coordinate = $"{groupId}:{artifactId}";

        var pkg = await _packageService.GetPackageAsync(coordinate, "maven");
        if (pkg == null) return NotFound();

        var latest = pkg.Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
        var versions = string.Join("\n", pkg.Versions.OrderBy(v => v.CreatedAt)
            .Select(v => $"        <version>{v.Version}</version>"));

        var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<metadata>
  <groupId>{groupId}</groupId>
  <artifactId>{artifactId}</artifactId>
  <versioning>
    <latest>{latest?.Version ?? "0.0.0"}</latest>
    <release>{latest?.Version ?? "0.0.0"}</release>
    <versions>
{versions}
    </versions>
    <lastUpdated>{DateTime.UtcNow:yyyyMMddHHmmss}</lastUpdated>
  </versioning>
</metadata>";

        return Content(xml, "application/xml");
    }

    // HEAD requests for artifact existence checks
    [HttpHead("{**path}")]
    public IActionResult Head(string path)
    {
        var segments = path.Split('/');
        var filePath = Path.Combine(StorePath, Path.Combine(segments));
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var fi = new FileInfo(filePath);
        Response.ContentLength = fi.Length;
        return Ok();
    }
}
