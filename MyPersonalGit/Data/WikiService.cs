using System.Text.Json;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public class WikiService
{
    private readonly string _dataPath;

    public WikiService(IConfiguration configuration)
    {
        var projectRoot = configuration["ProjectRoot"] ?? throw new InvalidOperationException("ProjectRoot not configured");
        _dataPath = Path.Combine(projectRoot, ".mypersonalgit");
        Directory.CreateDirectory(_dataPath);
    }

    private string GetWikiFilePath(string repoName) => Path.Combine(_dataPath, $"{repoName}_wiki.json");

    public async Task<List<WikiPage>> GetPagesAsync(string repoName)
    {
        var filePath = GetWikiFilePath(repoName);
        if (!File.Exists(filePath))
            return new List<WikiPage>();

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<List<WikiPage>>(json) ?? new List<WikiPage>();
    }

    public async Task<WikiPage?> GetPageAsync(string repoName, string slug)
    {
        var pages = await GetPagesAsync(repoName);
        return pages.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<WikiPage> CreatePageAsync(string repoName, string title, string content, string author)
    {
        var pages = await GetPagesAsync(repoName);
        var slug = GenerateSlug(title);
        
        var existingPage = pages.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        if (existingPage != null)
            throw new InvalidOperationException($"A page with slug '{slug}' already exists");

        var page = new WikiPage
        {
            Id = pages.Count > 0 ? pages.Max(p => p.Id) + 1 : 1,
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
                    Id = 1,
                    Content = content,
                    Author = author,
                    Message = "Initial page creation",
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        pages.Add(page);
        await SavePagesAsync(repoName, pages);
        return page;
    }

    public async Task<WikiPage> UpdatePageAsync(string repoName, string slug, string content, string author, string message)
    {
        var pages = await GetPagesAsync(repoName);
        var page = pages.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        
        if (page == null)
            throw new InvalidOperationException($"Page with slug '{slug}' not found");

        var revision = new WikiPageRevision
        {
            Id = page.Revisions.Count + 1,
            Content = content,
            Author = author,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };

        page.Content = content;
        page.UpdatedAt = DateTime.UtcNow;
        page.Revisions.Add(revision);

        await SavePagesAsync(repoName, pages);
        return page;
    }

    public async Task DeletePageAsync(string repoName, string slug)
    {
        var pages = await GetPagesAsync(repoName);
        var page = pages.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        
        if (page != null)
        {
            pages.Remove(page);
            await SavePagesAsync(repoName, pages);
        }
    }

    public async Task<WikiPageRevision?> GetRevisionAsync(string repoName, string slug, int revisionId)
    {
        var page = await GetPageAsync(repoName, slug);
        return page?.Revisions.FirstOrDefault(r => r.Id == revisionId);
    }

    private async Task SavePagesAsync(string repoName, List<WikiPage> pages)
    {
        var filePath = GetWikiFilePath(repoName);
        var json = JsonSerializer.Serialize(pages, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    private string GenerateSlug(string title)
    {
        return title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .Aggregate("", (current, c) => current + c);
    }
}
