namespace MyPersonalGit.Models;

public class Milestone
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public int Number { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public MilestoneState State { get; set; } = MilestoneState.Open;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public required string Creator { get; set; }
}

public enum MilestoneState
{
    Open,
    Closed
}
