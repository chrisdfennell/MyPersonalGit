using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public class RegistryCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RegistryCleanupService> _logger;
    private readonly IConfiguration _config;

    public RegistryCleanupService(IServiceScopeFactory scopeFactory, ILogger<RegistryCleanupService> logger, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Registry cleanup background service started");

        // Run once on startup after a short delay, then run every 24 hours
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupRegistryAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing registry retention cleanup");
            }

            try
            {
                await CleanupArtifactsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing expired artifact cleanup");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    public async Task CleanupArtifactsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var artifactService = scope.ServiceProvider.GetRequiredService<IArtifactService>();
        _logger.LogInformation("Running expired artifact cleanup.");
        await artifactService.CleanupExpiredArtifactsAsync();
    }

    public async Task CleanupRegistryAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();

        using var db = dbFactory.CreateDbContext();
        var settings = await adminService.GetSystemSettingsAsync();

        var retentionCount = settings.GenericPackageRetentionCount;
        if (retentionCount <= 0)
        {
            return;
        }

        _logger.LogInformation("Running registry cleanup. Keeping max {Count} latest versions of each generic package.", retentionCount);

        var projectRoot = settings.ProjectRoot;
        if (string.IsNullOrEmpty(projectRoot))
        {
            projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        }

        var storePath = Path.Combine(projectRoot, ".packages", "generic");

        var genericPackages = await db.Packages
            .Include(p => p.Versions)
            .Where(p => p.Type == "generic")
            .ToListAsync(ct);

        foreach (var pkg in genericPackages)
        {
            if (pkg.Versions.Count > retentionCount)
            {
                var orderedVersions = pkg.Versions
                    .OrderByDescending(v => v.CreatedAt)
                    .ToList();

                var versionsToDelete = orderedVersions.Skip(retentionCount).ToList();

                _logger.LogInformation("Package '{Name}' has {Total} versions. Retaining {Keep} latest versions, pruning {Prune} versions.", 
                    pkg.Name, pkg.Versions.Count, retentionCount, versionsToDelete.Count);

                foreach (var ver in versionsToDelete)
                {
                    var versionDir = Path.Combine(storePath, pkg.Name, ver.Version);
                    try
                    {
                        if (Directory.Exists(versionDir))
                        {
                            Directory.Delete(versionDir, true);
                            _logger.LogInformation("Deleted package files directory: {Dir}", versionDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete package files directory from disk: {Dir}", versionDir);
                    }

                    db.PackageVersions.Remove(ver);
                }

                await db.SaveChangesAsync(ct);
            }
        }
    }
}
