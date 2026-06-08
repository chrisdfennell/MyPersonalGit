using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/v1")]
[EnableRateLimiting("api")]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserProfileService _profileService;
    private readonly IWebHostEnvironment _env;

    public UsersController(IAuthService authService, IUserProfileService profileService, IWebHostEnvironment env)
    {
        _authService = authService;
        _profileService = profileService;
        _env = env;
    }

    [HttpPost("user/avatar")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { error = "Not authenticated" });

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            return BadRequest(new { error = "Only JPEG, PNG, GIF, and WebP images are allowed" });

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadsDir);

        // Delete any existing avatar for this user
        foreach (var existing in Directory.GetFiles(uploadsDir, $"{username}.*"))
            System.IO.File.Delete(existing);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) ext = ".png";
        var safeFilename = $"{username}{ext}";
        var filePath = Path.Combine(uploadsDir, safeFilename);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var avatarUrl = $"/uploads/avatars/{safeFilename}?v={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        // Update the user's profile with the new avatar URL
        var profile = await _profileService.GetProfileAsync(username);
        if (profile != null)
        {
            profile.AvatarUrl = avatarUrl;
            await _profileService.SaveProfileAsync(profile);
        }

        return Ok(new { avatar_url = avatarUrl });
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetAuthenticatedUser()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { error = "Not authenticated" });

        var profile = await _profileService.GetProfileAsync(username);
        return Ok(new
        {
            username,
            full_name = profile?.FullName,
            email = profile?.Email,
            bio = profile?.Bio,
            location = profile?.Location,
            website = profile?.Website,
            company = profile?.Company,
            avatar_url = profile?.AvatarUrl
        });
    }

    [HttpGet("users/{username}/profile")]
    public async Task<IActionResult> GetUserProfile(string username)
    {
        var profile = await _profileService.GetProfileAsync(username);
        if (profile == null) return NotFound(new { error = $"User '{username}' not found" });

        return Ok(new
        {
            username,
            full_name = profile.FullName,
            bio = profile.Bio,
            location = profile.Location,
            website = profile.Website,
            company = profile.Company,
            avatar_url = profile.AvatarUrl
        });
    }

    [HttpGet("users/{username}/stats")]
    public async Task<IActionResult> GetUserStats(string username)
    {
        var stats = await _profileService.GetStatisticsAsync(username);
        if (stats == null) return NotFound(new { error = $"User '{username}' not found" });

        return Ok(new
        {
            username,
            stats.TotalCommits,
            stats.TotalPullRequests,
            stats.TotalIssues,
            stats.TotalStars,
            stats.TotalForks
        });
    }
}
