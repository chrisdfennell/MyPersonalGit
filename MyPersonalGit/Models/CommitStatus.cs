namespace MyPersonalGit.Models;

public class CommitStatus
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string Sha { get; set; }
    public CommitStatusState State { get; set; }
    public required string Context { get; set; } // e.g. "ci/build", "ci/tests"
    public string? Description { get; set; }
    public string? TargetUrl { get; set; }
    public string? Creator { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum CommitStatusState
{
    Pending,
    Success,
    Failure,
    Error
}
