using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/v1/runners")]
public class RunnerController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public RunnerController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Register a new CI runner.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRunnerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Runner name is required" });
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { error = "Runner token is required" });

        await using var db = await _dbFactory.CreateDbContextAsync();

        // Check for duplicate token
        if (await db.Runners.AnyAsync(r => r.Token == request.Token))
            return Conflict(new { error = "A runner with this token already exists" });

        var runner = new Runner
        {
            Name = request.Name,
            Token = request.Token,
            Labels = request.Labels ?? Array.Empty<string>(),
            IsOnline = true,
            LastHeartbeat = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        db.Runners.Add(runner);
        await db.SaveChangesAsync();

        return Ok(new { id = runner.Id, name = runner.Name, labels = runner.Labels });
    }

    /// <summary>
    /// Runner polls for the next available queued job.
    /// Also acts as a heartbeat — updates the runner's last-seen time.
    /// </summary>
    [HttpGet("jobs/request")]
    public async Task<IActionResult> RequestJob([FromHeader(Name = "X-Runner-Token")] string? runnerToken)
    {
        if (string.IsNullOrWhiteSpace(runnerToken))
            return Unauthorized(new { error = "X-Runner-Token header is required" });

        await using var db = await _dbFactory.CreateDbContextAsync();

        var runner = await db.Runners.FirstOrDefaultAsync(r => r.Token == runnerToken);
        if (runner == null)
            return Unauthorized(new { error = "Invalid runner token" });

        // Update heartbeat
        runner.IsOnline = true;
        runner.LastHeartbeat = DateTime.UtcNow;

        // Find the next queued job (optionally matching runner labels via RunsOn)
        var query = db.WorkflowJobs
            .Where(j => j.Status == WorkflowStatus.Queued)
            .OrderBy(j => j.Id);

        WorkflowJob? job;
        if (runner.Labels.Length > 0)
        {
            // Prefer jobs whose RunsOn matches one of the runner's labels
            job = await query
                .FirstOrDefaultAsync(j => runner.Labels.Contains(j.RunsOn));
            // Fall back to any queued job if no label match
            job ??= await query.FirstOrDefaultAsync();
        }
        else
        {
            job = await query.FirstOrDefaultAsync();
        }

        if (job == null)
        {
            await db.SaveChangesAsync(); // persist heartbeat
            return NoContent(); // 204 — no jobs available
        }

        // Claim the job
        job.Status = WorkflowStatus.InProgress;
        job.StartedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(new
        {
            jobId = job.Id,
            name = job.Name,
            runsOn = job.RunsOn,
            workflowRunId = job.WorkflowRunId
        });
    }

    /// <summary>
    /// Runner reports job status (running, success, failure) with optional log output.
    /// </summary>
    [HttpPut("jobs/{jobId}/status")]
    public async Task<IActionResult> UpdateJobStatus(
        int jobId,
        [FromHeader(Name = "X-Runner-Token")] string? runnerToken,
        [FromBody] UpdateJobStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(runnerToken))
            return Unauthorized(new { error = "X-Runner-Token header is required" });

        await using var db = await _dbFactory.CreateDbContextAsync();

        var runner = await db.Runners.FirstOrDefaultAsync(r => r.Token == runnerToken);
        if (runner == null)
            return Unauthorized(new { error = "Invalid runner token" });

        var job = await db.WorkflowJobs
            .Include(j => j.Steps)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
            return NotFound(new { error = "Job not found" });

        // Map status string to enum
        if (!Enum.TryParse<WorkflowStatus>(request.Status, ignoreCase: true, out var status))
            return BadRequest(new { error = "Invalid status. Expected: Running, Success, Failure, Cancelled" });

        job.Status = status;

        if (status is WorkflowStatus.Success or WorkflowStatus.Failure or WorkflowStatus.Cancelled)
        {
            job.CompletedAt = DateTime.UtcNow;
        }

        // Append log output to the last step (or create a summary step)
        if (!string.IsNullOrEmpty(request.Log))
        {
            var lastStep = job.Steps.OrderByDescending(s => s.Id).FirstOrDefault();
            if (lastStep != null)
            {
                lastStep.Output = string.IsNullOrEmpty(lastStep.Output)
                    ? request.Log
                    : lastStep.Output + "\n" + request.Log;
                lastStep.Status = status;
                if (status is WorkflowStatus.Success or WorkflowStatus.Failure or WorkflowStatus.Cancelled)
                    lastStep.CompletedAt = DateTime.UtcNow;
            }
        }

        // Update heartbeat
        runner.LastHeartbeat = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(new { jobId = job.Id, status = job.Status.ToString() });
    }

    /// <summary>
    /// Get job details including steps, environment, and commands.
    /// </summary>
    [HttpGet("jobs/{jobId}")]
    public async Task<IActionResult> GetJob(
        int jobId,
        [FromHeader(Name = "X-Runner-Token")] string? runnerToken)
    {
        if (string.IsNullOrWhiteSpace(runnerToken))
            return Unauthorized(new { error = "X-Runner-Token header is required" });

        await using var db = await _dbFactory.CreateDbContextAsync();

        var runner = await db.Runners.FirstOrDefaultAsync(r => r.Token == runnerToken);
        if (runner == null)
            return Unauthorized(new { error = "Invalid runner token" });

        var job = await db.WorkflowJobs
            .Include(j => j.Steps)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
            return NotFound(new { error = "Job not found" });

        // Load the parent workflow run for context (repo, branch, commit)
        var run = await db.WorkflowRuns.FirstOrDefaultAsync(r => r.Id == job.WorkflowRunId);

        // Load repository secrets to pass as env vars
        var secrets = run != null
            ? await db.RepositorySecrets
                .Where(s => s.RepoName == run.RepoName)
                .Select(s => new { s.Name })
                .ToListAsync()
            : new();

        return Ok(new
        {
            jobId = job.Id,
            name = job.Name,
            status = job.Status.ToString(),
            runsOn = job.RunsOn,
            needs = job.Needs,
            condition = job.Condition,
            timeoutMinutes = job.TimeoutMinutes,
            workflowRun = run != null ? new
            {
                id = run.Id,
                repoName = run.RepoName,
                branch = run.Branch,
                commitSha = run.CommitSha,
                workflowName = run.WorkflowName
            } : null,
            steps = job.Steps.OrderBy(s => s.Id).Select(s => new
            {
                id = s.Id,
                name = s.Name,
                command = s.Command,
                condition = s.Condition,
                continueOnError = s.ContinueOnError,
                status = s.Status.ToString()
            }),
            secretNames = secrets.Select(s => s.Name)
        });
    }

    // --- Request DTOs ---

    public class RegisterRunnerRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string[]? Labels { get; set; }
    }

    public class UpdateJobStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Log { get; set; }
    }
}
