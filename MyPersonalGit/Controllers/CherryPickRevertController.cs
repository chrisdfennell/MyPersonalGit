using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/v1/repos/{repoName}")]
[EnableRateLimiting("api")]
public class CherryPickRevertController : ControllerBase
{
    private readonly ICherryPickRevertService _service;

    public CherryPickRevertController(ICherryPickRevertService service)
    {
        _service = service;
    }

    [HttpPost("cherry-pick")]
    public async Task<IActionResult> CherryPick(string repoName, [FromBody] CherryPickRequest request)
    {
        var username = User.Identity?.Name ?? "api-user";

        if (request.CreatePR)
        {
            var (success, error, prNumber) = await _service.CherryPickAsPullRequestAsync(repoName, request.CommitSha, request.TargetBranch, username);
            if (!success) return BadRequest(new { error });
            return Ok(new { success = true, pull_request_number = prNumber });
        }
        else
        {
            var (success, error, newSha) = await _service.CherryPickAsync(repoName, request.CommitSha, request.TargetBranch, username);
            if (!success) return BadRequest(new { error });
            return Ok(new { success = true, new_sha = newSha });
        }
    }

    [HttpPost("revert")]
    public async Task<IActionResult> Revert(string repoName, [FromBody] RevertRequest request)
    {
        var username = User.Identity?.Name ?? "api-user";

        if (request.CreatePR)
        {
            var (success, error, prNumber) = await _service.RevertAsPullRequestAsync(repoName, request.CommitSha, request.Branch, username);
            if (!success) return BadRequest(new { error });
            return Ok(new { success = true, pull_request_number = prNumber });
        }
        else
        {
            var (success, error, newSha) = await _service.RevertAsync(repoName, request.CommitSha, request.Branch, username);
            if (!success) return BadRequest(new { error });
            return Ok(new { success = true, new_sha = newSha });
        }
    }

    public record CherryPickRequest(string CommitSha, string TargetBranch, bool CreatePR = true);
    public record RevertRequest(string CommitSha, string Branch, bool CreatePR = true);
}
