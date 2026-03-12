namespace MyPersonalGit.Models;

public class CommitComment
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string CommitSha { get; set; }
    public required string Author { get; set; }
    public required string Body { get; set; }
    public string? FilePath { get; set; }
    public int? LineNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
