using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/rubygems")]
public class RubyGemsController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<RubyGemsController> _logger;

    public RubyGemsController(IPackageService packageService, IConfiguration config, ILogger<RubyGemsController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "rubygems");

    // Push a .gem file (multipart form or raw body)
    [HttpPost("api/v1/gems")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Push()
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        byte[] gemBytes;
        string? name = null;
        string? version = null;
        string? description = null;
        string? originalFilename = null;

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            var file = form.Files.GetFile("gem") ?? form.Files.FirstOrDefault();
            if (file == null)
                return BadRequest("No gem file provided");

            name = form["name"].FirstOrDefault();
            version = form["version"].FirstOrDefault();
            description = form["description"].FirstOrDefault();
            originalFilename = file.FileName;

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            gemBytes = ms.ToArray();
        }
        else
        {
            // Raw body upload (gem push sends application/octet-stream)
            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            gemBytes = ms.ToArray();
        }

        if (gemBytes.Length == 0)
            return BadRequest("Empty gem file");

        // Try to extract name/version from the gem filename or metadata
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
        {
            // Try to parse from filename pattern: name-version.gem
            var fn = originalFilename ?? "";
            if (fn.EndsWith(".gem", StringComparison.OrdinalIgnoreCase) && fn.Contains('-'))
            {
                var baseName = fn[..^4]; // strip .gem
                var lastDash = baseName.LastIndexOf('-');
                if (lastDash > 0)
                {
                    name ??= baseName[..lastDash];
                    version ??= baseName[(lastDash + 1)..];
                }
            }

            // If still missing, require them
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
                return BadRequest("Missing required fields: name and version (provide via form fields or filename pattern name-version.gem)");
        }

        // Store the .gem file
        var normalizedName = name.ToLowerInvariant();
        var pkgDir = Path.Combine(StorePath, normalizedName, version);
        Directory.CreateDirectory(pkgDir);
        var filename = $"{name}-{version}.gem";
        var destPath = Path.Combine(pkgDir, filename);

        await System.IO.File.WriteAllBytesAsync(destPath, gemBytes);

        var sha = BitConverter.ToString(SHA256.HashData(gemBytes))
            .Replace("-", "").ToLowerInvariant();

        var metadataJson = JsonSerializer.Serialize(new { name, version, description = description ?? "" });

        var pkg = await _packageService.CreateOrUpdatePackageAsync(
            name, "rubygems", username, version, description, metadata: metadataJson);

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
        if (ver != null)
            await _packageService.AddPackageFileAsync(ver.Id, filename, gemBytes.Length, sha);

        _logger.LogInformation("RubyGems package uploaded: {Name}-{Version} by {User}", name, version, username);

        return Ok($"Successfully registered gem: {name} ({version})");
    }

    // Gem metadata JSON
    [HttpGet("api/v1/gems/{name}.json")]
    public async Task<IActionResult> GemInfo(string name)
    {
        var pkg = await _packageService.GetPackageAsync(name, "rubygems");
        if (pkg == null)
        {
            var all = await _packageService.GetPackagesAsync("rubygems");
            pkg = all.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        if (pkg == null) return NotFound();

        var latestVersion = pkg.Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/rubygems";

        return Ok(new
        {
            name = pkg.Name,
            version = latestVersion?.Version ?? "0.0.0",
            info = pkg.Description ?? "",
            downloads = pkg.Versions.Sum(v => v.Downloads),
            gem_uri = latestVersion != null
                ? $"{baseUrl}/gems/{pkg.Name}-{latestVersion.Version}.gem"
                : "",
            versions = pkg.Versions.Select(v => new
            {
                number = v.Version,
                created_at = v.CreatedAt,
                downloads_count = v.Downloads,
                sha = v.Files.FirstOrDefault()?.Sha256 ?? "",
            })
        });
    }

    // Download .gem file
    [HttpGet("gems/{name}-{version}.gem")]
    public async Task<IActionResult> Download(string name, string version)
    {
        var normalizedName = name.ToLowerInvariant();
        var filename = $"{name}-{version}.gem";
        var filePath = Path.Combine(StorePath, normalizedName, version, filename);

        if (!System.IO.File.Exists(filePath))
        {
            // Try case-insensitive lookup
            var pkgDir = Path.Combine(StorePath, normalizedName, version);
            if (Directory.Exists(pkgDir))
            {
                var files = Directory.GetFiles(pkgDir, "*.gem");
                if (files.Length > 0)
                {
                    filePath = files[0];
                    filename = Path.GetFileName(filePath);
                }
                else
                    return NotFound();
            }
            else
                return NotFound();
        }

        await _packageService.IncrementDownloadAsync(name, "rubygems", version);

        return PhysicalFile(filePath, "application/octet-stream", filename);
    }

    // Dependency info (used by bundler)
    [HttpGet("api/v1/dependencies")]
    public async Task<IActionResult> Dependencies([FromQuery] string? gems)
    {
        if (string.IsNullOrEmpty(gems))
            return Ok(Array.Empty<object>());

        var gemNames = gems.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var results = new List<object>();

        foreach (var gemName in gemNames)
        {
            var pkg = await _packageService.GetPackageAsync(gemName, "rubygems");
            if (pkg == null)
            {
                var all = await _packageService.GetPackagesAsync("rubygems");
                pkg = all.FirstOrDefault(p => p.Name.Equals(gemName, StringComparison.OrdinalIgnoreCase));
            }
            if (pkg == null) continue;

            foreach (var ver in pkg.Versions)
            {
                var deps = new List<object>();
                if (!string.IsNullOrEmpty(ver.Metadata))
                {
                    try
                    {
                        var meta = JsonDocument.Parse(ver.Metadata);
                        if (meta.RootElement.TryGetProperty("dependencies", out var depsObj))
                        {
                            if (depsObj.TryGetProperty("runtime", out var runtime))
                            {
                                foreach (var dep in runtime.EnumerateObject())
                                {
                                    deps.Add(new { name = dep.Name, requirements = dep.Value.GetString() ?? ">= 0" });
                                }
                            }
                        }
                    }
                    catch { /* metadata not parseable */ }
                }

                results.Add(new
                {
                    name = pkg.Name,
                    number = ver.Version,
                    platform = "ruby",
                    dependencies = deps
                });
            }
        }

        return Ok(results);
    }
}
