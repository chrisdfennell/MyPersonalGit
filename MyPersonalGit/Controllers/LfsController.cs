using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Data;
using MyPersonalGit.Models;
using MyPersonalGit.Services;

namespace MyPersonalGit.Controllers;

/// <summary>
/// Git LFS Batch API implementation.
/// Handles object negotiation, upload, and download.
/// Spec: https://github.com/git-lfs/git-lfs/blob/main/docs/api/batch.md
/// </summary>
[ApiController]
public class LfsController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<LfsController> _logger;

    public LfsController(IDbContextFactory<AppDbContext> dbFactory, IConfiguration config, ILogger<LfsController> logger)
    {
        _dbFactory = dbFactory;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// LFS Batch API — negotiates download/upload URLs for objects.
    /// </summary>
    [HttpPost("/git/{repoName}.git/info/lfs/objects/batch")]
    [Consumes("application/vnd.git-lfs+json", "application/json")]
    public async Task<IActionResult> Batch(string repoName, [FromBody] LfsBatchRequest request)
    {
        if (request?.Objects == null || request.Objects.Count == 0)
            return BadRequest(new { message = "No objects in request." });

        using var db = _dbFactory.CreateDbContext();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var responseObjects = new List<object>();

        foreach (var obj in request.Objects)
        {
            if (request.Operation == "download")
            {
                var existing = await db.LfsObjects.FirstOrDefaultAsync(o => o.RepoName == repoName && o.Oid == obj.Oid);
                if (existing != null)
                {
                    var filePath = GetLfsObjectPath(repoName, obj.Oid);
                    if (filePath != null && System.IO.File.Exists(filePath))
                    {
                        responseObjects.Add(new
                        {
                            oid = obj.Oid,
                            size = existing.Size,
                            actions = new
                            {
                                download = new
                                {
                                    href = $"{baseUrl}/git/{repoName}.git/info/lfs/objects/{obj.Oid}",
                                    header = new Dictionary<string, string>()
                                }
                            }
                        });
                        continue;
                    }
                }

                responseObjects.Add(new
                {
                    oid = obj.Oid,
                    size = obj.Size,
                    error = new { code = 404, message = "Object not found" }
                });
            }
            else if (request.Operation == "upload")
            {
                var existing = await db.LfsObjects.FirstOrDefaultAsync(o => o.RepoName == repoName && o.Oid == obj.Oid);
                if (existing != null)
                {
                    // Already uploaded — no action needed
                    responseObjects.Add(new
                    {
                        oid = obj.Oid,
                        size = existing.Size
                    });
                }
                else
                {
                    responseObjects.Add(new
                    {
                        oid = obj.Oid,
                        size = obj.Size,
                        actions = new
                        {
                            upload = new
                            {
                                href = $"{baseUrl}/git/{repoName}.git/info/lfs/objects/{obj.Oid}",
                                header = new Dictionary<string, string>()
                            }
                        }
                    });
                }
            }
        }

        return new JsonResult(new { transfer = "basic", objects = responseObjects })
        {
            ContentType = "application/vnd.git-lfs+json",
            StatusCode = 200
        };
    }

    /// <summary>
    /// Upload an LFS object.
    /// </summary>
    [HttpPut("/git/{repoName}.git/info/lfs/objects/{oid}")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(string repoName, string oid)
    {
        var filePath = GetLfsObjectPath(repoName, oid);
        if (filePath == null)
            return BadRequest(new { message = "Invalid object id." });
        var dir = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(dir);

        await using (var fs = new FileStream(filePath, FileMode.Create))
        {
            await Request.Body.CopyToAsync(fs);
        }

        var fileInfo = new FileInfo(filePath);

        using var db = _dbFactory.CreateDbContext();
        var existing = await db.LfsObjects.FirstOrDefaultAsync(o => o.RepoName == repoName && o.Oid == oid);
        if (existing == null)
        {
            db.LfsObjects.Add(new LfsObject
            {
                RepoName = repoName,
                Oid = oid,
                Size = fileInfo.Length,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        _logger.LogInformation("LFS object uploaded: {Repo}/{Oid} ({Size} bytes)", repoName, oid, fileInfo.Length);
        return Ok();
    }

    /// <summary>
    /// Download an LFS object.
    /// </summary>
    [HttpGet("/git/{repoName}.git/info/lfs/objects/{oid}")]
    public IActionResult Download(string repoName, string oid)
    {
        var filePath = GetLfsObjectPath(repoName, oid);
        if (filePath == null || !System.IO.File.Exists(filePath))
            return NotFound(new { message = "Object not found." });

        return PhysicalFile(filePath, "application/octet-stream");
    }

    private string? GetLfsObjectPath(string repoName, string oid)
    {
        // oid is an attacker-controlled route param — must be a 64-char hex SHA-256
        // before we slice it ([..2]/[2..4] would throw on short input) or use it in a path.
        if (!IsValidLfsOid(oid)) return null;
        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        var lfsBase = Path.Combine(projectRoot, ".lfs");
        // Store in <root>/.lfs/{repoName}/{oid[0:2]}/{oid[2:4]}/{oid} — route every
        // user-controlled segment (repoName, oid) through SafePath to block traversal.
        return SafePath.CombineUnder(lfsBase, repoName, oid[..2], oid[2..4], oid);
    }

    private static bool IsValidLfsOid(string? oid)
        => !string.IsNullOrEmpty(oid)
           && oid.Length == 64
           && oid.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
}

// Request/response DTOs for Git LFS Batch API
public class LfsBatchRequest
{
    public string Operation { get; set; } = "";
    public List<LfsBatchObject> Objects { get; set; } = new();
}

public class LfsBatchObject
{
    public string Oid { get; set; } = "";
    public long Size { get; set; }
}
