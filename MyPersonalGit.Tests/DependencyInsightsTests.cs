using System.Text.Json;
using MyPersonalGit.Data;

namespace MyPersonalGit.Tests;

/// <summary>
/// Unit tests for the dependency-insights additions: CycloneDX SBOM generation
/// and the OSV.dev vulnerability request/response shaping. All pure — no network.
/// </summary>
public class SbomBuilderTests
{
    private static readonly DateTime Ts = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("npm", "left-pad", "1.3.0", "pkg:npm/left-pad@1.3.0")]
    [InlineData("NuGet", "Newtonsoft.Json", "13.0.3", "pkg:nuget/Newtonsoft.Json@13.0.3")]
    [InlineData("pip", "requests", "2.31.0", "pkg:pypi/requests@2.31.0")]
    [InlineData("Go", "github.com/gin-gonic/gin", "1.9.1", "pkg:golang/github.com%2Fgin-gonic%2Fgin@1.9.1")]
    [InlineData("Cargo", "serde", "1.0.0", "pkg:cargo/serde@1.0.0")]
    [InlineData("RubyGems", "rails", "7.1.0", "pkg:gem/rails@7.1.0")]
    public void BuildPurl_FlatEcosystems(string eco, string name, string ver, string expected)
        => Assert.Equal(expected, SbomBuilder.BuildPurl(eco, name, ver));

    [Fact]
    public void BuildPurl_Maven_SplitsGroupAndArtifact()
        => Assert.Equal("pkg:maven/com.google.guava/guava@33.0",
                        SbomBuilder.BuildPurl("Maven", "com.google.guava:guava", "33.0"));

    [Fact]
    public void BuildPurl_Composer_KeepsVendorPath()
        => Assert.Equal("pkg:composer/monolog/monolog@2.9.1",
                        SbomBuilder.BuildPurl("Composer", "monolog/monolog", "2.9.1"));

    [Fact]
    public void BuildPurl_UnknownEcosystem_IsEmpty()
        => Assert.Equal("", SbomBuilder.BuildPurl("Conan", "boost", "1.0"));

    [Theory]
    [InlineData("^1.2.3", "1.2.3")]
    [InlineData("~1.2.0", "1.2.0")]
    [InlineData(">=2.0", "2.0")]
    [InlineData("v3.1.4", "3.1.4")]
    [InlineData(">=1.0, <2.0", "1.0")]
    [InlineData("*", "")]
    [InlineData("", "")]
    public void CleanVersion_StripsRangeOperators(string raw, string expected)
        => Assert.Equal(expected, SbomBuilder.CleanVersion(raw));

    [Fact]
    public void BuildCycloneDx_ProducesValidDocumentWithComponents()
    {
        var deps = new List<DependencyItem>
        {
            new("npm", "left-pad", "1.3.0", "package.json", false),
            new("npm", "jest", "29.0.0", "package.json", true),
            new("NuGet", "xunit", "2.9.3", "src/app.csproj", false),
        };

        var json = SbomBuilder.BuildCycloneDx("my-repo", deps, Ts, "urn:uuid:1111");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("CycloneDX", root.GetProperty("bomFormat").GetString());
        Assert.Equal("1.5", root.GetProperty("specVersion").GetString());
        Assert.Equal("urn:uuid:1111", root.GetProperty("serialNumber").GetString());
        Assert.Equal("my-repo", root.GetProperty("metadata").GetProperty("component").GetProperty("name").GetString());

        var components = root.GetProperty("components").EnumerateArray().ToList();
        Assert.Equal(3, components.Count);

        var jest = components.Single(c => c.GetProperty("name").GetString() == "jest");
        Assert.Equal("optional", jest.GetProperty("scope").GetString()); // dev dependency
        Assert.Equal("pkg:npm/jest@29.0.0", jest.GetProperty("purl").GetString());

        var leftPad = components.Single(c => c.GetProperty("name").GetString() == "left-pad");
        Assert.Equal("required", leftPad.GetProperty("scope").GetString());
    }

    [Fact]
    public void BuildCycloneDx_DeDupesAcrossManifests()
    {
        var deps = new List<DependencyItem>
        {
            new("npm", "left-pad", "1.3.0", "a/package.json", false),
            new("npm", "left-pad", "1.3.0", "b/package.json", false),
        };
        var json = SbomBuilder.BuildCycloneDx("r", deps, Ts);
        using var doc = JsonDocument.Parse(json);
        Assert.Single(doc.RootElement.GetProperty("components").EnumerateArray());
    }
}

