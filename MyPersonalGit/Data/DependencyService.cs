using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LibGit2Sharp;

namespace MyPersonalGit.Data;

/// <summary>One declared dependency parsed from a manifest file in a repository.</summary>
public record DependencyItem(string Ecosystem, string Name, string Version, string ManifestPath, bool IsDev);

public interface IDependencyService
{
    /// <summary>
    /// Reads known package manifests from the repo's git tree (default branch unless
    /// <paramref name="branch"/> is given) and returns the declared dependencies.
    /// </summary>
    Task<IReadOnlyList<DependencyItem>> GetDependenciesAsync(string repoName, string? branch = null);

    /// <summary>Returns the HEAD commit sha (used as a scan cache key), or null if unavailable.</summary>
    Task<string?> GetHeadCommitShaAsync(string repoName);
}

public class DependencyService : IDependencyService
{
    private readonly IConfiguration _config;
    private readonly IAdminService _adminService;
    private readonly ILogger<DependencyService> _logger;

    public DependencyService(IConfiguration config, IAdminService adminService, ILogger<DependencyService> logger)
    {
        _config = config;
        _adminService = adminService;
        _logger = logger;
    }

    private async Task<string> ResolveRepoPathAsync(string repoName)
    {
        var settings = await _adminService.GetSystemSettingsAsync();
        var projectRoot = !string.IsNullOrEmpty(settings.ProjectRoot)
            ? settings.ProjectRoot
            : _config["Git:ProjectRoot"] ?? "/repos";
        return Path.Combine(projectRoot, $"{repoName}.git");
    }

    public async Task<string?> GetHeadCommitShaAsync(string repoName)
    {
        try
        {
            var repoPath = await ResolveRepoPathAsync(repoName);
            if (!Repository.IsValid(repoPath)) return null;
            using var repo = new Repository(repoPath);
            return repo.Head?.Tip?.Sha;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read HEAD sha for {Repo}", repoName);
            return null;
        }
    }

