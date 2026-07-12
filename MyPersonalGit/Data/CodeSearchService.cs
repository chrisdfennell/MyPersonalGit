using System.IO;
using System.Text;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

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

        try
        {
            using var db = _dbFactory.CreateDbContext();

            var repos = repoName != null
                ? await db.Repositories.Where(r => r.Name == repoName).Select(r => new { r.Name, r.DefaultBranch }).ToListAsync()
                : await db.Repositories.Where(r => !r.IsPrivate).Select(r => new { r.Name, r.DefaultBranch }).ToListAsync();

            var repoBranchMap = repos.ToDictionary(r => r.Name, r => r.DefaultBranch ?? "main", StringComparer.OrdinalIgnoreCase);
            var allowedRepoNames = repoBranchMap.Keys.ToList();

            if (!allowedRepoNames.Any())
                return results;

            var lowerQuery = query.ToLower();

            // FTS5 (SQLite): index-backed substring search via the trigram tokenizer.
            // Requires >= 3 chars; shorter queries and other providers use the LIKE scan.
            IQueryable<CodeSearchIndex> baseQuery;
            if (db.Database.IsSqlite() && query.Length >= 3)
            {
                // Quote the query so FTS operators (AND/OR/NEAR/*) in user input are literal.
                var ftsQuery = "\"" + query.Replace("\"", "\"\"") + "\"";
                baseQuery = db.CodeSearchIndices.FromSqlRaw(
                    @"SELECT i.* FROM ""CodeSearchIndices"" i
                      JOIN ""CodeSearchFts"" f ON f.rowid = i.""Id""
                      WHERE ""CodeSearchFts"" MATCH {0}", ftsQuery);
            }
            else
            {
                baseQuery = db.CodeSearchIndices.Where(i => i.Content.ToLower().Contains(lowerQuery));
            }

            IQueryable<CodeSearchIndex> dbQuery = baseQuery
                .Where(i => allowedRepoNames.Contains(i.RepoName));

            if (!string.IsNullOrEmpty(fileExtension))
            {
                var ext = fileExtension.StartsWith(".") ? fileExtension : "." + fileExtension;
                var lowerExt = ext.ToLower();
                dbQuery = dbQuery.Where(i => i.FilePath.ToLower().EndsWith(lowerExt));
            }

            List<CodeSearchIndex> matchedFiles;
            try
            {
                matchedFiles = await dbQuery
                    .OrderBy(i => i.RepoName)
                    .ThenBy(i => i.FilePath)
                    .Take(maxResults)
                    .ToListAsync();
            }
            catch (Exception ftsEx) when (db.Database.IsSqlite() && query.Length >= 3)
            {
                // FTS table missing or corrupt — fall back to the unindexed scan.
                _logger.LogWarning(ftsEx, "FTS5 search failed; falling back to LIKE scan");
                matchedFiles = await db.CodeSearchIndices
                    .Where(i => allowedRepoNames.Contains(i.RepoName))
                    .Where(i => i.Content.ToLower().Contains(lowerQuery))
                    .OrderBy(i => i.RepoName)
                    .ThenBy(i => i.FilePath)
                    .Take(maxResults)
                    .ToListAsync();
            }

            foreach (var file in matchedFiles)
            {
                using var reader = new StringReader(file.Content);
                var matches = new List<CodeSearchMatch>();
                int lineNumber = 0;

                string? line;
                while ((line = reader.ReadLine()) != null && matches.Count < 5)
                {
                    lineNumber++;
                    if (line.Contains(query, StringComparison.OrdinalIgnoreCase))
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
                        RepoName = file.RepoName,
                        FilePath = file.FilePath,
                        Branch = repoBranchMap.TryGetValue(file.RepoName, out var b) ? b : "main",
                        Matches = matches
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing indexed code search for query '{Query}'", query);
        }

        return results;
    }
}
