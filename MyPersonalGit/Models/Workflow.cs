namespace MyPersonalGit.Models;

public class WorkflowRun
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string WorkflowName { get; set; }
    public required string Branch { get; set; }
    public required string CommitSha { get; set; }
    public required string CommitMessage { get; set; }
    public required string TriggeredBy { get; set; }
    public WorkflowStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<WorkflowJob> Jobs { get; set; } = new();
}

public enum WorkflowStatus
{
    Queued,
    InProgress,
    Success,
    Failure,
    Cancelled
}

public class WorkflowJob
{
    public int Id { get; set; }
    public int WorkflowRunId { get; set; }
    public required string Name { get; set; }
    public WorkflowStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<WorkflowStep> Steps { get; set; } = new();
}

public class WorkflowStep
{
    public int Id { get; set; }
    public int WorkflowJobId { get; set; }
    public required string Name { get; set; }
    public WorkflowStatus Status { get; set; }
    public string? Output { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class Webhook
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string Url { get; set; }
    public required string Secret { get; set; }
    public List<string> Events { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
}

public class WebhookDelivery
{
    public int Id { get; set; }
    public int WebhookId { get; set; }
    public required string Event { get; set; }
    public required string Payload { get; set; }
    public int StatusCode { get; set; }
    public string? Response { get; set; }
    public DateTime DeliveredAt { get; set; }
    public bool Success { get; set; }
}
