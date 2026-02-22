using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/v1/repos/{repoName}/pulls")]
[EnableRateLimiting("api")]
public class PullRequestsController : ControllerBase
{
    private readonly IPullRequestService _prService;

    public PullRequestsController(IPullRequestService prService)
    {
        _prService = prService;
    }

    [HttpGet]
    public async Task<IActionResult> ListPullRequests(string repoName, [FromQuery] string? state = null)
    {
        var prs = await _prService.GetPullRequestsAsync(repoName);

        if (!string.IsNullOrEmpty(state) && Enum.TryParse<Models.PullRequestState>(state, true, out var prState))
        {
            prs = prs.Where(pr => pr.State == prState).ToList();
        }

        return Ok(prs.Select(pr => new
        {
            pr.Number,
            pr.Title,
            pr.Body,
            state = pr.State.ToString().ToLower(),
            pr.Author,
            source_branch = pr.SourceBranch,
            target_branch = pr.TargetBranch,
            pr.IsDraft,
            pr.Labels,
            created_at = pr.CreatedAt,
            merged_at = pr.MergedAt,
            merged_by = pr.MergedBy
        }));
    }

    [HttpGet("{number:int}")]
    public async Task<IActionResult> GetPullRequest(string repoName, int number)
    {
        var pr = await _prService.GetPullRequestAsync(repoName, number);
        if (pr == null) return NotFound(new { error = $"Pull request #{number} not found" });

        return Ok(new
        {
            pr.Number,
            pr.Title,
            pr.Body,
            state = pr.State.ToString().ToLower(),
            pr.Author,
            source_branch = pr.SourceBranch,
            target_branch = pr.TargetBranch,
            pr.IsDraft,
            pr.Labels,
            created_at = pr.CreatedAt,
            merged_at = pr.MergedAt,
            merged_by = pr.MergedBy,
            reviews = pr.Reviews?.Select(r => new
            {
                r.Author,
                state = r.State.ToString().ToLower(),
                r.Body,
                created_at = r.CreatedAt
            }),
            comments = pr.Comments?.Select(c => new
            {
                c.Author,
                c.Body,
                created_at = c.CreatedAt
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePullRequest(string repoName, [FromBody] CreatePrRequest request)
    {
        var username = User.Identity?.Name ?? "api-user";
        var pr = await _prService.CreatePullRequestAsync(
            repoName, request.Title, request.Body, request.SourceBranch, request.TargetBranch, username);

        return Created($"/api/v1/repos/{repoName}/pulls/{pr.Number}", new
        {
            pr.Number,
            pr.Title,
            state = pr.State.ToString().ToLower(),
            pr.Author,
            source_branch = pr.SourceBranch,
            target_branch = pr.TargetBranch,
            created_at = pr.CreatedAt
        });
    }

    [HttpPost("{number:int}/merge")]
    public async Task<IActionResult> MergePullRequest(string repoName, int number)
    {
        var username = User.Identity?.Name ?? "api-user";
        var (success, error) = await _prService.MergePullRequestAsync(repoName, number, username);
        if (!success) return BadRequest(new { error = error ?? "Failed to merge pull request" });

        return Ok(new { message = $"Pull request #{number} merged successfully" });
    }

    [HttpPatch("{number:int}")]
    public async Task<IActionResult> UpdatePullRequest(string repoName, int number, [FromBody] UpdatePrRequest request)
    {
        if (request.State?.ToLower() == "closed")
        {
            await _prService.ClosePullRequestAsync(repoName, number);
        }

        var pr = await _prService.GetPullRequestAsync(repoName, number);
        if (pr == null) return NotFound(new { error = $"Pull request #{number} not found" });

        return Ok(new
        {
            pr.Number,
            pr.Title,
            state = pr.State.ToString().ToLower(),
            closed_at = pr.ClosedAt
        });
    }

    public record CreatePrRequest(
        [property: System.ComponentModel.DataAnnotations.Required]
        [property: System.ComponentModel.DataAnnotations.MaxLength(256)]
        string Title,
        [property: System.ComponentModel.DataAnnotations.MaxLength(65536)]
        string? Body,
        [property: System.ComponentModel.DataAnnotations.Required]
        string SourceBranch,
        [property: System.ComponentModel.DataAnnotations.Required]
        string TargetBranch);

    public record UpdatePrRequest(string? State);
}
