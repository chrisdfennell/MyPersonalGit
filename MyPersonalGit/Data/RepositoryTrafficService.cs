using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IRepositoryTrafficService
{
    Task RecordEventAsync(string repoName, string eventType, string? referrer = null, string? path = null, string? ipAddress = null);
    Task<List<RepositoryTrafficSummary>> GetTrafficSummaryAsync(string repoName, int days = 14);
    Task<List<(string Referrer, int Count)>> GetTopReferrersAsync(string repoName, int days = 14);
    Task<List<(string Path, int Count)>> GetPopularPagesAsync(string repoName, int days = 14);
    Task<(int TotalClones, int UniqueCloners, int TotalViews, int UniqueVisitors)> GetTotalsAsync(string repoName, int days = 14);
    Task AggregateTrafficAsync();
}

public class RepositoryTrafficService : IRepositoryTrafficService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<RepositoryTrafficService> _logger;

    public RepositoryTrafficService(IDbContextFactory<AppDbContext> dbFactory, ILogger<RepositoryTrafficService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task RecordEventAsync(string repoName, string eventType, string? referrer = null, string? path = null, string? ipAddress = null)
    {
        try
        {
            using var db = _dbFactory.CreateDbContext();
            db.RepositoryTrafficEvents.Add(new RepositoryTrafficEvent
            {
                RepoName = repoName,
                EventType = eventType,
                Referrer = referrer,
                Path = path,
                IpHash = ipAddress != null ? HashIp(ipAddress) : null,
                Timestamp = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record traffic event for {RepoName}", repoName);
        }
    }

    public async Task<List<RepositoryTrafficSummary>> GetTrafficSummaryAsync(string repoName, int days = 14)
    {
        using var db = _dbFactory.CreateDbContext();
        var cutoff = DateTime.UtcNow.Date.AddDays(-days);
        return await db.RepositoryTrafficSummaries
            .Where(s => s.RepoName == repoName && s.Date >= cutoff)
            .OrderBy(s => s.Date)
            .ToListAsync();
    }

    public async Task<List<(string Referrer, int Count)>> GetTopReferrersAsync(string repoName, int days = 14)
    {
        using var db = _dbFactory.CreateDbContext();
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var results = await db.RepositoryTrafficEvents
            .Where(e => e.RepoName == repoName && e.Timestamp >= cutoff && e.Referrer != null && e.Referrer != "")
            .GroupBy(e => e.Referrer!)
            .Select(g => new { Referrer = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(10)
            .ToListAsync();
        return results.Select(r => (r.Referrer, r.Count)).ToList();
    }

    public async Task<List<(string Path, int Count)>> GetPopularPagesAsync(string repoName, int days = 14)
    {
        using var db = _dbFactory.CreateDbContext();
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var results = await db.RepositoryTrafficEvents
            .Where(e => e.RepoName == repoName && e.EventType == "page_view" && e.Timestamp >= cutoff && e.Path != null)
            .GroupBy(e => e.Path!)
            .Select(g => new { Path = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(10)
            .ToListAsync();
        return results.Select(r => (r.Path, r.Count)).ToList();
    }

    public async Task<(int TotalClones, int UniqueCloners, int TotalViews, int UniqueVisitors)> GetTotalsAsync(string repoName, int days = 14)
    {
        using var db = _dbFactory.CreateDbContext();
        var cutoff = DateTime.UtcNow.Date.AddDays(-days);
        var summaries = await db.RepositoryTrafficSummaries
            .Where(s => s.RepoName == repoName && s.Date >= cutoff)
            .ToListAsync();

        return (
            summaries.Sum(s => s.Clones),
            summaries.Sum(s => s.UniqueCloners),
            summaries.Sum(s => s.PageViews),
            summaries.Sum(s => s.UniqueVisitors)
        );
    }

    public async Task AggregateTrafficAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var dayStart = yesterday;
        var dayEnd = yesterday.AddDays(1);

        // Get all events from yesterday that haven't been aggregated
        var events = await db.RepositoryTrafficEvents
            .Where(e => e.Timestamp >= dayStart && e.Timestamp < dayEnd)
            .ToListAsync();

        var grouped = events.GroupBy(e => e.RepoName);

        foreach (var group in grouped)
        {
            var repoName = group.Key;

            // Check if summary already exists
            var existing = await db.RepositoryTrafficSummaries
                .FirstOrDefaultAsync(s => s.RepoName == repoName && s.Date == yesterday);
            if (existing != null) continue;

            var cloneEvents = group.Where(e => e.EventType == "clone").ToList();
            var viewEvents = group.Where(e => e.EventType == "page_view").ToList();

            db.RepositoryTrafficSummaries.Add(new RepositoryTrafficSummary
            {
                RepoName = repoName,
                Date = yesterday,
                Clones = cloneEvents.Count,
                UniqueCloners = cloneEvents.Select(e => e.IpHash).Distinct().Count(),
                PageViews = viewEvents.Count,
                UniqueVisitors = viewEvents.Select(e => e.IpHash).Distinct().Count()
            });
        }

        await db.SaveChangesAsync();

        // Prune old events (keep 90 days)
        var pruneDate = DateTime.UtcNow.AddDays(-90);
        var oldEvents = await db.RepositoryTrafficEvents
            .Where(e => e.Timestamp < pruneDate)
            .ToListAsync();
        if (oldEvents.Any())
        {
            db.RepositoryTrafficEvents.RemoveRange(oldEvents);
            await db.SaveChangesAsync();
            _logger.LogInformation("Pruned {Count} old traffic events", oldEvents.Count);
        }
    }

    private static string HashIp(string ip)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(ip + "traffic-salt"));
        return Convert.ToHexString(hash)[..16];
    }
}

public class TrafficAggregationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TrafficAggregationService> _logger;

    public TrafficAggregationService(IServiceScopeFactory scopeFactory, ILogger<TrafficAggregationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var trafficService = scope.ServiceProvider.GetRequiredService<IRepositoryTrafficService>();
                await trafficService.AggregateTrafficAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error aggregating traffic data");
            }

            // Run daily
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
