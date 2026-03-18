using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IAutoMergeService
{
    Task TryAutoMergeAsync(string repoName);
}

public class AutoMergeService : IAutoMergeService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutoMergeService> _logger;

    public AutoMergeService(IServiceScopeFactory scopeFactory, ILogger<AutoMergeService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task TryAutoMergeAsync(string repoName)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var prService = scope.ServiceProvider.GetRequiredService<IPullRequestService>();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = dbFactory.CreateDbContext();

            var autoMergePrs = await db.PullRequests
                .Where(p => p.RepoName == repoName &&
                            p.State == PullRequestState.Open &&
                            p.AutoMergeEnabled &&
                            !p.IsDraft)
                .ToListAsync();

            foreach (var pr in autoMergePrs)
            {
                var (canMerge, _) = await prService.CanMergeAsync(repoName, pr.Number);
                if (!canMerge) continue;

                var strategy = Enum.TryParse<MergeStrategy>(pr.AutoMergeStrategy, out var s)
                    ? s : MergeStrategy.MergeCommit;

                var (success, error) = await prService.MergePullRequestAsync(
                    repoName, pr.Number, "auto-merge", strategy);

                if (success)
                    _logger.LogInformation("Auto-merged PR #{Number} in {RepoName}", pr.Number, repoName);
                else
                    _logger.LogWarning("Auto-merge failed for PR #{Number} in {RepoName}: {Error}", pr.Number, repoName, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking auto-merge for {RepoName}", repoName);
        }
    }
}
