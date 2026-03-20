using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;
using GitRepository = LibGit2Sharp.Repository;

namespace MyPersonalGit.Data;

public interface ISecretScanService
{
    Task<List<SecretScanResult>> GetResultsAsync(string repoName, SecretScanResultState? state = null);
    Task<List<SecretScanResult>> ScanPushAsync(string repoName, string repoPath, string sha);
    Task<List<SecretScanResult>> FullScanAsync(string repoName, string repoPath);
    Task<bool> ResolveResultAsync(int resultId, string resolvedBy, SecretScanResultState state);
    Task<List<SecretScanPattern>> GetPatternsAsync();
    Task<SecretScanPattern> AddPatternAsync(string name, string pattern);
    Task<bool> TogglePatternAsync(int patternId);
    Task<bool> DeletePatternAsync(int patternId);
    Task EnsureBuiltInPatternsAsync();
}

public class SecretScanService : ISecretScanService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<SecretScanService> _logger;

    private static readonly (string Name, string Pattern)[] BuiltInPatterns = new[]
    {
        ("AWS Access Key", @"AKIA[0-9A-Z]{16}"),
        ("AWS Secret Key", @"(?i)aws_secret_access_key\s*[=:]\s*[A-Za-z0-9/+=]{40}"),
        ("GitHub Token (classic)", @"ghp_[a-zA-Z0-9]{36}"),
        ("GitHub Token (fine-grained)", @"github_pat_[a-zA-Z0-9_]{82}"),
        ("GitLab Token", @"glpat-[a-zA-Z0-9\-_]{20,}"),
        ("Slack Token", @"xox[bpors]-[a-zA-Z0-9\-]{10,}"),
        ("Slack Webhook", @"https://hooks\.slack\.com/services/T[a-zA-Z0-9_]+/B[a-zA-Z0-9_]+/[a-zA-Z0-9_]+"),
        ("Private Key", @"-----BEGIN (RSA |EC |DSA |OPENSSH )?PRIVATE KEY-----"),
        ("Generic API Key", @"(?i)(api[_-]?key|apikey)\s*[=:]\s*[""']?[A-Za-z0-9_\-]{20,}[""']?"),
        ("Generic Secret", @"(?i)(secret|password|passwd|pwd)\s*[=:]\s*[""'][^""']{8,}[""']"),
        ("Stripe Key", @"[sr]k_(live|test)_[a-zA-Z0-9]{20,}"),
        ("Google API Key", @"AIza[0-9A-Za-z\-_]{35}"),
        ("Twilio API Key", @"SK[a-f0-9]{32}"),
        ("SendGrid API Key", @"SG\.[a-zA-Z0-9_\-]{22}\.[a-zA-Z0-9_\-]{43}"),
        ("Heroku API Key", @"[hH][eE][rR][oO][kK][uU].*[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}"),
        ("NPM Token", @"npm_[a-zA-Z0-9]{36}"),
        ("PyPI Token", @"pypi-AgEIcHlwaS5vcmc[A-Za-z0-9\-_]{50,}"),
        ("Connection String", @"(?i)(Server|Data Source|mongodb(\+srv)?://|postgres(ql)?://|mysql://|redis://).*(?:password|pwd)\s*=\s*[^;\s]+"),
        ("JWT Token", @"eyJ[A-Za-z0-9_-]{10,}\.eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_\-]{10,}"),
        ("Discord Token", @"[MN][A-Za-z\d]{23,}\.[\w-]{6}\.[\w-]{27}"),
    };

    public SecretScanService(IDbContextFactory<AppDbContext> dbFactory, ILogger<SecretScanService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task EnsureBuiltInPatternsAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        var existing = await db.SecretScanPatterns.Where(p => p.IsBuiltIn).Select(p => p.Name).ToListAsync();

        foreach (var (name, pattern) in BuiltInPatterns)
        {
            if (existing.Contains(name)) continue;
            db.SecretScanPatterns.Add(new SecretScanPattern
            {
                Name = name,
                Pattern = pattern,
                IsEnabled = true,
                IsBuiltIn = true
            });
        }
        await db.SaveChangesAsync();
    }

    public async Task<List<SecretScanResult>> GetResultsAsync(string repoName, SecretScanResultState? state = null)
    {
        using var db = _dbFactory.CreateDbContext();
        var query = db.SecretScanResults.Where(r => r.RepoName == repoName);
        if (state.HasValue) query = query.Where(r => r.State == state.Value);
        return await query.OrderByDescending(r => r.DetectedAt).ToListAsync();
    }

    public async Task<List<SecretScanResult>> ScanPushAsync(string repoName, string repoPath, string sha)
    {
        var results = new List<SecretScanResult>();

        if (!GitRepository.IsValid(repoPath)) return results;

        using var db = _dbFactory.CreateDbContext();
        var patterns = await db.SecretScanPatterns.Where(p => p.IsEnabled).ToListAsync();
        if (!patterns.Any()) return results;

        var compiledPatterns = patterns
            .Select(p => { try { return (p.Name, Regex: new Regex(p.Pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(2))); } catch { return (p.Name, Regex: (Regex?)null); } })
            .Where(p => p.Regex != null)
            .ToList();

        try
        {
            using var repo = new GitRepository(repoPath);
            var commit = repo.Lookup<Commit>(sha);
            if (commit == null) return results;

            var parent = commit.Parents.FirstOrDefault();
            if (parent == null)
            {
                // Initial commit — scan all files in the tree
                results.AddRange(ScanTree(compiledPatterns!, commit.Tree, "", repoName, sha));
            }
            else
            {
                var diff = repo.Diff.Compare<Patch>(parent.Tree, commit.Tree);
                foreach (var change in diff)
                {
                    if (change.Status == ChangeKind.Deleted) continue;
                    var content = change.Patch;
                    foreach (var (addedLine, lineNum) in GetAddedLines(content))
                    {
                        foreach (var (name, regex) in compiledPatterns!)
                        {
                            if (regex!.IsMatch(addedLine))
                            {
                                results.Add(new SecretScanResult
                                {
                                    RepoName = repoName,
                                    CommitSha = sha,
                                    FilePath = change.Path,
                                    LineNumber = lineNum,
                                    SecretType = name,
                                    MatchSnippet = RedactSecret(addedLine),
                                    State = SecretScanResultState.Open,
                                    DetectedAt = DateTime.UtcNow
                                });
                                break; // One match per line is enough
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scanning push {Sha} in {RepoName}", sha, repoName);
        }

        if (results.Any())
        {
            // Deduplicate against existing results
            var existingKeys = (await db.SecretScanResults
                .Where(r => r.RepoName == repoName && r.CommitSha == sha)
                .ToListAsync())
                .Select(r => $"{r.FilePath}:{r.LineNumber}:{r.SecretType}")
                .ToHashSet();

            var newResults = results.Where(r => !existingKeys.Contains($"{r.FilePath}:{r.LineNumber}:{r.SecretType}")).ToList();
            if (newResults.Any())
            {
                db.SecretScanResults.AddRange(newResults);
                await db.SaveChangesAsync();
                _logger.LogWarning("Secret scanning found {Count} potential secrets in {RepoName}@{Sha}", newResults.Count, repoName, sha[..7]);
            }
        }

        return results;
    }

    public async Task<List<SecretScanResult>> FullScanAsync(string repoName, string repoPath)
    {
        var results = new List<SecretScanResult>();

        if (!GitRepository.IsValid(repoPath)) return results;

        using var db = _dbFactory.CreateDbContext();
        var patterns = await db.SecretScanPatterns.Where(p => p.IsEnabled).ToListAsync();
        if (!patterns.Any()) return results;

        var compiledPatterns = patterns
            .Select(p => { try { return (p.Name, Regex: new Regex(p.Pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(2))); } catch { return (p.Name, Regex: (Regex?)null); } })
            .Where(p => p.Regex != null)
            .ToList();

        try
        {
            using var repo = new GitRepository(repoPath);
            var head = repo.Head;
            if (head?.Tip == null) return results;

            results.AddRange(ScanTree(compiledPatterns!, head.Tip.Tree, "", repoName, head.Tip.Sha));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during full scan of {RepoName}", repoName);
        }

        if (results.Any())
        {
            // Clear existing open results and replace
            var existingOpen = await db.SecretScanResults
                .Where(r => r.RepoName == repoName && r.State == SecretScanResultState.Open)
                .ToListAsync();
            db.SecretScanResults.RemoveRange(existingOpen);

            db.SecretScanResults.AddRange(results);
            await db.SaveChangesAsync();
            _logger.LogWarning("Full secret scan found {Count} potential secrets in {RepoName}", results.Count, repoName);
        }

        return results;
    }

    private static List<SecretScanResult> ScanTree(List<(string Name, Regex? Regex)> patterns, Tree tree, string path, string repoName, string sha)
    {
        var results = new List<SecretScanResult>();

        foreach (var entry in tree)
        {
            var fullPath = string.IsNullOrEmpty(path) ? entry.Name : $"{path}/{entry.Name}";

            if (entry.TargetType == TreeEntryTargetType.Tree)
            {
                results.AddRange(ScanTree(patterns, (Tree)entry.Target, fullPath, repoName, sha));
            }
            else if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                // Skip binary/large files and common non-code files
                if (IsBinaryOrSkippable(entry.Name)) continue;

                var blob = (Blob)entry.Target;
                if (blob.Size > 1_000_000) continue; // Skip files > 1MB

                try
                {
                    using var reader = new StreamReader(blob.GetContentStream());
                    int lineNum = 0;
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lineNum++;
                        foreach (var (name, regex) in patterns)
                        {
                            if (regex!.IsMatch(line))
                            {
                                results.Add(new SecretScanResult
                                {
                                    RepoName = repoName,
                                    CommitSha = sha,
                                    FilePath = fullPath,
                                    LineNumber = lineNum,
                                    SecretType = name,
                                    MatchSnippet = RedactSecret(line),
                                    State = SecretScanResultState.Open,
                                    DetectedAt = DateTime.UtcNow
                                });
                                break;
                            }
                        }
                    }
                }
                catch { }
            }
        }

        return results;
    }

    public async Task<bool> ResolveResultAsync(int resultId, string resolvedBy, SecretScanResultState state)
    {
        using var db = _dbFactory.CreateDbContext();
        var result = await db.SecretScanResults.FindAsync(resultId);
        if (result == null) return false;

        result.State = state;
        result.ResolvedBy = resolvedBy;
        result.ResolvedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<SecretScanPattern>> GetPatternsAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.SecretScanPatterns.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<SecretScanPattern> AddPatternAsync(string name, string pattern)
    {
        // Validate regex
        _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(2));

        using var db = _dbFactory.CreateDbContext();
        var p = new SecretScanPattern { Name = name, Pattern = pattern, IsEnabled = true, IsBuiltIn = false };
        db.SecretScanPatterns.Add(p);
        await db.SaveChangesAsync();
        return p;
    }

    public async Task<bool> TogglePatternAsync(int patternId)
    {
        using var db = _dbFactory.CreateDbContext();
        var p = await db.SecretScanPatterns.FindAsync(patternId);
        if (p == null) return false;
        p.IsEnabled = !p.IsEnabled;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePatternAsync(int patternId)
    {
        using var db = _dbFactory.CreateDbContext();
        var p = await db.SecretScanPatterns.FindAsync(patternId);
        if (p == null || p.IsBuiltIn) return false;
        db.SecretScanPatterns.Remove(p);
        await db.SaveChangesAsync();
        return true;
    }

    private static List<(string Line, int LineNumber)> GetAddedLines(string patch)
    {
        var results = new List<(string, int)>();
        int lineNum = 0;
        foreach (var line in patch.Split('\n'))
        {
            if (line.StartsWith("@@"))
            {
                var match = Regex.Match(line, @"\+(\d+)");
                if (match.Success) lineNum = int.Parse(match.Groups[1].Value) - 1;
                continue;
            }
            if (line.StartsWith("+") && !line.StartsWith("+++"))
            {
                lineNum++;
                results.Add((line[1..], lineNum));
            }
            else if (!line.StartsWith("-"))
            {
                lineNum++;
            }
        }
        return results;
    }

    private static string RedactSecret(string line)
    {
        if (line.Length <= 20) return line;
        // Show first 10 and last 5 chars, redact the middle
        var trimmed = line.Trim();
        if (trimmed.Length > 80) trimmed = trimmed[..80] + "...";
        return trimmed;
    }

    private static bool IsBinaryOrSkippable(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".ico" or ".bmp" or ".webp"
            or ".zip" or ".gz" or ".tar" or ".7z" or ".rar"
            or ".exe" or ".dll" or ".so" or ".dylib" or ".bin"
            or ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx"
            or ".woff" or ".woff2" or ".ttf" or ".eot" or ".otf"
            or ".mp3" or ".mp4" or ".avi" or ".mov" or ".wav"
            or ".min.js" or ".min.css"
            || fileName == "package-lock.json" || fileName == "yarn.lock";
    }
}
