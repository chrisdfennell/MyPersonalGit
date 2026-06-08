using System.Text.Json;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public class ProjectService
{
    private readonly string _dataPath;

    public ProjectService(IConfiguration configuration)
    {
        var projectRoot = configuration["ProjectRoot"] ?? throw new InvalidOperationException("ProjectRoot not configured");
        _dataPath = Path.Combine(projectRoot, ".mypersonalgit");
        Directory.CreateDirectory(_dataPath);
    }

    private string GetProjectsFilePath(string repoName) => Path.Combine(_dataPath, $"{repoName}_projects.json");

    public async Task<List<Project>> GetProjectsAsync(string repoName)
    {
        var filePath = GetProjectsFilePath(repoName);
        if (!File.Exists(filePath))
            return new List<Project>();

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<List<Project>>(json) ?? new List<Project>();
    }

    public async Task<Project?> GetProjectAsync(string repoName, int projectId)
    {
        var projects = await GetProjectsAsync(repoName);
        return projects.FirstOrDefault(p => p.Id == projectId);
    }

    public async Task<Project> CreateProjectAsync(string repoName, string name, string? description, string owner)
    {
        var projects = await GetProjectsAsync(repoName);
        
        var project = new Project
        {
            Id = projects.Count > 0 ? projects.Max(p => p.Id) + 1 : 1,
            RepoName = repoName,
            Name = name,
            Description = description,
            Owner = owner,
            State = ProjectState.Open,
            CreatedAt = DateTime.UtcNow,
            Columns = new List<ProjectColumn>
            {
                new ProjectColumn { Id = 1, Name = "To Do", Order = 1, Cards = new List<ProjectCard>() },
                new ProjectColumn { Id = 2, Name = "In Progress", Order = 2, Cards = new List<ProjectCard>() },
                new ProjectColumn { Id = 3, Name = "Done", Order = 3, Cards = new List<ProjectCard>() }
            }
        };

        projects.Add(project);
        await SaveProjectsAsync(repoName, projects);
        return project;
    }

    public async Task AddColumnAsync(string repoName, int projectId, string columnName)
    {
        var projects = await GetProjectsAsync(repoName);
        var project = projects.FirstOrDefault(p => p.Id == projectId);
        
        if (project == null)
            throw new InvalidOperationException($"Project {projectId} not found");

        var maxOrder = project.Columns.Count > 0 ? project.Columns.Max(c => c.Order) : 0;
        var maxId = project.Columns.Count > 0 ? project.Columns.Max(c => c.Id) : 0;
        
        var column = new ProjectColumn
        {
            Id = maxId + 1,
            Name = columnName,
            Order = maxOrder + 1,
            Cards = new List<ProjectCard>()
        };

        project.Columns.Add(column);
        await SaveProjectsAsync(repoName, projects);
    }

    public async Task<ProjectCard> AddCardAsync(string repoName, int projectId, int columnId, string title, string? note, string creator, CardType type = CardType.Note, int? issueNumber = null, int? prNumber = null)
    {
        var projects = await GetProjectsAsync(repoName);
        var project = projects.FirstOrDefault(p => p.Id == projectId);
        
        if (project == null)
            throw new InvalidOperationException($"Project {projectId} not found");

        var column = project.Columns.FirstOrDefault(c => c.Id == columnId);
        if (column == null)
            throw new InvalidOperationException($"Column {columnId} not found");

        var maxOrder = column.Cards.Count > 0 ? column.Cards.Max(c => c.Order) : 0;
        var maxId = project.Columns.SelectMany(c => c.Cards).Count() > 0 
            ? project.Columns.SelectMany(c => c.Cards).Max(c => c.Id) 
            : 0;

        var card = new ProjectCard
        {
            Id = maxId + 1,
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
        await SaveProjectsAsync(repoName, projects);
        return card;
    }

    public async Task MoveCardAsync(string repoName, int projectId, int cardId, int targetColumnId, int targetOrder)
    {
        var projects = await GetProjectsAsync(repoName);
        var project = projects.FirstOrDefault(p => p.Id == projectId);
        
        if (project == null)
            throw new InvalidOperationException($"Project {projectId} not found");

        ProjectCard? card = null;
        ProjectColumn? sourceColumn = null;

        foreach (var col in project.Columns)
        {
            card = col.Cards.FirstOrDefault(c => c.Id == cardId);
            if (card != null)
            {
                sourceColumn = col;
                break;
            }
        }

        if (card == null || sourceColumn == null)
            throw new InvalidOperationException($"Card {cardId} not found");

        var targetColumn = project.Columns.FirstOrDefault(c => c.Id == targetColumnId);
        if (targetColumn == null)
            throw new InvalidOperationException($"Column {targetColumnId} not found");

        sourceColumn.Cards.Remove(card);
        card.Order = targetOrder;
        targetColumn.Cards.Insert(Math.Min(targetOrder, targetColumn.Cards.Count), card);

        for (int i = 0; i < targetColumn.Cards.Count; i++)
        {
            targetColumn.Cards[i].Order = i;
        }

        await SaveProjectsAsync(repoName, projects);
    }

    public async Task DeleteCardAsync(string repoName, int projectId, int cardId)
    {
        var projects = await GetProjectsAsync(repoName);
        var project = projects.FirstOrDefault(p => p.Id == projectId);
        
        if (project == null)
            return;

        foreach (var column in project.Columns)
        {
            var card = column.Cards.FirstOrDefault(c => c.Id == cardId);
            if (card != null)
            {
                column.Cards.Remove(card);
                await SaveProjectsAsync(repoName, projects);
                return;
            }
        }
    }

    public async Task CloseProjectAsync(string repoName, int projectId)
    {
        var projects = await GetProjectsAsync(repoName);
        var project = projects.FirstOrDefault(p => p.Id == projectId);
        
        if (project != null)
        {
            project.State = ProjectState.Closed;
            project.ClosedAt = DateTime.UtcNow;
            await SaveProjectsAsync(repoName, projects);
        }
    }

    public async Task ReopenProjectAsync(string repoName, int projectId)
    {
        var projects = await GetProjectsAsync(repoName);
        var project = projects.FirstOrDefault(p => p.Id == projectId);
        
        if (project != null)
        {
            project.State = ProjectState.Open;
            project.ClosedAt = null;
            await SaveProjectsAsync(repoName, projects);
        }
    }

    private async Task SaveProjectsAsync(string repoName, List<Project> projects)
    {
        var filePath = GetProjectsFilePath(repoName);
        var json = JsonSerializer.Serialize(projects, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }
}
