using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public record IssueReference(string Keyword, string? RepoOwner, string? RepoName, int IssueNumber, bool IsClosing);

public interface IIssueAutoCloseService
{
    /// <summary>
    /// Parses commit messages for issue references (both closing and non-closing).
    /// </summary>
    List<IssueReference> ParseIssueReferences(string commitMessage);

    /// <summary>
    /// Processes a commit message: closes referenced issues and adds cross-reference comments.
    /// </summary>
    Task ProcessCommitMessage(string repoName, string commitMessage, string commitSha, string authorName);

    /// <summary>
    /// Closes referenced issues from a commit and adds "Closed via commit" comments.
    /// </summary>
    Task CloseReferencedIssues(string repoName, string commitSha, string commitMessage, string authorName);

    /// <summary>
    /// Processes all commits from a merged pull request.
    /// </summary>
    Task ProcessPullRequestMerge(string repoName, int prNumber, List<string> commitMessages, string mergedBy);

    /// <summary>
    /// Renders a commit message with #123 references as clickable links.
    /// </summary>
    string RenderIssueLinks(string message, string repoName);
}

public partial class IssueAutoCloseService : IIssueAutoCloseService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<IssueAutoCloseService> _logger;
    private readonly IIssueService _issueService;

    // Closing keywords per GitHub conventions
    private static readonly string[] ClosingKeywords =
    {
        "close", "closes", "closed",
        "fix", "fixes", "fixed",
        "resolve", "resolves", "resolved"
    };

    // Pattern: keyword #123 or keyword owner/repo#123
    // Case-insensitive, supports optional owner/repo prefix
    [GeneratedRegex(
        @"(?<keyword>close[sd]?|fix(?:e[sd])?|resolve[sd]?)\s+(?:(?<owner>[a-zA-Z0-9\-_.]+)/(?<repo>[a-zA-Z0-9\-_.]+))?#(?<number>\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ClosingPatternRegex();

    // Non-closing reference: standalone #123 not preceded by a closing keyword
    // We match all #NNN and then filter out those that were part of a closing pattern
    [GeneratedRegex(@"(?<!\w)#(?<number>\d+)\b", RegexOptions.Compiled)]
    private static partial Regex IssueRefPatternRegex();

    public IssueAutoCloseService(
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<IssueAutoCloseService> logger,
        IIssueService issueService)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _issueService = issueService;
    }

    public List<IssueReference> ParseIssueReferences(string commitMessage)
    {
        if (string.IsNullOrWhiteSpace(commitMessage))
            return new List<IssueReference>();

        var references = new List<IssueReference>();
        var closingMatches = new HashSet<int>(); // track issue numbers matched by closing patterns

        // Find all closing references first
        foreach (Match match in ClosingPatternRegex().Matches(commitMessage))
        {
            var keyword = match.Groups["keyword"].Value.ToLowerInvariant();
            var owner = match.Groups["owner"].Success ? match.Groups["owner"].Value : null;
            var repo = match.Groups["repo"].Success ? match.Groups["repo"].Value : null;
            var number = int.Parse(match.Groups["number"].Value);

            references.Add(new IssueReference(keyword, owner, repo, number, IsClosing: true));

            // Only track as closing if it's a same-repo reference (no owner/repo prefix)
            if (owner == null && repo == null)
                closingMatches.Add(number);
        }

        // Find all non-closing #NNN references
        // Cache closing regex matches to avoid recomputing in inner loop
        var closingRegexMatches = ClosingPatternRegex().Matches(commitMessage);
        foreach (Match match in IssueRefPatternRegex().Matches(commitMessage))
        {
            var number = int.Parse(match.Groups["number"].Value);

            // Skip if this number was already captured by a closing keyword
            if (closingMatches.Contains(number))
                continue;

            // Check if this match position overlaps with a closing pattern match
            var isPartOfClosing = false;
            foreach (Match closingMatch in closingRegexMatches)
            {
                if (match.Index >= closingMatch.Index && match.Index < closingMatch.Index + closingMatch.Length)
                {
                    isPartOfClosing = true;
                    break;
                }
            }

            if (!isPartOfClosing)
            {
                // Only add if not already present as a non-closing reference
                if (!references.Any(r => !r.IsClosing && r.IssueNumber == number && r.RepoOwner == null))
                    references.Add(new IssueReference("ref", null, null, number, IsClosing: false));
            }
        }

        return references;
    }

    public async Task ProcessCommitMessage(string repoName, string commitMessage, string commitSha, string authorName)
    {
        var references = ParseIssueReferences(commitMessage);
        if (!references.Any())
            return;

        var shortSha = commitSha.Length >= 7 ? commitSha[..7] : commitSha;

        foreach (var reference in references)
        {
            // For cross-repo references (owner/repo#123), skip — this instance only manages local repos
            if (reference.RepoOwner != null && reference.RepoName != null)
            {
                _logger.LogInformation(
                    "Cross-repo reference {Owner}/{Repo}#{Number} in commit {Sha} — skipping (not supported yet)",
                    reference.RepoOwner, reference.RepoName, reference.IssueNumber, shortSha);
                continue;
            }

            var issue = await _issueService.GetIssueAsync(repoName, reference.IssueNumber);
            if (issue == null)
            {
                _logger.LogDebug("Issue #{Number} referenced in commit {Sha} not found in {Repo}",
                    reference.IssueNumber, shortSha, repoName);
                continue;
            }

            // Check if this commit was already processed for this issue (prevents duplicate comments on re-push)
            var alreadyReferenced = issue.Comments.Any(c =>
                c.Body.Contains(shortSha) &&
                (c.Body.StartsWith("Closed via commit") || c.Body.StartsWith("Referenced in commit")));
            if (alreadyReferenced)
                continue;

            if (reference.IsClosing)
            {
                // Close the issue and add a comment
                if (issue.State == IssueState.Open)
                {
                    await _issueService.AddCommentAsync(repoName, reference.IssueNumber, authorName,
                        $"Closed via commit [`{shortSha}`](/repo/{repoName}/commit/{commitSha})");
                    await _issueService.CloseIssueAsync(repoName, reference.IssueNumber);

                    _logger.LogInformation("Issue #{Number} in {Repo} auto-closed by commit {Sha}",
                        reference.IssueNumber, repoName, shortSha);
                }
            }
            else
            {
                // Non-closing reference — add a cross-reference comment
                await _issueService.AddCommentAsync(repoName, reference.IssueNumber, authorName,
                    $"Referenced in commit [`{shortSha}`](/repo/{repoName}/commit/{commitSha})");

                _logger.LogInformation("Issue #{Number} in {Repo} referenced by commit {Sha}",
                    reference.IssueNumber, repoName, shortSha);
            }
        }
    }

    public async Task CloseReferencedIssues(string repoName, string commitSha, string commitMessage, string authorName)
    {
        // This is a convenience method that delegates to ProcessCommitMessage
        await ProcessCommitMessage(repoName, commitMessage, commitSha, authorName);
    }

    public async Task ProcessPullRequestMerge(string repoName, int prNumber, List<string> commitMessages, string mergedBy)
    {
        var processedIssues = new HashSet<int>();

        foreach (var message in commitMessages)
        {
            var references = ParseIssueReferences(message);
            foreach (var reference in references)
            {
                // Skip cross-repo references
                if (reference.RepoOwner != null && reference.RepoName != null)
                    continue;

                // Skip already processed issues to avoid duplicate comments
                if (!processedIssues.Add(reference.IssueNumber))
                    continue;

                var issue = await _issueService.GetIssueAsync(repoName, reference.IssueNumber);
                if (issue == null)
                    continue;

                if (reference.IsClosing)
                {
                    if (issue.State == IssueState.Open)
                    {
                        await _issueService.AddCommentAsync(repoName, reference.IssueNumber, mergedBy,
                            $"Closed via pull request [#{prNumber}](/repo/{repoName}/pulls/{prNumber})");
                        await _issueService.CloseIssueAsync(repoName, reference.IssueNumber);

                        _logger.LogInformation("Issue #{Number} in {Repo} auto-closed by PR #{PrNumber}",
                            reference.IssueNumber, repoName, prNumber);
                    }
                }
                else
                {
                    await _issueService.AddCommentAsync(repoName, reference.IssueNumber, mergedBy,
                        $"Referenced in pull request [#{prNumber}](/repo/{repoName}/pulls/{prNumber})");

                    _logger.LogInformation("Issue #{Number} in {Repo} referenced by PR #{PrNumber}",
                        reference.IssueNumber, repoName, prNumber);
                }
            }
        }
    }

    public string RenderIssueLinks(string message, string repoName)
    {
        if (string.IsNullOrWhiteSpace(message))
            return message;

        // Replace #NNN with clickable links, but HTML-encode the rest (restore common safe chars)
        var encoded = System.Net.WebUtility.HtmlEncode(message).Replace("&#39;", "'").Replace("&#x27;", "'");
        return Regex.Replace(encoded, @"(?<!\w)#(\d+)\b", match =>
        {
            var number = match.Groups[1].Value;
            return $"<a href=\"/repo/{System.Net.WebUtility.HtmlEncode(repoName)}/issues/{number}\" class=\"text-primary text-decoration-none fw-semibold\">#{number}</a>";
        });
    }
}
