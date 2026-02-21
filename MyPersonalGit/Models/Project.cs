namespace MyPersonalGit.Models;

public class Project
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Owner { get; set; }
    public ProjectState State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public List<ProjectColumn> Columns { get; set; } = new();
}

public enum ProjectState
{
    Open,
    Closed
}

public class ProjectColumn
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public required string Name { get; set; }
    public int Order { get; set; }
    public List<ProjectCard> Cards { get; set; } = new();
}

public class ProjectCard
{
    public int Id { get; set; }
    public int ProjectColumnId { get; set; }
    public required string Title { get; set; }
    public string? Note { get; set; }
    public int Order { get; set; }
    public CardType Type { get; set; }
    public int? IssueNumber { get; set; }
    public int? PullRequestNumber { get; set; }
    public required string Creator { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum CardType
{
    Note,
    Issue,
    PullRequest
}
