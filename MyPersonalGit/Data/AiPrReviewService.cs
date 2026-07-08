using System.Text;
using LibGit2Sharp;
using MyPersonalGit.Models;
using GitRepository = LibGit2Sharp.Repository;

namespace MyPersonalGit.Data;

public interface IAiPrReviewService
{
    /// <summary>Whether AI review is available (AI completion configured in admin settings).</summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Generates an AI code review for a pull request's diff and posts it as a
    /// "Commented" review on the PR (so it never counts toward required approvals).
    /// </summary>
    Task<(bool Success, string? Error)> ReviewPullRequestAsync(string repoName, int number, string requestedBy);
}

public class AiPrReviewService : IAiPrReviewService
{
    /// <summary>Reviews are posted under this author name so they are clearly machine-generated.</summary>
    public const string ReviewAuthor = "ai-reviewer";

    private const int MaxDiffChars = 30_000;

    private readonly IAiChatService _aiChatService;
    private readonly IPullRequestService _pullRequestService;
    private readonly IAdminService _adminService;
    private readonly IConfiguration _config;
    private readonly ILogger<AiPrReviewService> _logger;

    public AiPrReviewService(IAiChatService aiChatService, IPullRequestService pullRequestService,
        IAdminService adminService, IConfiguration config, ILogger<AiPrReviewService> logger)
    {
        _aiChatService = aiChatService;
        _pullRequestService = pullRequestService;
        _adminService = adminService;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> IsAvailableAsync()
    {
        var settings = await _adminService.GetSystemSettingsAsync();
        return settings.AiCompletionEnabled
               && !string.IsNullOrEmpty(settings.AiCompletionEndpoint)
               && !string.IsNullOrEmpty(settings.AiCompletionApiKey);
    }

    public async Task<(bool Success, string? Error)> ReviewPullRequestAsync(string repoName, int number, string requestedBy)
    {
        if (!await IsAvailableAsync())
            return (false, "AI Completion is not configured in Admin settings.");

        var pr = await _pullRequestService.GetPullRequestAsync(repoName, number);
        if (pr == null)
            return (false, "Pull request not found.");
        if (pr.State != PullRequestState.Open)
            return (false, "Pull request is not open.");

        string diff;
        try
        {
            var built = await BuildDiffAsync(repoName, pr.SourceBranch, pr.TargetBranch);
            if (built == null)
                return (false, "Could not compute the diff — check that both branches still exist.");
            diff = built;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build diff for AI review of PR #{Number} in {RepoName}", number, repoName);
            return (false, $"Failed to build diff: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(diff))
            return (false, "The pull request has no changes to review.");

        var systemPrompt =
            "You are an expert code reviewer for a pull request. Review the diff and provide constructive feedback. " +
            "Structure your review in markdown with these sections: " +
            "'### Summary' (2-3 sentences on what the change does), " +
            "'### Findings' (bullet list of potential bugs, security issues, or correctness problems, each referencing the file — say 'None found' if the diff looks correct), " +
            "'### Suggestions' (optional improvements: readability, naming, tests, edge cases). " +
            "Be specific and reference file paths from the diff. Do not invent issues; if the change looks good, say so. " +
            "Lines starting with '+' are added, '-' are removed.";

        var prompt = new StringBuilder();
        prompt.AppendLine($"Review this pull request titled \"{pr.Title}\".");
        if (!string.IsNullOrWhiteSpace(pr.Body))
            prompt.AppendLine($"Description: {Trim(pr.Body, 2000)}");
        prompt.AppendLine();
        prompt.AppendLine("```diff");
        prompt.AppendLine(diff);
        prompt.AppendLine("```");

        var review = await _aiChatService.ChatAsync(prompt.ToString(), systemPrompt);
        if (string.IsNullOrWhiteSpace(review))
            return (false, "The AI endpoint returned no response — check the AI settings and endpoint logs.");

        var body = $"🤖 **AI Code Review** — requested by @{requestedBy}\n\n{review}";
        await _pullRequestService.AddReviewAsync(repoName, number, ReviewAuthor, ReviewState.Commented, body);

        _logger.LogInformation("AI review posted on PR #{Number} in {RepoName} (requested by {User})", number, repoName, requestedBy);
        return (true, null);
    }

    private async Task<string?> BuildDiffAsync(string repoName, string sourceBranch, string targetBranch)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        if (repoPath == null) return null;

        using var repo = new GitRepository(repoPath);
        var source = repo.Branches[sourceBranch];
        var target = repo.Branches[targetBranch];
        if (source?.Tip == null || target?.Tip == null) return null;

        var patch = repo.Diff.Compare<Patch>(target.Tip.Tree, source.Tip.Tree);

        // Accumulate per-file patches so a truncated diff still ends on a file boundary.
        var sb = new StringBuilder();
        var skippedFiles = new List<string>();
        foreach (var change in patch)
        {
            if (sb.Length + change.Patch.Length > MaxDiffChars)
            {
                skippedFiles.Add(change.Path);
                continue;
            }
            sb.Append(change.Patch);
        }

        if (skippedFiles.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"# NOTE: {skippedFiles.Count} file(s) omitted to fit the size limit: {string.Join(", ", skippedFiles.Take(20))}");
        }

        return sb.ToString();
    }

    private async Task<string?> GetRepoPathAsync(string repoName)
    {
        var systemSettings = await _adminService.GetSystemSettingsAsync();
        var projectRoot = !string.IsNullOrEmpty(systemSettings.ProjectRoot)
            ? systemSettings.ProjectRoot
            : _config["Git:ProjectRoot"] ?? "/repos";

        var path = Path.Combine(projectRoot, repoName);
        if (GitRepository.IsValid(path)) return path;
        if (GitRepository.IsValid(path + ".git")) return path + ".git";
        return null;
    }

    private static string Trim(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..maxLength] + "…";
}
