using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/v1/repos/{repoName}/environments")]
[EnableRateLimiting("api")]
public class EnvironmentsController : ControllerBase
{
    private readonly IEnvironmentService _environmentService;

    public EnvironmentsController(IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
    }

    [HttpGet]
    public async Task<IActionResult> ListEnvironments(string repoName)
    {
        var envs = await _environmentService.GetEnvironmentsAsync(repoName);
        return Ok(envs.Select(e => new
        {
            e.Id, e.Name, e.Url, wait_timer_minutes = e.WaitTimerMinutes,
            require_approval = e.RequireApproval, required_reviewers = e.RequiredReviewers,
            allowed_branches = e.AllowedBranches, created_at = e.CreatedAt
        }));
    }

    [HttpPut("{name}")]
    public async Task<IActionResult> CreateOrUpdate(string repoName, string name, [FromBody] EnvironmentRequest request)
    {
        var env = await _environmentService.CreateOrUpdateEnvironmentAsync(
            repoName, name, request.Url, request.WaitTimerMinutes,
            request.RequireApproval, request.RequiredReviewers, request.AllowedBranches);
        return Ok(new { env.Id, env.Name, env.Url, created_at = env.CreatedAt });
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string repoName, string name)
    {
        var result = await _environmentService.DeleteEnvironmentAsync(repoName, name);
        if (!result) return NotFound(new { error = "Environment not found" });
        return NoContent();
    }

    [HttpGet("{name}/deployments")]
    public async Task<IActionResult> ListDeployments(string repoName, string name)
    {
        var deployments = await _environmentService.GetDeploymentsAsync(repoName, name);
        return Ok(deployments.Select(d => new
        {
            d.Id, environment = d.EnvironmentName, d.CommitSha, d.Ref, d.Creator,
            status = d.Status.ToString().ToLower(), d.Description,
            created_at = d.CreatedAt, started_at = d.StartedAt, completed_at = d.CompletedAt
        }));
    }

    [HttpPost("{name}/deployments")]
    public async Task<IActionResult> CreateDeployment(string repoName, string name, [FromBody] CreateDeploymentRequest request)
    {
        var username = User.Identity?.Name ?? "api-user";
        try
        {
            var deployment = await _environmentService.CreateDeploymentAsync(
                repoName, name, request.CommitSha, request.Ref, username, description: request.Description);
            return Created($"/api/v1/repos/{repoName}/environments/{name}/deployments/{deployment.Id}", new
            {
                deployment.Id, environment = deployment.EnvironmentName,
                status = deployment.Status.ToString().ToLower(), created_at = deployment.CreatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{name}/deployments/{id:int}/approve")]
    public async Task<IActionResult> ApproveDeployment(string repoName, string name, int id, [FromBody] ApproveRequest request)
    {
        var username = User.Identity?.Name ?? "api-user";
        var result = await _environmentService.ApproveDeploymentAsync(id, username, request.Approved, request.Comment);
        if (!result) return NotFound(new { error = "Deployment not found or not awaiting approval" });
        return Ok(new { success = true });
    }

    [HttpPost("{name}/deployments/{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(string repoName, string name, int id, [FromBody] UpdateStatusRequest request)
    {
        if (!Enum.TryParse<DeploymentStatus>(request.Status, true, out var status))
            return BadRequest(new { error = "Invalid status" });

        var result = await _environmentService.UpdateDeploymentStatusAsync(id, status);
        if (!result) return NotFound(new { error = "Deployment not found" });
        return Ok(new { success = true });
    }

    public record EnvironmentRequest(string? Url, int WaitTimerMinutes = 0, bool RequireApproval = false, List<string>? RequiredReviewers = null, List<string>? AllowedBranches = null);
    public record CreateDeploymentRequest(string CommitSha, string Ref, string? Description = null);
    public record ApproveRequest(bool Approved, string? Comment = null);
    public record UpdateStatusRequest(string Status);
}