public class VulnerabilityServiceTests
{
    [Theory]
    [InlineData("npm", "npm")]
    [InlineData("pip", "PyPI")]
    [InlineData("Cargo", "crates.io")]
    [InlineData("Composer", "Packagist")]
    [InlineData("Go", "Go")]
    [InlineData("Maven", "Maven")]
    public void OsvEcosystem_MapsKnown(string eco, string expected)
        => Assert.Equal(expected, VulnerabilityService.OsvEcosystem(eco));

    [Fact]
    public void OsvEcosystem_UnknownIsNull()
        => Assert.Null(VulnerabilityService.OsvEcosystem("Conan"));

    [Fact]
    public void BuildQueries_SkipsUnknownEcosystemsAndUnpinnedVersions()
    {
        var deps = new List<DependencyItem>
        {
            new("npm", "left-pad", "1.3.0", "package.json", false),   // ok
            new("npm", "wildcard", "*", "package.json", false),       // unpinned -> skip
            new("Conan", "boost", "1.0.0", "conanfile.txt", false),   // unknown eco -> skip
            new("pip", "requests", "^2.31.0", "requirements.txt", false), // ^ cleaned -> ok
            new("npm", "left-pad", "1.3.0", "other/package.json", false), // dup -> skip
        };

        var queries = VulnerabilityService.BuildQueries(deps);

        Assert.Equal(2, queries.Count);
        Assert.Contains(queries, q => q.Name == "left-pad" && q.Version == "1.3.0" && q.OsvEcosystem == "npm");
        Assert.Contains(queries, q => q.Name == "requests" && q.Version == "2.31.0" && q.OsvEcosystem == "PyPI");
    }

    [Fact]
    public void BuildRequestBody_ShapesOsvBatchPayload()
    {
        var queries = new List<VulnerabilityService.OsvQuery>
        {
            new("npm|left-pad|1.3.0", "npm", "left-pad", "1.3.0"),
        };
        var body = VulnerabilityService.BuildRequestBody(queries);
        using var doc = JsonDocument.Parse(body);
        var q0 = doc.RootElement.GetProperty("queries").EnumerateArray().First();
        Assert.Equal("1.3.0", q0.GetProperty("version").GetString());
        Assert.Equal("left-pad", q0.GetProperty("package").GetProperty("name").GetString());
        Assert.Equal("npm", q0.GetProperty("package").GetProperty("ecosystem").GetString());
    }

    [Fact]
    public void ParseResponse_MapsVulnsToDependencyKeysPositionally()
    {
        var queries = new List<VulnerabilityService.OsvQuery>
        {
            new("npm|left-pad|1.3.0", "npm", "left-pad", "1.3.0"),
            new("npm|safe|1.0.0", "npm", "safe", "1.0.0"),
        };
        // First query has two advisories, second has none.
        var json = """
        { "results": [
            { "vulns": [ { "id": "GHSA-xxxx" }, { "id": "CVE-2020-1" } ] },
            { }
        ] }
        """;

        var map = VulnerabilityService.ParseResponse(json, queries);

        Assert.True(map.ContainsKey("npm|left-pad|1.3.0"));
        Assert.Equal(2, map["npm|left-pad|1.3.0"].Count);
        Assert.Equal("https://osv.dev/vulnerability/GHSA-xxxx", map["npm|left-pad|1.3.0"][0].Url);
        Assert.False(map.ContainsKey("npm|safe|1.0.0"));
    }

    [Fact]
    public void ParseResponse_MalformedJson_ReturnsEmpty()
    {
        var queries = new List<VulnerabilityService.OsvQuery>();
        Assert.Empty(VulnerabilityService.ParseResponse("not json", queries));
    }
}

