using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IRepositoryLabelService
{
    Task<List<RepositoryLabel>> GetLabelsAsync(string repoName);
    Task<RepositoryLabel?> AddLabelAsync(string repoName, string name, string color, string? description);
    Task<RepositoryLabel?> UpdateLabelAsync(string repoName, int labelId, string name, string color, string? description);
    Task<bool> DeleteLabelAsync(string repoName, int labelId);
}

public class RepositoryLabelService : IRepositoryLabelService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<RepositoryLabelService> _logger;

    public RepositoryLabelService(IDbContextFactory<AppDbContext> dbFactory, ILogger<RepositoryLabelService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<RepositoryLabel>> GetLabelsAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.RepositoryLabels
            .Where(l => l.RepoName == repoName)
            .OrderBy(l => l.Name)
            .ToListAsync();
    }

    public async Task<RepositoryLabel?> AddLabelAsync(string repoName, string name, string color, string? description)
    {
        using var db = _dbFactory.CreateDbContext();

        var existing = await db.RepositoryLabels
            .FirstOrDefaultAsync(l => l.RepoName == repoName && l.Name == name);
        if (existing != null)
            return null;

        var label = new RepositoryLabel
        {
            RepoName = repoName,
            Name = name,
            Color = color,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        db.RepositoryLabels.Add(label);
        await db.SaveChangesAsync();

        _logger.LogInformation("Label '{Name}' added to {RepoName}", name, repoName);
        return label;
    }

    public async Task<RepositoryLabel?> UpdateLabelAsync(string repoName, int labelId, string name, string color, string? description)
    {
        using var db = _dbFactory.CreateDbContext();
        var label = await db.RepositoryLabels
            .FirstOrDefaultAsync(l => l.Id == labelId && l.RepoName == repoName);

        if (label == null) return null;

        label.Name = name;
        label.Color = color;
        label.Description = description;
        await db.SaveChangesAsync();

        _logger.LogInformation("Label '{Name}' updated in {RepoName}", name, repoName);
        return label;
    }

    public async Task<bool> DeleteLabelAsync(string repoName, int labelId)
    {
        using var db = _dbFactory.CreateDbContext();
        var label = await db.RepositoryLabels
            .FirstOrDefaultAsync(l => l.Id == labelId && l.RepoName == repoName);

        if (label == null) return false;

        db.RepositoryLabels.Remove(label);
        await db.SaveChangesAsync();

        _logger.LogInformation("Label '{Name}' deleted from {RepoName}", label.Name, repoName);
        return true;
    }
}
