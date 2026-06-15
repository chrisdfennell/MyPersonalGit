using System.Collections.Concurrent;
using System.Text.Json;

namespace MyPersonalGit.Data;

/// <summary>Latest-version status for a declared dependency.</summary>
public record OutdatedInfo(string Current, string Latest, bool IsOutdated);

public interface IOutdatedService
{
    /// <summary>
    /// Looks up the latest published version of each dependency from its ecosystem's
    /// public registry and reports which declared versions are behind. Best-effort and
    /// fails open: any per-package error is swallowed, and only entries we could both
    /// resolve and confidently compare are returned (keyed by
    /// <see cref="VulnerabilityService.Key"/>).
    /// </summary>
    Task<IReadOnlyDictionary<string, OutdatedInfo>> CheckAsync(
        IReadOnlyList<DependencyItem> deps, CancellationToken ct = default);
}

/// <summary>
/// Outdated-dependency checks against each ecosystem's public registry (npm, NuGet,
/// PyPI, crates.io, Packagist, RubyGems, Go module proxy, Maven Central). URL building,
/// response parsing and version comparison are pure static helpers so they unit-test
/// without the network; <see cref="CheckAsync"/> wires them with a bounded-concurrency
/// HTTP fan-out.
/// </summary>
public class OutdatedService : IOutdatedService
{
    // Guard rails for pathological repos: cap distinct packages queried and concurrency.
    private const int MaxPackages = 300;
    private const int MaxConcurrency = 8;

    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<OutdatedService> _logger;

