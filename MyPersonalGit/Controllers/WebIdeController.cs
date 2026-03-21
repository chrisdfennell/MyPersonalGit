using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

// --- Request / Response DTOs ---

public record IdeGetMultipleFilesRequest(List<string> Paths);

public record IdeFileChangeDto(string Path, string? Content, string Action, string? OldPath = null);

public record IdeCommitRequest(string Branch, string Message, List<IdeFileChangeDto> Changes);

public record IdeCreateFileRequest(string Branch, string Path, string Content);

public record IdeDeletePathRequest(string Branch, string Path);

public record IdeRenameRequest(string Branch, string OldPath, string NewPath);

// --- Controller ---

[ApiController]
[Route("api/v1/ide")]
[EnableRateLimiting("api")]
public class WebIdeController : ControllerBase
{
    private readonly IWebIdeService _ideService;
    private readonly IRepositoryService _repoService;
    private readonly IAuthService _authService;
    private readonly IConfiguration _config;
    private readonly ILogger<WebIdeController> _logger;

    public WebIdeController(
        IWebIdeService ideService,
        IRepositoryService repoService,
        IAuthService authService,
        IConfiguration config,
        ILogger<WebIdeController> logger)
    {
        _ideService = ideService;
        _repoService = repoService;
        _authService = authService;
        _config = config;
        _logger = logger;
    }

    // ── helpers ────────────────────────────────────────────────────

    private async Task<IActionResult?> EnsureAuthenticatedAndAuthorized(string repoName)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized(new { error = "Authentication required" });

        var repo = await _repoService.GetRepositoryAsync(repoName);
        if (repo == null)
            return NotFound(new { error = $"Repository '{repoName}' not found" });

        if (repo.IsPrivate)
        {
            var currentUser = User.Identity?.Name;
            if (currentUser == null || !repo.Owner.Equals(currentUser, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = $"Repository '{repoName}' not found" });
        }

