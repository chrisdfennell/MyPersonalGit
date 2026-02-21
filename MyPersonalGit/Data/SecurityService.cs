using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface ISecurityService
{
    Task<List<SecurityAdvisory>> GetAdvisoriesAsync(string repoName);
    Task<SecurityAdvisory> CreateAdvisoryAsync(string repoName, string title, string description, SecuritySeverity severity, string affectedVersions, string reporter);
    Task<bool> PublishAdvisoryAsync(string repoName, int advisoryId);
    Task<bool> CloseAdvisoryAsync(string repoName, int advisoryId, string? patchedVersions = null);
    Task<List<SecurityScan>> GetScansAsync(string repoName);
    Task<SecurityScan?> GetLatestScanAsync(string repoName);
    Task<SecurityScan> CreateScanAsync(string repoName, List<Dependency> dependencies);
}

public class SecurityService : ISecurityService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(IDbContextFactory<AppDbContext> dbFactory, ILogger<SecurityService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<SecurityAdvisory>> GetAdvisoriesAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.SecurityAdvisories.Where(a => a.RepoName == repoName).ToListAsync();
    }

    public async Task<SecurityAdvisory> CreateAdvisoryAsync(
        string repoName, string title, string description,
        SecuritySeverity severity, string affectedVersions, string reporter)
    {
        using var db = _dbFactory.CreateDbContext();

        var advisory = new SecurityAdvisory
        {
            RepoName = repoName,
            Title = title,
            Description = description,
            Severity = severity,
            AffectedVersions = affectedVersions,
            Reporter = reporter,
            State = SecurityAdvisoryState.Draft,
            CreatedAt = DateTime.UtcNow
        };

        db.SecurityAdvisories.Add(advisory);
        await db.SaveChangesAsync();

        _logger.LogInformation("Security advisory '{Title}' created for {RepoName} by {Reporter}", title, repoName, reporter);
        return advisory;
    }

    public async Task<bool> PublishAdvisoryAsync(string repoName, int advisoryId)
    {
        using var db = _dbFactory.CreateDbContext();

        var advisory = await db.SecurityAdvisories
            .FirstOrDefaultAsync(a => a.Id == advisoryId && a.RepoName == repoName);

        if (advisory == null)
            return false;

        advisory.State = SecurityAdvisoryState.Published;
        advisory.PublishedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("Advisory {AdvisoryId} published for {RepoName}", advisoryId, repoName);
        return true;
    }

    public async Task<bool> CloseAdvisoryAsync(string repoName, int advisoryId, string? patchedVersions = null)
    {
        using var db = _dbFactory.CreateDbContext();

        var advisory = await db.SecurityAdvisories
            .FirstOrDefaultAsync(a => a.Id == advisoryId && a.RepoName == repoName);

        if (advisory == null)
            return false;

        advisory.State = SecurityAdvisoryState.Closed;
        advisory.ClosedAt = DateTime.UtcNow;
        advisory.PatchedVersions = patchedVersions;
        await db.SaveChangesAsync();

        _logger.LogInformation("Advisory {AdvisoryId} closed for {RepoName}", advisoryId, repoName);
        return true;
    }

    public async Task<List<SecurityScan>> GetScansAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.SecurityScans
            .Include(s => s.Dependencies)
                .ThenInclude(d => d.Vulnerabilities)
            .Where(s => s.RepoName == repoName)
            .ToListAsync();
    }

    public async Task<SecurityScan?> GetLatestScanAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.SecurityScans
            .Include(s => s.Dependencies)
                .ThenInclude(d => d.Vulnerabilities)
            .Where(s => s.RepoName == repoName)
            .OrderByDescending(s => s.ScannedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<SecurityScan> CreateScanAsync(string repoName, List<Dependency> dependencies)
    {
        using var db = _dbFactory.CreateDbContext();

        var vulnerabilitiesFound = dependencies.Sum(d => d.Vulnerabilities.Count);

        var scan = new SecurityScan
        {
            RepoName = repoName,
            ScannedAt = DateTime.UtcNow,
            VulnerabilitiesFound = vulnerabilitiesFound,
            CriticalCount = dependencies.Sum(d => d.Vulnerabilities.Count(v => v.Severity == SecuritySeverity.Critical)),
            HighCount = dependencies.Sum(d => d.Vulnerabilities.Count(v => v.Severity == SecuritySeverity.High)),
            MediumCount = dependencies.Sum(d => d.Vulnerabilities.Count(v => v.Severity == SecuritySeverity.Medium)),
            LowCount = dependencies.Sum(d => d.Vulnerabilities.Count(v => v.Severity == SecuritySeverity.Low)),
            Dependencies = dependencies
        };

        db.SecurityScans.Add(scan);
        await db.SaveChangesAsync();

        _logger.LogInformation("Security scan completed for {RepoName}: {Count} vulnerabilities found", repoName, scan.VulnerabilitiesFound);
        return scan;
    }
}
