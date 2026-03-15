using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Controllers;

[ApiController]
public class BadgeController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public BadgeController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Returns an SVG badge showing the latest workflow run status for a repo.
    /// Usage: /api/badge/{repoName}/workflow
    /// Optional: ?workflow=name to filter by workflow name
    /// </summary>
    [HttpGet("api/badge/{repoName}/workflow")]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> GetWorkflowBadge(string repoName, [FromQuery] string? workflow = null)
    {
        using var db = _dbFactory.CreateDbContext();

        var query = db.WorkflowRuns
            .Where(r => r.RepoName == repoName)
            .OrderByDescending(r => r.CreatedAt);

        WorkflowRun? latestRun;
        if (!string.IsNullOrEmpty(workflow))
            latestRun = await query.FirstOrDefaultAsync(r => r.WorkflowName == workflow);
        else
            latestRun = await query.FirstOrDefaultAsync();

        var (label, message, color) = latestRun?.Status switch
        {
            WorkflowStatus.Success => ("build", "passing", "#4c1"),
            WorkflowStatus.Failure => ("build", "failing", "#e05d44"),
            WorkflowStatus.InProgress => ("build", "running", "#dfb317"),
            WorkflowStatus.Queued => ("build", "queued", "#dfb317"),
            WorkflowStatus.Cancelled => ("build", "cancelled", "#9f9f9f"),
            _ => ("build", "unknown", "#9f9f9f")
        };

        if (!string.IsNullOrEmpty(workflow))
            label = workflow.ToLowerInvariant().Replace(' ', '-');

        var svg = GenerateBadgeSvg(label, message, color);
        return Content(svg, "image/svg+xml");
    }

    /// <summary>
    /// Returns an SVG badge showing the latest commit status for a repo.
    /// Usage: /api/badge/{repoName}/status
    /// </summary>
    [HttpGet("api/badge/{repoName}/status")]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> GetStatusBadge(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();

        var latestStatus = await db.CommitStatuses
            .Where(s => s.RepoName == repoName)
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync();

        var (label, message, color) = latestStatus?.State switch
        {
            CommitStatusState.Success => ("status", "passing", "#4c1"),
            CommitStatusState.Failure => ("status", "failing", "#e05d44"),
            CommitStatusState.Pending => ("status", "pending", "#dfb317"),
            CommitStatusState.Error => ("status", "error", "#e05d44"),
            _ => ("status", "unknown", "#9f9f9f")
        };

        var svg = GenerateBadgeSvg(label, message, color);
        return Content(svg, "image/svg+xml");
    }

    private static string GenerateBadgeSvg(string label, string message, string color)
    {
        var labelWidth = label.Length * 7 + 10;
        var messageWidth = message.Length * 7 + 10;
        var totalWidth = labelWidth + messageWidth;

        return $@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{totalWidth}"" height=""20"" role=""img"">
  <linearGradient id=""s"" x2=""0"" y2=""100%"">
    <stop offset=""0"" stop-color=""#bbb"" stop-opacity="".1""/>
    <stop offset=""1"" stop-opacity="".1""/>
  </linearGradient>
  <clipPath id=""r""><rect width=""{totalWidth}"" height=""20"" rx=""3"" fill=""#fff""/></clipPath>
  <g clip-path=""url(#r)"">
    <rect width=""{labelWidth}"" height=""20"" fill=""#555""/>
    <rect x=""{labelWidth}"" width=""{messageWidth}"" height=""20"" fill=""{color}""/>
    <rect width=""{totalWidth}"" height=""20"" fill=""url(#s)""/>
  </g>
  <g fill=""#fff"" text-anchor=""middle"" font-family=""Verdana,Geneva,DejaVu Sans,sans-serif"" text-rendering=""geometricPrecision"" font-size=""11"">
    <text x=""{labelWidth / 2}"" y=""15"" fill=""#010101"" fill-opacity="".3"">{label}</text>
    <text x=""{labelWidth / 2}"" y=""14"" fill=""#fff"">{label}</text>
    <text x=""{labelWidth + messageWidth / 2}"" y=""15"" fill=""#010101"" fill-opacity="".3"">{message}</text>
    <text x=""{labelWidth + messageWidth / 2}"" y=""14"" fill=""#fff"">{message}</text>
  </g>
</svg>";
    }
}
