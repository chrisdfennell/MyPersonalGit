namespace MyPersonalGit.Models;

public class TimeEntry
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public int IssueNumber { get; set; }
    public required string Username { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? StoppedAt { get; set; }
    public bool IsRunning { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
