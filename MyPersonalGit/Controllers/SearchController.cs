using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/v1/search")]
[EnableRateLimiting("api")]
public class SearchController : ControllerBase
{
    private readonly ICodeSearchService _searchService;

    public SearchController(ICodeSearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet("code")]
    public async Task<IActionResult> SearchCode(
        [FromQuery] string q,
        [FromQuery] string? repo = null,
        [FromQuery] string? ext = null,
        [FromQuery] int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(new { error = "Query must be at least 2 characters" });

        var results = await _searchService.SearchAsync(q, repo, ext, Math.Min(limit, 100));

        return Ok(new
        {
            query = q,
            total_count = results.Count,
            results = results.Select(r => new
            {
                repo = r.RepoName,
                file = r.FilePath,
                branch = r.Branch,
                matches = r.Matches.Select(m => new { line = m.LineNumber, content = m.Line })
            })
        });
    }
}
