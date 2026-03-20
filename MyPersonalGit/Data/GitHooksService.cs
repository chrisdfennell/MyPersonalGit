using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MyPersonalGit.Data;

public interface IGitHooksService
{
    Task<Dictionary<string, string>> GetHooksAsync(string repoPath);
    Task<bool> SaveHookAsync(string repoPath, string hookName, string content);
    Task<bool> DeleteHookAsync(string repoPath, string hookName);
}

public class GitHooksService : IGitHooksService
{
    private static readonly string[] SupportedHooks = { "pre-receive", "update", "post-receive", "post-update", "pre-push" };
    private readonly ILogger<GitHooksService> _logger;

    public GitHooksService(ILogger<GitHooksService> logger)
    {
        _logger = logger;
    }

    public Task<Dictionary<string, string>> GetHooksAsync(string repoPath)
    {
        var hooks = new Dictionary<string, string>();

        foreach (var hookName in SupportedHooks)
        {
            var content = string.Empty;

            // Check custom_hooks first (takes priority), then hooks
            var customPath = Path.Combine(repoPath, "custom_hooks", hookName);
            var standardPath = Path.Combine(repoPath, "hooks", hookName);

            if (File.Exists(customPath))
            {
                content = File.ReadAllText(customPath);
            }
            else if (File.Exists(standardPath))
            {
                content = File.ReadAllText(standardPath);
            }

            hooks[hookName] = content;
        }

        return Task.FromResult(hooks);
    }

    public Task<bool> SaveHookAsync(string repoPath, string hookName, string content)
    {
        if (!SupportedHooks.Contains(hookName))
            return Task.FromResult(false);

        try
        {
            var customHooksDir = Path.Combine(repoPath, "custom_hooks");
            Directory.CreateDirectory(customHooksDir);

            var hookPath = Path.Combine(customHooksDir, hookName);
            File.WriteAllText(hookPath, content);

            // Set executable permission on Linux/macOS
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    Process.Start("chmod", $"+x \"{hookPath}\"")?.WaitForExit(5000);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set executable permission on hook {HookName}", hookName);
                }
            }

            _logger.LogInformation("Saved hook {HookName} for repo at {RepoPath}", hookName, repoPath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save hook {HookName} for repo at {RepoPath}", hookName, repoPath);
            return Task.FromResult(false);
        }
    }

    public Task<bool> DeleteHookAsync(string repoPath, string hookName)
    {
        if (!SupportedHooks.Contains(hookName))
            return Task.FromResult(false);

        try
        {
            var customPath = Path.Combine(repoPath, "custom_hooks", hookName);
            if (File.Exists(customPath))
            {
                File.Delete(customPath);
                _logger.LogInformation("Deleted hook {HookName} for repo at {RepoPath}", hookName, repoPath);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete hook {HookName} for repo at {RepoPath}", hookName, repoPath);
            return Task.FromResult(false);
        }
    }
}
