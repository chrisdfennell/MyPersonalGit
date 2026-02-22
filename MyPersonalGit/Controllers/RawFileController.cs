using LibGit2Sharp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
public class RawFileController : ControllerBase
{
    private readonly IRepositoryService _repoService;
    private readonly IConfiguration _config;
    private readonly IAdminService _adminService;

    public RawFileController(IRepositoryService repoService, IConfiguration config, IAdminService adminService)
    {
        _repoService = repoService;
        _config = config;
        _adminService = adminService;
    }

    [HttpGet("raw/{repoName}/{*path}")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> GetRawFile(string repoName, string path, [FromQuery] string? branch = null)
    {
        if (string.IsNullOrEmpty(path))
            return NotFound();

        // Resolve project root
        var systemSettings = await _adminService.GetSystemSettingsAsync();
        var projectRoot = !string.IsNullOrEmpty(systemSettings.ProjectRoot)
            ? systemSettings.ProjectRoot
            : _config["Git:ProjectRoot"] ?? "/repos";

        // Resolve repo path (same 3-check pattern as RepoDetails.razor)
        var repoPath = ResolveRepoPath(projectRoot, repoName);
        if (repoPath == null)
            return NotFound();

        // Enforce private repo access
        var meta = await _repoService.GetRepositoryAsync(repoName);
        if (meta is { IsPrivate: true })
            return NotFound();

        try
        {
            using var repo = new Repository(repoPath);
            var targetBranch = repo.Branches[branch ?? ""]
                ?? repo.Branches["main"]
                ?? repo.Branches["master"]
                ?? repo.Head;

            if (targetBranch?.Tip == null)
                return NotFound();

            var entry = targetBranch.Tip[path];
            if (entry == null || entry.TargetType != TreeEntryTargetType.Blob)
                return NotFound();

            var blob = (Blob)entry.Target;

            // 50MB size limit
            if (blob.Size > 50 * 1024 * 1024)
                return StatusCode(413);

            // Determine MIME type
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
                contentType = "application/octet-stream";

            // SVG XSS protection
            if (contentType == "image/svg+xml")
                Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'";

            // Copy blob to MemoryStream (repo is disposed before response is sent)
            var ms = new MemoryStream();
            using (var blobStream = blob.GetContentStream())
            {
                await blobStream.CopyToAsync(ms);
            }
            ms.Position = 0;

            return File(ms, contentType);
        }
        catch
        {
            return NotFound();
        }
    }

    private static string? ResolveRepoPath(string projectRoot, string repoName)
    {
        var path = Path.Combine(projectRoot, repoName);
        if (Repository.IsValid(path)) return path;
        if (Repository.IsValid(path + ".git")) return path + ".git";
        var nested = Path.Combine(path, repoName + ".git");
        if (Repository.IsValid(nested)) return nested;
        return null;
    }
}
