using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/v1")]
[EnableRateLimiting("api")]
public class GpgKeysController : ControllerBase
{
    private readonly IGpgKeyService _gpgKeyService;
    private readonly IAuthService _authService;

    public GpgKeysController(IGpgKeyService gpgKeyService, IAuthService authService)
    {
        _gpgKeyService = gpgKeyService;
        _authService = authService;
    }

    [HttpGet("user/gpg_keys")]
    public async Task<IActionResult> ListGpgKeys()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { error = "Not authenticated" });

        var user = await _authService.GetUserByUsernameAsync(username);
        if (user == null)
            return Unauthorized(new { error = "User not found" });

        var keys = await _gpgKeyService.GetUserGpgKeysAsync(user.Id);
        return Ok(keys.Select(k => new
        {
            id = k.Id,
            key_id = k.KeyId,
            long_key_id = k.LongKeyId,
            primary_email = k.PrimaryEmail,
            emails = k.Emails,
            created_at = k.CreatedAt,
            expires_at = k.ExpiresAt,
            is_verified = k.IsVerified
        }));
    }

    [HttpPost("user/gpg_keys")]
    public async Task<IActionResult> AddGpgKey([FromBody] AddGpgKeyRequest request)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { error = "Not authenticated" });

        var user = await _authService.GetUserByUsernameAsync(username);
        if (user == null)
            return Unauthorized(new { error = "User not found" });

        if (string.IsNullOrWhiteSpace(request.ArmoredPublicKey))
            return BadRequest(new { error = "Public key is required" });

        var key = await _gpgKeyService.AddGpgKeyAsync(user.Id, request.ArmoredPublicKey);
        if (key == null)
            return BadRequest(new { error = "Invalid GPG public key or key already exists" });

        return Created($"/api/v1/user/gpg_keys/{key.Id}", new
        {
            id = key.Id,
            key_id = key.KeyId,
            long_key_id = key.LongKeyId,
            primary_email = key.PrimaryEmail,
            emails = key.Emails,
            created_at = key.CreatedAt,
            expires_at = key.ExpiresAt,
            is_verified = key.IsVerified
        });
    }

    [HttpDelete("user/gpg_keys/{id}")]
    public async Task<IActionResult> DeleteGpgKey(int id)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { error = "Not authenticated" });

        var user = await _authService.GetUserByUsernameAsync(username);
        if (user == null)
            return Unauthorized(new { error = "User not found" });

        // Verify the key belongs to this user
        var keys = await _gpgKeyService.GetUserGpgKeysAsync(user.Id);
        if (!keys.Any(k => k.Id == id))
            return NotFound(new { error = "GPG key not found" });

        var deleted = await _gpgKeyService.DeleteGpgKeyAsync(id);
        if (!deleted)
            return NotFound(new { error = "GPG key not found" });

        return NoContent();
    }

    public class AddGpgKeyRequest
    {
        public string ArmoredPublicKey { get; set; } = "";
    }
}
