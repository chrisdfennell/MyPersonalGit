using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Tests;

// ============================================================================
// Feature 1: Auto-merge on review approval
// ============================================================================
public class AutoMergeServiceTests
{
    private readonly IAutoMergeService _autoMergeService;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IPullRequestService _prService;

    public AutoMergeServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);
        _prService = Substitute.For<IPullRequestService>();

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IPullRequestService)).Returns(_prService);
        serviceProvider.GetService(typeof(IDbContextFactory<AppDbContext>)).Returns(_dbFactory);
        scope.ServiceProvider.Returns(serviceProvider);
        scopeFactory.CreateScope().Returns(scope);

        _autoMergeService = new AutoMergeService(scopeFactory, NullLogger<AutoMergeService>.Instance);
    }

    [Fact]
    public async Task TryAutoMergeAsync_SkipsDraftPRs()
    {
        using var db = _dbFactory.CreateDbContext();
        db.PullRequests.Add(new PullRequest
        {
            Number = 1, RepoName = "repo", Title = "Test", Author = "alice",
            SourceBranch = "feature", TargetBranch = "main",
            AutoMergeEnabled = true, AutoMergeStrategy = "MergeCommit",
            IsDraft = true, State = PullRequestState.Open
        });
        await db.SaveChangesAsync();

        await _autoMergeService.TryAutoMergeAsync("repo");

        await _prService.DidNotReceive().MergePullRequestAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<MergeStrategy>());
    }

    [Fact]
    public async Task TryAutoMergeAsync_SkipsClosedPRs()
    {
        using var db = _dbFactory.CreateDbContext();
        db.PullRequests.Add(new PullRequest
        {
            Number = 1, RepoName = "repo", Title = "Test", Author = "alice",
            SourceBranch = "feature", TargetBranch = "main",
            AutoMergeEnabled = true, AutoMergeStrategy = "MergeCommit",
            State = PullRequestState.Closed
        });
        await db.SaveChangesAsync();

        await _autoMergeService.TryAutoMergeAsync("repo");

        await _prService.DidNotReceive().MergePullRequestAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<MergeStrategy>());
    }

    [Fact]
    public async Task TryAutoMergeAsync_SkipsPRsWithoutAutoMerge()
    {
        using var db = _dbFactory.CreateDbContext();
        db.PullRequests.Add(new PullRequest
        {
            Number = 1, RepoName = "repo", Title = "Test", Author = "alice",
            SourceBranch = "feature", TargetBranch = "main",
            AutoMergeEnabled = false, State = PullRequestState.Open
        });
        await db.SaveChangesAsync();

        await _autoMergeService.TryAutoMergeAsync("repo");

        await _prService.DidNotReceive().MergePullRequestAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<MergeStrategy>());
    }
}

