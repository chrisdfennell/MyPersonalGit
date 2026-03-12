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
    public string RunsOn { get; set; } = "ubuntu-latest";
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
    public string? Command { get; set; }
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

public class WorkflowArtifact
{
    public int Id { get; set; }
    public int WorkflowRunId { get; set; }
    public required string Name { get; set; }
    public required string FilePath { get; set; }
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}

public class GlobalSecret
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string EncryptedValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class WorkflowSchedule
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string WorkflowFileName { get; set; }
    public required string CronExpression { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
}
