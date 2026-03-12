using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IIssueTemplateService
{
    Task<List<IssueTemplate>> GetTemplatesAsync(string repoName);
    Task<IssueTemplate?> GetTemplateAsync(int id);
    Task<IssueTemplate> CreateTemplateAsync(string repoName, string name, string body, string? description = null, string? labels = null);
    Task<bool> UpdateTemplateAsync(int id, string name, string body, string? description = null, string? labels = null);
    Task<bool> DeleteTemplateAsync(int id);
}

public class IssueTemplateService : IIssueTemplateService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<IssueTemplateService> _logger;

    public IssueTemplateService(IDbContextFactory<AppDbContext> dbFactory, ILogger<IssueTemplateService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<IssueTemplate>> GetTemplatesAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.IssueTemplates
            .Where(t => t.RepoName == repoName)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IssueTemplate?> GetTemplateAsync(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.IssueTemplates.FindAsync(id);
    }

    public async Task<IssueTemplate> CreateTemplateAsync(string repoName, string name, string body, string? description = null, string? labels = null)
    {
        using var db = _dbFactory.CreateDbContext();

        var maxOrder = await db.IssueTemplates
            .Where(t => t.RepoName == repoName)
            .MaxAsync(t => (int?)t.SortOrder) ?? 0;

        var template = new IssueTemplate
        {
            RepoName = repoName,
            Name = name,
            Body = body,
            Description = description,
            Labels = labels,
            SortOrder = maxOrder + 1,
            CreatedAt = DateTime.UtcNow
        };

        db.IssueTemplates.Add(template);
        await db.SaveChangesAsync();

        _logger.LogInformation("Issue template '{Name}' created for {Repo}", name, repoName);
        return template;
    }

    public async Task<bool> UpdateTemplateAsync(int id, string name, string body, string? description = null, string? labels = null)
    {
        using var db = _dbFactory.CreateDbContext();
        var template = await db.IssueTemplates.FindAsync(id);
        if (template == null) return false;

        template.Name = name;
        template.Body = body;
        template.Description = description;
        template.Labels = labels;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTemplateAsync(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        var template = await db.IssueTemplates.FindAsync(id);
        if (template == null) return false;

        db.IssueTemplates.Remove(template);
        await db.SaveChangesAsync();
        return true;
    }
}
