using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MyPersonalGit.Data;

/// <summary>
/// Periodically re-scans every repository's dependencies so the cached vulnerability +
/// outdated findings stay current (advisory data drifts even when code doesn't). Page
/// views then read the cache instead of hitting the registries live.
/// </summary>
public class DependencyScanSchedulerService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(12);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DependencyScanSchedulerService> _logger;

    public DependencyScanSchedulerService(IServiceScopeFactory scopeFactory, ILogger<DependencyScanSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dependency scan scheduler started");

        // Let the app finish booting before the first sweep.
        try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scanService = scope.ServiceProvider.GetRequiredService<IDependencyScanService>();
                var count = await scanService.ScanAllAsync(stoppingToken);
                _logger.LogInformation("Dependency scan sweep complete: {Count} repositories scanned", count);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dependency scan sweep failed");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
