using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/v1/repos/{owner}/{repo}/keys")]
[EnableRateLimiting("api")]
public class DeployKeysController : ControllerBase
{
    private readonly IDeployKeyService _deployKeyService;
    private readonly IRepositoryService _repoService;
    private readonly ILogger<DeployKeysController> _logger;

    public DeployKeysController(IDeployKeyService deployKeyService, IRepositoryService repoService, ILogger<DeployKeysController> logger)
    {
        _deployKeyService = deployKeyService;
        _repoService = repoService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ListDeployKeys(string owner, string repo)
    {
        var repository = await _repoService.GetRepositoryAsync(repo);
        if (repository == null)
            return NotFound(new { error = $"Repository '{owner}/{repo}' not found" });

        var keys = await _deployKeyService.GetDeployKeysAsync(repository.Id);
        return Ok(keys.Select(k => new
        {
            k.Id,
            k.Title,
            fingerprint = k.KeyFingerprint,
            read_only = k.ReadOnly,
            created_at = k.CreatedAt,
            last_used_at = k.LastUsedAt
        }));
    }

    [HttpPost]
    public async Task<IActionResult> AddDeployKey(string owner, string repo, [FromBody] AddDeployKeyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Key))
            return BadRequest(new { error = "Title and key are required" });

        var repository = await _repoService.GetRepositoryAsync(repo);
        if (repository == null)
            return NotFound(new { error = $"Repository '{owner}/{repo}' not found" });

        var key = await _deployKeyService.AddDeployKeyAsync(repository.Id, request.Title, request.Key, request.ReadOnly);
        if (key == null)
            return Conflict(new { error = "A deploy key with this fingerprint already exists for this repository" });

        return Created($"/api/v1/repos/{owner}/{repo}/keys/{key.Id}", new
        {
            key.Id,
            key.Title,
            fingerprint = key.KeyFingerprint,
            read_only = key.ReadOnly,
            created_at = key.CreatedAt
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDeployKey(string owner, string repo, int id)
    {
        var repository = await _repoService.GetRepositoryAsync(repo);
        if (repository == null)
            return NotFound(new { error = $"Repository '{owner}/{repo}' not found" });

        var deleted = await _deployKeyService.DeleteDeployKeyAsync(id);
        if (!deleted)
            return NotFound(new { error = "Deploy key not found" });

        return NoContent();
    }
}

public class AddDeployKeyRequest
{
    public string Title { get; set; } = "";
    public string Key { get; set; } = "";
    public bool ReadOnly { get; set; } = true;
}
