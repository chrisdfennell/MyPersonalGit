namespace MyPersonalGit.Models;

public class Issue
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string Title { get; set; }
    public string? Body { get; set; }
    public required string Author { get; set; }
    public string? Assignee { get; set; }
    public IssueState State { get; set; } = IssueState.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public List<string> Labels { get; set; } = new();
    public List<IssueComment> Comments { get; set; } = new();
    public int Number { get; set; }
}

public enum IssueState
{
    Open,
    Closed
}

public class IssueComment
{
    public int Id { get; set; }
    public int? IssueId { get; set; }
    public int? PullRequestId { get; set; }
    public required string Author { get; set; }
    public required string Body { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