public class OutdatedServiceTests
{
    [Theory]
    [InlineData("npm", "left-pad", "https://registry.npmjs.org/left-pad/latest")]
    [InlineData("npm", "@scope/pkg", "https://registry.npmjs.org/@scope%2Fpkg/latest")]
    [InlineData("NuGet", "Newtonsoft.Json", "https://api.nuget.org/v3-flatcontainer/newtonsoft.json/index.json")]
    [InlineData("pip", "requests", "https://pypi.org/pypi/requests/json")]
    [InlineData("Cargo", "serde", "https://crates.io/api/v1/crates/serde")]
    [InlineData("RubyGems", "rails", "https://rubygems.org/api/v1/versions/rails/latest.json")]
    public void LatestVersionUrl_BuildsKnownEndpoints(string eco, string name, string expected)
        => Assert.Equal(expected, OutdatedService.LatestVersionUrl(eco, name));

    [Fact]
    public void LatestVersionUrl_Composer_RequiresVendorSlash()
    {
        Assert.Null(OutdatedService.LatestVersionUrl("Composer", "monolog"));
        Assert.Equal("https://repo.packagist.org/p2/monolog/monolog.json",
                     OutdatedService.LatestVersionUrl("Composer", "monolog/monolog"));
    }

    [Fact]
    public void LatestVersionUrl_Maven_RequiresGroupArtifact()
        => Assert.Null(OutdatedService.LatestVersionUrl("Maven", "guava"));

    [Fact]
    public void LatestVersionUrl_UnknownEcosystem_IsNull()
        => Assert.Null(OutdatedService.LatestVersionUrl("Conan", "boost"));

    [Fact]
    public void EscapeGoModule_EscapesUppercase()
        => Assert.Equal("github.com/!burnt!sushi/toml",
                        OutdatedService.EscapeGoModule("github.com/BurntSushi/toml"));

    [Theory]
    [InlineData("npm", "left-pad", "{\"version\":\"1.5.0\"}", "1.5.0")]
    [InlineData("pip", "requests", "{\"info\":{\"version\":\"2.3.1\"}}", "2.3.1")]
    [InlineData("RubyGems", "rails", "{\"version\":\"7.1.3\"}", "7.1.3")]
    [InlineData("Go", "x", "{\"Version\":\"v1.9.1\"}", "v1.9.1")]
    public void ParseLatest_FlatShapes(string eco, string name, string json, string expected)
        => Assert.Equal(expected, OutdatedService.ParseLatest(eco, name, json));

    [Fact]
    public void ParseLatest_NuGet_SkipsPrerelease_TakesLastStable()
        => Assert.Equal("1.1.0",
            OutdatedService.ParseLatest("NuGet", "x", "{\"versions\":[\"1.0.0\",\"1.1.0\",\"2.0.0-beta\"]}"));

    [Fact]
    public void ParseLatest_Cargo_PrefersMaxStable()
        => Assert.Equal("1.0.5",
            OutdatedService.ParseLatest("Cargo", "serde",
                "{\"crate\":{\"max_stable_version\":\"1.0.5\",\"newest_version\":\"1.1.0-rc\"}}"));

    [Fact]
    public void ParseLatest_Composer_TakesNewestNonDev()
        => Assert.Equal("3.0.0",
            OutdatedService.ParseLatest("Composer", "monolog/monolog",
                "{\"packages\":{\"monolog/monolog\":[{\"version\":\"dev-main\"},{\"version\":\"3.0.0\"},{\"version\":\"2.9.1\"}]}}"));

    [Fact]
    public void ParseLatest_Maven_ReadsLatestVersion()
        => Assert.Equal("33.0",
            OutdatedService.ParseLatest("Maven", "g:a", "{\"response\":{\"docs\":[{\"latestVersion\":\"33.0\"}]}}"));

    [Fact]
    public void ParseLatest_Malformed_ReturnsNull()
        => Assert.Null(OutdatedService.ParseLatest("npm", "x", "not json"));

    [Theory]
    [InlineData("1.2.0", "1.5.0", true)]
    [InlineData("^1.2.0", "1.5.0", true)]   // range operator stripped
    [InlineData("1.5.0", "1.5.0", false)]
    [InlineData("2.0.0", "1.9.9", false)]   // ahead of "latest"
    [InlineData("1.2", "1.2.0", false)]     // zero-padded equal
    [InlineData("*", "1.0.0", false)]       // current not comparable
    [InlineData("1.9.0", "1.10.0", true)]   // numeric, not lexical
    public void IsOutdated_ComparesNumericComponents(string current, string latest, bool expected)
        => Assert.Equal(expected, OutdatedService.IsOutdated(current, latest));
}