    public OutdatedService(IHttpClientFactory httpFactory, ILogger<OutdatedService> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, OutdatedInfo>> CheckAsync(
        IReadOnlyList<DependencyItem> deps, CancellationToken ct = default)
    {
        var result = new Dictionary<string, OutdatedInfo>();

        // One lookup per distinct (ecosystem, name); several manifests/versions reuse it.
        var packages = deps
            .Where(d => LatestVersionUrl(d.Ecosystem, d.Name) != null)
            .GroupBy(d => (d.Ecosystem, d.Name))
            .Select(g => g.Key)
            .Take(MaxPackages)
            .ToList();
        if (packages.Count == 0) return result;

        if (deps.Select(d => (d.Ecosystem, d.Name)).Distinct().Count() > MaxPackages)
            _logger.LogInformation("Outdated check capped at {Max} packages", MaxPackages);

        var latestByPackage = new ConcurrentDictionary<(string, string), string>();
        var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(10);
        // crates.io (and good manners elsewhere) require a User-Agent.
        if (!http.DefaultRequestHeaders.Contains("User-Agent"))
            http.DefaultRequestHeaders.Add("User-Agent", "MyPersonalGit-DependencyCheck");

        using var gate = new SemaphoreSlim(MaxConcurrency);
        var tasks = packages.Select(async pkg =>
        {
            await gate.WaitAsync(ct);
            try
            {
                var latest = await FetchLatestAsync(http, pkg.Ecosystem, pkg.Name, ct);
                if (latest != null) latestByPackage[pkg] = latest;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Latest-version lookup failed for {Eco}/{Name}", pkg.Ecosystem, pkg.Name);
            }
            finally
            {
                gate.Release();
            }
        });
        await Task.WhenAll(tasks);

        foreach (var d in deps)
        {
            if (!latestByPackage.TryGetValue((d.Ecosystem, d.Name), out var latest)) continue;
            if (!IsOutdated(d.Version, latest)) continue;
            result[VulnerabilityService.Key(d)] = new OutdatedInfo(d.Version, latest, true);
        }
        return result;
    }

    private async Task<string?> FetchLatestAsync(HttpClient http, string eco, string name, CancellationToken ct)
    {
        var url = LatestVersionUrl(eco, name);
        if (url == null) return null;
        using var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;
        var json = await resp.Content.ReadAsStringAsync(ct);
        return ParseLatest(eco, name, json);
    }

    // ---- Pure helpers (unit-tested) ----

    /// <summary>Builds the registry "latest version" lookup URL, or null for unknown ecosystems.</summary>
    internal static string? LatestVersionUrl(string ecosystem, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        switch (ecosystem)
        {
            case "npm":
                return $"https://registry.npmjs.org/{name.Replace("/", "%2F")}/latest";
            case "NuGet":
                return $"https://api.nuget.org/v3-flatcontainer/{name.ToLowerInvariant()}/index.json";
            case "pip":
                return $"https://pypi.org/pypi/{Uri.EscapeDataString(name)}/json";
            case "Cargo":
                return $"https://crates.io/api/v1/crates/{Uri.EscapeDataString(name)}";
            case "Composer":
                return name.Contains('/') ? $"https://repo.packagist.org/p2/{name}.json" : null;
            case "RubyGems":
                return $"https://rubygems.org/api/v1/versions/{Uri.EscapeDataString(name)}/latest.json";
            case "Go":
                return $"https://proxy.golang.org/{EscapeGoModule(name)}/@latest";
            case "Maven":
                if (!name.Contains(':')) return null;
                var parts = name.Split(':', 2);
                return $"https://search.maven.org/solrsearch/select?q=g:%22{Uri.EscapeDataString(parts[0])}%22+AND+a:%22{Uri.EscapeDataString(parts[1])}%22&rows=1&wt=json";
            default:
                return null;
        }
    }

    /// <summary>
    /// The Go module proxy lowercases path elements and escapes each uppercase letter
    /// as "!" + the lowercase letter (e.g. <c>github.com/BurntSushi/toml</c> →
    /// <c>github.com/!burnt!sushi/toml</c>).
    /// </summary>
    internal static string EscapeGoModule(string module)
    {
        var sb = new System.Text.StringBuilder(module.Length + 8);
        foreach (var ch in module)
        {
            if (char.IsUpper(ch)) { sb.Append('!').Append(char.ToLowerInvariant(ch)); }
            else sb.Append(ch);
        }
        return sb.ToString();
    }

    /// <summary>Extracts the latest version from an ecosystem registry response.</summary>
    internal static string? ParseLatest(string ecosystem, string name, string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            switch (ecosystem)
            {
                case "npm": // /latest manifest
                    return root.TryGetProperty("version", out var nv) ? nv.GetString() : null;

                case "NuGet": // flat-container index: ascending versions, take last stable
                    if (!root.TryGetProperty("versions", out var versions) || versions.ValueKind != JsonValueKind.Array)
                        return null;
                    string? lastStable = null, lastAny = null;
                    foreach (var v in versions.EnumerateArray())
                    {
                        var s = v.GetString();
                        if (s == null) continue;
                        lastAny = s;
                        if (!s.Contains('-')) lastStable = s; // skip prerelease
                    }
                    return lastStable ?? lastAny;

                case "pip":
                    return root.TryGetProperty("info", out var info) && info.TryGetProperty("version", out var pv)
                        ? pv.GetString() : null;

                case "Cargo":
                    if (!root.TryGetProperty("crate", out var crate)) return null;
                    return (crate.TryGetProperty("max_stable_version", out var ms) ? ms.GetString() : null)
                        ?? (crate.TryGetProperty("newest_version", out var nw) ? nw.GetString() : null);

                case "Composer": // p2: { packages: { "vendor/pkg": [ { version }, ... ] } }, newest first
                    if (!root.TryGetProperty("packages", out var pkgs) ||
                        !pkgs.TryGetProperty(name, out var releases) || releases.ValueKind != JsonValueKind.Array)
                        return null;
                    foreach (var rel in releases.EnumerateArray())
                    {
                        var s = rel.TryGetProperty("version", out var cv) ? cv.GetString() : null;
                        if (string.IsNullOrEmpty(s)) continue;
                        if (s.Contains("dev", StringComparison.OrdinalIgnoreCase)) continue; // skip dev branches
                        return s;
                    }
                    return null;

                case "RubyGems":
                    return root.TryGetProperty("version", out var rv) ? rv.GetString() : null;

                case "Go":
                    return root.TryGetProperty("Version", out var gv) ? gv.GetString() : null;

                case "Maven":
                    if (!root.TryGetProperty("response", out var mr) ||
                        !mr.TryGetProperty("docs", out var docs) || docs.ValueKind != JsonValueKind.Array)
                        return null;
                    var first = docs.EnumerateArray().FirstOrDefault();
                    return first.ValueKind == JsonValueKind.Object && first.TryGetProperty("latestVersion", out var lv)
                        ? lv.GetString() : null;

                default:
                    return null;
            }
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>True when <paramref name="current"/> is a strictly older version than <paramref name="latest"/>.</summary>
    internal static bool IsOutdated(string current, string latest)
    {
        var c = ParseVersion(current);
        var l = ParseVersion(latest);
        if (c == null || l == null) return false;
        return CompareVersions(c, l) < 0;
    }

    // Reduces a version to its numeric release components, dropping any range operator
    // and pre-release/build metadata. Returns null if nothing comparable remains.
    internal static int[]? ParseVersion(string raw)
    {
        var v = SbomBuilder.CleanVersion(raw);
        if (string.IsNullOrEmpty(v)) return null;
        var cut = v.IndexOfAny(new[] { '-', '+' });
        if (cut >= 0) v = v[..cut];
        var parts = v.Split('.');
        var nums = new List<int>();
        foreach (var p in parts)
        {
            if (int.TryParse(p, out var n)) nums.Add(n);
            else return null; // wildcard / non-numeric -> can't compare confidently
        }
        return nums.Count > 0 ? nums.ToArray() : null;
    }

    internal static int CompareVersions(int[] a, int[] b)
    {
        var len = Math.Max(a.Length, b.Length);
        for (var i = 0; i < len; i++)
        {
            var ai = i < a.Length ? a[i] : 0;
            var bi = i < b.Length ? b[i] : 0;
            if (ai != bi) return ai.CompareTo(bi);
        }
        return 0;
    }
}
