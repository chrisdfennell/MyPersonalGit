using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace MyPersonalGit.Data;

/// <summary>
/// Configuration for a language server binary.
/// </summary>
public record LspServerConfig(string Command, string Arguments, string[] FileExtensions);

/// <summary>
/// Represents a running language server session.
/// </summary>
public sealed class LspSession : IDisposable
{
    public string Key { get; init; } = "";
    public Process? Process { get; set; }
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public SemaphoreSlim WriteLock { get; } = new(1, 1);
    private bool _disposed;

    public void Touch() => LastActivity = DateTime.UtcNow;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        WriteLock.Dispose();
        if (Process is { HasExited: false } p)
        {
            try { p.Kill(entireProcessTree: true); } catch { }
        }
        Process?.Dispose();
    }
}

/// <summary>
/// Manages language server process lifecycles.
/// Spawns one language server per (repo, language, user) and reuses it across WebSocket reconnections.
/// </summary>
public sealed class LspProcessManager : IDisposable
{
    private static readonly Dictionary<string, LspServerConfig> Servers = new()
    {
        ["csharp"] = new("/usr/local/bin/omnisharp/OmniSharp", "--languageserver", new[] { ".cs", ".csproj" }),
        ["typescript"] = new("typescript-language-server", "--stdio", new[] { ".ts", ".tsx", ".js", ".jsx" }),
        ["python"] = new("pylsp", "", new[] { ".py" }),
    };

    private readonly ConcurrentDictionary<string, LspSession> _sessions = new();
    private readonly Timer _reapTimer;
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(10);

    public LspProcessManager()
    {
        _reapTimer = new Timer(ReapIdleSessions, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
    }

    /// <summary>
    /// Returns the set of supported language IDs.
    /// </summary>
    public static IReadOnlyCollection<string> SupportedLanguages => Servers.Keys;

    /// <summary>
    /// Check if a language ID has a configured server.
    /// </summary>
    public static bool IsSupported(string language) => Servers.ContainsKey(language);

    /// <summary>
    /// Get or start a language server session.
    /// </summary>
    public LspSession? GetOrStartSession(string repoName, string language, string userId, string workingDirectory)
    {
        if (!Servers.TryGetValue(language, out var config))
            return null;

        var key = $"{repoName}:{language}:{userId}";

        return _sessions.GetOrAdd(key, _ =>
        {
            var session = new LspSession { Key = key };
            session.Process = StartLanguageServer(config, workingDirectory);
            return session;
        });
    }

    /// <summary>
    /// Stop and remove a specific session.
    /// </summary>
    public void StopSession(string key)
    {
        if (_sessions.TryRemove(key, out var session))
        {
            session.Dispose();
        }
    }

    private static Process? StartLanguageServer(LspServerConfig config, string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = config.Command,
            Arguments = config.Arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        try
        {
            return Process.Start(psi);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LSP] Failed to start {config.Command}: {ex.Message}");
            return null;
        }
    }

    private void ReapIdleSessions(object? state)
    {
        var cutoff = DateTime.UtcNow - IdleTimeout;
        foreach (var kvp in _sessions)
        {
            if (kvp.Value.LastActivity < cutoff || kvp.Value.Process is { HasExited: true })
            {
                StopSession(kvp.Key);
            }
        }
    }

    public void Dispose()
    {
        _reapTimer.Dispose();
        foreach (var kvp in _sessions)
        {
            kvp.Value.Dispose();
        }
        _sessions.Clear();
    }
}
