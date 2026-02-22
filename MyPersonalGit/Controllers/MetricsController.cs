using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Data;
using MyPersonalGit.Models;
using MyPersonalGit.Services;

namespace MyPersonalGit.Controllers;

/// <summary>
/// Prometheus-compatible /metrics endpoint for monitoring.
/// Returns text/plain in Prometheus exposition format.
/// </summary>
[ApiController]
public class MetricsController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IConfiguration _config;

    public MetricsController(IDbContextFactory<AppDbContext> dbFactory, IConfiguration config)
    {
        _dbFactory = dbFactory;
        _config = config;
    }

    [HttpGet("/metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        using var db = _dbFactory.CreateDbContext();

        var usersTotal = await db.Users.CountAsync();
        var reposTotal = await db.Repositories.CountAsync();
        var issuesOpen = await db.Issues.CountAsync(i => i.State == IssueState.Open);
        var issuesClosed = await db.Issues.CountAsync(i => i.State == IssueState.Closed);
        var prsOpen = await db.PullRequests.CountAsync(p => p.State == PullRequestState.Open);
        var prsClosed = await db.PullRequests.CountAsync(p => p.State == PullRequestState.Closed);
        var prsMerged = await db.PullRequests.CountAsync(p => p.State == PullRequestState.Merged);
        var webhookSuccess = await db.WebhookDeliveries.CountAsync(w => w.Success);
        var webhookFailed = await db.WebhookDeliveries.CountAsync(w => !w.Success);

        // Calculate storage bytes
        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        long storageBytes = 0;
        if (Directory.Exists(projectRoot))
        {
            try
            {
                storageBytes = new DirectoryInfo(projectRoot)
                    .EnumerateFiles("*", SearchOption.AllDirectories)
                    .Sum(f => f.Length);
            }
            catch { /* permission errors on some dirs */ }
        }

        // Git operation counters from middleware
        var gitFetch = GitOperationCounters.Get("fetch");
        var gitPush = GitOperationCounters.Get("push");
        var gitClone = GitOperationCounters.Get("clone");

        var lines = new List<string>
        {
            "# HELP mypersonalgit_users_total Total number of users.",
            "# TYPE mypersonalgit_users_total gauge",
            $"mypersonalgit_users_total {usersTotal}",
            "",
            "# HELP mypersonalgit_repositories_total Total number of repositories.",
            "# TYPE mypersonalgit_repositories_total gauge",
            $"mypersonalgit_repositories_total {reposTotal}",
            "",
            "# HELP mypersonalgit_issues_total Total issues by state.",
            "# TYPE mypersonalgit_issues_total gauge",
            $"mypersonalgit_issues_total{{state=\"open\"}} {issuesOpen}",
            $"mypersonalgit_issues_total{{state=\"closed\"}} {issuesClosed}",
            "",
            "# HELP mypersonalgit_pull_requests_total Total pull requests by state.",
            "# TYPE mypersonalgit_pull_requests_total gauge",
            $"mypersonalgit_pull_requests_total{{state=\"open\"}} {prsOpen}",
            $"mypersonalgit_pull_requests_total{{state=\"closed\"}} {prsClosed}",
            $"mypersonalgit_pull_requests_total{{state=\"merged\"}} {prsMerged}",
            "",
            "# HELP mypersonalgit_storage_bytes Total storage used by repositories in bytes.",
            "# TYPE mypersonalgit_storage_bytes gauge",
            $"mypersonalgit_storage_bytes {storageBytes}",
            "",
            "# HELP mypersonalgit_webhook_deliveries_total Total webhook deliveries by status.",
            "# TYPE mypersonalgit_webhook_deliveries_total counter",
            $"mypersonalgit_webhook_deliveries_total{{status=\"success\"}} {webhookSuccess}",
            $"mypersonalgit_webhook_deliveries_total{{status=\"failed\"}} {webhookFailed}",
            "",
            "# HELP mypersonalgit_git_operations_total Total git operations by type.",
            "# TYPE mypersonalgit_git_operations_total counter",
            $"mypersonalgit_git_operations_total{{type=\"fetch\"}} {gitFetch}",
            $"mypersonalgit_git_operations_total{{type=\"push\"}} {gitPush}",
            $"mypersonalgit_git_operations_total{{type=\"clone\"}} {gitClone}",
            ""
        };

        return Content(string.Join("\n", lines), "text/plain; version=0.0.4; charset=utf-8");
    }
}
