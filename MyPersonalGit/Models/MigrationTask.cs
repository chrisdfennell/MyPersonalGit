namespace MyPersonalGit.Models;

public enum MigrationStatus
{
    Pending,
    Cloning,
    ImportingIssues,
    ImportingPullRequests,
    Completed,
    Failed
}

public enum MigrationSource
{
    GitUrl,
    GitHub,
    GitLab,
    Bitbucket
}

public class MigrationTask
{
    public int Id { get; set; }
    public required string SourceUrl { get; set; }
    public MigrationSource Source { get; set; }
    public required string TargetRepoName { get; set; }
    public required string Owner { get; set; }
    public MigrationStatus Status { get; set; } = MigrationStatus.Pending;
    public int ProgressPercent { get; set; }
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ImportIssues { get; set; }
    public bool ImportPullRequests { get; set; }
    public bool MakePrivate { get; set; }
    public string? AuthToken { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
