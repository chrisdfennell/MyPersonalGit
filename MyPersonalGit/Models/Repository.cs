namespace MyPersonalGit.Models;

public class Repository
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Owner { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Stars { get; set; }
    public int Forks { get; set; }
    public List<string> Topics { get; set; } = new();
    public string? DefaultBranch { get; set; } = "main";
    public bool HasIssues { get; set; } = true;
    public bool HasWiki { get; set; } = true;
    public bool HasProjects { get; set; } = true;
}

public class RepositoryStar
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string Username { get; set; }
    public DateTime StarredAt { get; set; } = DateTime.UtcNow;
}

public class RepositoryFork
{
    public int Id { get; set; }
    public required string OriginalRepo { get; set; }
    public required string ForkedRepo { get; set; }
    public required string Owner { get; set; }
    public DateTime ForkedAt { get; set; } = DateTime.UtcNow;
}
