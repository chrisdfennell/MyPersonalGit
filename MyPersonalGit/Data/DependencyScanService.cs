using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

/// <summary>A dependency that has a known vulnerability and/or an available update.</summary>
public record DependencyFinding(
    string Ecosystem,
    string Name,
    string Version,
    string ManifestPath,
    bool IsDev,
    IReadOnlyList<DependencyVuln> Advisories,
    string? Latest);

/// <summary>Decoded result of a dependency scan (the persisted shape, plus helpers).</summary>
public record DependencyScanData(
    string CommitSha,
    DateTime ScannedAt,
    int TotalCount,
    int VulnerableCount,
    int OutdatedCount,
    IReadOnlyList<DependencyFinding> Findings)
{
    private static string Key(DependencyFinding f) => $"{f.Ecosystem}|{f.Name}|{f.Version}";

    /// <summary>Advisories keyed by dependency, for overlaying on the full dependency table.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<DependencyVuln>> VulnsByKey() =>
        Findings.Where(f => f.Advisories.Count > 0).ToDictionary(Key, f => f.Advisories);

    /// <summary>Outdated info keyed by dependency, for overlaying on the full dependency table.</summary>
    public IReadOnlyDictionary<string, OutdatedInfo> OutdatedByKey() =>
        Findings.Where(f => f.Latest != null)
                .ToDictionary(Key, f => new OutdatedInfo(f.Version, f.Latest!, true));
}

public interface IDependencyScanService
{
    /// <summary>Returns the stored scan only if it matches the current HEAD and is recent; otherwise null.</summary>
    Task<DependencyScanData?> GetCachedIfFreshAsync(string repoName, CancellationToken ct = default);

    /// <summary>Runs a fresh scan, upserts it, and returns the data.</summary>
    Task<DependencyScanData> ScanAndStoreAsync(string repoName, CancellationToken ct = default);

    /// <summary>The raw stored scan row (for the Security/alerts view), or null.</summary>
    Task<DependencyScan?> GetLatestAsync(string repoName, CancellationToken ct = default);

    /// <summary>The latest stored scan decoded into findings (for the Security/alerts view), or null.</summary>
    Task<DependencyScanData?> GetLatestDataAsync(string repoName, CancellationToken ct = default);

    /// <summary>Scans every repository (used by the background scheduler). Returns the count scanned.</summary>
    Task<int> ScanAllAsync(CancellationToken ct = default);
}

/// <summary>
/// Orchestrates the dependency scan (manifest parse → OSV vulnerabilities + outdated
/// checks), caches the result in the <see cref="DependencyScan"/> table keyed by repo,
/// and serves it back. The stored row IS the cache: it is reused while the repo HEAD is
/// unchanged and the scan is recent, which both speeds up page views and limits how
/// often we hit the public registries.
/// </summary>
public class DependencyScanService : IDependencyScanService
{
    // Advisory data drifts even when code doesn't, so re-scan at least this often.
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly IDependencyService _depService;
    private readonly IVulnerabilityService _vulnService;
    private readonly IOutdatedService _outdatedService;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<DependencyScanService> _logger;

