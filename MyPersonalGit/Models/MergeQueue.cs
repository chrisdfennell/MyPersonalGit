namespace MyPersonalGit.Models;

/// <summary>
/// An entry in a repository's merge queue. Entries for the same target branch are
/// validated and merged strictly one at a time: the PR branch is updated with the
/// current target head, required status checks re-run against the result, and the
/// PR merges only when they pass — so every merge is tested against the branch
/// state it will actually land on.
/// </summary>
public class MergeQueueEntry
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public int PullRequestNumber { get; set; }
    public required string TargetBranch { get; set; }
    public required string EnqueuedBy { get; set; }
    public MergeQueueState State { get; set; } = MergeQueueState.Queued;
    /// <summary>MergeStrategy name: MergeCommit, Squash, or Rebase.</summary>
    public string MergeStrategy { get; set; } = "MergeCommit";
    public string? FailureReason { get; set; }
    public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum MergeQueueState
{
    Queued = 0,
    Validating = 1,
    Merged = 2,
    Failed = 3,
    Cancelled = 4
}
