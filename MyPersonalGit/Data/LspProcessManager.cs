using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using LibGit2Sharp;

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

        // Clean up the temporary working directory
        if (!string.IsNullOrEmpty(WorkTree) && Directory.Exists(WorkTree))
        {
            try { Directory.Delete(WorkTree, recursive: true); }
            catch (Exception ex) { Console.WriteLine($"[LSP] Failed to clean up worktree {WorkTree}: {ex.Message}"); }
        }
    }
}

/// <summary>
/// Manages language server process lifecycles.
/// Spawns one language server per (repo, language, user) and reuses it across WebSocket reconnections.
/// Because repos are bare, each session checks out the branch into a temporary working directory.
/// </summary>
public sealed class LspProcessManager : IDisposable
{
    private static readonly Dictionary<string, LspServerConfig> Servers = new()
    {
        ["csharp"] = new("/usr/local/bin/omnisharp/OmniSharp", "--languageserver", new[] { ".cs", ".csproj" }),
        ["typescript"] = new("typescript-language-server", "--stdio", new[] { ".ts", ".tsx", ".js", ".jsx" }),
        ["python"] = new("pylsp", "", new[] { ".py" }),
        ["go"] = new("gopls", "serve", new[] { ".go" }),
        ["rust"] = new("rust-analyzer", "", new[] { ".rs" }),
        ["html"] = new("vscode-html-language-server", "--stdio", new[] { ".html", ".htm" }),
        ["css"] = new("vscode-css-language-server", "--stdio", new[] { ".css", ".scss", ".less" }),
        ["json"] = new("vscode-json-language-server", "--stdio", new[] { ".json" }),
        ["yaml"] = new("yaml-language-server", "--stdio", new[] { ".yml", ".yaml" }),
        ["bash"] = new("bash-language-server", "start", new[] { ".sh", ".bash" }),
        ["dockerfile"] = new("docker-langserver", "--stdio", new[] { ".dockerfile" }),
        ["markdown"] = new("marksman", "server", new[] { ".md" }),
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
    /// Checks out the specified branch from the bare repo into a temporary directory.
    /// </summary>
    public LspSession? GetOrStartSession(string repoName, string language, string userId, string repoPath, string branch)
    {
        if (!Servers.TryGetValue(language, out var config))
            return null;

        var key = $"{repoName}:{language}:{userId}";

        return _sessions.GetOrAdd(key, _ =>
        {
            var workTree = CheckoutToTempDirectory(repoPath, branch);
            if (workTree == null)
            {
                Console.WriteLine($"[LSP] Failed to checkout {repoName}/{branch} to temp directory");
                return new LspSession { Key = key };
            }

            var session = new LspSession { Key = key, WorkTree = workTree };
            session.Process = StartLanguageServer(config, workTree);
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

    /// <summary>
    /// Checkout a branch from a bare repo into a temporary working directory.
    /// </summary>
    private static string? CheckoutToTempDirectory(string repoPath, string branch)
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "lsp-workdir", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            using var repo = new Repository(repoPath);

            // Resolve the branch
            var targetBranch = repo.Branches[branch]
                ?? repo.Branches["main"]
                ?? repo.Branches["master"]
                ?? repo.Head;

            if (targetBranch?.Tip == null)
            {
                Directory.Delete(tempDir, recursive: true);
                return null;
            }

            // Recursively write all blobs to the temp directory
            WriteTree(repo, targetBranch.Tip.Tree, tempDir);

            Console.WriteLine($"[LSP] Checked out {branch} ({targetBranch.Tip.Id.Sha[..7]}) to {tempDir}");
            return tempDir;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LSP] Checkout failed: {ex.Message}");
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
            var process = Process.Start(psi);
            Console.WriteLine($"[LSP] Started {config.Command} in {workingDirectory} (PID: {process?.Id})");
            return process;
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
