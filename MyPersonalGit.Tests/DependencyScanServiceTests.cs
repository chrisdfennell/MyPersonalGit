using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyPersonalGit.Data;
using NSubstitute;

namespace MyPersonalGit.Tests;

public class VulnerabilityDetailParsingTests
{
    [Fact]
    public void ParseVulnDetail_ReadsGhsaSeverityAndSummary()
    {
        var json = """
        { "summary": "Prototype pollution in foo",
          "database_specific": { "severity": "HIGH" } }
        """;
        var (sev, summary) = VulnerabilityService.ParseVulnDetail(json);
        Assert.Equal("HIGH", sev);
        Assert.Equal("Prototype pollution in foo", summary);
    }

    [Theory]
    [InlineData("MODERATE", "MODERATE")]
    [InlineData("MEDIUM", "MODERATE")]   // normalized
    [InlineData("critical", "CRITICAL")] // case-insensitive
    [InlineData("bogus", "UNKNOWN")]
    public void ParseVulnDetail_NormalizesSeverity(string raw, string expected)
    {
        var json = $"{{ \"database_specific\": {{ \"severity\": \"{raw}\" }} }}";
        Assert.Equal(expected, VulnerabilityService.ParseVulnDetail(json).Severity);
    }

    [Fact]
    public void ParseVulnDetail_FallsBackToCvssNumericScore()
    {
        var json = """{ "severity": [ { "type": "CVSS_V3", "score": "9.8" } ] }""";
        Assert.Equal("CRITICAL", VulnerabilityService.ParseVulnDetail(json).Severity);
    }

    [Fact]
    public void ParseVulnDetail_Malformed_IsUnknown()
        => Assert.Equal("UNKNOWN", VulnerabilityService.ParseVulnDetail("not json").Severity);

    [Theory]
    [InlineData("CRITICAL", 4)]
    [InlineData("HIGH", 3)]
    [InlineData("MODERATE", 2)]
    [InlineData("LOW", 1)]
    [InlineData("UNKNOWN", 0)]
    public void SeverityRank_OrdersWorstFirst(string sev, int rank)
        => Assert.Equal(rank, VulnerabilityService.SeverityRank(sev));
}

public class DependencyScanServiceTests
{
    private static IDbContextFactory<AppDbContext> NewFactory() =>
        new TestDbContextFactory(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static DependencyScanService Build(
        IDbContextFactory<AppDbContext> factory,
        IReadOnlyList<DependencyItem> deps,
        IReadOnlyDictionary<string, IReadOnlyList<DependencyVuln>> vulns,
        IReadOnlyDictionary<string, OutdatedInfo> outdated,
        string? headSha = "sha-1")
    {
        var dep = Substitute.For<IDependencyService>();
        dep.GetHeadCommitShaAsync("repo").Returns(headSha);
        dep.GetDependenciesAsync("repo", Arg.Any<string?>()).Returns(deps);

        var vuln = Substitute.For<IVulnerabilityService>();
        vuln.CheckAsync(Arg.Any<IReadOnlyList<DependencyItem>>(), Arg.Any<CancellationToken>()).Returns(vulns);

        var outdatedSvc = Substitute.For<IOutdatedService>();
        outdatedSvc.CheckAsync(Arg.Any<IReadOnlyList<DependencyItem>>(), Arg.Any<CancellationToken>()).Returns(outdated);

        return new DependencyScanService(dep, vuln, outdatedSvc, factory, NullLogger<DependencyScanService>.Instance);
    }

    [Fact]
    public async Task ScanAndStore_PersistsFindingsAndCounts()
    {
        var d1 = new DependencyItem("npm", "left-pad", "1.0.0", "package.json", false);
        var d2 = new DependencyItem("npm", "jest", "28.0.0", "package.json", true);
        var deps = new List<DependencyItem> { d1, d2 };

        var vulns = new Dictionary<string, IReadOnlyList<DependencyVuln>>
        {
            [VulnerabilityService.Key(d1)] = new List<DependencyVuln>
            {
                new("GHSA-1", "https://osv.dev/vulnerability/GHSA-1", "HIGH", "bad bug")
            }
        };
        var outdated = new Dictionary<string, OutdatedInfo>
        {
            [VulnerabilityService.Key(d2)] = new OutdatedInfo("28.0.0", "29.7.0", true)
        };

        var factory = NewFactory();
        var svc = Build(factory, deps, vulns, outdated);

        var data = await svc.ScanAndStoreAsync("repo");

        Assert.Equal(2, data.TotalCount);
        Assert.Equal(1, data.VulnerableCount);
        Assert.Equal(1, data.OutdatedCount);
        Assert.Equal(2, data.Findings.Count); // one vulnerable + one outdated

        // Persisted and decodable.
        var stored = await svc.GetLatestDataAsync("repo");
        Assert.NotNull(stored);
        Assert.Equal("HIGH", stored!.VulnsByKey()[VulnerabilityService.Key(d1)][0].Severity);
        Assert.Equal("29.7.0", stored.OutdatedByKey()[VulnerabilityService.Key(d2)].Latest);
    }

    [Fact]
    public async Task ScanAndStore_UpsertsSingleRowPerRepo()
    {
        var deps = new List<DependencyItem>();
        var empty = (IReadOnlyDictionary<string, IReadOnlyList<DependencyVuln>>)new Dictionary<string, IReadOnlyList<DependencyVuln>>();
        var emptyOut = (IReadOnlyDictionary<string, OutdatedInfo>)new Dictionary<string, OutdatedInfo>();

        var factory = NewFactory();
        var svc = Build(factory, deps, empty, emptyOut);

        await svc.ScanAndStoreAsync("repo");
        await svc.ScanAndStoreAsync("repo");

        await using var db = factory.CreateDbContext();
        Assert.Equal(1, db.DependencyScans.Count(s => s.RepoName == "repo"));
    }

    [Fact]
    public async Task GetCachedIfFresh_ReturnsCache_WhenShaMatches()
    {
        var d1 = new DependencyItem("npm", "x", "1.0.0", "package.json", false);
        var deps = new List<DependencyItem> { d1 };
        var vulns = (IReadOnlyDictionary<string, IReadOnlyList<DependencyVuln>>)new Dictionary<string, IReadOnlyList<DependencyVuln>>();
        var outdated = (IReadOnlyDictionary<string, OutdatedInfo>)new Dictionary<string, OutdatedInfo>();

        var factory = NewFactory();
        var svc = Build(factory, deps, vulns, outdated, headSha: "sha-1");

        await svc.ScanAndStoreAsync("repo");
        var cached = await svc.GetCachedIfFreshAsync("repo");

        Assert.NotNull(cached);
        Assert.Equal("sha-1", cached!.CommitSha);
    }

    [Fact]
    public async Task GetCachedIfFresh_ReturnsNull_WhenHeadMoved()
    {
        var deps = new List<DependencyItem>();
        var vulns = (IReadOnlyDictionary<string, IReadOnlyList<DependencyVuln>>)new Dictionary<string, IReadOnlyList<DependencyVuln>>();
        var outdated = (IReadOnlyDictionary<string, OutdatedInfo>)new Dictionary<string, OutdatedInfo>();

        var factory = NewFactory();

        // Store against sha-1.
        var stored = Build(factory, deps, vulns, outdated, headSha: "sha-1");
        await stored.ScanAndStoreAsync("repo");

        // A new HEAD invalidates the cache.
        var moved = Build(factory, deps, vulns, outdated, headSha: "sha-2");
        Assert.Null(await moved.GetCachedIfFreshAsync("repo"));
    }
}
