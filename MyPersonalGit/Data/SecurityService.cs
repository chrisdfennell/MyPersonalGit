using System.Text.Json;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public class SecurityService
{
    private readonly string _dataPath;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(IConfiguration configuration, ILogger<SecurityService> logger)
    {
        var projectRoot = configuration["ProjectRoot"] ?? throw new InvalidOperationException("ProjectRoot not configured");
        _dataPath = Path.Combine(projectRoot, ".mypersonalgit");
        _logger = logger;
        Directory.CreateDirectory(_dataPath);
    }

    private string GetAdvisoriesFilePath(string repoName) => Path.Combine(_dataPath, $"{repoName}_advisories.json");
    private string GetScansFilePath(string repoName) => Path.Combine(_dataPath, $"{repoName}_scans.json");

    public async Task<List<SecurityAdvisory>> GetAdvisoriesAsync(string repoName)
    {
        var filePath = GetAdvisoriesFilePath(repoName);
        if (!File.Exists(filePath))
            return new List<SecurityAdvisory>();

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<SecurityAdvisory>>(json) ?? new List<SecurityAdvisory>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load advisories for {RepoName}", repoName);
            return new List<SecurityAdvisory>();
        }
    }

    public async Task<SecurityAdvisory> CreateAdvisoryAsync(
        string repoName, string title, string description, 
        SecuritySeverity severity, string affectedVersions, string reporter)
    {
        var advisories = await GetAdvisoriesAsync(repoName);
        
        var advisory = new SecurityAdvisory
        {
            Id = advisories.Count > 0 ? advisories.Max(a => a.Id) + 1 : 1,
            RepoName = repoName,
            Title = title,
            Description = description,
            Severity = severity,
            AffectedVersions = affectedVersions,
            Reporter = reporter,
            State = SecurityAdvisoryState.Draft,
            CreatedAt = DateTime.UtcNow
        };

        advisories.Add(advisory);
        await SaveAdvisoriesAsync(repoName, advisories);
        return advisory;
    }

    public async Task<bool> PublishAdvisoryAsync(string repoName, int advisoryId)
    {
        var advisories = await GetAdvisoriesAsync(repoName);
        var advisory = advisories.FirstOrDefault(a => a.Id == advisoryId);
        
        if (advisory == null)
            return false;

        advisory.State = SecurityAdvisoryState.Published;
        advisory.PublishedAt = DateTime.UtcNow;
        await SaveAdvisoriesAsync(repoName, advisories);
        return true;
    }

    public async Task<bool> CloseAdvisoryAsync(string repoName, int advisoryId, string? patchedVersions = null)
    {
        var advisories = await GetAdvisoriesAsync(repoName);
        var advisory = advisories.FirstOrDefault(a => a.Id == advisoryId);
        
        if (advisory == null)
            return false;

        advisory.State = SecurityAdvisoryState.Closed;
        advisory.ClosedAt = DateTime.UtcNow;
        advisory.PatchedVersions = patchedVersions;
        await SaveAdvisoriesAsync(repoName, advisories);
        return true;
    }

    public async Task<List<SecurityScan>> GetScansAsync(string repoName)
    {
        var filePath = GetScansFilePath(repoName);
        if (!File.Exists(filePath))
            return new List<SecurityScan>();

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<SecurityScan>>(json) ?? new List<SecurityScan>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load scans for {RepoName}", repoName);
            return new List<SecurityScan>();
        }
    }

    public async Task<SecurityScan?> GetLatestScanAsync(string repoName)
    {
        var scans = await GetScansAsync(repoName);
        return scans.OrderByDescending(s => s.ScannedAt).FirstOrDefault();
    }

    public async Task<SecurityScan> CreateScanAsync(string repoName, List<Dependency> dependencies)
    {
        var scans = await GetScansAsync(repoName);
        
        var vulnerabilitiesFound = dependencies.Sum(d => d.Vulnerabilities.Count);
        var criticalCount = dependencies.Sum(d => d.Vulnerabilities.Count(v => v.Severity == SecuritySeverity.Critical));
        var highCount = dependencies.Sum(d => d.Vulnerabilities.Count(v => v.Severity == SecuritySeverity.High));
        var mediumCount = dependencies.Sum(d => d.Vulnerabilities.Count(v => v.Severity == SecuritySeverity.Medium));
        var lowCount = dependencies.Sum(d => d.Vulnerabilities.Count(v => v.Severity == SecuritySeverity.Low));

        var scan = new SecurityScan
        {
            Id = scans.Count > 0 ? scans.Max(s => s.Id) + 1 : 1,
            RepoName = repoName,
            ScannedAt = DateTime.UtcNow,
            VulnerabilitiesFound = vulnerabilitiesFound,
            CriticalCount = criticalCount,
            HighCount = highCount,
            MediumCount = mediumCount,
            LowCount = lowCount,
            Dependencies = dependencies
        };

        scans.Add(scan);
        await SaveScansAsync(repoName, scans);
        return scan;
    }

    private async Task SaveAdvisoriesAsync(string repoName, List<SecurityAdvisory> advisories)
    {
        var filePath = GetAdvisoriesFilePath(repoName);
        var json = JsonSerializer.Serialize(advisories, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    private async Task SaveScansAsync(string repoName, List<SecurityScan> scans)
    {
        var filePath = GetScansFilePath(repoName);
        var json = JsonSerializer.Serialize(scans, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }
}
