using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/v1/repos/{repoName}/issues")]
[EnableRateLimiting("api")]
public class IssuesController : ControllerBase
{
    private readonly IIssueService _issueService;
    private readonly IIssueDependencyService _dependencyService;

    public IssuesController(IIssueService issueService, IIssueDependencyService dependencyService)
    {
        _issueService = issueService;
        _dependencyService = dependencyService;
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

    // --- Issue Dependencies ---

    [HttpGet("{number:int}/dependencies")]
    public async Task<IActionResult> GetDependencies(string repoName, int number)
    {
        var deps = await _dependencyService.GetDependenciesAsync(repoName, number);
        var blockingIssues = await _dependencyService.GetBlockingIssuesAsync(repoName, number);
        var blockedIssues = await _dependencyService.GetBlockedByIssuesAsync(repoName, number);
        var isBlocked = await _dependencyService.IsBlockedAsync(repoName, number);

        return Ok(new
        {
            is_blocked = isBlocked,
            blocked_by = blockingIssues.Select(i => new { i.Number, i.Title, state = i.State.ToString().ToLower() }),
            blocks = blockedIssues.Select(i => new { i.Number, i.Title, state = i.State.ToString().ToLower() }),
            dependencies = deps.Select(d => new { d.Id, d.BlockingIssueNumber, d.BlockedIssueNumber, d.CreatedBy, created_at = d.CreatedAt })
        });
    }

    [HttpPost("{number:int}/dependencies")]
    public async Task<IActionResult> AddDependency(string repoName, int number, [FromBody] AddDependencyRequest request)
    {
        var username = User.Identity?.Name ?? "api-user";

        IssueDependency? dep;
        if (request.Blocks.HasValue)
            dep = await _dependencyService.AddDependencyAsync(repoName, number, request.Blocks.Value, username);
        else if (request.BlockedBy.HasValue)
            dep = await _dependencyService.AddDependencyAsync(repoName, request.BlockedBy.Value, number, username);
        else
            return BadRequest(new { error = "Specify either 'blocks' or 'blocked_by' issue number" });

        if (dep == null)
            return BadRequest(new { error = "Failed to add dependency. Issues may not exist, dependency already exists, or would create a cycle." });

        return Created($"/api/v1/repos/{repoName}/issues/{number}/dependencies", new
        {
            dep.Id,
            dep.BlockingIssueNumber,
            dep.BlockedIssueNumber,
            dep.CreatedBy,
            created_at = dep.CreatedAt
        });
    }

    [HttpDelete("{number:int}/dependencies/{depId:int}")]
    public async Task<IActionResult> RemoveDependency(string repoName, int number, int depId)
    {
        var removed = await _dependencyService.RemoveDependencyAsync(repoName, depId);
        if (!removed) return NotFound(new { error = "Dependency not found" });
        return NoContent();
    }

    public record AddDependencyRequest(int? Blocks, int? BlockedBy);

    // --- Comment Edit/Delete ---

    [HttpPut("{number:int}/comments/{commentId:int}")]
    public async Task<IActionResult> EditComment(string repoName, int number, int commentId, [FromBody] EditCommentRequest request)
    {
        var username = User.Identity?.Name ?? "api-user";
        var result = await _issueService.EditCommentAsync(commentId, request.Body, username);
        if (!result) return NotFound(new { error = "Comment not found or not authorized" });
        return Ok(new { id = commentId, body = request.Body, updated_at = DateTime.UtcNow });
    }

    [HttpDelete("{number:int}/comments/{commentId:int}")]
    public async Task<IActionResult> DeleteComment(string repoName, int number, int commentId)
    {
        var username = User.Identity?.Name ?? "api-user";
        var result = await _issueService.DeleteCommentAsync(commentId, username);
        if (!result) return NotFound(new { error = "Comment not found or not authorized" });
        return NoContent();
    }

    // --- Pin/Lock ---

    [HttpPost("{number:int}/pin")]
    public async Task<IActionResult> TogglePin(string repoName, int number)
    {
        var result = await _issueService.TogglePinAsync(repoName, number);
        if (!result) return NotFound(new { error = $"Issue #{number} not found" });
        return Ok(new { success = true });
    }

    [HttpPost("{number:int}/lock")]
    public async Task<IActionResult> ToggleLock(string repoName, int number, [FromBody] LockRequest? request = null)
    {
        var result = await _issueService.ToggleLockAsync(repoName, number, request?.Reason);
        if (!result) return NotFound(new { error = $"Issue #{number} not found" });
        return Ok(new { success = true });
    }

    // --- Assignees ---

    [HttpPut("{number:int}/assignees")]
    public async Task<IActionResult> SetAssignees(string repoName, int number, [FromBody] SetAssigneesRequest request)
    {
        var result = await _issueService.SetAssigneesAsync(repoName, number, request.Assignees);
        if (!result) return NotFound(new { error = $"Issue #{number} not found" });
        return Ok(new { assignees = request.Assignees });
    }

    // --- Due Date ---

    [HttpPut("{number:int}/due-date")]
    public async Task<IActionResult> SetDueDate(string repoName, int number, [FromBody] SetDueDateRequest request)
    {
        var result = await _issueService.SetDueDateAsync(repoName, number, request.DueDate);
        if (!result) return NotFound(new { error = $"Issue #{number} not found" });
        return Ok(new { due_date = request.DueDate });
    }

    public record EditCommentRequest(string Body);
    public record LockRequest(string? Reason);
    public record SetAssigneesRequest(List<string> Assignees);
    public record SetDueDateRequest(DateTime? DueDate);
}
