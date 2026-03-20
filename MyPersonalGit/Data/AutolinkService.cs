using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IAutolinkService
{
    Task<List<AutolinkPattern>> GetPatternsAsync(string repoName);
    Task<AutolinkPattern> AddPatternAsync(string repoName, string prefix, string urlTemplate);
    Task<bool> DeletePatternAsync(int id);
    string ApplyAutolinks(string text, string repoName, List<AutolinkPattern>? patterns = null);
}

public class AutolinkService : IAutolinkService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AutolinkService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<AutolinkPattern>> GetPatternsAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.AutolinkPatterns
            .Where(p => p.RepoName == repoName)
            .OrderBy(p => p.Prefix)
            .ToListAsync();
    }

    public async Task<AutolinkPattern> AddPatternAsync(string repoName, string prefix, string urlTemplate)
    {
        using var db = _dbFactory.CreateDbContext();
        var pattern = new AutolinkPattern
        {
            RepoName = repoName,
            Prefix = prefix,
            UrlTemplate = urlTemplate
        };
        db.AutolinkPatterns.Add(pattern);
        await db.SaveChangesAsync();
        return pattern;
    }

    public async Task<bool> DeletePatternAsync(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        var pattern = await db.AutolinkPatterns.FindAsync(id);
        if (pattern == null) return false;
        db.AutolinkPatterns.Remove(pattern);
        await db.SaveChangesAsync();
        return true;
    }

    public string ApplyAutolinks(string text, string repoName, List<AutolinkPattern>? patterns = null)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Built-in: convert #123 to issue links (but not inside existing href/tags)
        // Use negative lookbehind to avoid matching inside HTML attributes
        var result = Regex.Replace(text, @"(?<![\w&/""=])#(\d+)\b", m =>
        {
            var number = m.Groups[1].Value;
            return $"<a href=\"/repo/{repoName}/issues/{number}\">#{number}</a>";
        });

        // Apply custom autolink patterns
        if (patterns != null)
        {
            foreach (var pattern in patterns)
            {
                var escapedPrefix = Regex.Escape(pattern.Prefix);
                result = Regex.Replace(result, $@"(?<![\w""])({escapedPrefix})(\d+)\b", m =>
                {
                    var number = m.Groups[2].Value;
                    var fullRef = m.Groups[1].Value + number;
                    var url = pattern.UrlTemplate.Replace("{0}", number);
                    return $"<a href=\"{url}\" target=\"_blank\" rel=\"noopener\">{fullRef}</a>";
                });
            }
        }

        return result;
    }
}
