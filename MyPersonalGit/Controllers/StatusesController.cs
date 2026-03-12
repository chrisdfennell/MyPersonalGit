using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Controllers;

/// <summary>
/// Commit status checks API — allows external CI systems to report status on commits.
/// Similar to GitHub's commit status API.
/// </summary>
[ApiController]
[Route("api/v1/repos/{repoName}/statuses")]
public class StatusesController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<StatusesController> _logger;

    public StatusesController(IDbContextFactory<AppDbContext> dbFactory, ILogger<StatusesController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Create or update a commit status. If a status with the same context already exists
    /// for this SHA, it will be updated instead of creating a duplicate.
    /// </summary>
    [HttpPost("{sha}")]
    public async Task<IActionResult> CreateStatus(string repoName, string sha, [FromBody] CreateStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Context))
            return BadRequest(new { error = "context is required" });

        if (!Enum.TryParse<CommitStatusState>(request.State, true, out var state))
            return BadRequest(new { error = "state must be one of: pending, success, failure, error" });

        var creator = User.FindFirst(ClaimTypes.Name)?.Value ?? "api";

        using var db = _dbFactory.CreateDbContext();

        // Upsert: update existing status for same repo+sha+context, or create new
        var existing = await db.CommitStatuses
            .FirstOrDefaultAsync(s => s.RepoName == repoName && s.Sha == sha && s.Context == request.Context);

        if (existing != null)
        {
            existing.State = state;
            existing.Description = request.Description;
            existing.TargetUrl = request.TargetUrl;
            existing.Creator = creator;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.CommitStatuses.Add(new CommitStatus
            {
                RepoName = repoName,
                Sha = sha,
                State = state,
                Context = request.Context,
                Description = request.Description,
                TargetUrl = request.TargetUrl,
                Creator = creator,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();

        _logger.LogInformation("Commit status {Context}={State} set for {RepoName}@{Sha} by {Creator}",
            request.Context, state, repoName, sha[..7], creator);

        return Ok(new { state = state.ToString().ToLower(), context = request.Context, sha, description = request.Description });
    }

    /// <summary>
    /// Get all statuses for a specific commit SHA.
    /// </summary>
    [HttpGet("{sha}")]
    public async Task<IActionResult> GetStatuses(string repoName, string sha)
    {
        using var db = _dbFactory.CreateDbContext();

        var statuses = await db.CommitStatuses
            .Where(s => s.RepoName == repoName && s.Sha == sha)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();

        // Compute combined status (GitHub-style):
        // - If any are error/failure, combined is failure
        // - If any are pending, combined is pending
        // - If all are success, combined is success
        var combinedState = "success";
        if (!statuses.Any())
            combinedState = "pending";
        else if (statuses.Any(s => s.State == CommitStatusState.Error || s.State == CommitStatusState.Failure))
            combinedState = "failure";
        else if (statuses.Any(s => s.State == CommitStatusState.Pending))
            combinedState = "pending";

        return Ok(new
        {
            state = combinedState,
            total_count = statuses.Count,
            statuses = statuses.Select(s => new
            {
                id = s.Id,
                state = s.State.ToString().ToLower(),
                context = s.Context,
                description = s.Description,
                target_url = s.TargetUrl,
                creator = s.Creator,
                created_at = s.CreatedAt,
                updated_at = s.UpdatedAt
            })
        });
    }
}

public class CreateStatusRequest
{
    public string State { get; set; } = ""; // pending, success, failure, error
    public string Context { get; set; } = ""; // e.g. "ci/build"
    public string? Description { get; set; }
    public string? TargetUrl { get; set; }
}
