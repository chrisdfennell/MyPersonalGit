namespace MyPersonalGit.Models;

public class RepositoryMirror
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string RemoteUrl { get; set; }
    public MirrorDirection Direction { get; set; } = MirrorDirection.Pull;
    public int IntervalMinutes { get; set; } = 60;
    public DateTime? LastSyncAt { get; set; }
    public string? LastSyncStatus { get; set; }
    public string? LastSyncError { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? AuthToken { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum MirrorDirection
{
    Pull,
    Push
}
