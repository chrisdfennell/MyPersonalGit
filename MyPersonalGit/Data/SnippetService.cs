using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface ISnippetService
{
    Task<List<Snippet>> GetSnippetsAsync(string? owner = null, bool includePrivate = false);
    Task<Snippet?> GetSnippetAsync(int id);
    Task<Snippet> CreateSnippetAsync(string title, string? description, string owner, bool isPublic, List<SnippetFile> files);
    Task<bool> UpdateSnippetAsync(int id, string title, string? description, bool isPublic, List<SnippetFile> files);
    Task<bool> DeleteSnippetAsync(int id, string requestingUser);
}

public class SnippetService : ISnippetService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<SnippetService> _logger;

    public SnippetService(IDbContextFactory<AppDbContext> dbFactory, ILogger<SnippetService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<Snippet>> GetSnippetsAsync(string? owner = null, bool includePrivate = false)
    {
        using var db = _dbFactory.CreateDbContext();
        var query = db.Snippets.Include(s => s.Files).AsQueryable();

        if (!string.IsNullOrEmpty(owner))
            query = query.Where(s => s.Owner == owner);

        if (!includePrivate)
            query = query.Where(s => s.IsPublic);

        return await query.OrderByDescending(s => s.UpdatedAt).ToListAsync();
    }

    public async Task<Snippet?> GetSnippetAsync(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Snippets.Include(s => s.Files).FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Snippet> CreateSnippetAsync(string title, string? description, string owner, bool isPublic, List<SnippetFile> files)
    {
        using var db = _dbFactory.CreateDbContext();

        var snippet = new Snippet
        {
            Title = title,
            Description = description,
            Owner = owner,
            IsPublic = isPublic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Files = files
        };

        db.Snippets.Add(snippet);
        await db.SaveChangesAsync();
        _logger.LogInformation("Snippet '{Title}' created by {Owner}", title, owner);
        return snippet;
    }

    public async Task<bool> UpdateSnippetAsync(int id, string title, string? description, bool isPublic, List<SnippetFile> files)
    {
        using var db = _dbFactory.CreateDbContext();
        var snippet = await db.Snippets.Include(s => s.Files).FirstOrDefaultAsync(s => s.Id == id);
        if (snippet == null) return false;

        snippet.Title = title;
        snippet.Description = description;
        snippet.IsPublic = isPublic;
        snippet.UpdatedAt = DateTime.UtcNow;

        // Replace files
        db.SnippetFiles.RemoveRange(snippet.Files);
        snippet.Files = files;

        await db.SaveChangesAsync();
        _logger.LogInformation("Snippet #{Id} updated", id);
        return true;
    }

    public async Task<bool> DeleteSnippetAsync(int id, string requestingUser)
    {
        using var db = _dbFactory.CreateDbContext();
        var snippet = await db.Snippets.FirstOrDefaultAsync(s => s.Id == id);
        if (snippet == null) return false;

        db.Snippets.Remove(snippet);
        await db.SaveChangesAsync();
        _logger.LogInformation("Snippet #{Id} deleted by {User}", id, requestingUser);
        return true;
    }
}
