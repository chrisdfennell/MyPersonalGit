using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using LibGit2Sharp;

namespace MyPersonalGit.Data;

/// <summary>
/// Represents a running debug adapter session.
/// </summary>
public sealed class DapSession : IDisposable
{
    public string Key { get; init; } = "";
    public Process? Process { get; set; }
    public string? WorkTree { get; set; }
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

        if (!string.IsNullOrEmpty(WorkTree) && Directory.Exists(WorkTree))
        {
            try { Directory.Delete(WorkTree, recursive: true); }
            catch (Exception ex) { Console.WriteLine($"[DAP] Failed to clean up worktree {WorkTree}: {ex.Message}"); }
        }
    }
}

/// <summary>
/// Manages debug adapter process lifecycles.
/// Currently supports Python via debugpy adapter (stdin/stdout DAP).
/// </summary>
public sealed class DapSessionManager : IDisposable
{
    private static readonly Dictionary<string, (string Command, string Arguments)> Adapters = new()
    {
        ["python"] = ("python3", "-m debugpy.adapter"),
    };

    private readonly ConcurrentDictionary<string, DapSession> _sessions = new();
    private readonly Timer _reapTimer;
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(10);

    public DapSessionManager()
    {
        _reapTimer = new Timer(ReapIdleSessions, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
    }

    public static bool IsSupported(string language) => Adapters.ContainsKey(language);

    public DapSession? GetOrStartSession(string repoName, string language, string userId, string repoPath, string branch)
    {
        if (!Adapters.TryGetValue(language, out var adapter))
            return null;

        var key = $"{repoName}:dap:{language}:{userId}";

        return _sessions.GetOrAdd(key, _ =>
        {
            var workTree = CheckoutToTempDirectory(repoPath, branch);
            if (workTree == null)
            {
                Console.WriteLine($"[DAP] Failed to checkout {repoName}/{branch}");
                return new DapSession { Key = key };
            }

            var session = new DapSession { Key = key, WorkTree = workTree };
            session.Process = StartAdapter(adapter.Command, adapter.Arguments, workTree);
            return session;
        });
    }

    public void StopSession(string key)
    {
        if (_sessions.TryRemove(key, out var session))
            session.Dispose();
    }

    private static string? CheckoutToTempDirectory(string repoPath, string branch)
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "dap-workdir", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            using var repo = new Repository(repoPath);
            var targetBranch = repo.Branches[branch]
                ?? repo.Branches["main"]
                ?? repo.Branches["master"]
                ?? repo.Head;

            if (targetBranch?.Tip == null)
            {
                Directory.Delete(tempDir, recursive: true);
                return null;
            }

            WriteTree(repo, targetBranch.Tip.Tree, tempDir);
            Console.WriteLine($"[DAP] Checked out {branch} ({targetBranch.Tip.Id.Sha[..7]}) to {tempDir}");
            return tempDir;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DAP] Checkout failed: {ex.Message}");
            return null;
        }
    }

    private static void WriteTree(Repository repo, Tree tree, string basePath)
    {
        foreach (var entry in tree)
        {
            var fullPath = Path.Combine(basePath, entry.Name);
            if (entry.TargetType == TreeEntryTargetType.Tree)
            {
                Directory.CreateDirectory(fullPath);
                WriteTree(repo, (Tree)entry.Target, fullPath);
            }
            else if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                var blob = (Blob)entry.Target;
                using var contentStream = blob.GetContentStream();
                using var fileStream = File.Create(fullPath);
                contentStream.CopyTo(fileStream);
            }
        }
    }

    private static Process? StartAdapter(string command, string arguments, string workDir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            WorkingDirectory = workDir,
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
            var process = Process.Start(psi);
            Console.WriteLine($"[DAP] Started {command} {arguments} in {workDir} (PID: {process?.Id})");
            return process;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DAP] Failed to start {command}: {ex.Message}");
            return null;
        }
    }

    private void ReapIdleSessions(object? state)
    {
        var cutoff = DateTime.UtcNow - IdleTimeout;
        foreach (var kvp in _sessions)
        {
            if (kvp.Value.LastActivity < cutoff || kvp.Value.Process is { HasExited: true })
                StopSession(kvp.Key);
        }
    }

    public void Dispose()
    {
        _reapTimer.Dispose();
        foreach (var kvp in _sessions) kvp.Value.Dispose();
        _sessions.Clear();
    }
}