    public async Task<IReadOnlyList<DependencyItem>> GetDependenciesAsync(string repoName, string? branch = null)
    {
        var repoPath = await ResolveRepoPathAsync(repoName);

        var result = new List<DependencyItem>();
        try
        {
            if (!Repository.IsValid(repoPath)) return result;
            using var repo = new Repository(repoPath);
            var tip = (!string.IsNullOrEmpty(branch) ? repo.Branches[branch]?.Tip : null) ?? repo.Head?.Tip;
            if (tip == null) return result;
            CollectManifests(tip.Tree, "", result, depth: 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read dependencies from {Repo}", repoName);
        }

        // De-dupe within a manifest and present grouped by manifest then name.
        return result
            .GroupBy(d => (d.Ecosystem, d.Name, d.ManifestPath, d.IsDev))
            .Select(g => g.First())
            .OrderBy(d => d.ManifestPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // Cap recursion so a pathological tree can't spin; manifests live near the top anyway.
    private void CollectManifests(Tree tree, string prefix, List<DependencyItem> result, int depth)
    {
        if (depth > 8) return;
        foreach (var entry in tree)
        {
            if (entry.TargetType == TreeEntryTargetType.Tree)
            {
                if (IsIgnoredDir(entry.Name)) continue;
                CollectManifests((Tree)entry.Target, prefix + entry.Name + "/", result, depth + 1);
            }
            else if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                var parser = MatchParser(entry.Name);
                if (parser == null) continue;
                try
                {
                    var text = ((Blob)entry.Target).GetContentText();
                    result.AddRange(parser(text, prefix + entry.Name));
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed parsing manifest {Path}", prefix + entry.Name);
                }
            }
        }
    }

    private static bool IsIgnoredDir(string name) => name is
        "node_modules" or "vendor" or "packages" or "bin" or "obj" or
        "dist" or "build" or "target" or ".venv" or "venv" or ".git";

    internal static Func<string, string, IEnumerable<DependencyItem>>? MatchParser(string fileName)
    {
        if (fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase)) return ParsePackageJson;
        if (fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) return ParseCsproj;
        if (fileName.Equals("requirements.txt", StringComparison.OrdinalIgnoreCase)) return ParseRequirements;
        if (fileName.Equals("go.mod", StringComparison.OrdinalIgnoreCase)) return ParseGoMod;
        if (fileName.Equals("Cargo.toml", StringComparison.OrdinalIgnoreCase)) return ParseCargo;
        if (fileName.Equals("composer.json", StringComparison.OrdinalIgnoreCase)) return ParseComposer;
        if (fileName.Equals("Gemfile", StringComparison.OrdinalIgnoreCase)) return ParseGemfile;
        if (fileName.Equals("pom.xml", StringComparison.OrdinalIgnoreCase)) return ParsePomXml;
        return null;
    }

    internal static IEnumerable<DependencyItem> ParsePackageJson(string text, string path)
    {
        var list = new List<DependencyItem>();
        try
        {
            using var doc = JsonDocument.Parse(text);
            foreach (var (section, isDev) in new[] { ("dependencies", false), ("devDependencies", true), ("peerDependencies", false) })
            {
                if (doc.RootElement.TryGetProperty(section, out var deps) && deps.ValueKind == JsonValueKind.Object)
                    foreach (var p in deps.EnumerateObject())
                        list.Add(new DependencyItem("npm", p.Name, p.Value.GetString() ?? "", path, isDev));
            }
        }
        catch { /* malformed JSON — skip */ }
        return list;
    }

    internal static IEnumerable<DependencyItem> ParseCsproj(string text, string path)
    {
        var list = new List<DependencyItem>();
        try
        {
            var doc = XDocument.Parse(text);
            foreach (var pr in doc.Descendants().Where(e => e.Name.LocalName == "PackageReference"))
            {
                var name = pr.Attribute("Include")?.Value ?? pr.Attribute("Update")?.Value;
                if (string.IsNullOrWhiteSpace(name)) continue;
                var version = pr.Attribute("Version")?.Value
                    ?? pr.Elements().FirstOrDefault(e => e.Name.LocalName == "Version")?.Value
                    ?? "";
                list.Add(new DependencyItem("NuGet", name, version, path, false));
            }
        }
        catch { /* malformed XML — skip */ }
        return list;
    }

    internal static IEnumerable<DependencyItem> ParseRequirements(string text, string path)
    {
        var list = new List<DependencyItem>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("-")) continue; // comments, -r/-e flags
            var comment = line.IndexOf(" #", StringComparison.Ordinal);
            if (comment >= 0) line = line[..comment].Trim();
            var m = Regex.Match(line, @"^([A-Za-z0-9_.\-]+)\s*(?:\[[^\]]*\])?\s*(?:(===|==|>=|<=|~=|!=|<|>)\s*(.+))?$");
            if (!m.Success) continue;
            var version = m.Groups[3].Success ? (m.Groups[2].Value + m.Groups[3].Value).Trim() : "";
            list.Add(new DependencyItem("pip", m.Groups[1].Value, version, path, false));
        }
        return list;
    }

    internal static IEnumerable<DependencyItem> ParseGoMod(string text, string path)
    {
        var list = new List<DependencyItem>();
        var inBlock = false;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("require (")) { inBlock = true; continue; }
            if (inBlock && line == ")") { inBlock = false; continue; }
            string? dep = inBlock ? line : (line.StartsWith("require ") ? line["require ".Length..].Trim() : null);
            if (string.IsNullOrEmpty(dep) || dep.StartsWith("//")) continue;
            var c = dep.IndexOf("//", StringComparison.Ordinal);
            if (c >= 0) dep = dep[..c].Trim();
            var parts = dep.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) list.Add(new DependencyItem("Go", parts[0], parts[1], path, false));
        }
        return list;
    }

    internal static IEnumerable<DependencyItem> ParseCargo(string text, string path)
    {
        var list = new List<DependencyItem>();
        string? section = null;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            if (line.StartsWith("[") && line.EndsWith("]")) { section = line.Trim('[', ']').Trim(); continue; }
            if (section is "dependencies" or "dev-dependencies" or "build-dependencies")
            {
                var eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var name = line[..eq].Trim();
                var rhs = line[(eq + 1)..].Trim();
                string version;
                if (rhs.StartsWith("{"))
                {
                    var vm = Regex.Match(rhs, "version\\s*=\\s*\"([^\"]*)\"");
                    version = vm.Success ? vm.Groups[1].Value : "";
                }
                else version = rhs.Trim().Trim('"');
                list.Add(new DependencyItem("Cargo", name, version, path, section == "dev-dependencies"));
            }
        }
        return list;
    }

    internal static IEnumerable<DependencyItem> ParseComposer(string text, string path)
    {
        var list = new List<DependencyItem>();
        try
        {
            using var doc = JsonDocument.Parse(text);
            foreach (var (section, isDev) in new[] { ("require", false), ("require-dev", true) })
            {
                if (!doc.RootElement.TryGetProperty(section, out var deps) || deps.ValueKind != JsonValueKind.Object) continue;
                foreach (var p in deps.EnumerateObject())
                {
                    // Skip platform requirements (php, ext-*, lib-*).
                    if (p.Name.Equals("php", StringComparison.OrdinalIgnoreCase) ||
                        p.Name.StartsWith("ext-", StringComparison.OrdinalIgnoreCase) ||
                        p.Name.StartsWith("lib-", StringComparison.OrdinalIgnoreCase)) continue;
                    list.Add(new DependencyItem("Composer", p.Name, p.Value.GetString() ?? "", path, isDev));
                }
            }
        }
        catch { /* malformed JSON — skip */ }
        return list;
    }

    internal static IEnumerable<DependencyItem> ParseGemfile(string text, string path)
    {
        var list = new List<DependencyItem>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (!line.StartsWith("gem ")) continue;
            var m = Regex.Match(line, "gem\\s+['\"]([^'\"]+)['\"]\\s*(?:,\\s*['\"]([^'\"]+)['\"])?");
            if (!m.Success) continue;
            list.Add(new DependencyItem("RubyGems", m.Groups[1].Value, m.Groups[2].Success ? m.Groups[2].Value : "", path, false));
        }
        return list;
    }

    internal static IEnumerable<DependencyItem> ParsePomXml(string text, string path)
    {
        var list = new List<DependencyItem>();
        try
        {
            var doc = XDocument.Parse(text);
            foreach (var dep in doc.Descendants().Where(e => e.Name.LocalName == "dependency"))
            {
                var gid = dep.Elements().FirstOrDefault(e => e.Name.LocalName == "groupId")?.Value;
                var aid = dep.Elements().FirstOrDefault(e => e.Name.LocalName == "artifactId")?.Value;
                var ver = dep.Elements().FirstOrDefault(e => e.Name.LocalName == "version")?.Value ?? "";
                if (string.IsNullOrWhiteSpace(aid)) continue;
                var name = string.IsNullOrWhiteSpace(gid) ? aid : $"{gid}:{aid}";
                list.Add(new DependencyItem("Maven", name, ver, path, false));
            }
        }
        catch { /* malformed XML — skip */ }
        return list;
    }
}
