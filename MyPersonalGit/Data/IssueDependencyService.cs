using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IIssueDependencyService
{
    Task<IssueDependency?> AddDependencyAsync(string repoName, int blockingIssueNumber, int blockedIssueNumber, string createdBy);
    Task<bool> RemoveDependencyAsync(string repoName, int dependencyId);
    Task<List<Issue>> GetBlockingIssuesAsync(string repoName, int issueNumber);
    Task<List<Issue>> GetBlockedByIssuesAsync(string repoName, int issueNumber);
    Task<List<IssueDependency>> GetDependenciesAsync(string repoName, int issueNumber);
    Task<bool> HasCircularDependencyAsync(string repoName, int blockingNumber, int blockedNumber);
    Task<bool> IsBlockedAsync(string repoName, int issueNumber);
}

public class IssueDependencyService : IIssueDependencyService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<IssueDependencyService> _logger;
    private readonly INotificationService _notificationService;

    public IssueDependencyService(IDbContextFactory<AppDbContext> dbFactory, ILogger<IssueDependencyService> logger, INotificationService notificationService)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<IssueDependency?> AddDependencyAsync(string repoName, int blockingIssueNumber, int blockedIssueNumber, string createdBy)
    {
        if (blockingIssueNumber == blockedIssueNumber)
            return null;

        using var db = _dbFactory.CreateDbContext();

        // Verify both issues exist
        var blocking = await db.Issues.FirstOrDefaultAsync(i => i.RepoName == repoName && i.Number == blockingIssueNumber);
        var blocked = await db.Issues.FirstOrDefaultAsync(i => i.RepoName == repoName && i.Number == blockedIssueNumber);
        if (blocking == null || blocked == null)
            return null;

        // Check for duplicate
        var existing = await db.IssueDependencies.FirstOrDefaultAsync(d =>
            d.RepoName == repoName &&
            d.BlockingIssueNumber == blockingIssueNumber &&
            d.BlockedIssueNumber == blockedIssueNumber);
        if (existing != null)
            return null;

        // Check for circular dependency
        if (await HasCircularDependencyAsync(repoName, blockingIssueNumber, blockedIssueNumber))
            return null;

        var dep = new IssueDependency
        {
            RepoName = repoName,
            BlockingIssueNumber = blockingIssueNumber,
            BlockedIssueNumber = blockedIssueNumber,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        db.IssueDependencies.Add(dep);
        await db.SaveChangesAsync();

        _logger.LogInformation("Issue dependency added: #{Blocking} blocks #{Blocked} in {RepoName}", blockingIssueNumber, blockedIssueNumber, repoName);
        return dep;
    }

    public async Task<bool> RemoveDependencyAsync(string repoName, int dependencyId)
    {
        using var db = _dbFactory.CreateDbContext();
        var dep = await db.IssueDependencies.FirstOrDefaultAsync(d => d.Id == dependencyId && d.RepoName == repoName);
        if (dep == null) return false;

        db.IssueDependencies.Remove(dep);
        await db.SaveChangesAsync();

        _logger.LogInformation("Issue dependency {Id} removed in {RepoName}", dependencyId, repoName);
        return true;
    }

    public async Task<List<Issue>> GetBlockingIssuesAsync(string repoName, int issueNumber)
    {
        using var db = _dbFactory.CreateDbContext();
        var blockingNumbers = await db.IssueDependencies
            .Where(d => d.RepoName == repoName && d.BlockedIssueNumber == issueNumber)
            .Select(d => d.BlockingIssueNumber)
            .ToListAsync();

        return await db.Issues
            .Where(i => i.RepoName == repoName && blockingNumbers.Contains(i.Number))
            .ToListAsync();
    }

    public async Task<List<Issue>> GetBlockedByIssuesAsync(string repoName, int issueNumber)
    {
        using var db = _dbFactory.CreateDbContext();
        var blockedNumbers = await db.IssueDependencies
            .Where(d => d.RepoName == repoName && d.BlockingIssueNumber == issueNumber)
            .Select(d => d.BlockedIssueNumber)
            .ToListAsync();

        return await db.Issues
            .Where(i => i.RepoName == repoName && blockedNumbers.Contains(i.Number))
            .ToListAsync();
    }

    public async Task<List<IssueDependency>> GetDependenciesAsync(string repoName, int issueNumber)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.IssueDependencies
            .Where(d => d.RepoName == repoName &&
                       (d.BlockingIssueNumber == issueNumber || d.BlockedIssueNumber == issueNumber))
            .ToListAsync();
    }

    public async Task<bool> HasCircularDependencyAsync(string repoName, int blockingNumber, int blockedNumber)
    {
        using var db = _dbFactory.CreateDbContext();
        var allDeps = await db.IssueDependencies
            .Where(d => d.RepoName == repoName)
            .ToListAsync();

        // BFS from blockedNumber's blocking chain to see if we reach blockingNumber
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(blockedNumber);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == blockingNumber)
                return true;

            if (!visited.Add(current))
                continue;

            // Find issues that `current` blocks (i.e., current is the blocking issue)
            foreach (var dep in allDeps.Where(d => d.BlockedIssueNumber == current))
            {
                queue.Enqueue(dep.BlockingIssueNumber);
            }
        }

        return false;
    }

    public async Task<bool> IsBlockedAsync(string repoName, int issueNumber)
    {
        using var db = _dbFactory.CreateDbContext();
        var blockingNumbers = await db.IssueDependencies
            .Where(d => d.RepoName == repoName && d.BlockedIssueNumber == issueNumber)
            .Select(d => d.BlockingIssueNumber)
            .ToListAsync();

        if (!blockingNumbers.Any())
            return false;

        // Check if any blocking issue is still open
        return await db.Issues.AnyAsync(i =>
            i.RepoName == repoName &&
            blockingNumbers.Contains(i.Number) &&
            i.State == IssueState.Open);
    }
}
