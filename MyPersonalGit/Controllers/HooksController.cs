using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Controllers;

/// <summary>
/// Internal API called by git pre-receive hooks to enforce branch protection rules.
/// </summary>
[ApiController]
[Route("api/v1/hooks")]
public class HooksController : ControllerBase
{
    private readonly IBranchProtectionService _branchProtectionService;
    private readonly ITagProtectionService _tagProtectionService;
    private readonly ISecretScanService _secretScanService;
    private readonly IConfiguration _config;
    private readonly IAdminService _adminService;

    public HooksController(IBranchProtectionService branchProtectionService, ITagProtectionService tagProtectionService, ISecretScanService secretScanService, IConfiguration config, IAdminService adminService)
    {
        _branchProtectionService = branchProtectionService;
        _tagProtectionService = tagProtectionService;
        _secretScanService = secretScanService;
        _config = config;
        _adminService = adminService;
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
            // Push protection for secrets
            if (refUpdate.NewSha != "0000000000000000000000000000000000000000")
            {
                // Same resolution order as the git middleware: DB setting, then config.
                var systemSettings = await _adminService.GetSystemSettingsAsync();
                var projectRoot = !string.IsNullOrEmpty(systemSettings.ProjectRoot)
                    ? systemSettings.ProjectRoot
                    : _config["Git:ProjectRoot"] ?? "/repos";
                var repoDir = Path.Combine(projectRoot, request.RepoName);
                if (!repoDir.EndsWith(".git") && !Directory.Exists(repoDir)) repoDir += ".git";

                if (Directory.Exists(repoDir))
                {
                    var scanResults = await _secretScanService.ScanPushAsync(request.RepoName, repoDir, refUpdate.NewSha);
                    if (scanResults != null && scanResults.Any(r => r.State == SecretScanResultState.Open))
                    {
                        var secretsList = string.Join(", ", scanResults.Where(r => r.State == SecretScanResultState.Open).Select(r => $"{r.SecretType} in {r.FilePath}:{r.LineNumber}").Distinct());
                        return Ok(new PreReceiveResponse
                        {
                            Allowed = false,
                            Message = $"Push Protection: push blocked. Detected potential secrets: {secretsList}"
                        });
                    }
                }
            }
            // Check tag protection rules
            if (refUpdate.RefName.StartsWith("refs/tags/"))
            {
                var tagName = refUpdate.RefName["refs/tags/".Length..];
                var tagRule = await _tagProtectionService.GetMatchingRuleAsync(request.RepoName, tagName);
                if (tagRule != null)
                {
                    // Tag deletion
                    if (refUpdate.NewSha == "0000000000000000000000000000000000000000")
                    {
                        if (tagRule.PreventDeletion)
                        {
                            var pushUser = request.PushUser ?? "";
                            if (!tagRule.AllowedUsers.Contains(pushUser, StringComparer.OrdinalIgnoreCase))
                                return Ok(new PreReceiveResponse
                                {
                                    Allowed = false,
                                    Message = $"Tag protection: deletion of tag '{tagName}' is not allowed"
                                });
                        }
                        continue;
                    }

                    // Tag creation
                    if (refUpdate.OldSha == "0000000000000000000000000000000000000000")
                    {
                        if (tagRule.RestrictCreation)
                        {
                            var pushUser = request.PushUser ?? "";
                            if (!tagRule.AllowedUsers.Contains(pushUser, StringComparer.OrdinalIgnoreCase))
                                return Ok(new PreReceiveResponse
                                {
                                    Allowed = false,
                                    Message = $"Tag protection: creation of tag '{tagName}' is restricted"
                                });
                        }
                        continue;
                    }

                    // Tag force update (move)
                    if (refUpdate.IsForcePush && tagRule.PreventForcePush)
                    {
                        var pushUser = request.PushUser ?? "";
                        if (!tagRule.AllowedUsers.Contains(pushUser, StringComparer.OrdinalIgnoreCase))
                            return Ok(new PreReceiveResponse
                            {
                                Allowed = false,
                                Message = $"Tag protection: force update of tag '{tagName}' is not allowed"
                            });
                    }
                }
                continue;
            }

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
