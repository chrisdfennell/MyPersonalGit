namespace MyPersonalGit.Services;

/// <summary>
/// Safe composition of filesystem paths from user-supplied segments (package names,
/// versions, filenames, channels, …). Rejects path traversal and guarantees the result
/// stays inside the intended base directory. Returns null on anything suspicious so callers
/// can fail closed (e.g. 400/404) instead of touching arbitrary files.
/// </summary>
public static class SafePath
{
    /// <summary>
    /// Combine <paramref name="segments"/> under <paramref name="baseDir"/>. Returns the
    /// absolute path only if every segment is a single safe path component AND the resolved
    /// path is inside <paramref name="baseDir"/>; otherwise null.
    /// </summary>
    public static string? CombineUnder(string baseDir, params string[] segments)
    {
        if (string.IsNullOrEmpty(baseDir))
            return null;

        foreach (var s in segments)
        {
            if (string.IsNullOrEmpty(s)) return null;
            if (s.Contains("..", StringComparison.Ordinal)) return null;   // traversal
            if (s.IndexOfAny(new[] { '/', '\\' }) >= 0) return null;        // sub-paths
            if (s.Contains(':')) return null;                              // drive letters / NTFS ADS
            if (Path.IsPathRooted(s)) return null;                         // absolute segment
            if (s.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return null;
        }

        var baseFull = Path.GetFullPath(baseDir);

        var all = new string[segments.Length + 1];
        all[0] = baseFull;
        Array.Copy(segments, 0, all, 1, segments.Length);
        var full = Path.GetFullPath(Path.Combine(all));

        // Canonicalization backstop: the resolved path must be the base dir or live beneath it.
        var prefix = baseFull.EndsWith(Path.DirectorySeparatorChar)
            ? baseFull
            : baseFull + Path.DirectorySeparatorChar;
        if (full != baseFull && !full.StartsWith(prefix, StringComparison.Ordinal))
            return null;

        return full;
    }
}
