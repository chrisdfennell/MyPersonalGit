using System.Text.Json;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public class RepositoryService
{
    private readonly string _dataPath;
    private readonly ILogger<RepositoryService> _logger;
    private readonly NotificationService _notificationService;

    public RepositoryService(IConfiguration config, ILogger<RepositoryService> logger, NotificationService notificationService)
    {
        var projectRoot = config["Git:ProjectRoot"] ?? "/repos";
        _dataPath = Path.Combine(projectRoot, ".data");
        _logger = logger;
        _notificationService = notificationService;
        Directory.CreateDirectory(_dataPath);
    }

    private string GetReposFilePath() => Path.Combine(_dataPath, "repositories.json");
    private string GetStarsFilePath() => Path.Combine(_dataPath, "stars.json");

    public async Task<List<Repository>> GetRepositoriesAsync()
    {
        var filePath = GetReposFilePath();
        if (!File.Exists(filePath))
            return new List<Repository>();

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<Repository>>(json) ?? new List<Repository>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load repositories");
            return new List<Repository>();
        }
    }

    public async Task<Repository?> GetRepositoryAsync(string name)
    {
        var repos = await GetRepositoriesAsync();
        return repos.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> StarRepositoryAsync(string repoName, string username)
    {
        var stars = await GetStarsAsync();
        if (stars.Any(s => s.RepoName == repoName && s.Username == username))
            return false;

        stars.Add(new RepositoryStar
        {
            RepoName = repoName,
            Username = username,
            StarredAt = DateTime.UtcNow
        });

        await SaveStarsAsync(stars);

        var repos = await GetRepositoriesAsync();
        var repo = repos.FirstOrDefault(r => r.Name == repoName);
        if (repo != null)
        {
            repo.Stars++;
            await SaveRepositoriesAsync(repos);

            await _notificationService.CreateNotificationAsync(
                "current-user",
                NotificationType.RepositoryStarred,
                $"Repository starred",
                $"{username} starred {repoName}",
                repoName,
                $"/repo/{repoName}"
            );
        }

        return true;
    }

    public async Task<bool> UnstarRepositoryAsync(string repoName, string username)
    {
        var stars = await GetStarsAsync();
        var star = stars.FirstOrDefault(s => s.RepoName == repoName && s.Username == username);
        if (star == null)
            return false;

        stars.Remove(star);
        await SaveStarsAsync(stars);

        var repos = await GetRepositoriesAsync();
        var repo = repos.FirstOrDefault(r => r.Name == repoName);
        if (repo != null && repo.Stars > 0)
        {
            repo.Stars--;
            await SaveRepositoriesAsync(repos);
        }

        return true;
    }

    public async Task<bool> IsStarredAsync(string repoName, string username)
    {
        var stars = await GetStarsAsync();
        return stars.Any(s => s.RepoName == repoName && s.Username == username);
    }

    private async Task<List<RepositoryStar>> GetStarsAsync()
    {
        var filePath = GetStarsFilePath();
        if (!File.Exists(filePath))
            return new List<RepositoryStar>();

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<RepositoryStar>>(json) ?? new List<RepositoryStar>();
        }
        catch
        {
            return new List<RepositoryStar>();
        }
    }

    private async Task SaveStarsAsync(List<RepositoryStar> stars)
    {
        var filePath = GetStarsFilePath();
        var json = JsonSerializer.Serialize(stars, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    private async Task SaveRepositoriesAsync(List<Repository> repos)
    {
        var filePath = GetReposFilePath();
        var json = JsonSerializer.Serialize(repos, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }
}
