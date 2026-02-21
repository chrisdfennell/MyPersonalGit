namespace MyPersonalGit.Models;

public class PullRequest
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public int Number { get; set; }
    public required string Title { get; set; }
    public string? Body { get; set; }
    public required string Author { get; set; }
    public required string SourceBranch { get; set; }
    public required string TargetBranch { get; set; }
    public PullRequestState State { get; set; } = PullRequestState.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? MergedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? MergedBy { get; set; }
    public List<string> Reviewers { get; set; } = new();
    public List<PullRequestReview> Reviews { get; set; } = new();
    public List<IssueComment> Comments { get; set; } = new();
    public List<string> Labels { get; set; } = new();
    public bool IsDraft { get; set; }
}

public enum PullRequestState
{
    Open,
    Closed,
    Merged
}

public class PullRequestReview
{
    public int Id { get; set; }
    public int PullRequestId { get; set; }
    public required string Author { get; set; }
    public ReviewState State { get; set; }
    public string? Body { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ReviewState
{
    Pending,
    Approved,
    ChangesRequested,
    Commented
}
