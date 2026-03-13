using System.Text.RegularExpressions;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using GitRepository = LibGit2Sharp.Repository;

namespace MyPersonalGit.Data;

public class CodeOwnerRule
{
    public string Pattern { get; set; } = "";
    public List<string> Owners { get; set; } = new();
}

public interface ICodeOwnersService
{
    List<CodeOwnerRule> ParseCodeOwners(string content);
    List<string> GetCodeOwnersForFile(string repoPath, string branch, string filePath);
    Dictionary<string, List<string>> GetCodeOwnersForPullRequest(string repoPath, string branch, List<string> changedFiles);
    string? GetCodeOwnersFile(string repoPath, string branch);
}

public class CodeOwnersService : ICodeOwnersService
{
    private readonly IConfiguration _config;
    private readonly IAdminService _adminService;
    private readonly ILogger<CodeOwnersService> _logger;

    private static readonly string[] CodeOwnersPaths = new[]
    {
        "CODEOWNERS",
        ".github/CODEOWNERS",
        "docs/CODEOWNERS"
    };

    public CodeOwnersService(IConfiguration config, IAdminService adminService, ILogger<CodeOwnersService> logger)
    {
        _config = config;
        _adminService = adminService;
        _logger = logger;
    }

    public List<CodeOwnerRule> ParseCodeOwners(string content)
    {
        var rules = new List<CodeOwnerRule>();
        if (string.IsNullOrWhiteSpace(content)) return rules;

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                continue;

            var pattern = parts[0];
            var owners = parts.Skip(1)
                .Select(o => o.TrimStart('@'))
                .Where(o => !string.IsNullOrEmpty(o))
                .ToList();

            if (owners.Any())
            {
                rules.Add(new CodeOwnerRule { Pattern = pattern, Owners = owners });
            }
        }

        return rules;
    }

    public List<string> GetCodeOwnersForFile(string repoPath, string branch, string filePath)
    {
        var content = GetCodeOwnersFile(repoPath, branch);
        if (content == null) return new List<string>();

        var rules = ParseCodeOwners(content);
        return MatchFileToOwners(rules, filePath);
    }

    public Dictionary<string, List<string>> GetCodeOwnersForPullRequest(string repoPath, string branch, List<string> changedFiles)
    {
        var content = GetCodeOwnersFile(repoPath, branch);
        if (content == null) return new Dictionary<string, List<string>>();

        var rules = ParseCodeOwners(content);
        var result = new Dictionary<string, List<string>>();

        foreach (var file in changedFiles)
        {
            var owners = MatchFileToOwners(rules, file);
            if (owners.Any())
            {
                result[file] = owners;
            }
        }

        return result;
    }

    public string? GetCodeOwnersFile(string repoPath, string branch)
    {
        try
        {
            if (!GitRepository.IsValid(repoPath)) return null;

            using var repo = new GitRepository(repoPath);
            var branchRef = repo.Branches[branch];
            if (branchRef?.Tip == null) return null;

            foreach (var path in CodeOwnersPaths)
            {
                var entry = branchRef.Tip[path];
                if (entry?.Target is Blob blob)
                {
                    return blob.GetContentText();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read CODEOWNERS from {RepoPath} branch {Branch}", repoPath, branch);
        }

        return null;
    }

    /// <summary>
    /// Returns the owners for a file path by matching against CODEOWNERS rules.
    /// Last matching pattern wins (like .gitignore).
    /// </summary>
    private static List<string> MatchFileToOwners(List<CodeOwnerRule> rules, string filePath)
    {
        // Normalize path separators
        filePath = filePath.Replace('\\', '/').TrimStart('/');

        List<string> matchedOwners = new();

        foreach (var rule in rules)
        {
            if (PatternMatches(rule.Pattern, filePath))
            {
                matchedOwners = rule.Owners;
            }
        }

        return matchedOwners;
    }

    /// <summary>
    /// Matches a CODEOWNERS pattern against a file path using GitHub-compatible rules.
    /// </summary>
    private static bool PatternMatches(string pattern, string filePath)
    {
        // Normalize
        filePath = filePath.Replace('\\', '/').TrimStart('/');
        var originalPattern = pattern;
        pattern = pattern.TrimStart('/');

        // Exact wildcard — matches everything
        if (originalPattern == "*")
            return true;

        // Directory pattern (trailing slash): matches all files under that directory
        if (originalPattern.EndsWith("/"))
        {
            var dir = pattern.TrimEnd('/');
            return filePath.StartsWith(dir + "/", StringComparison.OrdinalIgnoreCase)
                   || filePath.Equals(dir, StringComparison.OrdinalIgnoreCase);
        }

        // Extension pattern like *.js — no directory separators in pattern
        // If pattern has no slash, it matches any file in any directory
        bool anchoredToRoot = originalPattern.StartsWith("/");
        bool hasSlash = pattern.Contains('/');

        // Convert glob pattern to regex
        var regexPattern = GlobToRegex(pattern, anchoredToRoot || hasSlash);

        try
        {
            return Regex.IsMatch(filePath, regexPattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a CODEOWNERS glob pattern to a regex string.
    /// If anchored, the pattern must match from the start of the path.
    /// If not anchored, the pattern matches against the filename or any subpath.
    /// </summary>
    private static string GlobToRegex(string glob, bool anchored)
    {
        var regex = new System.Text.StringBuilder();

        if (anchored)
        {
            regex.Append('^');
        }
        else
        {
            // Unanchored patterns match anywhere in the path
            regex.Append("(^|/)");
        }

        for (int i = 0; i < glob.Length; i++)
        {
            char c = glob[i];
            switch (c)
            {
                case '*':
                    if (i + 1 < glob.Length && glob[i + 1] == '*')
                    {
                        // ** matches any number of directories
                        if (i + 2 < glob.Length && glob[i + 2] == '/')
                        {
                            regex.Append("(.*/)?");
                            i += 2; // skip **/
                        }
                        else
                        {
                            regex.Append(".*");
                            i += 1; // skip **
                        }
                    }
                    else
                    {
                        // * matches anything except /
                        regex.Append("[^/]*");
                    }
                    break;
                case '?':
                    regex.Append("[^/]");
                    break;
                case '.':
                    regex.Append("\\.");
                    break;
                case '+':
                case '(':
                case ')':
                case '{':
                case '}':
                case '^':
                case '$':
                case '|':
                case '\\':
                    regex.Append('\\');
                    regex.Append(c);
                    break;
                default:
                    regex.Append(c);
                    break;
            }
        }

        regex.Append('$');
        return regex.ToString();
    }
}
