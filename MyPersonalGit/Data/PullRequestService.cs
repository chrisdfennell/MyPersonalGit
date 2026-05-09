using System.Text.Json;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public class PullRequestService
{
    private readonly string _dataPath;
    private readonly ILogger<PullRequestService> _logger;
    private readonly NotificationService _notificationService;

    public PullRequestService(IConfiguration config, ILogger<PullRequestService> logger, NotificationService notificationService)
    {
        var projectRoot = config["Git:ProjectRoot"] ?? "/repos";
        _dataPath = Path.Combine(projectRoot, ".data");
        _logger = logger;
        _notificationService = notificationService;
        Directory.CreateDirectory(_dataPath);
    }

    private string GetPRsFilePath(string repoName) =>
        Path.Combine(_dataPath, $"{repoName}_prs.json");

    public async Task<List<PullRequest>> GetPullRequestsAsync(string repoName)
    {
        var filePath = GetPRsFilePath(repoName);
        if (!File.Exists(filePath))
            return new List<PullRequest>();

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<PullRequest>>(json) ?? new List<PullRequest>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load PRs for {RepoName}", repoName);
            return new List<PullRequest>();
        }
    }

    public async Task<PullRequest?> GetPullRequestAsync(string repoName, int number)
    {
        var prs = await GetPullRequestsAsync(repoName);
        return prs.FirstOrDefault(pr => pr.Number == number);
    }

    public async Task<PullRequest> CreatePullRequestAsync(
        string repoName, string title, string? body, string author,
        string sourceBranch, string targetBranch, bool isDraft = false)
    {
        var prs = await GetPullRequestsAsync(repoName);
        var nextNumber = prs.Any() ? prs.Max(pr => pr.Number) + 1 : 1;

        var pr = new PullRequest
        {
            Number = nextNumber,
            RepoName = repoName,
            Title = title,
            Body = body,
            Author = author,
            SourceBranch = sourceBranch,
            TargetBranch = targetBranch,
            IsDraft = isDraft,
            CreatedAt = DateTime.UtcNow
        };

        prs.Add(pr);
        await SavePullRequestsAsync(repoName, prs);

        await _notificationService.CreateNotificationAsync(
            "current-user",
            NotificationType.PullRequestCreated,
            $"New pull request #{pr.Number}",
            $"{author} created PR: {title}",
            repoName,
            $"/repo/{repoName}/pulls/{pr.Number}"
        );

        return pr;
    }

    public async Task<bool> MergePullRequestAsync(string repoName, int number, string mergedBy)
    {
        var result = await UpdatePullRequestAsync(repoName, number, pr =>
        {
            pr.State = PullRequestState.Merged;
            pr.MergedAt = DateTime.UtcNow;
            pr.MergedBy = mergedBy;
        });

        if (result)
        {
            var pr = await GetPullRequestAsync(repoName, number);
            if (pr != null)
            {
                await _notificationService.CreateNotificationAsync(
                    "current-user",
                    NotificationType.PullRequestMerged,
                    $"Pull request #{number} merged",
                    $"{mergedBy} merged PR: {pr.Title}",
                    repoName,
                    $"/repo/{repoName}/pulls/{number}"
                );
            }
        }

        return result;
    }

    public async Task<bool> ClosePullRequestAsync(string repoName, int number)
    {
        return await UpdatePullRequestAsync(repoName, number, pr =>
        {
            pr.State = PullRequestState.Closed;
            pr.ClosedAt = DateTime.UtcNow;
        });
    }

    public async Task<bool> AddReviewAsync(string repoName, int number, string author, ReviewState state, string? body = null)
    {
        var result = await UpdatePullRequestAsync(repoName, number, pr =>
        {
            pr.Reviews.Add(new PullRequestReview
            {
                Id = pr.Reviews.Any() ? pr.Reviews.Max(r => r.Id) + 1 : 1,
                Author = author,
                State = state,
                Body = body,
                CreatedAt = DateTime.UtcNow
            });
        });

        if (result)
        {
            var pr = await GetPullRequestAsync(repoName, number);
            if (pr != null)
            {
                await _notificationService.CreateNotificationAsync(
                    "current-user",
                    NotificationType.PullRequestReview,
                    $"New review on PR #{number}",
                    $"{author} reviewed PR: {state}",
                    repoName,
                    $"/repo/{repoName}/pulls/{number}"
                );
            }
        }

        return result;
    }

    private async Task<bool> UpdatePullRequestAsync(string repoName, int number, Action<PullRequest> updateAction)
    {
        var prs = await GetPullRequestsAsync(repoName);
        var pr = prs.FirstOrDefault(p => p.Number == number);
        if (pr == null)
            return false;

        updateAction(pr);
        await SavePullRequestsAsync(repoName, prs);
        return true;
    }

    private async Task SavePullRequestsAsync(string repoName, List<PullRequest> prs)
    {
        var filePath = GetPRsFilePath(repoName);
        var json = JsonSerializer.Serialize(prs, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }
}
