using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/v1")]
public class UsersController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly UserProfileService _profileService;

    public UsersController(AuthService authService, UserProfileService profileService)
    {
        _authService = authService;
        _profileService = profileService;
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
