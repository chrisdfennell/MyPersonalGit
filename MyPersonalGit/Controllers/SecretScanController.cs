using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/v1/repos/{repoName}/secret-scanning")]
[EnableRateLimiting("api")]
public class SecretScanController : ControllerBase
{
    private readonly ISecretScanService _secretScanService;
    private readonly IRepositoryService _repoService;
    private readonly ICollaboratorService _collaboratorService;

    public SecretScanController(ISecretScanService secretScanService, IRepositoryService repoService, ICollaboratorService collaboratorService)
    {
        _secretScanService = secretScanService;
        _repoService = repoService;
        _collaboratorService = collaboratorService;
    }

    // Secret-scanning alerts contain detected secrets — restrict to users who can write
    // the repo (owner or Write+ collaborator), mirroring GitHub's secret-scanning access.
    private async Task<IActionResult?> EnsureCanAccessAsync(string repoName)
    {
        var repo = await _repoService.GetRepositoryAsync(repoName);
        if (repo == null)
            return NotFound(new { error = $"Repository '{repoName}' not found" });

        var currentUser = User.Identity?.Name;
        if (currentUser == null)
            return Unauthorized(new { error = "Authentication required" });

        var isOwner = repo.Owner.Equals(currentUser, StringComparison.OrdinalIgnoreCase);
        if (isOwner || await _collaboratorService.HasPermissionAsync(repoName, currentUser, CollaboratorPermission.Write))
            return null;

        return repo.IsPrivate
            ? NotFound(new { error = $"Repository '{repoName}' not found" })
            : StatusCode(StatusCodes.Status403Forbidden, new { error = "You do not have access to secret scanning for this repository" });
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> ListAlerts(string repoName, [FromQuery] string? state = null)
    {
        var auth = await EnsureCanAccessAsync(repoName);
        if (auth != null) return auth;

        SecretScanResultState? stateFilter = null;
        if (!string.IsNullOrEmpty(state) && Enum.TryParse<SecretScanResultState>(state, true, out var s))
            stateFilter = s;

        var results = await _secretScanService.GetResultsAsync(repoName, stateFilter);
        return Ok(results.Select(r => new
        {
            r.Id,
            r.CommitSha,
            r.FilePath,
            r.LineNumber,
            secret_type = r.SecretType,
            match_snippet = r.MatchSnippet,
            state = r.State.ToString().ToLower(),
            r.ResolvedBy,
            detected_at = r.DetectedAt,
            resolved_at = r.ResolvedAt
        }));
    }

    [HttpPatch("alerts/{id:int}")]
    public async Task<IActionResult> ResolveAlert(string repoName, int id, [FromBody] ResolveAlertRequest request)
    {
        var auth = await EnsureCanAccessAsync(repoName);
        if (auth != null) return auth;

        if (!Enum.TryParse<SecretScanResultState>(request.State, true, out var state) ||
            state == SecretScanResultState.Open)
            return BadRequest(new { error = "state must be 'resolved' or 'false_positive'" });

        // Scope the alert id to this repo so a caller can't resolve another repo's alerts.
        var repoAlerts = await _secretScanService.GetResultsAsync(repoName, null);
        if (repoAlerts.All(a => a.Id != id))
            return NotFound(new { error = "Alert not found" });

        var username = User.Identity?.Name ?? "api-user";
        var result = await _secretScanService.ResolveResultAsync(id, username, state);
        if (!result) return NotFound(new { error = "Alert not found" });

        return Ok(new { success = true });
    }

    [HttpPost("scan")]
    public async Task<IActionResult> TriggerFullScan(string repoName, [FromServices] IConfiguration config, [FromServices] IAdminService adminService)
    {
        var auth = await EnsureCanAccessAsync(repoName);
        if (auth != null) return auth;

        var systemSettings = await adminService.GetSystemSettingsAsync();
        var projectRoot = !string.IsNullOrEmpty(systemSettings.ProjectRoot) ? systemSettings.ProjectRoot : config["Git:ProjectRoot"] ?? "/repos";
        var repoPath = Path.Combine(projectRoot, repoName);
        if (!LibGit2Sharp.Repository.IsValid(repoPath) && !LibGit2Sharp.Repository.IsValid(repoPath + ".git"))
            return NotFound(new { error = "Repository not found" });

        if (LibGit2Sharp.Repository.IsValid(repoPath + ".git")) repoPath += ".git";

        var results = await _secretScanService.FullScanAsync(repoName, repoPath);
        return Ok(new { count = results.Count, alerts = results.Take(20).Select(r => new { r.FilePath, r.LineNumber, r.SecretType }) });
    }

    [HttpGet("patterns")]
    public async Task<IActionResult> ListPatterns()
    {
        var patterns = await _secretScanService.GetPatternsAsync();
        return Ok(patterns.Select(p => new { p.Id, p.Name, p.Pattern, p.IsEnabled, p.IsBuiltIn }));
    }

    public record ResolveAlertRequest(string State);
}
