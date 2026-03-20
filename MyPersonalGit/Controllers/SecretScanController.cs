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

    public SecretScanController(ISecretScanService secretScanService)
    {
        _secretScanService = secretScanService;
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> ListAlerts(string repoName, [FromQuery] string? state = null)
    {
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
        if (!Enum.TryParse<SecretScanResultState>(request.State, true, out var state) ||
            state == SecretScanResultState.Open)
            return BadRequest(new { error = "state must be 'resolved' or 'false_positive'" });

        var username = User.Identity?.Name ?? "api-user";
        var result = await _secretScanService.ResolveResultAsync(id, username, state);
        if (!result) return NotFound(new { error = "Alert not found" });

        return Ok(new { success = true });
    }

    [HttpPost("scan")]
    public async Task<IActionResult> TriggerFullScan(string repoName, [FromServices] IConfiguration config, [FromServices] IAdminService adminService)
    {
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
