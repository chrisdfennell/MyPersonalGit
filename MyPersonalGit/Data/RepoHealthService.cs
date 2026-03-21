using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;
using GitRepository = LibGit2Sharp.Repository;

namespace MyPersonalGit.Data;

public interface IRepoHealthService
{
    Task<RepoHealthReport> GetHealthReportAsync(string repoName);
}

public class RepoHealthReport
{
    public string Grade { get; set; } = "F";
    public string GradeColor { get; set; } = "secondary";
    public int Score { get; set; }
    public bool HasReadme { get; set; }
    public bool HasLicense { get; set; }
    public bool HasCI { get; set; }
    public bool HasDescription { get; set; }
    public bool HasRecentCommits { get; set; }
    public bool HasContributing { get; set; }
    public double IssueCloseRate { get; set; }
    public int OpenIssues { get; set; }
    public int StalePRs { get; set; }
    public List<string> Suggestions { get; set; } = new();
}

public class RepoHealthService : IRepoHealthService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAdminService _adminService;
    private readonly IConfiguration _config;

    public RepoHealthService(IDbContextFactory<AppDbContext> dbFactory, IAdminService adminService, IConfiguration config)
    {
        _dbFactory = dbFactory;
        _adminService = adminService;
        _config = config;
    }

    public async Task<RepoHealthReport> GetHealthReportAsync(string repoName)
    {
        var report = new RepoHealthReport();
        var score = 0;

        using var db = _dbFactory.CreateDbContext();
        var repoAlt = repoName.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? repoName[..^4] : repoName + ".git";

        // Check DB-level data
        var repo = await db.Repositories.FirstOrDefaultAsync(r => r.Name == repoName || r.Name == repoAlt);
        if (repo != null)
        {
            report.HasDescription = !string.IsNullOrWhiteSpace(repo.Description);
            if (report.HasDescription) score += 10;
            else report.Suggestions.Add("Add a repository description");
        }

        // Issue close rate
        var totalIssues = await db.Issues.CountAsync(i => i.RepoName == repoName || i.RepoName == repoAlt);
        var closedIssues = await db.Issues.CountAsync(i => (i.RepoName == repoName || i.RepoName == repoAlt) && i.State == IssueState.Closed);
        report.OpenIssues = totalIssues - closedIssues;
        report.IssueCloseRate = totalIssues > 0 ? closedIssues * 100.0 / totalIssues : 100;
        if (report.IssueCloseRate >= 50) score += 10;

        // Stale PRs (open > 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        report.StalePRs = await db.PullRequests.CountAsync(p =>
            (p.RepoName == repoName || p.RepoName == repoAlt) &&
            p.State == PullRequestState.Open && p.CreatedAt < thirtyDaysAgo);
        if (report.StalePRs == 0) score += 10;
        else report.Suggestions.Add($"Review {report.StalePRs} stale pull request(s) open for 30+ days");

        // Git-level checks
        var systemSettings = await _adminService.GetSystemSettingsAsync();
        var projectRoot = !string.IsNullOrEmpty(systemSettings.ProjectRoot) ? systemSettings.ProjectRoot : _config["Git:ProjectRoot"] ?? "/repos";
        var repoPath = Path.Combine(projectRoot, repoName);
        if (!GitRepository.IsValid(repoPath)) repoPath = Path.Combine(projectRoot, repoName + ".git");

        if (GitRepository.IsValid(repoPath))
        {
            try
            {
                using var gitRepo = new GitRepository(repoPath);
                var head = gitRepo.Head;
                if (head?.Tip != null)
                {
                    var tree = head.Tip.Tree;

                    // README
                    report.HasReadme = tree.Any(e =>
                        e.Name.StartsWith("README", StringComparison.OrdinalIgnoreCase));
                    if (report.HasReadme) score += 15;
                    else report.Suggestions.Add("Add a README.md file");

                    // LICENSE
                    report.HasLicense = tree.Any(e =>
                        e.Name.StartsWith("LICENSE", StringComparison.OrdinalIgnoreCase) ||
                        e.Name.StartsWith("LICENCE", StringComparison.OrdinalIgnoreCase) ||
                        e.Name.Equals("COPYING", StringComparison.OrdinalIgnoreCase));
                    if (report.HasLicense) score += 15;
                    else report.Suggestions.Add("Add a LICENSE file");

                    // CI workflows
                    var ciDir = tree[".github/workflows"];
                    report.HasCI = ciDir != null && ciDir.TargetType == TreeEntryTargetType.Tree &&
                        ((Tree)ciDir.Target).Any(e => e.Name.EndsWith(".yml") || e.Name.EndsWith(".yaml"));
                    if (report.HasCI) score += 15;
                    else report.Suggestions.Add("Add CI/CD workflows in .github/workflows/");

                    // CONTRIBUTING
                    report.HasContributing = tree.Any(e =>
                        e.Name.StartsWith("CONTRIBUTING", StringComparison.OrdinalIgnoreCase));
                    if (report.HasContributing) score += 5;

                    // Recent commits (within last 90 days)
                    var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);
                    report.HasRecentCommits = head.Tip.Author.When.UtcDateTime >= ninetyDaysAgo;
                    if (report.HasRecentCommits) score += 20;
                    else report.Suggestions.Add("Repository has no commits in the last 90 days");
                }
            }
            catch { }
        }

        report.Score = Math.Min(score, 100);
        (report.Grade, report.GradeColor) = report.Score switch
        {
            >= 90 => ("A", "success"),
            >= 75 => ("B", "primary"),
            >= 60 => ("C", "info"),
            >= 40 => ("D", "warning"),
            _ => ("F", "danger")
        };

        return report;
    }
}
