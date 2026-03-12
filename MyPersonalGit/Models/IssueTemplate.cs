namespace MyPersonalGit.Models;

public class IssueTemplate
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string Name { get; set; } // e.g. "Bug Report", "Feature Request"
    public string? Description { get; set; }
    public required string Body { get; set; } // markdown template content
    public string? Labels { get; set; } // comma-separated default labels
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