    public DependencyScanService(
        IDependencyService depService,
        IVulnerabilityService vulnService,
        IOutdatedService outdatedService,
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<DependencyScanService> logger)
    {
        _depService = depService;
        _vulnService = vulnService;
        _outdatedService = outdatedService;
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<DependencyScan?> GetLatestAsync(string repoName, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.DependencyScans.AsNoTracking().FirstOrDefaultAsync(s => s.RepoName == repoName, ct);
    }

    public async Task<DependencyScanData?> GetLatestDataAsync(string repoName, CancellationToken ct = default)
    {
        var row = await GetLatestAsync(repoName, ct);
        return row == null ? null : Decode(row);
    }

    public async Task<DependencyScanData?> GetCachedIfFreshAsync(string repoName, CancellationToken ct = default)
    {
        var sha = await _depService.GetHeadCommitShaAsync(repoName);
        if (string.IsNullOrEmpty(sha)) return null;

        var row = await GetLatestAsync(repoName, ct);
        if (row == null || row.CommitSha != sha) return null;
        if (DateTime.UtcNow - row.ScannedAt > CacheTtl) return null;

        return Decode(row);
    }

    public async Task<DependencyScanData> ScanAndStoreAsync(string repoName, CancellationToken ct = default)
    {
        var sha = await _depService.GetHeadCommitShaAsync(repoName) ?? "";
        var deps = await _depService.GetDependenciesAsync(repoName);

        // Vulnerability + outdated lookups are independent — run them together.
        var vulnsTask = _vulnService.CheckAsync(deps, ct);
        var outdatedTask = _outdatedService.CheckAsync(deps, ct);
        await Task.WhenAll(vulnsTask, outdatedTask);
        var vulns = await vulnsTask;
        var outdated = await outdatedTask;

        var findings = new List<DependencyFinding>();
        var seen = new HashSet<string>();
        foreach (var d in deps)
        {
            var key = VulnerabilityService.Key(d);
            var hasVuln = vulns.TryGetValue(key, out var advs) && advs.Count > 0;
            var isOutdated = outdated.TryGetValue(key, out var od);
            if (!hasVuln && !isOutdated) continue;
            if (!seen.Add(key)) continue; // collapse the same dep declared in multiple manifests

            findings.Add(new DependencyFinding(
                d.Ecosystem, d.Name, d.Version, d.ManifestPath, d.IsDev,
                hasVuln ? advs! : Array.Empty<DependencyVuln>(),
                isOutdated ? od!.Latest : null));
        }

        // Worst severity first, then by name, for stable display ordering.
        findings = findings
            .OrderByDescending(f => f.Advisories.Count == 0 ? -1 : f.Advisories.Max(a => VulnerabilityService.SeverityRank(a.Severity)))
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var data = new DependencyScanData(
            sha,
            DateTime.UtcNow,
            TotalCount: deps.Select(VulnerabilityService.Key).Distinct().Count(),
            VulnerableCount: findings.Count(f => f.Advisories.Count > 0),
            OutdatedCount: findings.Count(f => f.Latest != null),
            findings);

        await StoreAsync(repoName, data, ct);
        return data;
    }

    public async Task<int> ScanAllAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var repoNames = await db.Repositories.Select(r => r.Name).ToListAsync(ct);

        var scanned = 0;
        foreach (var name in repoNames)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                await ScanAndStoreAsync(name, ct);
                scanned++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dependency scan failed for {Repo}", name);
            }
        }
        return scanned;
    }

    private async Task StoreAsync(string repoName, DependencyScanData data, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var row = await db.DependencyScans.FirstOrDefaultAsync(s => s.RepoName == repoName, ct);
        var json = JsonSerializer.Serialize(data.Findings);
        if (row == null)
        {
            db.DependencyScans.Add(new DependencyScan
            {
                RepoName = repoName,
                CommitSha = data.CommitSha,
                ScannedAt = data.ScannedAt,
                TotalCount = data.TotalCount,
                VulnerableCount = data.VulnerableCount,
                OutdatedCount = data.OutdatedCount,
                ResultsJson = json
            });
        }
        else
        {
            row.CommitSha = data.CommitSha;
            row.ScannedAt = data.ScannedAt;
            row.TotalCount = data.TotalCount;
            row.VulnerableCount = data.VulnerableCount;
            row.OutdatedCount = data.OutdatedCount;
            row.ResultsJson = json;
        }
        await db.SaveChangesAsync(ct);
    }

    internal static DependencyScanData Decode(DependencyScan row)
    {
        IReadOnlyList<DependencyFinding> findings;
        try
        {
            findings = JsonSerializer.Deserialize<List<DependencyFinding>>(row.ResultsJson)
                       ?? new List<DependencyFinding>();
        }
        catch (JsonException)
        {
            findings = new List<DependencyFinding>();
        }
        return new DependencyScanData(
            row.CommitSha, row.ScannedAt, row.TotalCount, row.VulnerableCount, row.OutdatedCount, findings);
    }
}
