using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IWikiService
{
    Task<List<WikiPage>> GetPagesAsync(string repoName);
    Task<WikiPage?> GetPageAsync(string repoName, string slug);
    Task<WikiPage> CreatePageAsync(string repoName, string title, string content, string author);
    Task<WikiPage> UpdatePageAsync(string repoName, string slug, string content, string author, string message);
    Task DeletePageAsync(string repoName, string slug);
    Task<WikiPageRevision?> GetRevisionAsync(string repoName, string slug, int revisionId);
}

public class WikiService : IWikiService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<WikiService> _logger;

    public WikiService(IDbContextFactory<AppDbContext> dbFactory, ILogger<WikiService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<WikiPage>> GetPagesAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.WikiPages
            .Include(p => p.Revisions)
            .Where(p => p.RepoName == repoName)
            .ToListAsync();
    }

    public async Task<WikiPage?> GetPageAsync(string repoName, string slug)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.WikiPages
            .Include(p => p.Revisions)
            .FirstOrDefaultAsync(p => p.RepoName == repoName && p.Slug.ToLower() == slug.ToLower());
    }

    public async Task<WikiPage> CreatePageAsync(string repoName, string title, string content, string author)
    {
        using var db = _dbFactory.CreateDbContext();

        var slug = GenerateSlug(title);

        if (await db.WikiPages.AnyAsync(p => p.RepoName == repoName && p.Slug.ToLower() == slug.ToLower()))
            throw new InvalidOperationException($"A page with slug '{slug}' already exists");

        var page = new WikiPage
        {
            RepoName = repoName,
            Title = title,
            Slug = slug,
            Content = content,
            Author = author,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Revisions = new List<WikiPageRevision>
            {
                new WikiPageRevision
                {
                    Content = content,
                    Author = author,
                    Message = "Initial page creation",
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        db.WikiPages.Add(page);
        await db.SaveChangesAsync();

        _logger.LogInformation("Wiki page '{Title}' created in {RepoName} by {Author}", title, repoName, author);
        return page;
    }

    public async Task<WikiPage> UpdatePageAsync(string repoName, string slug, string content, string author, string message)
    {
        using var db = _dbFactory.CreateDbContext();

        var page = await db.WikiPages
            .Include(p => p.Revisions)
            .FirstOrDefaultAsync(p => p.RepoName == repoName && p.Slug.ToLower() == slug.ToLower())
            ?? throw new InvalidOperationException($"Page with slug '{slug}' not found");

        page.Content = content;
        page.UpdatedAt = DateTime.UtcNow;
        page.Revisions.Add(new WikiPageRevision
        {
            WikiPageId = page.Id,
            Content = content,
            Author = author,
            Message = message,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        _logger.LogInformation("Wiki page '{Slug}' updated in {RepoName} by {Author}", slug, repoName, author);
        return page;
    }

    public async Task DeletePageAsync(string repoName, string slug)
    {
        using var db = _dbFactory.CreateDbContext();

        var page = await db.WikiPages
            .FirstOrDefaultAsync(p => p.RepoName == repoName && p.Slug.ToLower() == slug.ToLower());

        if (page != null)
        {
            db.WikiPages.Remove(page);
            await db.SaveChangesAsync();
        }

        _logger.LogInformation("Wiki page '{Slug}' deleted from {RepoName}", slug, repoName);
    }

    public async Task<WikiPageRevision?> GetRevisionAsync(string repoName, string slug, int revisionId)
    {
        using var db = _dbFactory.CreateDbContext();

        var page = await db.WikiPages
            .Include(p => p.Revisions)
            .FirstOrDefaultAsync(p => p.RepoName == repoName && p.Slug.ToLower() == slug.ToLower());

        return page?.Revisions.FirstOrDefault(r => r.Id == revisionId);
    }

    private static string GenerateSlug(string title)
    {
        return title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .Aggregate("", (current, c) => current + c);
    }
}
