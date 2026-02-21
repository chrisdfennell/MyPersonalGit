using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IProjectService
{
    Task<List<Project>> GetProjectsAsync(string repoName);
    Task<Project?> GetProjectAsync(string repoName, int projectId);
    Task<Project> CreateProjectAsync(string repoName, string name, string? description, string owner);
    Task AddColumnAsync(string repoName, int projectId, string columnName);
    Task<ProjectCard> AddCardAsync(string repoName, int projectId, int columnId, string title, string? note, string creator, CardType type = CardType.Note, int? issueNumber = null, int? prNumber = null);
    Task MoveCardAsync(string repoName, int projectId, int cardId, int targetColumnId, int targetOrder);
    Task DeleteCardAsync(string repoName, int projectId, int cardId);
    Task CloseProjectAsync(string repoName, int projectId);
    Task ReopenProjectAsync(string repoName, int projectId);
}

public class ProjectService : IProjectService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IDbContextFactory<AppDbContext> dbFactory, ILogger<ProjectService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<Project>> GetProjectsAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Projects
            .Include(p => p.Columns)
                .ThenInclude(c => c.Cards)
            .Where(p => p.RepoName == repoName)
            .ToListAsync();
    }

    public async Task<Project?> GetProjectAsync(string repoName, int projectId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Projects
            .Include(p => p.Columns)
                .ThenInclude(c => c.Cards)
            .FirstOrDefaultAsync(p => p.RepoName == repoName && p.Id == projectId);
    }

    public async Task<Project> CreateProjectAsync(string repoName, string name, string? description, string owner)
    {
        using var db = _dbFactory.CreateDbContext();

        var project = new Project
        {
            RepoName = repoName,
            Name = name,
            Description = description,
            Owner = owner,
            State = ProjectState.Open,
            CreatedAt = DateTime.UtcNow,
            Columns = new List<ProjectColumn>
            {
                new ProjectColumn { Name = "To Do", Order = 1, Cards = new List<ProjectCard>() },
                new ProjectColumn { Name = "In Progress", Order = 2, Cards = new List<ProjectCard>() },
                new ProjectColumn { Name = "Done", Order = 3, Cards = new List<ProjectCard>() }
            }
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        _logger.LogInformation("Project '{Name}' created in {RepoName} by {Owner}", name, repoName, owner);
        return project;
    }

    public async Task AddColumnAsync(string repoName, int projectId, string columnName)
    {
        using var db = _dbFactory.CreateDbContext();

        var project = await db.Projects
            .Include(p => p.Columns)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.RepoName == repoName)
            ?? throw new InvalidOperationException($"Project {projectId} not found");

        var maxOrder = project.Columns.Count > 0 ? project.Columns.Max(c => c.Order) : 0;

        project.Columns.Add(new ProjectColumn
        {
            ProjectId = projectId,
            Name = columnName,
            Order = maxOrder + 1,
            Cards = new List<ProjectCard>()
        });

        await db.SaveChangesAsync();

        _logger.LogInformation("Column '{ColumnName}' added to project {ProjectId} in {RepoName}", columnName, projectId, repoName);
    }

    public async Task<ProjectCard> AddCardAsync(string repoName, int projectId, int columnId, string title, string? note, string creator, CardType type = CardType.Note, int? issueNumber = null, int? prNumber = null)
    {
        using var db = _dbFactory.CreateDbContext();

        var column = await db.ProjectColumns
            .Include(c => c.Cards)
            .FirstOrDefaultAsync(c => c.Id == columnId && c.ProjectId == projectId)
            ?? throw new InvalidOperationException($"Column {columnId} not found");

        var maxOrder = column.Cards.Count > 0 ? column.Cards.Max(c => c.Order) : 0;

        var card = new ProjectCard
        {
            ProjectColumnId = columnId,
            Title = title,
            Note = note,
            Order = maxOrder + 1,
            Type = type,
            IssueNumber = issueNumber,
            PullRequestNumber = prNumber,
            Creator = creator,
            CreatedAt = DateTime.UtcNow
        };

        column.Cards.Add(card);
        await db.SaveChangesAsync();

        return card;
    }

    public async Task MoveCardAsync(string repoName, int projectId, int cardId, int targetColumnId, int targetOrder)
    {
        using var db = _dbFactory.CreateDbContext();

        var card = await db.ProjectCards.FirstOrDefaultAsync(c => c.Id == cardId)
            ?? throw new InvalidOperationException($"Card {cardId} not found");

        var targetColumn = await db.ProjectColumns
            .Include(c => c.Cards)
            .FirstOrDefaultAsync(c => c.Id == targetColumnId && c.ProjectId == projectId)
            ?? throw new InvalidOperationException($"Column {targetColumnId} not found");

        card.ProjectColumnId = targetColumnId;
        card.Order = targetOrder;

        // Re-order cards in target column
        var cards = targetColumn.Cards.Where(c => c.Id != cardId).OrderBy(c => c.Order).ToList();
        cards.Insert(Math.Min(targetOrder, cards.Count), card);
        for (int i = 0; i < cards.Count; i++)
            cards[i].Order = i;

        await db.SaveChangesAsync();
    }

    public async Task DeleteCardAsync(string repoName, int projectId, int cardId)
    {
        using var db = _dbFactory.CreateDbContext();

        var card = await db.ProjectCards.FirstOrDefaultAsync(c => c.Id == cardId);
        if (card != null)
        {
            db.ProjectCards.Remove(card);
            await db.SaveChangesAsync();
        }
    }

    public async Task CloseProjectAsync(string repoName, int projectId)
    {
        using var db = _dbFactory.CreateDbContext();

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.RepoName == repoName);
        if (project != null)
        {
            project.State = ProjectState.Closed;
            project.ClosedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        _logger.LogInformation("Project {ProjectId} closed in {RepoName}", projectId, repoName);
    }

    public async Task ReopenProjectAsync(string repoName, int projectId)
    {
        using var db = _dbFactory.CreateDbContext();

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.RepoName == repoName);
        if (project != null)
        {
            project.State = ProjectState.Open;
            project.ClosedAt = null;
            await db.SaveChangesAsync();
        }

        _logger.LogInformation("Project {ProjectId} reopened in {RepoName}", projectId, repoName);
    }
}