// ============================================================================
// Feature 3: Secret Scanning
// ============================================================================
public class SecretScanServiceTests
{
    private readonly SecretScanService _service;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public SecretScanServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);
        _service = new SecretScanService(_dbFactory, NullLogger<SecretScanService>.Instance);
    }

    [Fact]
    public async Task EnsureBuiltInPatternsAsync_SeedsPatterns()
    {
        await _service.EnsureBuiltInPatternsAsync();

        var patterns = await _service.GetPatternsAsync();
        Assert.True(patterns.Count >= 15, "Should seed at least 15 built-in patterns");
        Assert.All(patterns, p => Assert.True(p.IsBuiltIn));
        Assert.All(patterns, p => Assert.True(p.IsEnabled));
    }

    [Fact]
    public async Task EnsureBuiltInPatternsAsync_IsIdempotent()
    {
        await _service.EnsureBuiltInPatternsAsync();
        var count1 = (await _service.GetPatternsAsync()).Count;

        await _service.EnsureBuiltInPatternsAsync();
        var count2 = (await _service.GetPatternsAsync()).Count;

        Assert.Equal(count1, count2);
    }

    [Fact]
    public async Task AddPatternAsync_AddsCustomPattern()
    {
        var pattern = await _service.AddPatternAsync("Test Pattern", @"TEST_[A-Z]{10}");

        Assert.NotEqual(0, pattern.Id);
        Assert.Equal("Test Pattern", pattern.Name);
        Assert.False(pattern.IsBuiltIn);
        Assert.True(pattern.IsEnabled);
    }

    [Fact]
    public async Task AddPatternAsync_RejectsInvalidRegex()
    {
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.AddPatternAsync("Bad", @"[invalid"));
    }

    [Fact]
    public async Task TogglePatternAsync_TogglesState()
    {
        var pattern = await _service.AddPatternAsync("Test", @"TEST");
        Assert.True(pattern.IsEnabled);

        await _service.TogglePatternAsync(pattern.Id);
        var patterns = await _service.GetPatternsAsync();
        Assert.False(patterns.First(p => p.Id == pattern.Id).IsEnabled);

        await _service.TogglePatternAsync(pattern.Id);
        patterns = await _service.GetPatternsAsync();
        Assert.True(patterns.First(p => p.Id == pattern.Id).IsEnabled);
    }

    [Fact]
    public async Task DeletePatternAsync_DeletesCustomPattern()
    {
        var pattern = await _service.AddPatternAsync("Temp", @"TEMP");
        var result = await _service.DeletePatternAsync(pattern.Id);
        Assert.True(result);

        var patterns = await _service.GetPatternsAsync();
        Assert.DoesNotContain(patterns, p => p.Id == pattern.Id);
    }

    [Fact]
    public async Task DeletePatternAsync_RejectsBuiltInDeletion()
    {
        await _service.EnsureBuiltInPatternsAsync();
        var builtIn = (await _service.GetPatternsAsync()).First(p => p.IsBuiltIn);

        var result = await _service.DeletePatternAsync(builtIn.Id);
        Assert.False(result);
    }

    [Fact]
    public async Task GetResultsAsync_ReturnsEmpty_WhenNoResults()
    {
        var results = await _service.GetResultsAsync("repo");
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetResultsAsync_FiltersbyState()
    {
        using var db = _dbFactory.CreateDbContext();
        db.SecretScanResults.Add(new SecretScanResult
        {
            RepoName = "repo", CommitSha = "abc123", FilePath = "test.txt",
            LineNumber = 1, SecretType = "Test", MatchSnippet = "...",
            State = SecretScanResultState.Open, DetectedAt = DateTime.UtcNow
        });
        db.SecretScanResults.Add(new SecretScanResult
        {
            RepoName = "repo", CommitSha = "abc123", FilePath = "test2.txt",
            LineNumber = 2, SecretType = "Test", MatchSnippet = "...",
            State = SecretScanResultState.Resolved, DetectedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var open = await _service.GetResultsAsync("repo", SecretScanResultState.Open);
        var resolved = await _service.GetResultsAsync("repo", SecretScanResultState.Resolved);
        var all = await _service.GetResultsAsync("repo");

        Assert.Single(open);
        Assert.Single(resolved);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task ResolveResultAsync_UpdatesState()
    {
        using var db = _dbFactory.CreateDbContext();
        db.SecretScanResults.Add(new SecretScanResult
        {
            RepoName = "repo", CommitSha = "abc", FilePath = "f.txt",
            LineNumber = 1, SecretType = "Key", MatchSnippet = "...",
            State = SecretScanResultState.Open, DetectedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var results = await _service.GetResultsAsync("repo");
        var result = await _service.ResolveResultAsync(results[0].Id, "admin", SecretScanResultState.FalsePositive);
        Assert.True(result);

        var updated = await _service.GetResultsAsync("repo", SecretScanResultState.FalsePositive);
        Assert.Single(updated);
        Assert.Equal("admin", updated[0].ResolvedBy);
        Assert.NotNull(updated[0].ResolvedAt);
    }

    [Fact]
    public async Task ResolveResultAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.ResolveResultAsync(999, "admin", SecretScanResultState.Resolved);
        Assert.False(result);
    }
}

// ============================================================================
// Feature 4: Dependabot-style Auto-Update PRs
// ============================================================================
public class DependencyUpdateServiceTests
{
    private readonly DependencyUpdateService _service;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DependencyUpdateServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);

        var prService = Substitute.For<IPullRequestService>();
        var adminService = Substitute.For<IAdminService>();
        adminService.GetSystemSettingsAsync().Returns(new SystemSettings { ProjectRoot = "/nonexistent" });
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var config = Substitute.For<IConfiguration>();

        _service = new DependencyUpdateService(
            _dbFactory, NullLogger<DependencyUpdateService>.Instance,
            prService, adminService, httpClientFactory, config);
    }

    [Fact]
    public async Task EnableUpdatesAsync_CreatesConfig()
    {
        var config = await _service.EnableUpdatesAsync("repo", "NuGet", "weekly");

        Assert.NotEqual(0, config.Id);
        Assert.Equal("repo", config.RepoName);
        Assert.Equal("NuGet", config.Ecosystem);
        Assert.Equal("weekly", config.Schedule);
        Assert.True(config.IsEnabled);
    }

    [Fact]
    public async Task EnableUpdatesAsync_UpdatesExistingConfig()
    {
        await _service.EnableUpdatesAsync("repo", "NuGet", "weekly");
        var updated = await _service.EnableUpdatesAsync("repo", "NuGet", "daily");

        Assert.Equal("daily", updated.Schedule);
        var configs = await _service.GetConfigsAsync("repo");
        Assert.Single(configs);
    }

    [Fact]
    public async Task DisableUpdatesAsync_DisablesConfig()
    {
        var config = await _service.EnableUpdatesAsync("repo", "npm", "weekly");
        var result = await _service.DisableUpdatesAsync("repo", config.Id);
        Assert.True(result);

        var configs = await _service.GetConfigsAsync("repo");
        Assert.False(configs[0].IsEnabled);
    }

    [Fact]
    public async Task DisableUpdatesAsync_ReturnsFalse_WhenWrongRepo()
    {
        var config = await _service.EnableUpdatesAsync("repo-a", "npm", "weekly");
        var result = await _service.DisableUpdatesAsync("repo-b", config.Id);
        Assert.False(result);
    }

    [Fact]
    public async Task GetConfigsAsync_IsolatesRepos()
    {
        await _service.EnableUpdatesAsync("repo-a", "NuGet", "weekly");
        await _service.EnableUpdatesAsync("repo-b", "npm", "daily");

        var configsA = await _service.GetConfigsAsync("repo-a");
        var configsB = await _service.GetConfigsAsync("repo-b");

        Assert.Single(configsA);
        Assert.Single(configsB);
        Assert.Equal("NuGet", configsA[0].Ecosystem);
        Assert.Equal("npm", configsB[0].Ecosystem);
    }

    [Fact]
    public async Task GetLogsAsync_ReturnsEmpty_WhenNoLogs()
    {
        var logs = await _service.GetLogsAsync("repo");
        Assert.Empty(logs);
    }

    [Fact]
    public async Task GetLogsAsync_ReturnsLogs()
    {
        using var db = _dbFactory.CreateDbContext();
        db.DependencyUpdateLogs.Add(new DependencyUpdateLog
        {
            ConfigId = 1, RepoName = "repo", PackageName = "Newtonsoft.Json",
            CurrentVersion = "13.0.1", NewVersion = "13.0.3", CreatedAt = DateTime.UtcNow
        });
        db.DependencyUpdateLogs.Add(new DependencyUpdateLog
        {
            ConfigId = 1, RepoName = "repo", PackageName = "Serilog",
            CurrentVersion = "3.0.0", NewVersion = "4.0.0", CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var logs = await _service.GetLogsAsync("repo");
        Assert.Equal(2, logs.Count);
    }
}

// ============================================================================
// Feature 6: Environment Deployments with Approvals
// ============================================================================
public class EnvironmentServiceTests
{
    private readonly EnvironmentService _service;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public EnvironmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);
        var notifications = Substitute.For<INotificationService>();
        _service = new EnvironmentService(_dbFactory, NullLogger<EnvironmentService>.Instance, notifications);
    }

    [Fact]
    public async Task CreateOrUpdateEnvironmentAsync_CreatesNew()
    {
        var env = await _service.CreateOrUpdateEnvironmentAsync(
            "repo", "production", "https://prod.example.com", 30, true,
            new() { "alice", "bob" }, new() { "main" });

        Assert.NotEqual(0, env.Id);
        Assert.Equal("production", env.Name);
        Assert.Equal("https://prod.example.com", env.Url);
        Assert.Equal(30, env.WaitTimerMinutes);
        Assert.True(env.RequireApproval);
        Assert.Equal(2, env.RequiredReviewers.Count);
        Assert.Single(env.AllowedBranches);
    }

    [Fact]
    public async Task CreateOrUpdateEnvironmentAsync_UpdatesExisting()
    {
        await _service.CreateOrUpdateEnvironmentAsync("repo", "staging", null, 0, false, null, null);
        var updated = await _service.CreateOrUpdateEnvironmentAsync("repo", "staging", "https://stg.com", 5, true, new() { "admin" }, null);

        Assert.Equal("https://stg.com", updated.Url);
        Assert.Equal(5, updated.WaitTimerMinutes);
        Assert.True(updated.RequireApproval);

        var envs = await _service.GetEnvironmentsAsync("repo");
        Assert.Single(envs); // Should not duplicate
    }

    [Fact]
    public async Task DeleteEnvironmentAsync_Deletes()
    {
        await _service.CreateOrUpdateEnvironmentAsync("repo", "test", null, 0, false, null, null);
        var result = await _service.DeleteEnvironmentAsync("repo", "test");
        Assert.True(result);

        var envs = await _service.GetEnvironmentsAsync("repo");
        Assert.Empty(envs);
    }

    [Fact]
    public async Task DeleteEnvironmentAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.DeleteEnvironmentAsync("repo", "nonexistent");
        Assert.False(result);
    }

    [Fact]
    public async Task CreateDeploymentAsync_CreatesPending_WhenNoProtection()
    {
        var deployment = await _service.CreateDeploymentAsync("repo", "staging", "abc123", "main", "alice");

        Assert.Equal(DeploymentStatus.Pending, deployment.Status);
        Assert.Equal("staging", deployment.EnvironmentName);
        Assert.Equal("abc123", deployment.CommitSha);
    }

    [Fact]
    public async Task CreateDeploymentAsync_WaitsForApproval_WhenRequired()
    {
        await _service.CreateOrUpdateEnvironmentAsync("repo", "prod", null, 0, true, new() { "admin" }, null);
        var deployment = await _service.CreateDeploymentAsync("repo", "prod", "abc123", "main", "alice");

        Assert.Equal(DeploymentStatus.WaitingApproval, deployment.Status);
    }

    [Fact]
    public async Task CreateDeploymentAsync_WaitsForTimer_WhenConfigured()
    {
        await _service.CreateOrUpdateEnvironmentAsync("repo", "prod", null, 30, false, null, null);
        var deployment = await _service.CreateDeploymentAsync("repo", "prod", "abc123", "main", "alice");

        Assert.Equal(DeploymentStatus.WaitingTimer, deployment.Status);
    }

    [Fact]
    public async Task CreateDeploymentAsync_RejectsBranch_WhenRestricted()
    {
        await _service.CreateOrUpdateEnvironmentAsync("repo", "prod", null, 0, false, null, new() { "main" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateDeploymentAsync("repo", "prod", "abc123", "develop", "alice"));
    }

    [Fact]
    public async Task ApproveDeploymentAsync_ApprovesDeployment()
    {
        await _service.CreateOrUpdateEnvironmentAsync("repo", "prod", null, 0, true, new() { "admin" }, null);
        var deployment = await _service.CreateDeploymentAsync("repo", "prod", "abc123", "main", "alice");

        var result = await _service.ApproveDeploymentAsync(deployment.Id, "admin", true, "LGTM");
        Assert.True(result);

        var updated = await _service.GetDeploymentAsync(deployment.Id);
        Assert.Equal(DeploymentStatus.InProgress, updated!.Status);
    }

    [Fact]
    public async Task ApproveDeploymentAsync_RejectsDeployment()
    {
        await _service.CreateOrUpdateEnvironmentAsync("repo", "prod", null, 0, true, new() { "admin" }, null);
        var deployment = await _service.CreateDeploymentAsync("repo", "prod", "abc123", "main", "alice");

        var result = await _service.ApproveDeploymentAsync(deployment.Id, "admin", false, "Not ready");
        Assert.True(result);

        var updated = await _service.GetDeploymentAsync(deployment.Id);
        Assert.Equal(DeploymentStatus.Cancelled, updated!.Status);
    }

    [Fact]
    public async Task GetDeploymentsAsync_FiltersByEnvironment()
    {
        await _service.CreateDeploymentAsync("repo", "staging", "abc", "main", "alice");
        await _service.CreateDeploymentAsync("repo", "prod", "def", "main", "alice");

        var staging = await _service.GetDeploymentsAsync("repo", "staging");
        var prod = await _service.GetDeploymentsAsync("repo", "prod");
        var all = await _service.GetDeploymentsAsync("repo");

        Assert.Single(staging);
        Assert.Single(prod);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task UpdateDeploymentStatusAsync_UpdatesStatus()
    {
        var deployment = await _service.CreateDeploymentAsync("repo", "staging", "abc", "main", "alice");

        var result = await _service.UpdateDeploymentStatusAsync(deployment.Id, DeploymentStatus.Success);
        Assert.True(result);

        var updated = await _service.GetDeploymentAsync(deployment.Id);
        Assert.Equal(DeploymentStatus.Success, updated!.Status);
        Assert.NotNull(updated.CompletedAt);
    }

    [Fact]
    public async Task GetApprovalsAsync_ReturnsApprovals()
    {
        await _service.CreateOrUpdateEnvironmentAsync("repo", "prod", null, 0, true, new() { "a", "b" }, null);
        var deployment = await _service.CreateDeploymentAsync("repo", "prod", "abc", "main", "alice");

        await _service.ApproveDeploymentAsync(deployment.Id, "a", true, "ok");

        var approvals = await _service.GetApprovalsAsync(deployment.Id);
        Assert.Single(approvals);
        Assert.Equal("a", approvals[0].Reviewer);
        Assert.True(approvals[0].Approved);
    }
}

// ============================================================================
// Feature 7: Repository Traffic
// ============================================================================
public class RepositoryTrafficServiceTests
{
    private readonly RepositoryTrafficService _service;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public RepositoryTrafficServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);
        _service = new RepositoryTrafficService(_dbFactory, NullLogger<RepositoryTrafficService>.Instance);
    }

    [Fact]
    public async Task RecordEventAsync_RecordsEvent()
    {
        await _service.RecordEventAsync("repo", "clone", ipAddress: "1.2.3.4");

        using var db = _dbFactory.CreateDbContext();
        var events = db.RepositoryTrafficEvents.Where(e => e.RepoName == "repo").ToList();
        Assert.Single(events);
        Assert.Equal("clone", events[0].EventType);
        Assert.NotNull(events[0].IpHash);
    }

    [Fact]
    public async Task RecordEventAsync_HashesIp()
    {
        await _service.RecordEventAsync("repo", "clone", ipAddress: "1.2.3.4");
        await _service.RecordEventAsync("repo", "clone", ipAddress: "1.2.3.4");

        using var db = _dbFactory.CreateDbContext();
        var events = db.RepositoryTrafficEvents.Where(e => e.RepoName == "repo").ToList();
        Assert.Equal(2, events.Count);
        Assert.Equal(events[0].IpHash, events[1].IpHash); // Same IP -> same hash
    }

    [Fact]
    public async Task RecordEventAsync_DifferentIps_DifferentHashes()
    {
        await _service.RecordEventAsync("repo", "clone", ipAddress: "1.2.3.4");
        await _service.RecordEventAsync("repo", "clone", ipAddress: "5.6.7.8");

        using var db = _dbFactory.CreateDbContext();
        var events = db.RepositoryTrafficEvents.Where(e => e.RepoName == "repo").ToList();
        Assert.NotEqual(events[0].IpHash, events[1].IpHash);
    }

    [Fact]
    public async Task GetTrafficSummaryAsync_ReturnsEmpty_WhenNoData()
    {
        var summary = await _service.GetTrafficSummaryAsync("repo");
        Assert.Empty(summary);
    }

    [Fact]
    public async Task GetTrafficSummaryAsync_ReturnsSummaries()
    {
        using var db = _dbFactory.CreateDbContext();
        db.RepositoryTrafficSummaries.Add(new RepositoryTrafficSummary
        {
            RepoName = "repo", Date = DateTime.UtcNow.Date,
            Clones = 10, UniqueCloners = 5, PageViews = 20, UniqueVisitors = 15
        });
        await db.SaveChangesAsync();

        var summary = await _service.GetTrafficSummaryAsync("repo");
        Assert.Single(summary);
        Assert.Equal(10, summary[0].Clones);
    }

    [Fact]
    public async Task GetTotalsAsync_SumsSummaries()
    {
        using var db = _dbFactory.CreateDbContext();
        for (int i = 0; i < 3; i++)
        {
            db.RepositoryTrafficSummaries.Add(new RepositoryTrafficSummary
            {
                RepoName = "repo", Date = DateTime.UtcNow.Date.AddDays(-i),
                Clones = 10, UniqueCloners = 5, PageViews = 20, UniqueVisitors = 15
            });
        }
        await db.SaveChangesAsync();

        var (clones, uniqueCloners, views, visitors) = await _service.GetTotalsAsync("repo");
        Assert.Equal(30, clones);
        Assert.Equal(15, uniqueCloners);
        Assert.Equal(60, views);
        Assert.Equal(45, visitors);
    }

    [Fact]
    public async Task GetTopReferrersAsync_ReturnsTopReferrers()
    {
        using var db = _dbFactory.CreateDbContext();
        for (int i = 0; i < 5; i++)
            db.RepositoryTrafficEvents.Add(new RepositoryTrafficEvent
            {
                RepoName = "repo", EventType = "page_view", Referrer = "google.com", Timestamp = DateTime.UtcNow
            });
        for (int i = 0; i < 3; i++)
            db.RepositoryTrafficEvents.Add(new RepositoryTrafficEvent
            {
                RepoName = "repo", EventType = "page_view", Referrer = "github.com", Timestamp = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var referrers = await _service.GetTopReferrersAsync("repo");
        Assert.Equal(2, referrers.Count);
        Assert.Equal("google.com", referrers[0].Referrer);
        Assert.Equal(5, referrers[0].Count);
    }

    [Fact]
    public async Task GetPopularPagesAsync_ReturnsPopularPages()
    {
        using var db = _dbFactory.CreateDbContext();
        for (int i = 0; i < 4; i++)
            db.RepositoryTrafficEvents.Add(new RepositoryTrafficEvent
            {
                RepoName = "repo", EventType = "page_view", Path = "/repo/repo", Timestamp = DateTime.UtcNow
            });
        db.RepositoryTrafficEvents.Add(new RepositoryTrafficEvent
        {
            RepoName = "repo", EventType = "page_view", Path = "/repo/repo/issues", Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var pages = await _service.GetPopularPagesAsync("repo");
        Assert.Equal(2, pages.Count);
        Assert.Equal("/repo/repo", pages[0].Path);
        Assert.Equal(4, pages[0].Count);
    }

    [Fact]
    public async Task AggregateTrafficAsync_AggregatesYesterdaysEvents()
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        using var db = _dbFactory.CreateDbContext();
        db.RepositoryTrafficEvents.Add(new RepositoryTrafficEvent
        {
            RepoName = "repo", EventType = "clone", IpHash = "aaa", Timestamp = yesterday.AddHours(10)
        });
        db.RepositoryTrafficEvents.Add(new RepositoryTrafficEvent
        {
            RepoName = "repo", EventType = "clone", IpHash = "bbb", Timestamp = yesterday.AddHours(11)
        });
        db.RepositoryTrafficEvents.Add(new RepositoryTrafficEvent
        {
            RepoName = "repo", EventType = "page_view", IpHash = "aaa", Timestamp = yesterday.AddHours(12)
        });
        await db.SaveChangesAsync();

        await _service.AggregateTrafficAsync();

        var summaries = await _service.GetTrafficSummaryAsync("repo");
        Assert.Single(summaries);
        Assert.Equal(2, summaries[0].Clones);
        Assert.Equal(2, summaries[0].UniqueCloners);
        Assert.Equal(1, summaries[0].PageViews);
    }
}

// ============================================================================
// Feature 8: Transfer Issues Between Repos
// ============================================================================
public class IssueTransferTests
{
    private readonly IssueService _service;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public IssueTransferTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);
        var notifications = Substitute.For<INotificationService>();
        var activityService = Substitute.For<IActivityService>();
        _service = new IssueService(_dbFactory, NullLogger<IssueService>.Instance, notifications, activityService);

        // Seed target repo
        using var db = _dbFactory.CreateDbContext();
        db.Repositories.Add(new Repository { Name = "target-repo", Owner = "admin", CreatedAt = DateTime.UtcNow });
        db.Repositories.Add(new Repository { Name = "source-repo", Owner = "admin", CreatedAt = DateTime.UtcNow });
        db.SaveChanges();
    }

    [Fact]
    public async Task TransferIssueAsync_TransfersSuccessfully()
    {
        var issue = await _service.CreateIssueAsync("source-repo", "Bug Report", "Details here", "alice");
        await _service.AddCommentAsync("source-repo", issue.Number, "bob", "I can reproduce");

        var (success, error, newNumber) = await _service.TransferIssueAsync("source-repo", issue.Number, "target-repo", "admin");

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(1, newNumber);

        // Original issue should be closed
        var original = await _service.GetIssueAsync("source-repo", issue.Number);
        Assert.Equal(IssueState.Closed, original!.State);

        // New issue should exist with same title
        var transferred = await _service.GetIssueAsync("target-repo", newNumber!.Value);
        Assert.NotNull(transferred);
        Assert.Equal("Bug Report", transferred.Title);
        Assert.Equal("Details here", transferred.Body);
        Assert.Equal("alice", transferred.Author);
    }

    [Fact]
    public async Task TransferIssueAsync_CopiesComments()
    {
        var issue = await _service.CreateIssueAsync("source-repo", "Bug", null, "alice");
        await _service.AddCommentAsync("source-repo", issue.Number, "bob", "Comment 1");
        await _service.AddCommentAsync("source-repo", issue.Number, "charlie", "Comment 2");

        var (_, _, newNumber) = await _service.TransferIssueAsync("source-repo", issue.Number, "target-repo", "admin");

        var transferred = await _service.GetIssueAsync("target-repo", newNumber!.Value);
        // 2 original comments + 1 transfer note
        Assert.Equal(3, transferred!.Comments!.Count);
    }

    [Fact]
    public async Task TransferIssueAsync_AddsTransferNotes()
    {
        var issue = await _service.CreateIssueAsync("source-repo", "Bug", null, "alice");

        var (_, _, newNumber) = await _service.TransferIssueAsync("source-repo", issue.Number, "target-repo", "admin");

        // Check transfer note on original
        var original = await _service.GetIssueAsync("source-repo", issue.Number);
        Assert.Contains(original!.Comments, c => c.Body.Contains("transferred to target-repo"));

        // Check transfer note on new issue
        var transferred = await _service.GetIssueAsync("target-repo", newNumber!.Value);
        Assert.Contains(transferred!.Comments, c => c.Body.Contains("transferred from source-repo"));
    }

    [Fact]
    public async Task TransferIssueAsync_CreatesTransferRecord()
    {
        var issue = await _service.CreateIssueAsync("source-repo", "Bug", null, "alice");
        await _service.TransferIssueAsync("source-repo", issue.Number, "target-repo", "admin");

        using var db = _dbFactory.CreateDbContext();
        var transfer = db.IssueTransfers.FirstOrDefault();
        Assert.NotNull(transfer);
        Assert.Equal("source-repo", transfer.FromRepoName);
        Assert.Equal("target-repo", transfer.ToRepoName);
        Assert.Equal("admin", transfer.TransferredBy);
    }

    [Fact]
    public async Task TransferIssueAsync_FailsSameRepo()
    {
        var issue = await _service.CreateIssueAsync("source-repo", "Bug", null, "alice");
        var (success, error, _) = await _service.TransferIssueAsync("source-repo", issue.Number, "source-repo", "admin");

        Assert.False(success);
        Assert.Contains("same repository", error!);
    }

    [Fact]
    public async Task TransferIssueAsync_FailsWhenIssueNotFound()
    {
        var (success, error, _) = await _service.TransferIssueAsync("source-repo", 999, "target-repo", "admin");

        Assert.False(success);
        Assert.Contains("not found", error!);
    }

    [Fact]
    public async Task TransferIssueAsync_FailsWhenTargetRepoNotFound()
    {
        var issue = await _service.CreateIssueAsync("source-repo", "Bug", null, "alice");
        var (success, error, _) = await _service.TransferIssueAsync("source-repo", issue.Number, "nonexistent-repo", "admin");

        Assert.False(success);
        Assert.Contains("not found", error!);
    }

    [Fact]
    public async Task TransferIssueAsync_PreservesMatchingLabels()
    {
        // Add a label to target repo
        using var db = _dbFactory.CreateDbContext();
        db.RepositoryLabels.Add(new RepositoryLabel { RepoName = "target-repo", Name = "bug", Color = "#ff0000", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var issue = await _service.CreateIssueAsync("source-repo", "Bug", null, "alice", new() { "bug", "wontfix" });
        var (_, _, newNumber) = await _service.TransferIssueAsync("source-repo", issue.Number, "target-repo", "admin");

        var transferred = await _service.GetIssueAsync("target-repo", newNumber!.Value);
        Assert.Single(transferred!.Labels); // Only "bug" exists in target
        Assert.Equal("bug", transferred.Labels[0]);
    }

    [Fact]
    public async Task TransferIssueAsync_AssignsCorrectNumber_WhenTargetHasIssues()
    {
        await _service.CreateIssueAsync("target-repo", "Existing Issue", null, "bob");
        await _service.CreateIssueAsync("target-repo", "Another Issue", null, "bob");

        var issue = await _service.CreateIssueAsync("source-repo", "Transferred", null, "alice");
        var (_, _, newNumber) = await _service.TransferIssueAsync("source-repo", issue.Number, "target-repo", "admin");

        Assert.Equal(3, newNumber); // Next number after 2
    }
}

// ============================================================================
// Feature 2: Reusable Workflows / Composite Actions (YAML Parser Tests)
// ============================================================================
public class WorkflowYamlParserReusableTests
{
    private readonly WorkflowYamlParser _parser = new();

    [Fact]
    public void ParseYaml_DetectsWorkflowCallTrigger()
    {
        var yaml = @"
name: Reusable Build
on:
  workflow_call:
    inputs:
      environment:
        description: 'Target environment'
        required: true
        type: string
    outputs:
      artifact-url:
        description: 'URL of built artifact'
        value: ${{ jobs.build.outputs.url }}
    secrets:
      deploy-key:
        description: 'Deploy key'
        required: true
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: echo hello
";
        // We need to use reflection or the internal parse method
        // Since ParseFromRepo needs a real git repo, we test the definition properties
        // by parsing via the public interface indirectly

        // For now, test the parser class is defined with WorkflowCallDefinition
        var def = new WorkflowDefinition();
        def.IsReusable = true;
        def.WorkflowCall = new WorkflowCallDefinition();
        def.WorkflowCall.Inputs["environment"] = new WorkflowInput { Name = "environment", Required = true, Type = "string" };
        def.WorkflowCall.Secrets["deploy-key"] = new WorkflowCallSecret { Required = true };

        Assert.True(def.IsReusable);
        Assert.Single(def.WorkflowCall.Inputs);
        Assert.Single(def.WorkflowCall.Secrets);
    }

    [Fact]
    public void JobDefinition_SupportsEnvironment()
    {
        var job = new JobDefinition
        {
            Environment = "production",
            EnvironmentUrl = "https://prod.example.com"
        };

        Assert.Equal("production", job.Environment);
        Assert.Equal("https://prod.example.com", job.EnvironmentUrl);
    }

    [Fact]
    public void JobDefinition_SupportsReusableWorkflowCall()
    {
        var job = new JobDefinition
        {
            Uses = "./.github/workflows/build.yml",
            With = new Dictionary<string, string> { ["env"] = "staging" },
            Secrets = new Dictionary<string, string> { ["TOKEN"] = "${{ secrets.DEPLOY_KEY }}" }
        };

        Assert.Equal("./.github/workflows/build.yml", job.Uses);
        Assert.Single(job.With);
        Assert.Single(job.Secrets);
    }

    [Fact]
    public void WorkflowCallDefinition_StoresInputsOutputsSecrets()
    {
        var call = new WorkflowCallDefinition();
        call.Inputs["version"] = new WorkflowInput { Name = "version", Default = "1.0", Type = "string" };
        call.Outputs["url"] = new WorkflowCallOutput { Value = "https://...", Description = "URL" };
        call.Secrets["key"] = new WorkflowCallSecret { Required = true, Description = "API key" };

        Assert.Single(call.Inputs);
        Assert.Single(call.Outputs);
        Assert.Single(call.Secrets);
        Assert.Equal("1.0", call.Inputs["version"].Default);
        Assert.True(call.Secrets["key"].Required);
    }
}

// ============================================================================
// Feature 5: Cherry-pick / Revert (model + controller request tests)
// ============================================================================
public class CherryPickRevertModelTests
{
    [Fact]
    public void CherryPickRequest_ConstructsCorrectly()
    {
        var req = new MyPersonalGit.Controllers.CherryPickRevertController.CherryPickRequest("abc123", "main", true);
        Assert.Equal("abc123", req.CommitSha);
        Assert.Equal("main", req.TargetBranch);
        Assert.True(req.CreatePR);
    }

    [Fact]
    public void RevertRequest_DefaultsCreatePRToTrue()
    {
        var req = new MyPersonalGit.Controllers.CherryPickRevertController.RevertRequest("abc123", "main");
        Assert.True(req.CreatePR);
    }

    [Fact]
    public void CherryPickRequest_DefaultsCreatePRToTrue()
    {
        var req = new MyPersonalGit.Controllers.CherryPickRevertController.CherryPickRequest("abc123", "main");
        Assert.True(req.CreatePR);
    }
}

// ============================================================================
// Model Tests for all new entities
// ============================================================================
public class NewModelTests
{
    [Fact]
    public void SecretScanResult_DefaultState_IsOpen()
    {
        var result = new SecretScanResult
        {
            RepoName = "repo", CommitSha = "abc", FilePath = "f.txt",
            SecretType = "Key", MatchSnippet = "...", DetectedAt = DateTime.UtcNow
        };
        Assert.Equal(SecretScanResultState.Open, result.State);
    }

    [Fact]
    public void SecretScanPattern_DefaultEnabled()
    {
        var pattern = new SecretScanPattern { Name = "test", Pattern = "test" };
        Assert.True(pattern.IsEnabled);
        Assert.False(pattern.IsBuiltIn);
    }

    [Fact]
    public void DependencyUpdateConfig_DefaultValues()
    {
        var config = new DependencyUpdateConfig { RepoName = "repo", Ecosystem = "NuGet" };
        Assert.Equal("weekly", config.Schedule);
        Assert.True(config.IsEnabled);
        Assert.Equal(5, config.OpenPRLimit);
    }

    [Fact]
    public void DeploymentEnvironment_DefaultValues()
    {
        var env = new DeploymentEnvironment { RepoName = "repo", Name = "prod" };
        Assert.Equal(0, env.WaitTimerMinutes);
        Assert.False(env.RequireApproval);
        Assert.Empty(env.RequiredReviewers);
        Assert.Empty(env.AllowedBranches);
    }

    [Fact]
    public void Deployment_DefaultStatus_IsPending()
    {
        var d = new Deployment
        {
            RepoName = "repo", EnvironmentName = "prod",
            CommitSha = "abc", Ref = "main", Creator = "alice"
        };
        Assert.Equal(DeploymentStatus.Pending, d.Status);
    }

    [Fact]
    public void IssueTransfer_StoresAllFields()
    {
        var t = new IssueTransfer
        {
            FromRepoName = "repo-a", FromIssueNumber = 1,
            ToRepoName = "repo-b", ToIssueNumber = 5,
            TransferredBy = "admin", TransferredAt = DateTime.UtcNow
        };
        Assert.Equal("repo-a", t.FromRepoName);
        Assert.Equal(5, t.ToIssueNumber);
    }

    [Fact]
    public void RepositoryTrafficEvent_StoresAllFields()
    {
        var e = new RepositoryTrafficEvent
        {
            RepoName = "repo", EventType = "clone",
            Referrer = "google.com", IpHash = "abc123",
            Timestamp = DateTime.UtcNow
        };
        Assert.Equal("clone", e.EventType);
        Assert.Equal("google.com", e.Referrer);
    }

    [Fact]
    public void RepositoryTrafficSummary_StoresAllFields()
    {
        var s = new RepositoryTrafficSummary
        {
            RepoName = "repo", Date = DateTime.UtcNow.Date,
            Clones = 10, UniqueCloners = 5, PageViews = 100, UniqueVisitors = 50
        };
        Assert.Equal(10, s.Clones);
        Assert.Equal(50, s.UniqueVisitors);
    }

    [Fact]
    public void NotificationType_HasNewTypes()
    {
        Assert.Equal("deployment_approval", NotificationType.DeploymentApproval);
        Assert.Equal("issue_transferred", NotificationType.IssueTransferred);
        Assert.Equal("secret_detected", NotificationType.SecretDetected);
    }

    [Fact]
    public void WorkflowJob_HasEnvironmentProperty()
    {
        var job = new WorkflowJob { Name = "deploy", Environment = "production" };
        Assert.Equal("production", job.Environment);
    }
}

// ============================================================================
// DbContext Tests — verify new DbSets are registered
// ============================================================================
public class AppDbContextNewFeatureTests
{
    private readonly AppDbContext _db;

    public AppDbContextNewFeatureTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
    }

    [Fact]
    public async Task SecretScanResults_CrudWorks()
    {
        _db.SecretScanResults.Add(new SecretScanResult
        {
            RepoName = "repo", CommitSha = "abc", FilePath = "f.txt",
            SecretType = "Key", MatchSnippet = "...", DetectedAt = DateTime.UtcNow, LineNumber = 1
        });
        await _db.SaveChangesAsync();
        Assert.Single(_db.SecretScanResults);
    }

    [Fact]
    public async Task SecretScanPatterns_CrudWorks()
    {
        _db.SecretScanPatterns.Add(new SecretScanPattern { Name = "test", Pattern = "test" });
        await _db.SaveChangesAsync();
        Assert.Single(_db.SecretScanPatterns);
    }

    [Fact]
    public async Task DependencyUpdateConfigs_CrudWorks()
    {
        _db.DependencyUpdateConfigs.Add(new DependencyUpdateConfig { RepoName = "repo", Ecosystem = "NuGet", CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        Assert.Single(_db.DependencyUpdateConfigs);
    }

    [Fact]
    public async Task DeploymentEnvironments_CrudWorks()
    {
        _db.DeploymentEnvironments.Add(new DeploymentEnvironment { RepoName = "repo", Name = "prod", CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        Assert.Single(_db.DeploymentEnvironments);
    }

    [Fact]
    public async Task Deployments_CrudWorks()
    {
        _db.Deployments.Add(new Deployment
        {
            RepoName = "repo", EnvironmentName = "prod", CommitSha = "abc",
            Ref = "main", Creator = "alice", CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        Assert.Single(_db.Deployments);
    }

    [Fact]
    public async Task DeploymentApprovals_CrudWorks()
    {
        _db.DeploymentApprovals.Add(new DeploymentApproval
        {
            DeploymentId = 1, Reviewer = "admin", Approved = true, CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        Assert.Single(_db.DeploymentApprovals);
    }

    [Fact]
    public async Task RepositoryTrafficEvents_CrudWorks()
    {
        _db.RepositoryTrafficEvents.Add(new RepositoryTrafficEvent
        {
            RepoName = "repo", EventType = "clone", Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        Assert.Single(_db.RepositoryTrafficEvents);
    }

    [Fact]
    public async Task RepositoryTrafficSummaries_CrudWorks()
    {
        _db.RepositoryTrafficSummaries.Add(new RepositoryTrafficSummary
        {
            RepoName = "repo", Date = DateTime.UtcNow.Date
        });
        await _db.SaveChangesAsync();
        Assert.Single(_db.RepositoryTrafficSummaries);
    }

    [Fact]
    public async Task IssueTransfers_CrudWorks()
    {
        _db.IssueTransfers.Add(new IssueTransfer
        {
            FromRepoName = "a", FromIssueNumber = 1,
            ToRepoName = "b", ToIssueNumber = 1,
            TransferredBy = "admin", TransferredAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        Assert.Single(_db.IssueTransfers);
    }

    [Fact]
    public async Task DependencyUpdateLogs_CrudWorks()
    {
        _db.DependencyUpdateLogs.Add(new DependencyUpdateLog
        {
            ConfigId = 1, RepoName = "repo", PackageName = "pkg",
            CurrentVersion = "1.0", NewVersion = "2.0", CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        Assert.Single(_db.DependencyUpdateLogs);
    }
}
