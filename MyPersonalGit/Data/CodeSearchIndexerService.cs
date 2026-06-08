using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface ICodeSearchIndexerService
{
    void QueueRepositoryIndex(string repoName);
    Task IndexRepositoryAsync(string repoName);
}

public class CodeSearchIndexerService : BackgroundService, ICodeSearchIndexerService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IConfiguration _config;
    private readonly IAdminService _adminService;
    private readonly ILogger<CodeSearchIndexerService> _logger;
    private readonly Channel<string> _queue = Channel.CreateUnbounded<string>();

    public CodeSearchIndexerService(
        IDbContextFactory<AppDbContext> dbFactory,
        IConfiguration config,
        IAdminService adminService,
        ILogger<CodeSearchIndexerService> logger)
    {
        _dbFactory = dbFactory;
        _config = config;
        _adminService = adminService;
        _logger = logger;
    }

    public void QueueRepositoryIndex(string repoName)
    {
        _queue.Writer.TryWrite(repoName);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Code search indexer background service started");

        await foreach (var repoName in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing search index queue for repository '{RepoName}'", repoName);
                await IndexRepositoryAsync(repoName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing repository '{RepoName}'", repoName);
            }
        }
    }

    public async Task IndexRepositoryAsync(string repoName)
    {
        var systemSettings = await _adminService.GetSystemSettingsAsync();
        var projectRoot = !string.IsNullOrEmpty(systemSettings.ProjectRoot)
            ? systemSettings.ProjectRoot
            : _config["Git:ProjectRoot"] ?? "/repos";

        var repoPath = Path.Combine(projectRoot, repoName);
        if (!LibGit2Sharp.Repository.IsValid(repoPath))
        {
            repoPath = Path.Combine(projectRoot, repoName + ".git");
            if (!LibGit2Sharp.Repository.IsValid(repoPath))
            {
                _logger.LogWarning("Repository directory not found on disk for '{RepoName}'", repoName);
                return;
            }
        }

        try
        {
            using var gitRepo = new LibGit2Sharp.Repository(repoPath);
            var branch = gitRepo.Head;
            if (branch?.Tip == null) return;

            var indexedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Read and index all files recursively
            using var db = _dbFactory.CreateDbContext();
            await IndexTree(gitRepo, branch.Tip.Tree, "", repoName, db, indexedPaths);

            // Clean up files that were deleted from the tree
            var existingIndexEntries = await db.CodeSearchIndices
                .Where(i => i.RepoName == repoName)
                .Select(i => new { i.Id, i.FilePath })
                .ToListAsync();

            var orphans = existingIndexEntries
                .Where(e => !indexedPaths.Contains(e.FilePath))
                .Select(e => new CodeSearchIndex { Id = e.Id })
                .ToList();

            if (orphans.Any())
            {
                db.CodeSearchIndices.RemoveRange(orphans);
                await db.SaveChangesAsync();
                _logger.LogInformation("Removed {Count} orphaned search index entries for '{RepoName}'", orphans.Count, repoName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index repository '{RepoName}' at path '{Path}'", repoName, repoPath);
        }
    }

    private async Task IndexTree(LibGit2Sharp.Repository repo, Tree tree, string prefix, string repoName, AppDbContext db, HashSet<string> indexedPaths)
    {
        foreach (var entry in tree)
        {
            var entryPath = string.IsNullOrEmpty(prefix) ? entry.Name : $"{prefix}/{entry.Name}";

            if (entry.TargetType == TreeEntryTargetType.Tree)
            {
                await IndexTree(repo, (Tree)entry.Target, entryPath, repoName, db, indexedPaths);
            }
            else if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                var blob = (Blob)entry.Target;
                if (blob.Size > 512 * 1024) continue; // Skip files > 512KB
                if (blob.IsBinary) continue;

                indexedPaths.Add(entryPath);

                var contentHash = blob.Sha; // Use Git object SHA as content hash (very fast!)

                // Check if this path already has the same hash
                var existing = await db.CodeSearchIndices
                    .FirstOrDefaultAsync(i => i.RepoName == repoName && i.FilePath == entryPath);

                if (existing != null)
                {
                    if (existing.ContentHash == contentHash)
                    {
                        // Already indexed and unmodified
                        continue;
                    }

                    // Content modified, update it
                    try
                    {
                        using var reader = new StreamReader(blob.GetContentStream(), Encoding.UTF8);
                        existing.Content = await reader.ReadToEndAsync();
                        existing.ContentHash = contentHash;
                        existing.IndexedAt = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read content for '{Path}' in '{RepoName}'", entryPath, repoName);
                    }
                }
                else
                {
                    // New file, insert it
                    try
                    {
                        using var reader = new StreamReader(blob.GetContentStream(), Encoding.UTF8);
                        var content = await reader.ReadToEndAsync();
                        db.CodeSearchIndices.Add(new CodeSearchIndex
                        {
                            RepoName = repoName,
                            FilePath = entryPath,
                            ContentHash = contentHash,
                            Content = content,
                            IndexedAt = DateTime.UtcNow
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read content for '{Path}' in '{RepoName}'", entryPath, repoName);
                    }
                }
            }
        }

        // Save batch of changes
        await db.SaveChangesAsync();
    }
}
