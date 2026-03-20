using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/packages/conan")]
public class ConanController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _config;
    private readonly ILogger<ConanController> _logger;

    public ConanController(IPackageService packageService, IConfiguration config, ILogger<ConanController> logger)
    {
        _packageService = packageService;
        _config = config;
        _logger = logger;
    }

    private string StorePath => Path.Combine(_config["Git:ProjectRoot"] ?? "/repos", ".packages", "conan");

    private static string CoordFromParts(string name, string version, string user, string channel) =>
        $"{name}/{version}@{user}/{channel}";

    private string RecipePath(string name, string version, string user, string channel) =>
        Path.Combine(StorePath, name, version, user, channel);

    // Health check
    [HttpGet("v1/ping")]
    public IActionResult Ping()
    {
        Response.Headers["X-Conan-Server-Capabilities"] = "";
        return Ok();
    }

    // Get upload URLs for a recipe
    [HttpPut("v1/conans/{name}/{version}/{user}/{channel}/upload_urls")]
    public async Task<IActionResult> UploadUrls(string name, string version, string user, string channel)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        // Read the requested file list from the body
        Dictionary<string, object>? requestedFiles;
        try
        {
            requestedFiles = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(Request.Body);
        }
        catch
        {
            return BadRequest("Invalid JSON body.");
        }

        if (requestedFiles == null || requestedFiles.Count == 0)
            return BadRequest("No files specified.");

        // Return upload URLs pointing to our files endpoint
        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/conan/v1/files/{name}/{version}/{user}/{channel}";
        var urls = new Dictionary<string, string>();
        foreach (var file in requestedFiles.Keys)
        {
            urls[file] = $"{baseUrl}/{file}";
        }

        return new JsonResult(urls);
    }

    // Upload a recipe file
    [HttpPut("v1/files/{name}/{version}/{user}/{channel}/{filename}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> UploadFile(string name, string version, string user, string channel, string filename)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        var diskDir = RecipePath(name, version, user, channel);
        Directory.CreateDirectory(diskDir);
        var destPath = Path.Combine(diskDir, filename);

        await using (var fs = new FileStream(destPath, FileMode.Create))
            await Request.Body.CopyToAsync(fs);

        var fi = new FileInfo(destPath);
        var sha = BitConverter.ToString(SHA256.HashData(await System.IO.File.ReadAllBytesAsync(destPath)))
            .Replace("-", "").ToLowerInvariant();

        var coordinate = CoordFromParts(name, version, user, channel);
        var metadata = JsonSerializer.Serialize(new
        {
            name,
            version,
            user,
            channel
        });

        var pkg = await _packageService.CreateOrUpdatePackageAsync(
            coordinate, "conan", username, version, metadata: metadata);

        var ver = pkg.Versions.FirstOrDefault(v => v.Version == version);
        if (ver != null)
            await _packageService.AddPackageFileAsync(ver.Id, filename, fi.Length, sha);

        _logger.LogInformation("Conan file uploaded: {Coordinate}/{File} by {User}",
            coordinate, filename, username);

        return StatusCode(201);
    }

    // Recipe info — list files in the recipe
    [HttpGet("v1/conans/{name}/{version}/{user}/{channel}")]
    public IActionResult RecipeInfo(string name, string version, string user, string channel)
    {
        var dir = RecipePath(name, version, user, channel);
        if (!Directory.Exists(dir))
            return NotFound();

        var files = Directory.GetFiles(dir)
            .Select(f => Path.GetFileName(f))
            .ToDictionary(f => f, _ => (object)new { });

        return new JsonResult(files);
    }

    // Get download URLs for a recipe
    [HttpGet("v1/conans/{name}/{version}/{user}/{channel}/download_urls")]
    public IActionResult DownloadUrls(string name, string version, string user, string channel)
    {
        var dir = RecipePath(name, version, user, channel);
        if (!Directory.Exists(dir))
            return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/packages/conan/v1/files/{name}/{version}/{user}/{channel}";
        var urls = Directory.GetFiles(dir)
            .ToDictionary(f => Path.GetFileName(f), f => $"{baseUrl}/{Path.GetFileName(f)}");

        return new JsonResult(urls);
    }

    // Download a recipe file
    [HttpGet("v1/files/{name}/{version}/{user}/{channel}/{filename}")]
    public async Task<IActionResult> DownloadFile(string name, string version, string user, string channel, string filename)
    {
        var filePath = Path.Combine(RecipePath(name, version, user, channel), filename);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var coordinate = CoordFromParts(name, version, user, channel);
        await _packageService.IncrementDownloadAsync(coordinate, "conan", version);

        return PhysicalFile(filePath, "application/octet-stream", filename);
    }
}
