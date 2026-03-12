using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IArtifactService
{
    Task<List<WorkflowArtifact>> GetArtifactsAsync(int workflowRunId);
    Task<WorkflowArtifact?> GetArtifactAsync(int artifactId);
    Task<WorkflowArtifact> SaveArtifactAsync(int workflowRunId, string name, Stream content);
    Task<Stream?> DownloadArtifactAsync(int artifactId);
    Task<bool> DeleteArtifactAsync(int artifactId);
    Task CleanupExpiredArtifactsAsync();
    string GetArtifactsDirectory();
}

public class ArtifactService : IArtifactService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<ArtifactService> _logger;
    private readonly string _artifactsRoot;

    public ArtifactService(IDbContextFactory<AppDbContext> dbFactory, ILogger<ArtifactService> logger, IConfiguration config)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _artifactsRoot = config["Artifacts:StoragePath"] ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".mypersonalgit", "artifacts");

        if (!Directory.Exists(_artifactsRoot))
            Directory.CreateDirectory(_artifactsRoot);
    }

    public string GetArtifactsDirectory() => _artifactsRoot;

    public async Task<List<WorkflowArtifact>> GetArtifactsAsync(int workflowRunId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.WorkflowArtifacts
            .Where(a => a.WorkflowRunId == workflowRunId)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<WorkflowArtifact?> GetArtifactAsync(int artifactId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.WorkflowArtifacts.FindAsync(artifactId);
    }

    public async Task<WorkflowArtifact> SaveArtifactAsync(int workflowRunId, string name, Stream content)
    {
        // Create directory for this run
        var runDir = Path.Combine(_artifactsRoot, workflowRunId.ToString());
        if (!Directory.Exists(runDir))
            Directory.CreateDirectory(runDir);

        // Sanitize filename
        var safeName = Path.GetFileName(name);
        var filePath = Path.Combine(runDir, safeName);

        // Write content to disk
        using (var fileStream = File.Create(filePath))
        {
            await content.CopyToAsync(fileStream);
        }

        var fileInfo = new FileInfo(filePath);

        using var db = _dbFactory.CreateDbContext();

        // Check if artifact with same name exists for this run
        var existing = await db.WorkflowArtifacts
            .FirstOrDefaultAsync(a => a.WorkflowRunId == workflowRunId && a.Name == safeName);

        if (existing != null)
        {
            existing.FilePath = filePath;
            existing.SizeBytes = fileInfo.Length;
            existing.CreatedAt = DateTime.UtcNow;
            existing.ExpiresAt = DateTime.UtcNow.AddDays(90);
            await db.SaveChangesAsync();
            _logger.LogInformation("Artifact '{Name}' updated for run {RunId} ({Size} bytes)", safeName, workflowRunId, fileInfo.Length);
            return existing;
        }

        var artifact = new WorkflowArtifact
        {
            WorkflowRunId = workflowRunId,
            Name = safeName,
            FilePath = filePath,
            SizeBytes = fileInfo.Length,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(90) // 90-day retention
        };

        db.WorkflowArtifacts.Add(artifact);
        await db.SaveChangesAsync();

        _logger.LogInformation("Artifact '{Name}' saved for run {RunId} ({Size} bytes)", safeName, workflowRunId, fileInfo.Length);
        return artifact;
    }

    public async Task<Stream?> DownloadArtifactAsync(int artifactId)
    {
        using var db = _dbFactory.CreateDbContext();
        var artifact = await db.WorkflowArtifacts.FindAsync(artifactId);
        if (artifact == null) return null;

        if (!File.Exists(artifact.FilePath)) return null;

        return File.OpenRead(artifact.FilePath);
    }

    public async Task<bool> DeleteArtifactAsync(int artifactId)
    {
        using var db = _dbFactory.CreateDbContext();
        var artifact = await db.WorkflowArtifacts.FindAsync(artifactId);
        if (artifact == null) return false;

        // Delete file from disk
        if (File.Exists(artifact.FilePath))
        {
            try { File.Delete(artifact.FilePath); } catch { }
        }

        db.WorkflowArtifacts.Remove(artifact);
        await db.SaveChangesAsync();

        _logger.LogInformation("Artifact '{Name}' deleted (id: {Id})", artifact.Name, artifactId);
        return true;
    }

    public async Task CleanupExpiredArtifactsAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        var now = DateTime.UtcNow;

        var expired = await db.WorkflowArtifacts
            .Where(a => a.ExpiresAt != null && a.ExpiresAt <= now)
            .ToListAsync();

        foreach (var artifact in expired)
        {
            if (File.Exists(artifact.FilePath))
            {
                try { File.Delete(artifact.FilePath); } catch { }
            }
            db.WorkflowArtifacts.Remove(artifact);
        }

        if (expired.Any())
        {
            await db.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired artifact(s)", expired.Count);
        }
    }
}
