using System.Text;
using System.Text.Json;

namespace MyPersonalGit.Data;

/// <summary>
/// Builds a CycloneDX 1.5 SBOM (Software Bill of Materials) from the dependencies
/// parsed out of a repository's manifests. Pure and deterministic apart from the
/// caller-supplied timestamp, so it is fully unit-testable without any I/O.
/// </summary>
public static class SbomBuilder
{
    /// <summary>
    /// Maps an ecosystem label used by <see cref="DependencyService"/> to a Package URL
    /// (purl) type. See https://github.com/package-url/purl-spec for the type names.
    /// </summary>
    internal static string? PurlType(string ecosystem) => ecosystem switch
    {
        "npm" => "npm",
        "NuGet" => "nuget",
        "pip" => "pypi",
        "Go" => "golang",
        "Cargo" => "cargo",
        "Composer" => "composer",
        "RubyGems" => "gem",
        "Maven" => "maven",
        _ => null
    };

    /// <summary>
    /// Builds a Package URL for a dependency, e.g. <c>pkg:npm/left-pad@1.3.0</c> or
    /// <c>pkg:maven/com.google.guava/guava@33.0</c>. Returns an empty string when the
    /// ecosystem has no known purl type.
    /// </summary>
    internal static string BuildPurl(string ecosystem, string name, string version)
    {
        var type = PurlType(ecosystem);
        if (type == null) return "";

        // Maven/Composer carry a "namespace/name" identity; purl wants them as a path
        // segment rather than url-encoded. Everything else is a flat name.
        string path;
        if (type == "maven" && name.Contains(':'))
            path = name.Replace(':', '/');
        else if (type == "composer" && name.Contains('/'))
            path = name; // already vendor/package
        else
            path = Uri.EscapeDataString(name);

        var purl = $"pkg:{type}/{path}";
        var v = CleanVersion(version);
        if (!string.IsNullOrEmpty(v)) purl += "@" + Uri.EscapeDataString(v);
        return purl;
    }

    /// <summary>
    /// Best-effort reduction of a declared version/range to a concrete-ish version for
    /// the purl, e.g. <c>^1.2.3</c> → <c>1.2.3</c>, <c>&gt;=2.0</c> → <c>2.0</c>. Returns
    /// empty when nothing version-like remains.
    /// </summary>
    internal static string CleanVersion(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var v = raw.Trim();
        // Strip a leading operator/prefix.
        v = v.TrimStart('^', '~', '=', '>', '<', ' ', 'v', 'V');
        // Take the first whitespace- or comma-separated token (e.g. ">=1.0, <2.0").
        var token = v.Split(new[] { ' ', ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
        v = token.Length > 0 ? token[0] : v;
        // Drop wildcards like 1.2.* → 1.2 is not meaningful; only keep if it looks versiony.
        if (v.Length == 0 || v == "*" || v.StartsWith("*")) return "";
        return v;
    }

    /// <summary>Builds a CycloneDX 1.5 JSON document for the given dependencies.</summary>
    /// <param name="repoName">Subject of the BOM (used for the metadata component).</param>
    /// <param name="deps">Parsed dependencies.</param>
    /// <param name="timestampUtc">Caller-supplied timestamp (kept out of this method so it stays pure/testable).</param>
    /// <param name="serialNumber">Optional RFC-4122 urn:uuid serial number.</param>
    public static string BuildCycloneDx(
        string repoName,
        IReadOnlyList<DependencyItem> deps,
        DateTime timestampUtc,
        string? serialNumber = null)
    {
        var buffer = new MemoryStream();
        using (var w = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartObject();
            w.WriteString("bomFormat", "CycloneDX");
            w.WriteString("specVersion", "1.5");
            if (!string.IsNullOrEmpty(serialNumber))
                w.WriteString("serialNumber", serialNumber);
            w.WriteNumber("version", 1);

            // metadata
            w.WriteStartObject("metadata");
            w.WriteString("timestamp", timestampUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            w.WriteStartArray("tools");
            w.WriteStartObject();
            w.WriteString("vendor", "MyPersonalGit");
            w.WriteString("name", "DependencyService");
            w.WriteEndObject();
            w.WriteEndArray();
            w.WriteStartObject("component");
            w.WriteString("type", "application");
            w.WriteString("name", repoName);
            w.WriteString("bom-ref", repoName);
            w.WriteEndObject();
            w.WriteEndObject(); // metadata

            // components
            w.WriteStartArray("components");
            // De-dupe across manifests: one component per (ecosystem, name, version).
            var seen = new HashSet<string>();
            foreach (var d in deps)
            {
                var key = $"{d.Ecosystem}|{d.Name}|{d.Version}";
                if (!seen.Add(key)) continue;

                w.WriteStartObject();
                w.WriteString("type", "library");
                var purl = BuildPurl(d.Ecosystem, d.Name, d.Version);
                w.WriteString("bom-ref", string.IsNullOrEmpty(purl) ? key : purl);
                w.WriteString("name", d.Name);
                if (!string.IsNullOrEmpty(d.Version))
                    w.WriteString("version", d.Version);
                w.WriteString("scope", d.IsDev ? "optional" : "required");
                if (!string.IsNullOrEmpty(purl))
                    w.WriteString("purl", purl);
                // Record the source manifest as a property for traceability.
                w.WriteStartArray("properties");
                w.WriteStartObject();
                w.WriteString("name", "mypersonalgit:manifest");
                w.WriteString("value", d.ManifestPath);
                w.WriteEndObject();
                w.WriteStartObject();
                w.WriteString("name", "mypersonalgit:ecosystem");
                w.WriteString("value", d.Ecosystem);
                w.WriteEndObject();
                w.WriteEndArray();
                w.WriteEndObject();
            }
            w.WriteEndArray(); // components

            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
