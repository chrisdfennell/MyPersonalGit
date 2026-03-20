using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/v1/repos/{repoName}/traffic")]
[EnableRateLimiting("api")]
public class TrafficController : ControllerBase
{
    private readonly IRepositoryTrafficService _trafficService;

    public TrafficController(IRepositoryTrafficService trafficService)
    {
        _trafficService = trafficService;
    }

    [HttpGet("clones")]
    public async Task<IActionResult> GetClones(string repoName, [FromQuery] int days = 14)
    {
        var summary = await _trafficService.GetTrafficSummaryAsync(repoName, days);
        var (totalClones, uniqueCloners, _, _) = await _trafficService.GetTotalsAsync(repoName, days);

        return Ok(new
        {
            count = totalClones,
            uniques = uniqueCloners,
            clones = summary.Select(s => new { timestamp = s.Date, count = s.Clones, uniques = s.UniqueCloners })
        });
    }

    [HttpGet("views")]
    public async Task<IActionResult> GetViews(string repoName, [FromQuery] int days = 14)
    {
        var summary = await _trafficService.GetTrafficSummaryAsync(repoName, days);
        var (_, _, totalViews, uniqueVisitors) = await _trafficService.GetTotalsAsync(repoName, days);

        return Ok(new
        {
            count = totalViews,
            uniques = uniqueVisitors,
            views = summary.Select(s => new { timestamp = s.Date, count = s.PageViews, uniques = s.UniqueVisitors })
        });
    }

    [HttpGet("referrers")]
    public async Task<IActionResult> GetReferrers(string repoName, [FromQuery] int days = 14)
    {
        var referrers = await _trafficService.GetTopReferrersAsync(repoName, days);
        return Ok(referrers.Select(r => new { referrer = r.Referrer, count = r.Count }));
    }

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularPages(string repoName, [FromQuery] int days = 14)
    {
        var pages = await _trafficService.GetPopularPagesAsync(repoName, days);
        return Ok(pages.Select(p => new { path = p.Path, count = p.Count }));
    }
}
