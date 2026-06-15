namespace MyPersonalGit.Models;

/// <summary>
/// The latest cached dependency scan for a repository: vulnerability + outdated
/// findings produced by the background scanner (and on-demand page views). One row
/// per repo (upserted); <see cref="ResultsJson"/> holds the serialized findings so
/// the UI can render without re-reading the repo or hitting the network.
/// </summary>
public class DependencyScan
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    /// <summary>HEAD commit the scan was taken against — used to invalidate the cache.</summary>
    public required string CommitSha { get; set; }
    public DateTime ScannedAt { get; set; }
    public int TotalCount { get; set; }
    public int VulnerableCount { get; set; }
    public int OutdatedCount { get; set; }
    /// <summary>Serialized list of findings (only deps with a vulnerability or an update).</summary>
    public string ResultsJson { get; set; } = "[]";
}