        return null; // authorized
    }

    private async Task<(string username, string email)> GetCurrentUserInfoAsync()
    {
        var username = User.Identity?.Name ?? "api-user";
        var user = await _authService.GetUserByUsernameAsync(username);
        var email = user?.Email ?? $"{username}@local";
        return (username, email);
    }

    // ── 1. GET {repoName}/tree ────────────────────────────────────

    [HttpGet("{repoName}/tree")]
    public async Task<IActionResult> GetTree(string repoName, [FromQuery] string? branch = null)
    {
        var authResult = await EnsureAuthenticatedAndAuthorized(repoName);
        if (authResult != null) return authResult;

        try
        {
            var tree = await _ideService.GetFullTreeAsync(repoName, branch ?? "");
            if (tree == null)
                return NotFound(new { error = $"Branch '{branch}' not found in repository '{repoName}'" });

            return Ok(tree);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tree for {Repo}/{Branch}", repoName, branch);
            return Problem(detail: ex.Message, title: "Failed to retrieve tree", statusCode: 500);
        }
    }

    // ── 2. GET {repoName}/file/{*path} ────────────────────────────

    [HttpGet("{repoName}/file/{*path}")]
    public async Task<IActionResult> GetFile(string repoName, string path, [FromQuery] string? branch = null)
    {
        var authResult = await EnsureAuthenticatedAndAuthorized(repoName);
        if (authResult != null) return authResult;

        try
        {
            var file = await _ideService.GetFileContentAsync(repoName, branch ?? "", path);
            if (file == null)
                return NotFound(new { error = $"File '{path}' not found" });

            return Ok(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file {Path} for {Repo}/{Branch}", path, repoName, branch);
            return Problem(detail: ex.Message, title: "Failed to retrieve file", statusCode: 500);
        }
    }

    // ── 3. POST {repoName}/files ──────────────────────────────────

    [HttpPost("{repoName}/files")]
    public async Task<IActionResult> GetMultipleFiles(
        string repoName,
        [FromQuery] string? branch,
        [FromBody] IdeGetMultipleFilesRequest request)
    {
        var authResult = await EnsureAuthenticatedAndAuthorized(repoName);
        if (authResult != null) return authResult;

        if (request.Paths == null || request.Paths.Count == 0)
            return BadRequest(new { error = "At least one path is required" });

        try
        {
            var files = await _ideService.GetMultipleFilesAsync(repoName, branch ?? "", request.Paths);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get multiple files for {Repo}/{Branch}", repoName, branch);
            return Problem(detail: ex.Message, title: "Failed to retrieve files", statusCode: 500);
        }
    }

    // ── 4. POST {repoName}/commit ─────────────────────────────────

    [HttpPost("{repoName}/commit")]
    public async Task<IActionResult> Commit(string repoName, [FromBody] IdeCommitRequest request)
    {
        var authResult = await EnsureAuthenticatedAndAuthorized(repoName);
        if (authResult != null) return authResult;

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Commit message is required" });

        if (request.Changes == null || request.Changes.Count == 0)
            return BadRequest(new { error = "At least one change is required" });

        try
        {
            var (username, email) = await GetCurrentUserInfoAsync();

            var changes = request.Changes.Select(c =>
                new FileChange(c.Path, c.Content ?? "", c.Action, c.OldPath)).ToList();

            await _ideService.CommitMultipleFilesAsync(
                repoName, request.Branch ?? "", username, email, request.Message, changes);

            return Ok(new { message = "Commit successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit to {Repo}/{Branch}", repoName, request.Branch);
            return Problem(detail: ex.Message, title: "Commit failed", statusCode: 500);
        }
    }

    // ── 5. POST {repoName}/create-file ────────────────────────────

    [HttpPost("{repoName}/create-file")]
    public async Task<IActionResult> CreateFile(string repoName, [FromBody] IdeCreateFileRequest request)
    {
        var authResult = await EnsureAuthenticatedAndAuthorized(repoName);
        if (authResult != null) return authResult;

        if (string.IsNullOrWhiteSpace(request.Path))
            return BadRequest(new { error = "File path is required" });

        try
        {
            var (username, email) = await GetCurrentUserInfoAsync();

            await _ideService.CreateFileAsync(
                repoName, request.Branch ?? "", request.Path, request.Content ?? "", username, email);

            return Created($"api/v1/ide/{repoName}/file/{request.Path}",
                new { path = request.Path, message = "File created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create file {Path} in {Repo}/{Branch}", request.Path, repoName, request.Branch);
            return Problem(detail: ex.Message, title: "Failed to create file", statusCode: 500);
        }
    }

    // ── 6. POST {repoName}/delete-path ────────────────────────────

    [HttpPost("{repoName}/delete-path")]
    public async Task<IActionResult> DeletePath(string repoName, [FromBody] IdeDeletePathRequest request)
    {
        var authResult = await EnsureAuthenticatedAndAuthorized(repoName);
        if (authResult != null) return authResult;

        if (string.IsNullOrWhiteSpace(request.Path))
            return BadRequest(new { error = "Path is required" });

        try
        {
            var (username, email) = await GetCurrentUserInfoAsync();

            await _ideService.DeletePathAsync(
                repoName, request.Branch ?? "", request.Path, username, email);

            return Ok(new { message = $"'{request.Path}' deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete {Path} in {Repo}/{Branch}", request.Path, repoName, request.Branch);
            return Problem(detail: ex.Message, title: "Failed to delete path", statusCode: 500);
        }
    }

    // ── 7. POST {repoName}/rename ─────────────────────────────────

    [HttpPost("{repoName}/rename")]
    public async Task<IActionResult> Rename(string repoName, [FromBody] IdeRenameRequest request)
    {
        var authResult = await EnsureAuthenticatedAndAuthorized(repoName);
        if (authResult != null) return authResult;

        if (string.IsNullOrWhiteSpace(request.OldPath) || string.IsNullOrWhiteSpace(request.NewPath))
            return BadRequest(new { error = "Both oldPath and newPath are required" });

        try
        {
            var (username, email) = await GetCurrentUserInfoAsync();

            await _ideService.RenamePathAsync(
                repoName, request.Branch ?? "", request.OldPath, request.NewPath, username, email);

            return Ok(new { message = $"Renamed '{request.OldPath}' to '{request.NewPath}'" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rename {OldPath} to {NewPath} in {Repo}/{Branch}",
                request.OldPath, request.NewPath, repoName, request.Branch);
            return Problem(detail: ex.Message, title: "Rename failed", statusCode: 500);
        }
    }

    // ── 8. GET {repoName}/blame/{*path} ─────────────────────────────

    [HttpGet("{repoName}/blame/{*path}")]
    public async Task<IActionResult> GetBlame(string repoName, string path, [FromQuery] string? branch = null)
    {
        var authResult = await EnsureAuthenticatedAndAuthorized(repoName);
        if (authResult != null) return authResult;

        try
        {
            var blame = await _ideService.GetFileBlameAsync(repoName, branch ?? "", path);
            return Ok(blame);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get blame for {Path} in {Repo}/{Branch}", path, repoName, branch);
            return Problem(detail: ex.Message, title: "Failed to retrieve blame", statusCode: 500);
        }
    }

    // ── 9. GET {repoName}/history/{*path} ─────────────────────────

    [HttpGet("{repoName}/history/{*path}")]
    public async Task<IActionResult> GetFileHistory(string repoName, string path, [FromQuery] string? branch = null)
    {
        var authResult = await EnsureAuthenticatedAndAuthorized(repoName);
        if (authResult != null) return authResult;

        try
        {
            var history = await _ideService.GetFileHistoryAsync(repoName, branch ?? "", path);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get history for {Path} in {Repo}/{Branch}", path, repoName, branch);
            return Problem(detail: ex.Message, title: "Failed to retrieve file history", statusCode: 500);
        }
    }

    // ── 10. GET {repoName}/search ─────────────────────────────────

    [HttpGet("{repoName}/search")]
    public async Task<IActionResult> Search(
        string repoName,
        [FromQuery] string q,
        [FromQuery] string? branch = null,
        [FromQuery] string? ext = null)
    {
        var authResult = await EnsureAuthenticatedAndAuthorized(repoName);
        if (authResult != null) return authResult;

        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(new { error = "Query must be at least 2 characters" });

        try
        {
            var results = await _ideService.SearchFilesAsync(repoName, branch ?? "", q, ext);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search {Repo}/{Branch} for '{Query}'", repoName, branch, q);
            return Problem(detail: ex.Message, title: "Search failed", statusCode: 500);
        }
    }
}
