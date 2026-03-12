using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

/// <summary>
/// Internal API called by git pre-receive hooks to enforce branch protection rules.
/// </summary>
[ApiController]
[Route("api/v1/hooks")]
public class HooksController : ControllerBase
{
    private readonly IBranchProtectionService _branchProtectionService;

    public HooksController(IBranchProtectionService branchProtectionService)
    {
        _branchProtectionService = branchProtectionService;
    }

    /// <summary>
    /// Validates a push against branch protection rules.
    /// Called by the pre-receive hook installed in each repository.
    /// </summary>
    [HttpPost("pre-receive")]
    public async Task<IActionResult> PreReceive([FromBody] PreReceiveRequest request)
    {
        if (string.IsNullOrEmpty(request.RepoName))
            return BadRequest("Missing repo name");

        foreach (var refUpdate in request.Updates)
        {
            // Extract branch name from ref (refs/heads/main -> main)
            if (!refUpdate.RefName.StartsWith("refs/heads/"))
                continue;

            var branchName = refUpdate.RefName["refs/heads/".Length..];
            var rule = await _branchProtectionService.GetMatchingRuleAsync(request.RepoName, branchName);
            if (rule == null) continue;

            // Check branch deletion
            if (refUpdate.NewSha == "0000000000000000000000000000000000000000")
            {
                if (rule.PreventDeletion)
                    return Ok(new PreReceiveResponse
                    {
                        Allowed = false,
                        Message = $"Branch protection: deletion of '{branchName}' is not allowed"
                    });
                continue;
            }

            // Check force push (non-fast-forward)
            if (refUpdate.IsForcePush && rule.PreventForcePush)
            {
                return Ok(new PreReceiveResponse
                {
                    Allowed = false,
                    Message = $"Branch protection: force push to '{branchName}' is not allowed"
                });
            }

            // Check direct push restriction
            if (rule.RestrictPushes)
            {
                var pushUser = request.PushUser ?? "";
                if (rule.AllowedPushUsers == null || !rule.AllowedPushUsers.Contains(pushUser, StringComparer.OrdinalIgnoreCase))
                {
                    if (rule.RequirePullRequest)
                    {
                        return Ok(new PreReceiveResponse
                        {
                            Allowed = false,
                            Message = $"Branch protection: direct pushes to '{branchName}' are restricted — use a pull request"
                        });
                    }
                }
            }
        }

        return Ok(new PreReceiveResponse { Allowed = true });
    }
}

public class PreReceiveRequest
{
    public string RepoName { get; set; } = "";
    public string? PushUser { get; set; }
    public List<RefUpdate> Updates { get; set; } = new();
}

public class RefUpdate
{
    public string OldSha { get; set; } = "";
    public string NewSha { get; set; } = "";
    public string RefName { get; set; } = "";
    public bool IsForcePush { get; set; }
}

public class PreReceiveResponse
{
    public bool Allowed { get; set; }
    public string? Message { get; set; }
}
