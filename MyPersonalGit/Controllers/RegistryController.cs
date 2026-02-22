using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Controllers;

/// <summary>
/// OCI Distribution Spec implementation for hosting Docker/OCI container images.
/// Endpoints live under /v2/ as required by the spec.
/// </summary>
[ApiController]
public class RegistryController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<RegistryController> _logger;

    public RegistryController(IDbContextFactory<AppDbContext> dbFactory, IConfiguration config, ILogger<RegistryController> logger)
    {
        _dbFactory = dbFactory;
        _config = config;
        _logger = logger;
    }

    private string ProjectRoot => _config["Git:ProjectRoot"] ?? "/repos";
    private string BlobStorePath => Path.Combine(ProjectRoot, ".registry", "blobs", "sha256");
    private string UploadStorePath => Path.Combine(ProjectRoot, ".registry", "uploads");

    private string GetBlobPath(string digest)
    {
        var hash = digest.StartsWith("sha256:") ? digest[7..] : digest;
        return Path.Combine(BlobStorePath, hash[..2], hash);
    }

    private string GetUploadPath(string uuid) => Path.Combine(UploadStorePath, uuid);

    // GET /v2/ — Version check
    [HttpGet("/v2/")]
    public IActionResult VersionCheck()
    {
        Response.Headers["Docker-Distribution-API-Version"] = "registry/2.0";
        return Ok(new { });
    }

    // GET /v2/_catalog — List all image repositories
    [HttpGet("/v2/_catalog")]
    public async Task<IActionResult> Catalog()
    {
        using var db = _dbFactory.CreateDbContext();
        var repos = await db.ContainerManifests
            .Select(m => m.RepositoryName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();
        return Ok(new { repositories = repos });
    }

    // GET /v2/{name}/tags/list
    [HttpGet("/v2/{*rest}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> CatchAllGet(string rest)
    {
        var (name, action, reference) = ParseRoute(rest);
        if (name == null) return NotFound();

        if (action == "tags" && reference == "list")
            return await ListTags(name);
        if (action == "manifests")
            return await GetManifest(name, reference);
        if (action == "blobs" && reference != null)
            return await GetBlob(name, reference);

        return NotFound();
    }

    [HttpHead("/v2/{*rest}")]
    public async Task<IActionResult> CatchAllHead(string rest)
    {
        var (name, action, reference) = ParseRoute(rest);
        if (name == null) return NotFound();

        if (action == "manifests")
            return await HeadManifest(name, reference);
        if (action == "blobs" && reference != null)
            return await HeadBlob(name, reference);

        return NotFound();
    }

    [HttpPut("/v2/{*rest}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> CatchAllPut(string rest)
    {
        var (name, action, reference) = ParseRoute(rest);
        if (name == null) return NotFound();

        if (action == "manifests")
            return await PutManifest(name, reference);
        if (action == "blobs" && reference != null && reference.Contains("uploads/"))
            return await CompleteBlobUpload(name, reference);

        return NotFound();
    }

    [HttpPost("/v2/{*rest}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> CatchAllPost(string rest)
    {
        var (name, action, reference) = ParseRoute(rest);
        if (name == null) return NotFound();

        if (action == "blobs" && reference == "uploads/")
            return await InitiateBlobUpload(name);

        return NotFound();
    }

    [HttpPatch("/v2/{*rest}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> CatchAllPatch(string rest)
    {
        var (name, action, reference) = ParseRoute(rest);
        if (name == null) return NotFound();

        if (action == "blobs" && reference != null && reference.StartsWith("uploads/"))
            return await UploadBlobChunk(name, reference);

        return NotFound();
    }

    [HttpDelete("/v2/{*rest}")]
    public async Task<IActionResult> CatchAllDelete(string rest)
    {
        var (name, action, reference) = ParseRoute(rest);
        if (name == null) return NotFound();

        if (action == "manifests")
            return await DeleteManifest(name, reference);

        return NotFound();
    }

    // --- Implementation methods ---

    private async Task<IActionResult> ListTags(string name)
    {
        using var db = _dbFactory.CreateDbContext();
        var tags = await db.ContainerManifests
            .Where(m => m.RepositoryName == name)
            .Select(m => m.Tag)
            .OrderBy(t => t)
            .ToListAsync();
        return Ok(new { name, tags });
    }

    private async Task<IActionResult> HeadManifest(string name, string? reference)
    {
        if (string.IsNullOrEmpty(reference)) return NotFound();

        using var db = _dbFactory.CreateDbContext();
        var manifest = reference.StartsWith("sha256:")
            ? await db.ContainerManifests.FirstOrDefaultAsync(m => m.RepositoryName == name && m.Digest == reference)
            : await db.ContainerManifests.FirstOrDefaultAsync(m => m.RepositoryName == name && m.Tag == reference);

        if (manifest == null) return NotFound();

        Response.Headers["Docker-Content-Digest"] = manifest.Digest;
        Response.ContentType = manifest.MediaType;
        Response.ContentLength = manifest.Size;
        return Ok();
    }

    private async Task<IActionResult> GetManifest(string name, string? reference)
    {
        if (string.IsNullOrEmpty(reference)) return NotFound();

        using var db = _dbFactory.CreateDbContext();
        var manifest = reference.StartsWith("sha256:")
            ? await db.ContainerManifests.FirstOrDefaultAsync(m => m.RepositoryName == name && m.Digest == reference)
            : await db.ContainerManifests.FirstOrDefaultAsync(m => m.RepositoryName == name && m.Tag == reference);

        if (manifest == null) return NotFound();

        Response.Headers["Docker-Content-Digest"] = manifest.Digest;
        return Content(manifest.Content, manifest.MediaType);
    }

    private async Task<IActionResult> PutManifest(string name, string? reference)
    {
        if (string.IsNullOrEmpty(reference)) return BadRequest();

        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms);
        var content = System.Text.Encoding.UTF8.GetString(ms.ToArray());

        var digest = "sha256:" + BitConverter.ToString(SHA256.HashData(ms.ToArray())).Replace("-", "").ToLowerInvariant();
        var mediaType = Request.ContentType ?? "application/vnd.oci.image.manifest.v1+json";

        using var db = _dbFactory.CreateDbContext();

        // Upsert by tag
        var existing = await db.ContainerManifests
            .FirstOrDefaultAsync(m => m.RepositoryName == name && m.Tag == reference);

        if (existing != null)
        {
            existing.Digest = digest;
            existing.MediaType = mediaType;
            existing.Content = content;
            existing.Size = ms.Length;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.ContainerManifests.Add(new ContainerManifest
            {
                RepositoryName = name,
                Tag = reference,
                Digest = digest,
                MediaType = mediaType,
                Content = content,
                Size = ms.Length
            });
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("Manifest pushed: {Name}:{Reference} ({Digest})", name, reference, digest);

        Response.Headers["Docker-Content-Digest"] = digest;
        Response.Headers["Location"] = $"/v2/{name}/manifests/{digest}";
        return StatusCode(201);
    }

    private async Task<IActionResult> DeleteManifest(string name, string? reference)
    {
        if (string.IsNullOrEmpty(reference)) return NotFound();

        using var db = _dbFactory.CreateDbContext();
        var manifest = reference.StartsWith("sha256:")
            ? await db.ContainerManifests.FirstOrDefaultAsync(m => m.RepositoryName == name && m.Digest == reference)
            : await db.ContainerManifests.FirstOrDefaultAsync(m => m.RepositoryName == name && m.Tag == reference);

        if (manifest == null) return NotFound();

        db.ContainerManifests.Remove(manifest);
        await db.SaveChangesAsync();
        return Accepted();
    }

    private async Task<IActionResult> HeadBlob(string name, string digest)
    {
        var blobPath = GetBlobPath(digest);
        if (!System.IO.File.Exists(blobPath)) return NotFound();

        var fi = new FileInfo(blobPath);
        Response.Headers["Docker-Content-Digest"] = digest;
        Response.ContentLength = fi.Length;
        Response.ContentType = "application/octet-stream";
        return Ok();
    }

    private async Task<IActionResult> GetBlob(string name, string digest)
    {
        var blobPath = GetBlobPath(digest);
        if (!System.IO.File.Exists(blobPath)) return NotFound();

        Response.Headers["Docker-Content-Digest"] = digest;
        return PhysicalFile(blobPath, "application/octet-stream");
    }

    private async Task<IActionResult> InitiateBlobUpload(string name)
    {
        var uuid = Guid.NewGuid().ToString("N");
        var uploadPath = GetUploadPath(uuid);
        Directory.CreateDirectory(Path.GetDirectoryName(uploadPath)!);
        await System.IO.File.WriteAllBytesAsync(uploadPath, Array.Empty<byte>());

        using var db = _dbFactory.CreateDbContext();
        db.ContainerUploadSessions.Add(new ContainerUploadSession
        {
            Uuid = uuid,
            RepositoryName = name
        });
        await db.SaveChangesAsync();

        Response.Headers["Location"] = $"/v2/{name}/blobs/uploads/{uuid}";
        Response.Headers["Docker-Upload-UUID"] = uuid;
        Response.Headers["Range"] = "0-0";
        return Accepted();
    }

    private async Task<IActionResult> UploadBlobChunk(string name, string reference)
    {
        var uuid = reference.Replace("uploads/", "");
        var uploadPath = GetUploadPath(uuid);
        if (!System.IO.File.Exists(uploadPath))
            return NotFound(new { errors = new[] { new { code = "BLOB_UPLOAD_UNKNOWN", message = "Upload not found" } } });

        await using var fs = new FileStream(uploadPath, FileMode.Append);
        await Request.Body.CopyToAsync(fs);

        var size = fs.Length;
        Response.Headers["Location"] = $"/v2/{name}/blobs/uploads/{uuid}";
        Response.Headers["Docker-Upload-UUID"] = uuid;
        Response.Headers["Range"] = $"0-{size - 1}";
        return Accepted();
    }

    private async Task<IActionResult> CompleteBlobUpload(string name, string reference)
    {
        var uuid = reference.Replace("uploads/", "");
        var uploadPath = GetUploadPath(uuid);
        if (!System.IO.File.Exists(uploadPath))
            return NotFound(new { errors = new[] { new { code = "BLOB_UPLOAD_UNKNOWN", message = "Upload not found" } } });

        // Append any remaining body data
        if (Request.ContentLength > 0)
        {
            await using var fs = new FileStream(uploadPath, FileMode.Append);
            await Request.Body.CopyToAsync(fs);
        }

        var expectedDigest = Request.Query["digest"].FirstOrDefault();
        if (string.IsNullOrEmpty(expectedDigest))
            return BadRequest(new { errors = new[] { new { code = "DIGEST_INVALID", message = "Missing digest query parameter" } } });

        // Verify digest
        var hash = "sha256:" + BitConverter.ToString(SHA256.HashData(await System.IO.File.ReadAllBytesAsync(uploadPath))).Replace("-", "").ToLowerInvariant();
        if (hash != expectedDigest)
        {
            System.IO.File.Delete(uploadPath);
            return BadRequest(new { errors = new[] { new { code = "DIGEST_INVALID", message = "Digest mismatch" } } });
        }

        // Move to blob store
        var blobPath = GetBlobPath(expectedDigest);
        Directory.CreateDirectory(Path.GetDirectoryName(blobPath)!);
        System.IO.File.Move(uploadPath, blobPath, overwrite: true);

        var fi = new FileInfo(blobPath);

        // Record in DB
        using var db = _dbFactory.CreateDbContext();
        if (!await db.ContainerBlobs.AnyAsync(b => b.RepositoryName == name && b.Digest == expectedDigest))
        {
            db.ContainerBlobs.Add(new ContainerBlob
            {
                RepositoryName = name,
                Digest = expectedDigest,
                Size = fi.Length
            });
        }

        // Clean up upload session
        var session = await db.ContainerUploadSessions.FirstOrDefaultAsync(s => s.Uuid == uuid);
        if (session != null) db.ContainerUploadSessions.Remove(session);

        await db.SaveChangesAsync();

        Response.Headers["Docker-Content-Digest"] = expectedDigest;
        Response.Headers["Location"] = $"/v2/{name}/blobs/{expectedDigest}";
        return StatusCode(201);
    }

    /// <summary>
    /// Parse OCI routes like: {name}/manifests/{reference}, {name}/blobs/{digest}, {name}/tags/list
    /// OCI names can contain slashes, so we look for known action segments.
    /// </summary>
    private static (string? name, string? action, string? reference) ParseRoute(string rest)
    {
        // Look for known action segments
        string[] actions = ["/manifests/", "/blobs/", "/tags/"];
        foreach (var action in actions)
        {
            var idx = rest.IndexOf(action, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var name = rest[..idx];
                var actionName = action.Trim('/').Split('/')[0]; // "manifests", "blobs", "tags"
                var reference = rest[(idx + action.Length)..];

                // For tags/list
                if (actionName == "tags")
                    return (name, "tags", reference);

                // For blobs/uploads/ (POST initiate or PATCH chunk or PUT complete)
                if (actionName == "blobs" && reference.StartsWith("uploads"))
                    return (name, "blobs", reference.EndsWith("/") || reference == "uploads" ? "uploads/" : reference);

                return (name, actionName, reference);
            }
        }

        return (null, null, null);
    }
}
