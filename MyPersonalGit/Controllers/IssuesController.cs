using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/v1/repos/{repoName}/issues")]
[EnableRateLimiting("api")]
public class IssuesController : ControllerBase
{
    private readonly IIssueService _issueService;

    public IssuesController(IIssueService issueService)
    {
        _issueService = issueService;
    }

    [HttpGet]
    public async Task<IActionResult> ListIssues(string repoName, [FromQuery] string? state = null)
    {
        var issues = await _issueService.GetIssuesAsync(repoName);

        if (!string.IsNullOrEmpty(state) && Enum.TryParse<Models.IssueState>(state, true, out var issueState))
        {
            issues = issues.Where(i => i.State == issueState).ToList();
        }

        return Ok(issues.Select(i => new
        {
            i.Number,
            i.Title,
            i.Body,
            state = i.State.ToString().ToLower(),
            i.Author,
            assignee = i.Assignee,
            i.Labels,
            created_at = i.CreatedAt,
            closed_at = i.ClosedAt,
            comment_count = i.Comments?.Count ?? 0
        }));
    }

    [HttpGet("{number:int}")]
    public async Task<IActionResult> GetIssue(string repoName, int number)
    {
        var issue = await _issueService.GetIssueAsync(repoName, number);
        if (issue == null) return NotFound(new { error = $"Issue #{number} not found" });

        return Ok(new
        {
            issue.Number,
            issue.Title,
            issue.Body,
            state = issue.State.ToString().ToLower(),
            issue.Author,
            assignee = issue.Assignee,
            issue.Labels,
            created_at = issue.CreatedAt,
            closed_at = issue.ClosedAt,
            comments = issue.Comments?.Select(c => new
            {
                c.Author,
                c.Body,
                created_at = c.CreatedAt
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateIssue(string repoName, [FromBody] CreateIssueRequest request)
    {
        var username = User.Identity?.Name ?? "api-user";
        var issue = await _issueService.CreateIssueAsync(repoName, request.Title, request.Body, username, request.Labels);
        return Created($"/api/v1/repos/{repoName}/issues/{issue.Number}", new
        {
            issue.Number,
            issue.Title,
            issue.Body,
            state = issue.State.ToString().ToLower(),
            issue.Author,
            created_at = issue.CreatedAt
        });
    }

    [HttpPost("{number:int}/comments")]
    public async Task<IActionResult> AddComment(string repoName, int number, [FromBody] AddCommentRequest request)
    {
        var username = User.Identity?.Name ?? "api-user";
        var result = await _issueService.AddCommentAsync(repoName, number, username, request.Body);
        if (!result) return NotFound(new { error = $"Issue #{number} not found" });

        return Created("", new
        {
            author = username,
            body = request.Body,
            created_at = DateTime.UtcNow
        });
    }

    [HttpPatch("{number:int}")]
    public async Task<IActionResult> UpdateIssue(string repoName, int number, [FromBody] UpdateIssueRequest request)
    {
        if (request.State?.ToLower() == "closed")
        {
            await _issueService.CloseIssueAsync(repoName, number);
        }
        else if (request.State?.ToLower() == "open")
        {
            await _issueService.ReopenIssueAsync(repoName, number);
        }

        var issue = await _issueService.GetIssueAsync(repoName, number);
        if (issue == null) return NotFound(new { error = $"Issue #{number} not found" });

        return Ok(new
        {
            issue.Number,
            issue.Title,
            state = issue.State.ToString().ToLower(),
            issue.Author,
            closed_at = issue.ClosedAt
        });
    }

    public record CreateIssueRequest(
        [property: System.ComponentModel.DataAnnotations.Required]
        [property: System.ComponentModel.DataAnnotations.MaxLength(256)]
        string Title,
        [property: System.ComponentModel.DataAnnotations.MaxLength(65536)]
        string? Body,
        List<string>? Labels);

    public record AddCommentRequest(
        [property: System.ComponentModel.DataAnnotations.Required]
        [property: System.ComponentModel.DataAnnotations.MaxLength(65536)]
        string Body);

    public record UpdateIssueRequest(string? State);
}
