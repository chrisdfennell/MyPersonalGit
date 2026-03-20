namespace MyPersonalGit.Models;

public class DeploymentEnvironment
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string Name { get; set; }
    public string? Url { get; set; }
    public int WaitTimerMinutes { get; set; }
    public bool RequireApproval { get; set; }
    public List<string> RequiredReviewers { get; set; } = new();
    public List<string> AllowedBranches { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class Deployment
{
    public int Id { get; set; }
    public int EnvironmentId { get; set; }
    public required string RepoName { get; set; }
    public required string EnvironmentName { get; set; }
    public int? WorkflowRunId { get; set; }
    public required string CommitSha { get; set; }
    public required string Ref { get; set; }
    public required string Creator { get; set; }
    public DeploymentStatus Status { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum DeploymentStatus { Pending, WaitingApproval, WaitingTimer, InProgress, Success, Failure, Cancelled }

public class DeploymentApproval
{
    public int Id { get; set; }
    public int DeploymentId { get; set; }
    public required string Reviewer { get; set; }
    public bool Approved { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
