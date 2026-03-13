using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

/// <summary>
/// API endpoints for SSH key authentication.
/// Used by OpenSSH's AuthorizedKeysCommand to look up keys,
/// and by the git-shell wrapper to validate access.
/// </summary>
[ApiController]
[Route("api/ssh")]
public class SshController : ControllerBase
{
    private readonly ISshAuthService _sshAuthService;
    private readonly IRepositoryService _repoService;
    private readonly IDeployKeyService _deployKeyService;
    private readonly ILogger<SshController> _logger;

    public SshController(ISshAuthService sshAuthService, IRepositoryService repoService, IDeployKeyService deployKeyService, ILogger<SshController> logger)
    {
        _sshAuthService = sshAuthService;
        _repoService = repoService;
        _deployKeyService = deployKeyService;
        _logger = logger;
    }

    /// <summary>
    /// Look up authorized keys for a given SSH key fingerprint.
    /// Called by OpenSSH AuthorizedKeysCommand.
    /// GET /api/ssh/authorized-keys?fingerprint=SHA256:xxx
    /// </summary>
    [HttpGet("authorized-keys")]
    public async Task<IActionResult> GetAuthorizedKeys([FromQuery] string? fingerprint, [FromQuery] string? key)
    {
        string? username = null;

        if (!string.IsNullOrEmpty(fingerprint))
        {
            username = await _sshAuthService.AuthenticateByFingerprintAsync(fingerprint);
        }
        else if (!string.IsNullOrEmpty(key))
        {
            username = await _sshAuthService.AuthenticateByKeyAsync(key);
        }

        if (username == null)
            return NotFound();

        return Ok(new { username });
    }

    /// <summary>
    /// Validate that a user has access to a repository for a given git operation.
    /// Called by the git-shell wrapper to check permissions before executing.
    /// Also supports deploy key authentication via fingerprint.
    /// POST /api/ssh/check-access
    /// </summary>
    [HttpPost("check-access")]
    public async Task<IActionResult> CheckAccess([FromBody] SshAccessCheckRequest request)
    {
        // Deploy key authentication path: if a fingerprint is provided, check deploy keys first
        if (!string.IsNullOrEmpty(request.KeyFingerprint) && !string.IsNullOrEmpty(request.RepoName))
        {
            var deployKeyResult = await _sshAuthService.AuthenticateDeployKeyByFingerprintAsync(request.KeyFingerprint, request.RepoName);
            if (deployKeyResult != null)
            {
                // Deploy keys with read-only access can only do git-upload-pack (fetch/clone)
                if (deployKeyResult.ReadOnly && request.Operation == "git-receive-pack")
                {
                    return Ok(new { allowed = false, reason = "deploy_key_read_only" });
                }

                return Ok(new { allowed = true, reason = "deploy_key", read_only = deployKeyResult.ReadOnly });
            }
        }

        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.RepoName))
            return BadRequest();

        var repo = await _repoService.GetRepositoryAsync(request.RepoName);

        // Repo doesn't exist — allow if creating via push
        if (repo == null)
        {
            return Ok(new { allowed = request.Operation == "git-receive-pack", reason = "new_repo" });
        }

        // Archived repos block pushes
        if (repo.IsArchived && request.Operation == "git-receive-pack")
        {
            return Ok(new { allowed = false, reason = "archived" });
        }

        // Private repos require ownership or collaboration
        if (repo.IsPrivate)
        {
            var isOwner = repo.Owner.Equals(request.Username, StringComparison.OrdinalIgnoreCase);
            return Ok(new { allowed = isOwner, reason = isOwner ? "owner" : "private" });
        }

        // Public repos: read always allowed, write requires ownership
        if (request.Operation == "git-upload-pack")
        {
            return Ok(new { allowed = true, reason = "public_read" });
        }

        var canWrite = repo.Owner.Equals(request.Username, StringComparison.OrdinalIgnoreCase);
        return Ok(new { allowed = canWrite, reason = canWrite ? "owner" : "not_owner" });
    }

    /// <summary>
    /// Force regeneration of the authorized_keys file.
    /// Admin only.
    /// POST /api/ssh/regenerate-keys
    /// </summary>
    [HttpPost("regenerate-keys")]
    public async Task<IActionResult> RegenerateKeys()
    {
        await _sshAuthService.RegenerateAuthorizedKeysAsync();
        return Ok(new { message = "Authorized keys regenerated" });
    }
}

public class SshAccessCheckRequest
{
    public string Username { get; set; } = "";
    public string RepoName { get; set; } = "";
    public string Operation { get; set; } = ""; // "git-upload-pack" or "git-receive-pack"
    public string? KeyFingerprint { get; set; } // Optional: for deploy key authentication
}
