using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/helm")]
public class HelmController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<HelmController> _logger;

    public HelmController(IPackageService packageService, IConfiguration config, ILogger<HelmController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "helm");

    // Upload a Helm chart (.tgz) via multipart form
    [HttpPost("api/charts")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> UploadChart(IFormFile chart)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        if (chart == null || chart.Length == 0)
            return BadRequest("No chart file provided.");

        var filename = chart.FileName;
        if (!filename.EndsWith(".tgz"))
            return BadRequest("Chart must be a .tgz file.");

        // Parse name and version from filename: {name}-{version}.tgz
        var baseName = Path.GetFileNameWithoutExtension(filename); // strip .tgz
        var lastDash = baseName.LastIndexOf('-');
        if (lastDash <= 0)
            return BadRequest("Invalid chart filename. Expected format: {name}-{version}.tgz");

        var chartName = baseName[..lastDash];
        var chartVersion = baseName[(lastDash + 1)..];

        // Store the file on disk
        var diskDir = Path.Combine(StorePath, chartName);
        Directory.CreateDirectory(diskDir);
        var destPath = Path.Combine(diskDir, filename);

        await using (var fs = new FileStream(destPath, FileMode.Create))
            await chart.CopyToAsync(fs);

        var fi = new FileInfo(destPath);
        var sha = BitConverter.ToString(SHA256.HashData(await System.IO.File.ReadAllBytesAsync(destPath)))
            .Replace("-", "").ToLowerInvariant();

        var metadata = JsonSerializer.Serialize(new
        {
            name = chartName,
            version = chartVersion
        });

        var pkg = await _packageService.CreateOrUpdatePackageAsync(
            chartName, "helm", username, chartVersion, metadata: metadata);

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == chartVersion);
        if (ver != null)
            await _packageService.AddPackageFileAsync(ver.Id, filename, fi.Length, sha);

        _logger.LogInformation("Helm chart uploaded: {Chart}:{Version} by {User}",
            chartName, chartVersion, username);

        return StatusCode(201);
    }

    // Helm repo index — dynamically generated YAML
    [HttpGet("index.yaml")]
    public async Task<IActionResult> Index()
    {
        var packages = await _packageService.GetPackagesAsync(type: "helm");

        var yaml = new System.Text.StringBuilder();
        yaml.AppendLine("apiVersion: v1");
        yaml.AppendLine($"generated: \"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffffffZ}\"");
        yaml.AppendLine("entries:");

        foreach (var pkg in packages)
        {
            yaml.AppendLine($"  {pkg.Name}:");
            foreach (var ver in pkg.Versions.OrderByDescending(v => v.CreatedAt))
            {
                var filename = $"{pkg.Name}-{ver.Version}.tgz";
                var file = ver.Files.FirstOrDefault();
                var digest = file?.Sha256 ?? "";

                yaml.AppendLine($"  - apiVersion: v2");
                yaml.AppendLine($"    name: {pkg.Name}");
                yaml.AppendLine($"    version: {ver.Version}");
                yaml.AppendLine($"    created: \"{ver.CreatedAt:yyyy-MM-ddTHH:mm:ss.fffffffZ}\"");
                yaml.AppendLine($"    digest: {digest}");
                yaml.AppendLine($"    urls:");
                yaml.AppendLine($"    - charts/{filename}");
            }
        }

        return Content(yaml.ToString(), "application/x-yaml");
    }

    // Download a chart .tgz
    [HttpGet("charts/{filename}")]
    public async Task<IActionResult> DownloadChart(string filename)
    {
        // Parse chart name and version from filename
        var baseName = Path.GetFileNameWithoutExtension(filename);
        var lastDash = baseName.LastIndexOf('-');
        if (lastDash > 0)
        {
            var chartName = baseName[..lastDash];
            var chartVersion = baseName[(lastDash + 1)..];

            var filePath = Path.Combine(StorePath, chartName, filename);
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            await _packageService.IncrementDownloadAsync(chartName, "helm", chartVersion);

            return PhysicalFile(filePath, "application/gzip", filename);
        }

        // Fallback: search all subdirectories
        var dirs = Directory.Exists(StorePath) ? Directory.GetDirectories(StorePath) : [];
        foreach (var dir in dirs)
        {
            var filePath = Path.Combine(dir, filename);
            if (System.IO.File.Exists(filePath))
                return PhysicalFile(filePath, "application/gzip", filename);
        }

        return NotFound();
    }
}
