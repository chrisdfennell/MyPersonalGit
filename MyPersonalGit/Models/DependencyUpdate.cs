namespace MyPersonalGit.Models;

public class DependencyUpdateConfig
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string Ecosystem { get; set; }
    public string Schedule { get; set; } = "weekly";
    public bool IsEnabled { get; set; } = true;
    public string? Directory { get; set; }
    public int OpenPRLimit { get; set; } = 5;
    public DateTime? LastRunAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DependencyUpdateLog
{
    public int Id { get; set; }
    public int ConfigId { get; set; }
    public required string RepoName { get; set; }
    public required string PackageName { get; set; }
    public required string CurrentVersion { get; set; }
    public required string NewVersion { get; set; }
    public int? PullRequestNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}
