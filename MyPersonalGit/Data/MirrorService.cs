using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IMirrorService
{
    Task<List<RepositoryMirror>> GetMirrorsAsync(string repoName);
    Task<RepositoryMirror?> GetMirrorAsync(int id);
    Task<RepositoryMirror> AddMirrorAsync(string repoName, string remoteUrl, MirrorDirection direction, int intervalMinutes, string? authToken);
    Task<bool> DeleteMirrorAsync(int id);
    Task<bool> ToggleMirrorAsync(int id);
    Task SyncMirrorAsync(RepositoryMirror mirror, string projectRoot);
    Task<List<RepositoryMirror>> GetDueMirrorsAsync();
}

public class MirrorService : IMirrorService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<MirrorService> _logger;

    public MirrorService(IDbContextFactory<AppDbContext> dbFactory, ILogger<MirrorService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<RepositoryMirror>> GetMirrorsAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.RepositoryMirrors
            .Where(m => m.RepoName == repoName)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<RepositoryMirror?> GetMirrorAsync(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.RepositoryMirrors.FindAsync(id);
    }

    public async Task<RepositoryMirror> AddMirrorAsync(string repoName, string remoteUrl, MirrorDirection direction, int intervalMinutes, string? authToken)
    {
        using var db = _dbFactory.CreateDbContext();
        var mirror = new RepositoryMirror
        {
            RepoName = repoName,
            RemoteUrl = remoteUrl,
            Direction = direction,
            IntervalMinutes = intervalMinutes,
            AuthToken = authToken,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        db.RepositoryMirrors.Add(mirror);
        await db.SaveChangesAsync();
        _logger.LogInformation("Mirror added for {Repo}: {Direction} from {Url}", repoName, direction, remoteUrl);
        return mirror;
    }

    public async Task<bool> DeleteMirrorAsync(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        var mirror = await db.RepositoryMirrors.FindAsync(id);
        if (mirror == null) return false;

        db.RepositoryMirrors.Remove(mirror);
        await db.SaveChangesAsync();
        _logger.LogInformation("Mirror #{Id} deleted for {Repo}", id, mirror.RepoName);
        return true;
    }

    public async Task<bool> ToggleMirrorAsync(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        var mirror = await db.RepositoryMirrors.FindAsync(id);
        if (mirror == null) return false;

        mirror.IsEnabled = !mirror.IsEnabled;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<RepositoryMirror>> GetDueMirrorsAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        var now = DateTime.UtcNow;
        return await db.RepositoryMirrors
            .Where(m => m.IsEnabled &&
                (m.LastSyncAt == null || m.LastSyncAt.Value.AddMinutes(m.IntervalMinutes) <= now))
            .ToListAsync();
    }

    public async Task SyncMirrorAsync(RepositoryMirror mirror, string projectRoot)
    {
        var repoPath = Path.Combine(projectRoot, mirror.RepoName);

        // Build the remote URL with auth token if provided
        var remoteUrl = mirror.RemoteUrl;
        if (!string.IsNullOrEmpty(mirror.AuthToken) && remoteUrl.StartsWith("https://"))
        {
            var uri = new Uri(remoteUrl);
            remoteUrl = $"https://token:{mirror.AuthToken}@{uri.Host}{uri.PathAndQuery}";
        }

        string args;
        if (mirror.Direction == MirrorDirection.Pull)
        {
            // For pull mirrors: fetch from remote and update local refs
            args = $"fetch \"{remoteUrl}\" \"+refs/*:refs/*\" --prune";
        }
        else
        {
            // For push mirrors: push all refs to remote
            args = $"push --mirror \"{remoteUrl}\"";
        }

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = repoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
            {
                await UpdateSyncStatus(mirror.Id, "error", "Failed to start git process");
                return;
            }

            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                await UpdateSyncStatus(mirror.Id, "success", null);
                _logger.LogInformation("Mirror sync successful for {Repo} ({Direction})", mirror.RepoName, mirror.Direction);
            }
            else
            {
                await UpdateSyncStatus(mirror.Id, "error", stderr.Length > 500 ? stderr[..500] : stderr);
                _logger.LogWarning("Mirror sync failed for {Repo}: {Error}", mirror.RepoName, stderr);
            }
        }
        catch (Exception ex)
        {
            await UpdateSyncStatus(mirror.Id, "error", ex.Message);
            _logger.LogError(ex, "Mirror sync exception for {Repo}", mirror.RepoName);
        }
    }

    private async Task UpdateSyncStatus(int mirrorId, string status, string? error)
    {
        using var db = _dbFactory.CreateDbContext();
        var mirror = await db.RepositoryMirrors.FindAsync(mirrorId);
        if (mirror != null)
        {
            mirror.LastSyncAt = DateTime.UtcNow;
            mirror.LastSyncStatus = status;
            mirror.LastSyncError = error;
            await db.SaveChangesAsync();
        }
    }
}
