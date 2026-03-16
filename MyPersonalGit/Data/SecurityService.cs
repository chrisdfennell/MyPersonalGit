using System.Text.Json;
using System.Text.RegularExpressions;
using LibGit2Sharp;
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
    Task<SecurityScan> ScanRepositoryAsync(string repoName, string repoPath);
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

    /// <summary>
    /// Scans a repository by extracting dependencies from project files and checking
    /// them against the OSV (Open Source Vulnerabilities) API.
    /// Supports: .csproj (NuGet), package.json (npm), requirements.txt (PyPI).
    /// </summary>
    public async Task<SecurityScan> ScanRepositoryAsync(string repoName, string repoPath)
    {
        _logger.LogInformation("Starting security scan for {RepoName}", repoName);

        // Extract dependencies from repo files
        var dependencies = ExtractDependencies(repoPath);
        _logger.LogInformation("Found {Count} dependencies in {RepoName}", dependencies.Count, repoName);

        // Check each dependency against OSV API
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        foreach (var dep in dependencies)
        {
            try
            {
                var vulns = await QueryOsvAsync(httpClient, dep.Name, dep.Version, dep.Type);
                dep.Vulnerabilities = vulns;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check vulnerabilities for {Package} {Version}", dep.Name, dep.Version);
            }
        }

        return await CreateScanAsync(repoName, dependencies);
    }

    /// <summary>Extracts dependencies from .csproj, package.json, and requirements.txt in the repo.</summary>
    private static List<Dependency> ExtractDependencies(string repoPath)
    {
        var dependencies = new List<Dependency>();

        if (!LibGit2Sharp.Repository.IsValid(repoPath)) return dependencies;

        using var repo = new LibGit2Sharp.Repository(repoPath);
        var head = repo.Head;
        if (head?.Tip == null) return dependencies;

        // Walk the tree to find dependency files
        WalkTree(head.Tip.Tree, "", dependencies);

        // Deduplicate (same package can appear in multiple .csproj files)
        return dependencies
            .GroupBy(d => $"{d.Type}:{d.Name}:{d.Version}")
            .Select(g => g.First())
            .ToList();
    }

    private static void WalkTree(Tree tree, string path, List<Dependency> dependencies)
    {
        foreach (var entry in tree)
        {
            var fullPath = string.IsNullOrEmpty(path) ? entry.Name : $"{path}/{entry.Name}";

            if (entry.TargetType == TreeEntryTargetType.Tree)
            {
                WalkTree((Tree)entry.Target, fullPath, dependencies);
            }
            else if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                var name = entry.Name.ToLowerInvariant();
                if (name.EndsWith(".csproj"))
                    ParseCsproj((Blob)entry.Target, dependencies);
                else if (name == "package.json")
                    ParsePackageJson((Blob)entry.Target, dependencies);
                else if (name == "requirements.txt")
                    ParseRequirementsTxt((Blob)entry.Target, dependencies);
            }
        }
    }

    private static void ParseCsproj(Blob blob, List<Dependency> dependencies)
    {
        try
        {
            using var reader = new StreamReader(blob.GetContentStream());
            var content = reader.ReadToEnd();
            // Match <PackageReference Include="Name" Version="1.2.3" />
            var matches = Regex.Matches(content,
                @"<PackageReference\s+Include=""([^""]+)""\s+Version=""([^""]+)""",
                RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                dependencies.Add(new Dependency
                {
                    Name = match.Groups[1].Value,
                    Version = match.Groups[2].Value,
                    Type = "NuGet",
                    Vulnerabilities = new List<Vulnerability>()
                });
            }
        }
        catch { }
    }

    private static void ParsePackageJson(Blob blob, List<Dependency> dependencies)
    {
        try
        {
            using var reader = new StreamReader(blob.GetContentStream());
            var content = reader.ReadToEnd();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            foreach (var section in new[] { "dependencies", "devDependencies" })
            {
                if (!root.TryGetProperty(section, out var deps)) continue;
                foreach (var dep in deps.EnumerateObject())
                {
                    var version = dep.Value.GetString() ?? "";
                    // Strip version prefixes: ^1.2.3 -> 1.2.3, ~1.2.3 -> 1.2.3, >=1.0 -> 1.0
                    version = Regex.Replace(version, @"^[\^~>=<]*", "");
                    if (string.IsNullOrEmpty(version) || !char.IsDigit(version[0])) continue;

                    dependencies.Add(new Dependency
                    {
                        Name = dep.Name,
                        Version = version,
                        Type = "npm",
                        Vulnerabilities = new List<Vulnerability>()
                    });
                }
            }
        }
        catch { }
    }

    private static void ParseRequirementsTxt(Blob blob, List<Dependency> dependencies)
    {
        try
        {
            using var reader = new StreamReader(blob.GetContentStream());
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith('#') || line.StartsWith('-')) continue;
                // Match: package==1.2.3 or package>=1.2.3
                var match = Regex.Match(line, @"^([a-zA-Z0-9_.-]+)\s*[=~!><]=?\s*([0-9][^\s,;]*)");
                if (match.Success)
                {
                    dependencies.Add(new Dependency
                    {
                        Name = match.Groups[1].Value,
                        Version = match.Groups[2].Value,
                        Type = "PyPI",
                        Vulnerabilities = new List<Vulnerability>()
                    });
                }
            }
        }
        catch { }
    }

    /// <summary>Queries the OSV.dev API for known vulnerabilities for a package.</summary>
    private static async Task<List<Vulnerability>> QueryOsvAsync(HttpClient client, string packageName, string version, string ecosystem)
    {
        var vulns = new List<Vulnerability>();

        // Map our ecosystem names to OSV ecosystem names
        var osvEcosystem = ecosystem switch
        {
            "NuGet" => "NuGet",
            "npm" => "npm",
            "PyPI" => "PyPI",
            _ => ecosystem
        };

        var payload = JsonSerializer.Serialize(new
        {
            version,
            package_ = new { name = packageName, ecosystem = osvEcosystem }
        });
        // OSV API uses "package" not "package_" — fix the JSON
        payload = payload.Replace("\"package_\"", "\"package\"");

        var response = await client.PostAsync("https://api.osv.dev/v1/query",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode) return vulns;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("vulns", out var vulnsArray)) return vulns;

        foreach (var vuln in vulnsArray.EnumerateArray())
        {
            var id = vuln.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
            var summary = vuln.TryGetProperty("summary", out var sumProp) ? sumProp.GetString() ?? "" : "";
            var details = vuln.TryGetProperty("details", out var detProp) ? detProp.GetString() ?? "" : "";

            // Determine severity from database_specific or severity array
            var severity = SecuritySeverity.Medium;
            if (vuln.TryGetProperty("database_specific", out var dbSpec) &&
                dbSpec.TryGetProperty("severity", out var sevProp))
            {
                severity = sevProp.GetString()?.ToUpperInvariant() switch
                {
                    "CRITICAL" => SecuritySeverity.Critical,
                    "HIGH" => SecuritySeverity.High,
                    "MODERATE" or "MEDIUM" => SecuritySeverity.Medium,
                    "LOW" => SecuritySeverity.Low,
                    _ => SecuritySeverity.Medium
                };
            }
            else if (vuln.TryGetProperty("severity", out var sevArray) && sevArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in sevArray.EnumerateArray())
                {
                    if (s.TryGetProperty("score", out var score))
                    {
                        var cvss = score.GetString() ?? "";
                        // Parse CVSS score from vector string if present
                        if (double.TryParse(cvss, out var cvssScore))
                        {
                            severity = cvssScore switch
                            {
                                >= 9.0 => SecuritySeverity.Critical,
                                >= 7.0 => SecuritySeverity.High,
                                >= 4.0 => SecuritySeverity.Medium,
                                _ => SecuritySeverity.Low
                            };
                        }
                    }
                }
            }

            // Find fixed version from affected ranges
            string? fixedVersion = null;
            if (vuln.TryGetProperty("affected", out var affected))
            {
                foreach (var aff in affected.EnumerateArray())
                {
                    if (!aff.TryGetProperty("ranges", out var ranges)) continue;
                    foreach (var range in ranges.EnumerateArray())
                    {
                        if (!range.TryGetProperty("events", out var events)) continue;
                        foreach (var evt in events.EnumerateArray())
                        {
                            if (evt.TryGetProperty("fixed", out var fixedProp))
                                fixedVersion = fixedProp.GetString();
                        }
                    }
                }
            }

            var url = $"https://osv.dev/vulnerability/{id}";
            if (vuln.TryGetProperty("references", out var refs))
            {
                foreach (var r in refs.EnumerateArray())
                {
                    if (r.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "ADVISORY" &&
                        r.TryGetProperty("url", out var urlProp))
                    {
                        url = urlProp.GetString() ?? url;
                        break;
                    }
                }
            }

            vulns.Add(new Vulnerability
            {
                VulnerabilityId = id,
                Title = string.IsNullOrEmpty(summary) ? id : summary,
                Description = string.IsNullOrEmpty(details) ? summary : (details.Length > 500 ? details[..500] + "..." : details),
                Severity = severity,
                FixedVersion = fixedVersion,
                Url = url
            });
        }

        return vulns;
    }
}
