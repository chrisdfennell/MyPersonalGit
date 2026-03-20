using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/v1/repos/{repoName}/dependency-updates")]
[EnableRateLimiting("api")]
public class DependencyUpdatesController : ControllerBase
{
    private readonly IDependencyUpdateService _updateService;

    public DependencyUpdatesController(IDependencyUpdateService updateService)
    {
        _updateService = updateService;
    }

    [HttpGet]
    public async Task<IActionResult> GetConfigs(string repoName)
    {
        var configs = await _updateService.GetConfigsAsync(repoName);
        return Ok(configs.Select(c => new
        {
            c.Id, c.RepoName, c.Ecosystem, c.Schedule, c.IsEnabled,
            c.Directory, open_pr_limit = c.OpenPRLimit,
            last_run_at = c.LastRunAt, created_at = c.CreatedAt
        }));
    }

    [HttpPost]
    public async Task<IActionResult> EnableUpdates(string repoName, [FromBody] EnableUpdatesRequest request)
    {
        var config = await _updateService.EnableUpdatesAsync(repoName, request.Ecosystem, request.Schedule ?? "weekly");
        return Ok(new
        {
            config.Id, config.RepoName, config.Ecosystem, config.Schedule,
            config.IsEnabled, created_at = config.CreatedAt
        });
    }

    [HttpDelete("{configId:int}")]
    public async Task<IActionResult> DisableUpdates(string repoName, int configId)
    {
        var result = await _updateService.DisableUpdatesAsync(repoName, configId);
        if (!result) return NotFound(new { error = "Config not found" });
        return Ok(new { success = true });
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(string repoName, [FromQuery] int limit = 50)
    {
        var logs = await _updateService.GetLogsAsync(repoName, limit);
        return Ok(logs.Select(l => new
        {
            l.Id, l.PackageName, current_version = l.CurrentVersion,
            new_version = l.NewVersion, pull_request_number = l.PullRequestNumber,
            created_at = l.CreatedAt
        }));
    }

    [HttpPost("check")]
    public async Task<IActionResult> RunCheck(string repoName)
    {
        await _updateService.RunUpdateCheckAsync(repoName);
        return Ok(new { success = true, message = "Update check completed" });
    }

    public record EnableUpdatesRequest(string Ecosystem, string? Schedule);
}
