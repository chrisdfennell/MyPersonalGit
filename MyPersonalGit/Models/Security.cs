namespace MyPersonalGit.Models;

public class SecurityAdvisory
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public SecuritySeverity Severity { get; set; }
    public required string AffectedVersions { get; set; }
    public string? PatchedVersions { get; set; }
    public required string Reporter { get; set; }
    public SecurityAdvisoryState State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public enum SecuritySeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum SecurityAdvisoryState
{
    Draft,
    Published,
    Closed
}

public class Dependency
{
    public int Id { get; set; }
    public int SecurityScanId { get; set; }
    public required string Name { get; set; }
    public required string Version { get; set; }
    public required string Type { get; set; }
    public List<Vulnerability> Vulnerabilities { get; set; } = new();
}

public class Vulnerability
{
    public int Id { get; set; }
    public int DependencyId { get; set; }
    public required string VulnerabilityId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public SecuritySeverity Severity { get; set; }
    public string? FixedVersion { get; set; }
    public string? Url { get; set; }
}

public class SecurityScan
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public DateTime ScannedAt { get; set; }
    public int VulnerabilitiesFound { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public List<Dependency> Dependencies { get; set; } = new();
}
