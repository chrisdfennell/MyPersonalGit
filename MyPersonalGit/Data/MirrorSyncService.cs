using MyPersonalGit.Data;

namespace MyPersonalGit.Services;

/// <summary>
/// Background service that polls for due mirror syncs every 60 seconds.
/// </summary>
public class MirrorSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<MirrorSyncService> _logger;

    public MirrorSyncService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<MirrorSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MirrorSyncService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                await SyncDueMirrors(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in mirror sync loop");
            }
        }
    }

    private async Task SyncDueMirrors(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var mirrorService = scope.ServiceProvider.GetRequiredService<IMirrorService>();
        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";

        var dueMirrors = await mirrorService.GetDueMirrorsAsync();
        if (dueMirrors.Count == 0) return;

        _logger.LogInformation("Syncing {Count} due mirror(s)", dueMirrors.Count);

        foreach (var mirror in dueMirrors)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                await mirrorService.SyncMirrorAsync(mirror, projectRoot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync mirror #{Id} for {Repo}", mirror.Id, mirror.RepoName);
            }
        }
    }
}
