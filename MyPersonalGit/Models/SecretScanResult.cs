namespace MyPersonalGit.Models;

public class SecretScanResult
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string CommitSha { get; set; }
    public required string FilePath { get; set; }
    public int LineNumber { get; set; }
    public required string SecretType { get; set; }
    public required string MatchSnippet { get; set; }
    public SecretScanResultState State { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime DetectedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public enum SecretScanResultState { Open, Resolved, FalsePositive }

public class SecretScanPattern
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Pattern { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsBuiltIn { get; set; }
}
