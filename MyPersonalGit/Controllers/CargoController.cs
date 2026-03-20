using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/cargo")]
public class CargoController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<CargoController> _logger;

    public CargoController(IPackageService packageService, IConfiguration config, ILogger<CargoController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "cargo");

    // Cargo publish — binary format: 4-byte LE json length + json metadata + 4-byte LE crate length + crate bytes
    [HttpPut("api/new")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Publish()
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms);
        var data = ms.ToArray();

        if (data.Length < 8)
            return BadRequest("Invalid cargo publish payload");

        // Parse binary format
        var jsonLen = BitConverter.ToUInt32(data, 0);
        if (data.Length < 4 + jsonLen + 4)
            return BadRequest("Invalid cargo publish payload: truncated JSON");

        var jsonBuf = new byte[(int)jsonLen];
        Array.Copy(data, 4, jsonBuf, 0, (int)jsonLen);
        var metadata = JsonDocument.Parse(jsonBuf);
        var root = metadata.RootElement;

        var name = root.GetProperty("name").GetString();
        var version = root.GetProperty("vers").GetString();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
            return BadRequest("Missing required fields: name, vers");

        var description = root.TryGetProperty("description", out var descProp)
            ? descProp.GetString() ?? ""
            : "";

        var crateLen = BitConverter.ToUInt32(data, 4 + (int)jsonLen);
        var crateOffset = 4 + (int)jsonLen + 4;
        if (data.Length < crateOffset + crateLen)
            return BadRequest("Invalid cargo publish payload: truncated crate data");

        var crateBytes = new byte[(int)crateLen];
        Array.Copy(data, crateOffset, crateBytes, 0, (int)crateLen);

        // Store the .crate file
        var normalizedName = name.ToLowerInvariant();
        var pkgDir = Path.Combine(StorePath, normalizedName, version);
        Directory.CreateDirectory(pkgDir);
        var filename = $"{name}-{version}.crate";
        var destPath = Path.Combine(pkgDir, filename);

        await System.IO.File.WriteAllBytesAsync(destPath, crateBytes);

        var sha = BitConverter.ToString(SHA256.HashData(crateBytes))
            .Replace("-", "").ToLowerInvariant();

        var metadataJson = Encoding.UTF8.GetString(data, 4, (int)jsonLen);

        var pkg = await _packageService.CreateOrUpdatePackageAsync(
            name, "cargo", username, version, description, metadata: metadataJson);

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
        if (ver != null)
            await _packageService.AddPackageFileAsync(ver.Id, filename, crateBytes.Length, sha);

        _logger.LogInformation("Cargo crate uploaded: {Name}@{Version} by {User}", name, version, username);

        // Cargo expects a JSON response with warnings
        return Ok(new { warnings = new { invalid_categories = Array.Empty<string>(), invalid_badges = Array.Empty<string>(), other = Array.Empty<string>() } });
    }

    // Crate metadata — list versions and deps
    [HttpGet("api/v1/crates/{name}")]
    public async Task<IActionResult> CrateMetadata(string name)
    {
        var pkg = await _packageService.GetPackageAsync(name, "cargo");
        if (pkg == null)
        {
            var all = await _packageService.GetPackagesAsync("cargo");
            pkg = all.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        if (pkg == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/cargo";
        var versions = pkg.Versions.Select(v =>
        {
            // Try to parse deps from stored metadata
            var deps = new List<object>();
            if (!string.IsNullOrEmpty(v.Metadata))
            {
                try
                {
                    var meta = JsonDocument.Parse(v.Metadata);
                    if (meta.RootElement.TryGetProperty("deps", out var depsArray))
                    {
                        foreach (var dep in depsArray.EnumerateArray())
                        {
                            deps.Add(new
                            {
                                name = dep.TryGetProperty("name", out var n) ? n.GetString() : "",
                                version_req = dep.TryGetProperty("version_req", out var vr) ? vr.GetString() : "*",
                                optional = dep.TryGetProperty("optional", out var opt) && opt.GetBoolean(),
                            });
                        }
                    }
                }
                catch { /* metadata not parseable */ }
            }

            return new
            {
                num = v.Version,
                dl_path = $"{baseUrl}/api/v1/crates/{pkg.Name}/{v.Version}/download",
                created_at = v.CreatedAt,
                downloads = v.Downloads,
                deps,
                files = v.Files.Select(f => new { f.Filename, f.Size, sha256 = f.Sha256 ?? "" })
            };
        });

        return Ok(new
        {
            @crate = new
            {
                name = pkg.Name,
                description = pkg.Description ?? "",
                created_at = pkg.CreatedAt,
                downloads = pkg.Versions.Sum(v => v.Downloads),
            },
            versions
        });
    }

    // Download .crate file
    [HttpGet("api/v1/crates/{name}/{version}/download")]
    public async Task<IActionResult> Download(string name, string version)
    {
        var normalizedName = name.ToLowerInvariant();
        var pkgDir = Path.Combine(StorePath, normalizedName, version);
        var filename = $"{name}-{version}.crate";
        var filePath = Path.Combine(pkgDir, filename);

        if (!System.IO.File.Exists(filePath))
        {
            // Try to find with case-insensitive name
            if (Directory.Exists(pkgDir))
            {
                var files = Directory.GetFiles(pkgDir, "*.crate");
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

        await _packageService.IncrementDownloadAsync(name, "cargo", version);

        return PhysicalFile(filePath, "application/octet-stream", filename);
    }
}
