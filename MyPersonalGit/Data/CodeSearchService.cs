using System.Text;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;

namespace MyPersonalGit.Data;

public class CodeSearchResult
{
    public string RepoName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string Branch { get; set; } = "";
    public List<CodeSearchMatch> Matches { get; set; } = new();
}

public class CodeSearchMatch
{
    public int LineNumber { get; set; }
    public string Line { get; set; } = "";
}

public interface ICodeSearchService
{
    Task<List<CodeSearchResult>> SearchAsync(string query, string? repoName = null, string? fileExtension = null, int maxResults = 50);
}

public class CodeSearchService : ICodeSearchService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IConfiguration _config;
    private readonly IAdminService _adminService;
    private readonly ILogger<CodeSearchService> _logger;

    public CodeSearchService(IDbContextFactory<AppDbContext> dbFactory, IConfiguration config, IAdminService adminService, ILogger<CodeSearchService> logger)
    {
        _dbFactory = dbFactory;
        _config = config;
        _adminService = adminService;
        _logger = logger;
    }

    public async Task<List<CodeSearchResult>> SearchAsync(string query, string? repoName = null, string? fileExtension = null, int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new();

        var results = new List<CodeSearchResult>();

        var systemSettings = await _adminService.GetSystemSettingsAsync();
        var projectRoot = !string.IsNullOrEmpty(systemSettings.ProjectRoot)
            ? systemSettings.ProjectRoot
            : _config["Git:ProjectRoot"] ?? "/repos";

        using var db = _dbFactory.CreateDbContext();
        var repos = repoName != null
            ? await db.Repositories.Where(r => r.Name == repoName).ToListAsync()
            : await db.Repositories.Where(r => !r.IsPrivate).ToListAsync();

        foreach (var repo in repos)
        {
            if (results.Count >= maxResults) break;

            var repoPath = Path.Combine(projectRoot, repo.Name);
            if (!Repository.IsValid(repoPath))
            {
                repoPath = Path.Combine(projectRoot, repo.Name + ".git");
                if (!Repository.IsValid(repoPath))
                    continue;
            }

            try
            {
                using var gitRepo = new Repository(repoPath);
                var branch = gitRepo.Head;
                if (branch?.Tip == null) continue;

                SearchTree(gitRepo, branch.Tip.Tree, "", query, fileExtension, repo.Name, branch.FriendlyName, results, maxResults);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search repository {RepoName}", repo.Name);
            }
        }

        return results;
    }

    private static void SearchTree(Repository repo, Tree tree, string prefix, string query, string? fileExtension, string repoName, string branchName, List<CodeSearchResult> results, int maxResults)
    {
        foreach (var entry in tree)
        {
            if (results.Count >= maxResults) return;

            var entryPath = string.IsNullOrEmpty(prefix) ? entry.Name : $"{prefix}/{entry.Name}";

            if (entry.TargetType == TreeEntryTargetType.Tree)
            {
                SearchTree(repo, (Tree)entry.Target, entryPath, query, fileExtension, repoName, branchName, results, maxResults);
            }
            else if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                if (fileExtension != null && !entry.Name.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase))
                    continue;

                var blob = (Blob)entry.Target;
                if (blob.Size > 512 * 1024) continue; // Skip files > 512KB
                if (blob.IsBinary) continue;

                try
                {
                    using var reader = new StreamReader(blob.GetContentStream(), Encoding.UTF8);
                    var matches = new List<CodeSearchMatch>();
                    int lineNumber = 0;

                    while (!reader.EndOfStream && matches.Count < 5)
                    {
                        lineNumber++;
                        var line = reader.ReadLine();
                        if (line != null && line.Contains(query, StringComparison.OrdinalIgnoreCase))
                        {
                            matches.Add(new CodeSearchMatch
                            {
                                LineNumber = lineNumber,
                                Line = line.Length > 200 ? line[..200] + "..." : line
                            });
                        }
                    }

                    if (matches.Any())
                    {
                        results.Add(new CodeSearchResult
                        {
                            RepoName = repoName,
                            FilePath = entryPath,
                            Branch = branchName,
                            Matches = matches
                        });
                    }
                }
                catch { }
            }
        }
    }
}
