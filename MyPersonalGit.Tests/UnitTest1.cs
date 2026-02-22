using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Tests;

/// <summary>
/// Helper that implements IDbContextFactory for testing with InMemory provider.
/// </summary>
internal class TestDbContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly DbContextOptions<AppDbContext> _options;

    public TestDbContextFactory(DbContextOptions<AppDbContext> options)
    {
        _options = options;
    }

    public AppDbContext CreateDbContext() => new AppDbContext(_options);
}

public class IssueServiceTests
{
    private readonly IssueService _service;
    private readonly INotificationService _notifications;

    public IssueServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        _notifications = Substitute.For<INotificationService>();
        var activityService = Substitute.For<IActivityService>();
        _service = new IssueService(factory, NullLogger<IssueService>.Instance, _notifications, activityService);
    }

    [Fact]
    public async Task CreateIssueAsync_AssignsSequentialNumbers()
    {
        var issue1 = await _service.CreateIssueAsync("repo", "First", "body1", "alice");
        var issue2 = await _service.CreateIssueAsync("repo", "Second", "body2", "bob");

        Assert.Equal(1, issue1.Number);
        Assert.Equal(2, issue2.Number);
    }

    [Fact]
    public async Task GetIssuesAsync_ReturnsAllIssues()
    {
        await _service.CreateIssueAsync("repo", "A", null, "alice");
        await _service.CreateIssueAsync("repo", "B", null, "bob");

        var issues = await _service.GetIssuesAsync("repo");
        Assert.Equal(2, issues.Count);
    }

    [Fact]
    public async Task GetIssueAsync_ReturnsSingleIssue()
    {
        await _service.CreateIssueAsync("repo", "Target", "content", "alice");

        var issue = await _service.GetIssueAsync("repo", 1);
        Assert.NotNull(issue);
        Assert.Equal("Target", issue.Title);
    }

    [Fact]
    public async Task GetIssueAsync_ReturnsNull_WhenNotFound()
    {
        var issue = await _service.GetIssueAsync("repo", 999);
        Assert.Null(issue);
    }

    [Fact]
    public async Task CloseIssueAsync_SetsClosedState()
    {
        await _service.CreateIssueAsync("repo", "Bug", null, "alice");

        var result = await _service.CloseIssueAsync("repo", 1);
        Assert.True(result);

        var issue = await _service.GetIssueAsync("repo", 1);
        Assert.Equal(IssueState.Closed, issue!.State);
        Assert.NotNull(issue.ClosedAt);
    }

    [Fact]
    public async Task ReopenIssueAsync_SetsOpenState()
    {
        await _service.CreateIssueAsync("repo", "Bug", null, "alice");
        await _service.CloseIssueAsync("repo", 1);

        var result = await _service.ReopenIssueAsync("repo", 1);
        Assert.True(result);

        var issue = await _service.GetIssueAsync("repo", 1);
        Assert.Equal(IssueState.Open, issue!.State);
        Assert.Null(issue.ClosedAt);
    }

    [Fact]
    public async Task AddCommentAsync_AddsComment()
    {
        await _service.CreateIssueAsync("repo", "Bug", null, "alice");

        var result = await _service.AddCommentAsync("repo", 1, "bob", "Fixed it");
        Assert.True(result);

        var issue = await _service.GetIssueAsync("repo", 1);
        Assert.Single(issue!.Comments);
        Assert.Equal("bob", issue.Comments[0].Author);
        Assert.Equal("Fixed it", issue.Comments[0].Body);
    }

    [Fact]
    public async Task CreateIssueAsync_TriggersNotification()
    {
        await _service.CreateIssueAsync("repo", "Bug", null, "alice");

        await _notifications.Received(1).CreateNotificationAsync(
            Arg.Any<string>(),
            Arg.Is(NotificationType.IssueCreated),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is("repo"),
            Arg.Any<string?>()
        );
    }

    [Fact]
    public async Task GetIssuesAsync_IsolatesRepositories()
    {
        await _service.CreateIssueAsync("repo-a", "Issue A", null, "alice");
        await _service.CreateIssueAsync("repo-b", "Issue B", null, "bob");

        var issuesA = await _service.GetIssuesAsync("repo-a");
        var issuesB = await _service.GetIssuesAsync("repo-b");

        Assert.Single(issuesA);
        Assert.Single(issuesB);
        Assert.Equal("Issue A", issuesA[0].Title);
        Assert.Equal("Issue B", issuesB[0].Title);
    }
}

public class AuthServiceTests
{
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        _service = new AuthService(factory, NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_CreatesUser()
    {
        var user = await _service.RegisterAsync("alice", "alice@test.com", "password123");

        Assert.NotNull(user);
        Assert.Equal("alice", user.Username);
        Assert.Equal("alice@test.com", user.Email);
    }

    [Fact]
    public async Task RegisterAsync_RejectsDuplicateUsername()
    {
        await _service.RegisterAsync("alice", "alice@test.com", "password123");
        var duplicate = await _service.RegisterAsync("alice", "other@test.com", "password456");

        Assert.Null(duplicate);
    }

    [Fact]
    public async Task RegisterAsync_RejectsDuplicateEmail()
    {
        await _service.RegisterAsync("alice", "shared@test.com", "password123");
        var duplicate = await _service.RegisterAsync("bob", "shared@test.com", "password456");

        Assert.Null(duplicate);
    }

    [Fact]
    public async Task LoginAsync_ReturnsSession_WithValidCredentials()
    {
        await _service.RegisterAsync("alice", "alice@test.com", "password123");
        var session = await _service.LoginAsync("alice", "password123");

        Assert.NotNull(session);
        Assert.Equal("alice", session.Username);
        Assert.False(string.IsNullOrEmpty(session.SessionId));
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WithInvalidPassword()
    {
        await _service.RegisterAsync("alice", "alice@test.com", "password123");
        var session = await _service.LoginAsync("alice", "wrongpassword");

        Assert.Null(session);
    }

    [Fact]
    public async Task LoginAsync_WorksWithEmail()
    {
        await _service.RegisterAsync("alice", "alice@test.com", "password123");
        var session = await _service.LoginAsync("alice@test.com", "password123");

        Assert.NotNull(session);
    }

    [Fact]
    public async Task GetUserBySessionAsync_ReturnsUser()
    {
        await _service.RegisterAsync("alice", "alice@test.com", "password123");
        var session = await _service.LoginAsync("alice", "password123");

        var user = await _service.GetUserBySessionAsync(session!.SessionId);
        Assert.NotNull(user);
        Assert.Equal("alice", user.Username);
    }

    [Fact]
    public async Task LogoutAsync_InvalidatesSession()
    {
        await _service.RegisterAsync("alice", "alice@test.com", "password123");
        var session = await _service.LoginAsync("alice", "password123");

        await _service.LogoutAsync(session!.SessionId);

        var user = await _service.GetUserBySessionAsync(session.SessionId);
        Assert.Null(user);
    }

    [Fact]
    public async Task ChangePasswordAsync_Works()
    {
        await _service.RegisterAsync("alice", "alice@test.com", "oldpass");
        var result = await _service.ChangePasswordAsync("alice", "oldpass", "newpass");
        Assert.True(result);

        var session = await _service.LoginAsync("alice", "newpass");
        Assert.NotNull(session);

        var oldSession = await _service.LoginAsync("alice", "oldpass");
        Assert.Null(oldSession);
    }
}

public class PullRequestServiceTests
{
    private readonly PullRequestService _service;
    private readonly INotificationService _notifications;

    public PullRequestServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        _notifications = Substitute.For<INotificationService>();
        var activityService = Substitute.For<IActivityService>();
        var adminService = Substitute.For<IAdminService>();
        var config = Substitute.For<IConfiguration>();
        _service = new PullRequestService(factory, NullLogger<PullRequestService>.Instance, _notifications, activityService, adminService, config);
    }

    [Fact]
    public async Task CreatePullRequestAsync_AssignsSequentialNumbers()
    {
        var pr1 = await _service.CreatePullRequestAsync("repo", "First PR", null, "alice", "feature-1", "main");
        var pr2 = await _service.CreatePullRequestAsync("repo", "Second PR", null, "bob", "feature-2", "main");

        Assert.Equal(1, pr1.Number);
        Assert.Equal(2, pr2.Number);
    }

    [Fact]
    public async Task CreatePullRequestAsync_SetsDraftFlag()
    {
        var pr = await _service.CreatePullRequestAsync("repo", "Draft PR", null, "alice", "feature", "main", isDraft: true);

        Assert.True(pr.IsDraft);
    }

    [Fact]
    public async Task CreatePullRequestAsync_TriggersNotification()
    {
        await _service.CreatePullRequestAsync("repo", "New Feature", null, "alice", "feature", "main");

        await _notifications.Received(1).CreateNotificationAsync(
            Arg.Any<string>(),
            Arg.Is(NotificationType.PullRequestCreated),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is("repo"),
            Arg.Any<string?>()
        );
    }

    [Fact]
    public async Task GetPullRequestsAsync_ReturnsAllPRs()
    {
        await _service.CreatePullRequestAsync("repo", "PR 1", null, "alice", "f1", "main");
        await _service.CreatePullRequestAsync("repo", "PR 2", null, "bob", "f2", "main");

        var prs = await _service.GetPullRequestsAsync("repo");
        Assert.Equal(2, prs.Count);
    }

    [Fact]
    public async Task GetPullRequestsAsync_IsolatesRepositories()
    {
        await _service.CreatePullRequestAsync("repo-a", "PR A", null, "alice", "f1", "main");
        await _service.CreatePullRequestAsync("repo-b", "PR B", null, "bob", "f1", "main");

        var prsA = await _service.GetPullRequestsAsync("repo-a");
        var prsB = await _service.GetPullRequestsAsync("repo-b");

        Assert.Single(prsA);
        Assert.Single(prsB);
    }

    [Fact]
    public async Task GetPullRequestAsync_ReturnsSinglePR()
    {
        await _service.CreatePullRequestAsync("repo", "Target PR", "body text", "alice", "feature", "main");

        var pr = await _service.GetPullRequestAsync("repo", 1);
        Assert.NotNull(pr);
        Assert.Equal("Target PR", pr.Title);
        Assert.Equal("body text", pr.Body);
        Assert.Equal("feature", pr.SourceBranch);
        Assert.Equal("main", pr.TargetBranch);
    }

    [Fact]
    public async Task GetPullRequestAsync_ReturnsNull_WhenNotFound()
    {
        var pr = await _service.GetPullRequestAsync("repo", 999);
        Assert.Null(pr);
    }

    [Fact]
    public async Task MergePullRequestAsync_SetsMergedState()
    {
        await _service.CreatePullRequestAsync("repo", "PR", null, "alice", "feature", "main");

        var result = await _service.MergePullRequestAsync("repo", 1, "bob");
        Assert.True(result.Success);

        var pr = await _service.GetPullRequestAsync("repo", 1);
        Assert.Equal(PullRequestState.Merged, pr!.State);
        Assert.NotNull(pr.MergedAt);
        Assert.Equal("bob", pr.MergedBy);
    }

    [Fact]
    public async Task MergePullRequestAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.MergePullRequestAsync("repo", 999, "bob");
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MergePullRequestAsync_TriggersNotification()
    {
        await _service.CreatePullRequestAsync("repo", "PR", null, "alice", "feature", "main");
        await _service.MergePullRequestAsync("repo", 1, "bob");

        await _notifications.Received(1).CreateNotificationAsync(
            Arg.Any<string>(),
            Arg.Is(NotificationType.PullRequestMerged),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is("repo"),
            Arg.Any<string?>()
        );
    }

    [Fact]
    public async Task ClosePullRequestAsync_SetsClosedState()
    {
        await _service.CreatePullRequestAsync("repo", "PR", null, "alice", "feature", "main");

        var result = await _service.ClosePullRequestAsync("repo", 1);
        Assert.True(result);

        var pr = await _service.GetPullRequestAsync("repo", 1);
        Assert.Equal(PullRequestState.Closed, pr!.State);
        Assert.NotNull(pr.ClosedAt);
    }

    [Fact]
    public async Task ClosePullRequestAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.ClosePullRequestAsync("repo", 999);
        Assert.False(result);
    }

    [Fact]
    public async Task AddReviewAsync_AddsReview()
    {
        await _service.CreatePullRequestAsync("repo", "PR", null, "alice", "feature", "main");

        var result = await _service.AddReviewAsync("repo", 1, "bob", ReviewState.Approved, "Looks good!");
        Assert.True(result);

        var pr = await _service.GetPullRequestAsync("repo", 1);
        Assert.Single(pr!.Reviews);
        Assert.Equal("bob", pr.Reviews[0].Author);
        Assert.Equal(ReviewState.Approved, pr.Reviews[0].State);
        Assert.Equal("Looks good!", pr.Reviews[0].Body);
    }

    [Fact]
    public async Task AddReviewAsync_ReturnsFalse_WhenPRNotFound()
    {
        var result = await _service.AddReviewAsync("repo", 999, "bob", ReviewState.Approved);
        Assert.False(result);
    }

    [Fact]
    public async Task AddReviewAsync_TriggersNotification()
    {
        await _service.CreatePullRequestAsync("repo", "PR", null, "alice", "feature", "main");
        await _service.AddReviewAsync("repo", 1, "bob", ReviewState.ChangesRequested, "Needs work");

        await _notifications.Received(1).CreateNotificationAsync(
            Arg.Any<string>(),
            Arg.Is(NotificationType.PullRequestReview),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is("repo"),
            Arg.Any<string?>()
        );
    }
}

public class NotificationServiceTests
{
    private readonly NotificationService _service;
    private readonly TestDbContextFactory _factory;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var adminService = Substitute.For<IAdminService>();
        _service = new NotificationService(_factory, NullLogger<NotificationService>.Instance, httpClientFactory, adminService);
    }

    [Fact]
    public async Task CreateNotificationAsync_CreatesNotification()
    {
        await _service.CreateNotificationAsync("alice", NotificationType.IssueCreated, "New issue", "Bug report", "repo", "/repo/issues/1");

        var notifications = await _service.GetNotificationsAsync("alice");
        Assert.Single(notifications);
        Assert.Equal("New issue", notifications[0].Title);
        Assert.Equal("Bug report", notifications[0].Message);
        Assert.Equal("repo", notifications[0].RepoName);
        Assert.False(notifications[0].IsRead);
    }

    [Fact]
    public async Task GetNotificationsAsync_FiltersUnreadOnly()
    {
        await _service.CreateNotificationAsync("alice", NotificationType.IssueCreated, "N1", "msg1", "repo");
        await _service.CreateNotificationAsync("alice", NotificationType.IssueComment, "N2", "msg2", "repo");

        var all = await _service.GetNotificationsAsync("alice");
        Assert.Equal(2, all.Count);

        // Mark one as read
        await _service.MarkAsReadAsync("alice", all[0].Id);

        var unread = await _service.GetNotificationsAsync("alice", unreadOnly: true);
        Assert.Single(unread);
    }

    [Fact]
    public async Task GetNotificationsAsync_IsolatesUsers()
    {
        await _service.CreateNotificationAsync("alice", NotificationType.IssueCreated, "For Alice", "msg", "repo");
        await _service.CreateNotificationAsync("bob", NotificationType.IssueCreated, "For Bob", "msg", "repo");

        var aliceNotifs = await _service.GetNotificationsAsync("alice");
        var bobNotifs = await _service.GetNotificationsAsync("bob");

        Assert.Single(aliceNotifs);
        Assert.Single(bobNotifs);
        Assert.Equal("For Alice", aliceNotifs[0].Title);
        Assert.Equal("For Bob", bobNotifs[0].Title);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        await _service.CreateNotificationAsync("alice", NotificationType.IssueCreated, "N1", "msg", "repo");
        await _service.CreateNotificationAsync("alice", NotificationType.IssueComment, "N2", "msg", "repo");
        await _service.CreateNotificationAsync("alice", NotificationType.Mention, "N3", "msg", "repo");

        var count = await _service.GetUnreadCountAsync("alice");
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task MarkAsReadAsync_MarksSpecificNotification()
    {
        await _service.CreateNotificationAsync("alice", NotificationType.IssueCreated, "N1", "msg", "repo");
        await _service.CreateNotificationAsync("alice", NotificationType.IssueComment, "N2", "msg", "repo");

        var all = await _service.GetNotificationsAsync("alice");
        await _service.MarkAsReadAsync("alice", all[0].Id);

        var count = await _service.GetUnreadCountAsync("alice");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task MarkAsReadAsync_IgnoresOtherUsersNotifications()
    {
        await _service.CreateNotificationAsync("alice", NotificationType.IssueCreated, "N1", "msg", "repo");

        var aliceNotifs = await _service.GetNotificationsAsync("alice");

        // Bob tries to mark Alice's notification as read
        await _service.MarkAsReadAsync("bob", aliceNotifs[0].Id);

        var count = await _service.GetUnreadCountAsync("alice");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_MarksAllForUser()
    {
        await _service.CreateNotificationAsync("alice", NotificationType.IssueCreated, "N1", "msg", "repo");
        await _service.CreateNotificationAsync("alice", NotificationType.IssueComment, "N2", "msg", "repo");
        await _service.CreateNotificationAsync("alice", NotificationType.Mention, "N3", "msg", "repo");

        await _service.MarkAllAsReadAsync("alice");

        var count = await _service.GetUnreadCountAsync("alice");
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_DoesNotAffectOtherUsers()
    {
        await _service.CreateNotificationAsync("alice", NotificationType.IssueCreated, "N1", "msg", "repo");
        await _service.CreateNotificationAsync("bob", NotificationType.IssueCreated, "N2", "msg", "repo");

        await _service.MarkAllAsReadAsync("alice");

        Assert.Equal(0, await _service.GetUnreadCountAsync("alice"));
        Assert.Equal(1, await _service.GetUnreadCountAsync("bob"));
    }

    [Fact]
    public async Task DeleteNotificationAsync_RemovesNotification()
    {
        await _service.CreateNotificationAsync("alice", NotificationType.IssueCreated, "N1", "msg", "repo");

        var all = await _service.GetNotificationsAsync("alice");
        await _service.DeleteNotificationAsync("alice", all[0].Id);

        var remaining = await _service.GetNotificationsAsync("alice");
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task DeleteNotificationAsync_IgnoresOtherUsersNotifications()
    {
        await _service.CreateNotificationAsync("alice", NotificationType.IssueCreated, "N1", "msg", "repo");

        var aliceNotifs = await _service.GetNotificationsAsync("alice");
        await _service.DeleteNotificationAsync("bob", aliceNotifs[0].Id);

        var remaining = await _service.GetNotificationsAsync("alice");
        Assert.Single(remaining);
    }
}

public class WikiServiceTests
{
    private readonly WikiService _service;

    public WikiServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        _service = new WikiService(factory, NullLogger<WikiService>.Instance);
    }

    [Fact]
    public async Task CreatePageAsync_CreatesPageWithSlug()
    {
        var page = await _service.CreatePageAsync("repo", "Getting Started", "# Welcome", "alice");

        Assert.Equal("Getting Started", page.Title);
        Assert.Equal("getting-started", page.Slug);
        Assert.Equal("# Welcome", page.Content);
        Assert.Equal("alice", page.Author);
    }

    [Fact]
    public async Task CreatePageAsync_CreatesInitialRevision()
    {
        var page = await _service.CreatePageAsync("repo", "Home", "Content here", "alice");

        Assert.Single(page.Revisions);
        Assert.Equal("Initial page creation", page.Revisions[0].Message);
        Assert.Equal("Content here", page.Revisions[0].Content);
    }

    [Fact]
    public async Task CreatePageAsync_RejectsDuplicateSlug()
    {
        await _service.CreatePageAsync("repo", "Home Page", "content1", "alice");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreatePageAsync("repo", "Home Page", "content2", "bob"));
    }

    [Fact]
    public async Task GetPagesAsync_ReturnsAllPages()
    {
        await _service.CreatePageAsync("repo", "Home", "content1", "alice");
        await _service.CreatePageAsync("repo", "Setup", "content2", "bob");

        var pages = await _service.GetPagesAsync("repo");
        Assert.Equal(2, pages.Count);
    }

    [Fact]
    public async Task GetPagesAsync_IsolatesRepositories()
    {
        await _service.CreatePageAsync("repo-a", "Home", "content", "alice");
        await _service.CreatePageAsync("repo-b", "Home", "content", "bob");

        var pagesA = await _service.GetPagesAsync("repo-a");
        Assert.Single(pagesA);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsBySlug()
    {
        await _service.CreatePageAsync("repo", "Getting Started", "content", "alice");

        var page = await _service.GetPageAsync("repo", "getting-started");
        Assert.NotNull(page);
        Assert.Equal("Getting Started", page.Title);
    }

    [Fact]
    public async Task GetPageAsync_IsCaseInsensitive()
    {
        await _service.CreatePageAsync("repo", "Home", "content", "alice");

        var page = await _service.GetPageAsync("repo", "HOME");
        Assert.NotNull(page);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsNull_WhenNotFound()
    {
        var page = await _service.GetPageAsync("repo", "nonexistent");
        Assert.Null(page);
    }

    [Fact]
    public async Task UpdatePageAsync_UpdatesContentAndAddsRevision()
    {
        await _service.CreatePageAsync("repo", "Home", "original", "alice");

        var updated = await _service.UpdatePageAsync("repo", "home", "updated content", "bob", "Fixed typo");

        Assert.Equal("updated content", updated.Content);
        Assert.Equal(2, updated.Revisions.Count);
        Assert.Equal("Fixed typo", updated.Revisions[1].Message);
        Assert.Equal("bob", updated.Revisions[1].Author);
    }

    [Fact]
    public async Task UpdatePageAsync_ThrowsWhenPageNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdatePageAsync("repo", "nonexistent", "content", "alice", "msg"));
    }

    [Fact]
    public async Task DeletePageAsync_RemovesPage()
    {
        await _service.CreatePageAsync("repo", "Home", "content", "alice");

        await _service.DeletePageAsync("repo", "home");

        var page = await _service.GetPageAsync("repo", "home");
        Assert.Null(page);
    }

    [Fact]
    public async Task DeletePageAsync_NoOpWhenNotFound()
    {
        await _service.DeletePageAsync("repo", "nonexistent"); // Should not throw
    }

    [Fact]
    public async Task GetRevisionAsync_ReturnsSpecificRevision()
    {
        var page = await _service.CreatePageAsync("repo", "Home", "v1", "alice");
        await _service.UpdatePageAsync("repo", "home", "v2", "bob", "Update");

        var revision = await _service.GetRevisionAsync("repo", "home", page.Revisions[0].Id);
        Assert.NotNull(revision);
        Assert.Equal("v1", revision.Content);
    }

    [Fact]
    public async Task GetRevisionAsync_ReturnsNull_WhenNotFound()
    {
        await _service.CreatePageAsync("repo", "Home", "content", "alice");

        var revision = await _service.GetRevisionAsync("repo", "home", 9999);
        Assert.Null(revision);
    }

    [Fact]
    public async Task CreatePageAsync_GeneratesSlugFromSpecialCharacters()
    {
        var page = await _service.CreatePageAsync("repo", "My Cool Page!!", "content", "alice");

        Assert.Equal("my-cool-page", page.Slug);
    }
}

public class ProjectServiceTests
{
    private readonly ProjectService _service;

    public ProjectServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        _service = new ProjectService(factory, NullLogger<ProjectService>.Instance);
    }

    [Fact]
    public async Task CreateProjectAsync_CreatesWithDefaultColumns()
    {
        var project = await _service.CreateProjectAsync("repo", "Sprint 1", "First sprint", "alice");

        Assert.Equal("Sprint 1", project.Name);
        Assert.Equal("First sprint", project.Description);
        Assert.Equal("alice", project.Owner);
        Assert.Equal(ProjectState.Open, project.State);
        Assert.Equal(3, project.Columns.Count);
        Assert.Equal("To Do", project.Columns[0].Name);
        Assert.Equal("In Progress", project.Columns[1].Name);
        Assert.Equal("Done", project.Columns[2].Name);
    }

    [Fact]
    public async Task GetProjectsAsync_ReturnsAllProjects()
    {
        await _service.CreateProjectAsync("repo", "Project 1", null, "alice");
        await _service.CreateProjectAsync("repo", "Project 2", null, "bob");

        var projects = await _service.GetProjectsAsync("repo");
        Assert.Equal(2, projects.Count);
    }

    [Fact]
    public async Task GetProjectsAsync_IsolatesRepositories()
    {
        await _service.CreateProjectAsync("repo-a", "Proj A", null, "alice");
        await _service.CreateProjectAsync("repo-b", "Proj B", null, "bob");

        var projects = await _service.GetProjectsAsync("repo-a");
        Assert.Single(projects);
        Assert.Equal("Proj A", projects[0].Name);
    }

    [Fact]
    public async Task GetProjectAsync_ReturnsProjectWithColumnsAndCards()
    {
        var created = await _service.CreateProjectAsync("repo", "Board", null, "alice");

        var project = await _service.GetProjectAsync("repo", created.Id);
        Assert.NotNull(project);
        Assert.Equal("Board", project.Name);
        Assert.Equal(3, project.Columns.Count);
    }

    [Fact]
    public async Task GetProjectAsync_ReturnsNull_WhenNotFound()
    {
        var project = await _service.GetProjectAsync("repo", 999);
        Assert.Null(project);
    }

    [Fact]
    public async Task AddColumnAsync_AddsColumnWithCorrectOrder()
    {
        var project = await _service.CreateProjectAsync("repo", "Board", null, "alice");

        await _service.AddColumnAsync("repo", project.Id, "Review");

        var updated = await _service.GetProjectAsync("repo", project.Id);
        Assert.Equal(4, updated!.Columns.Count);
        Assert.Equal("Review", updated.Columns[3].Name);
        Assert.Equal(4, updated.Columns[3].Order);
    }

    [Fact]
    public async Task AddColumnAsync_ThrowsWhenProjectNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddColumnAsync("repo", 999, "Column"));
    }

    [Fact]
    public async Task AddCardAsync_AddsCardToColumn()
    {
        var project = await _service.CreateProjectAsync("repo", "Board", null, "alice");
        var columnId = project.Columns[0].Id;

        var card = await _service.AddCardAsync("repo", project.Id, columnId, "Task 1", "Do something", "alice");

        Assert.Equal("Task 1", card.Title);
        Assert.Equal("Do something", card.Note);
        Assert.Equal("alice", card.Creator);
        Assert.Equal(1, card.Order);
    }

    [Fact]
    public async Task AddCardAsync_AssignsSequentialOrder()
    {
        var project = await _service.CreateProjectAsync("repo", "Board", null, "alice");
        var columnId = project.Columns[0].Id;

        var card1 = await _service.AddCardAsync("repo", project.Id, columnId, "Task 1", null, "alice");
        var card2 = await _service.AddCardAsync("repo", project.Id, columnId, "Task 2", null, "alice");

        Assert.Equal(1, card1.Order);
        Assert.Equal(2, card2.Order);
    }

    [Fact]
    public async Task AddCardAsync_SupportsIssueAndPRTypes()
    {
        var project = await _service.CreateProjectAsync("repo", "Board", null, "alice");
        var columnId = project.Columns[0].Id;

        var issueCard = await _service.AddCardAsync("repo", project.Id, columnId, "Bug #1", null, "alice", CardType.Issue, issueNumber: 1);
        var prCard = await _service.AddCardAsync("repo", project.Id, columnId, "PR #1", null, "alice", CardType.PullRequest, prNumber: 1);

        Assert.Equal(CardType.Issue, issueCard.Type);
        Assert.Equal(1, issueCard.IssueNumber);
        Assert.Equal(CardType.PullRequest, prCard.Type);
        Assert.Equal(1, prCard.PullRequestNumber);
    }

    [Fact]
    public async Task DeleteCardAsync_RemovesCard()
    {
        var project = await _service.CreateProjectAsync("repo", "Board", null, "alice");
        var columnId = project.Columns[0].Id;
        var card = await _service.AddCardAsync("repo", project.Id, columnId, "Task", null, "alice");

        await _service.DeleteCardAsync("repo", project.Id, card.Id);

        var updated = await _service.GetProjectAsync("repo", project.Id);
        Assert.Empty(updated!.Columns[0].Cards);
    }

    [Fact]
    public async Task DeleteCardAsync_NoOpWhenNotFound()
    {
        var project = await _service.CreateProjectAsync("repo", "Board", null, "alice");
        await _service.DeleteCardAsync("repo", project.Id, 999); // Should not throw
    }

    [Fact]
    public async Task CloseProjectAsync_SetsClosedState()
    {
        var project = await _service.CreateProjectAsync("repo", "Board", null, "alice");

        await _service.CloseProjectAsync("repo", project.Id);

        var closed = await _service.GetProjectAsync("repo", project.Id);
        Assert.Equal(ProjectState.Closed, closed!.State);
        Assert.NotNull(closed.ClosedAt);
    }

    [Fact]
    public async Task ReopenProjectAsync_SetsOpenState()
    {
        var project = await _service.CreateProjectAsync("repo", "Board", null, "alice");
        await _service.CloseProjectAsync("repo", project.Id);

        await _service.ReopenProjectAsync("repo", project.Id);

        var reopened = await _service.GetProjectAsync("repo", project.Id);
        Assert.Equal(ProjectState.Open, reopened!.State);
        Assert.Null(reopened.ClosedAt);
    }
}

public class RepositoryServiceTests
{
    private readonly RepositoryService _service;
    private readonly INotificationService _notifications;
    private readonly TestDbContextFactory _factory;

    public RepositoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        _notifications = Substitute.For<INotificationService>();
        var activityService = Substitute.For<IActivityService>();
        _service = new RepositoryService(_factory, NullLogger<RepositoryService>.Instance, _notifications, activityService);
    }

    private async Task SeedRepo(string name)
    {
        using var db = _factory.CreateDbContext();
        db.Repositories.Add(new Repository
        {
            Name = name,
            Owner = "admin",
            Description = $"Test repo {name}",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetRepositoriesAsync_ReturnsAll()
    {
        await SeedRepo("repo1");
        await SeedRepo("repo2");

        var repos = await _service.GetRepositoriesAsync();
        Assert.Equal(2, repos.Count);
    }

    [Fact]
    public async Task GetRepositoryAsync_ReturnsByName()
    {
        await SeedRepo("MyRepo");

        var repo = await _service.GetRepositoryAsync("MyRepo");
        Assert.NotNull(repo);
        Assert.Equal("MyRepo", repo.Name);
    }

    [Fact]
    public async Task GetRepositoryAsync_IsCaseInsensitive()
    {
        await SeedRepo("MyRepo");

        var repo = await _service.GetRepositoryAsync("myrepo");
        Assert.NotNull(repo);
    }

    [Fact]
    public async Task GetRepositoryAsync_ReturnsNull_WhenNotFound()
    {
        var repo = await _service.GetRepositoryAsync("nonexistent");
        Assert.Null(repo);
    }

    [Fact]
    public async Task StarRepositoryAsync_StarsRepo()
    {
        await SeedRepo("repo");

        var result = await _service.StarRepositoryAsync("repo", "alice");
        Assert.True(result);

        Assert.True(await _service.IsStarredAsync("repo", "alice"));
    }

    [Fact]
    public async Task StarRepositoryAsync_IncrementsStarCount()
    {
        await SeedRepo("repo");

        await _service.StarRepositoryAsync("repo", "alice");

        var repo = await _service.GetRepositoryAsync("repo");
        Assert.Equal(1, repo!.Stars);
    }

    [Fact]
    public async Task StarRepositoryAsync_PreventsDuplicateStars()
    {
        await SeedRepo("repo");

        await _service.StarRepositoryAsync("repo", "alice");
        var duplicate = await _service.StarRepositoryAsync("repo", "alice");

        Assert.False(duplicate);

        var repo = await _service.GetRepositoryAsync("repo");
        Assert.Equal(1, repo!.Stars);
    }

    [Fact]
    public async Task StarRepositoryAsync_TriggersNotification()
    {
        await SeedRepo("repo");

        await _service.StarRepositoryAsync("repo", "alice");

        await _notifications.Received(1).CreateNotificationAsync(
            Arg.Any<string>(),
            Arg.Is(NotificationType.RepositoryStarred),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is("repo"),
            Arg.Any<string?>()
        );
    }

    [Fact]
    public async Task UnstarRepositoryAsync_RemovesStar()
    {
        await SeedRepo("repo");

        await _service.StarRepositoryAsync("repo", "alice");
        var result = await _service.UnstarRepositoryAsync("repo", "alice");
        Assert.True(result);

        Assert.False(await _service.IsStarredAsync("repo", "alice"));
    }

    [Fact]
    public async Task UnstarRepositoryAsync_DecrementsStarCount()
    {
        await SeedRepo("repo");

        await _service.StarRepositoryAsync("repo", "alice");
        await _service.UnstarRepositoryAsync("repo", "alice");

        var repo = await _service.GetRepositoryAsync("repo");
        Assert.Equal(0, repo!.Stars);
    }

    [Fact]
    public async Task UnstarRepositoryAsync_ReturnsFalse_WhenNotStarred()
    {
        await SeedRepo("repo");

        var result = await _service.UnstarRepositoryAsync("repo", "alice");
        Assert.False(result);
    }

    [Fact]
    public async Task IsStarredAsync_ReturnsFalse_WhenNotStarred()
    {
        Assert.False(await _service.IsStarredAsync("repo", "alice"));
    }
}

public class BranchProtectionServiceTests
{
    private readonly BranchProtectionService _service;

    public BranchProtectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        _service = new BranchProtectionService(factory, NullLogger<BranchProtectionService>.Instance);
    }

    [Fact]
    public async Task AddRuleAsync_CreatesRule()
    {
        var rule = await _service.AddRuleAsync("repo", new BranchProtectionRule
        {
            BranchPattern = "main",
            RequirePullRequest = true,
            RequiredApprovals = 2,
            PreventForcePush = true
        });

        Assert.Equal("repo", rule.RepoName);
        Assert.Equal("main", rule.BranchPattern);
        Assert.True(rule.RequirePullRequest);
        Assert.Equal(2, rule.RequiredApprovals);
        Assert.True(rule.PreventForcePush);
    }

    [Fact]
    public async Task GetRulesAsync_ReturnsAllRulesForRepo()
    {
        await _service.AddRuleAsync("repo", new BranchProtectionRule { BranchPattern = "main" });
        await _service.AddRuleAsync("repo", new BranchProtectionRule { BranchPattern = "release/*" });

        var rules = await _service.GetRulesAsync("repo");
        Assert.Equal(2, rules.Count);
    }

    [Fact]
    public async Task GetRulesAsync_IsolatesRepositories()
    {
        await _service.AddRuleAsync("repo-a", new BranchProtectionRule { BranchPattern = "main" });
        await _service.AddRuleAsync("repo-b", new BranchProtectionRule { BranchPattern = "main" });

        var rules = await _service.GetRulesAsync("repo-a");
        Assert.Single(rules);
    }

    [Fact]
    public async Task UpdateRuleAsync_UpdatesExistingRule()
    {
        var rule = await _service.AddRuleAsync("repo", new BranchProtectionRule
        {
            BranchPattern = "main",
            RequiredApprovals = 1
        });

        rule.RequiredApprovals = 3;
        rule.PreventForcePush = true;
        var updated = await _service.UpdateRuleAsync("repo", rule);

        Assert.NotNull(updated);
        Assert.Equal(3, updated.RequiredApprovals);
        Assert.True(updated.PreventForcePush);
    }

    [Fact]
    public async Task UpdateRuleAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _service.UpdateRuleAsync("repo", new BranchProtectionRule
        {
            Id = 999,
            BranchPattern = "main"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteRuleAsync_RemovesRule()
    {
        var rule = await _service.AddRuleAsync("repo", new BranchProtectionRule { BranchPattern = "main" });

        var result = await _service.DeleteRuleAsync("repo", rule.Id);
        Assert.True(result);

        var rules = await _service.GetRulesAsync("repo");
        Assert.Empty(rules);
    }

    [Fact]
    public async Task DeleteRuleAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.DeleteRuleAsync("repo", 999);
        Assert.False(result);
    }

    [Fact]
    public async Task IsBranchProtectedAsync_ReturnsTrueForExactMatch()
    {
        await _service.AddRuleAsync("repo", new BranchProtectionRule { BranchPattern = "main" });

        Assert.True(await _service.IsBranchProtectedAsync("repo", "main"));
    }

    [Fact]
    public async Task IsBranchProtectedAsync_ReturnsFalseForNoMatch()
    {
        await _service.AddRuleAsync("repo", new BranchProtectionRule { BranchPattern = "main" });

        Assert.False(await _service.IsBranchProtectedAsync("repo", "develop"));
    }

    [Fact]
    public async Task IsBranchProtectedAsync_SupportsWildcardPatterns()
    {
        await _service.AddRuleAsync("repo", new BranchProtectionRule { BranchPattern = "release/*" });

        Assert.True(await _service.IsBranchProtectedAsync("repo", "release/1.0"));
        Assert.True(await _service.IsBranchProtectedAsync("repo", "release/2.0-beta"));
        Assert.False(await _service.IsBranchProtectedAsync("repo", "feature/new"));
    }

    [Fact]
    public async Task GetMatchingRuleAsync_ReturnsMatchingRule()
    {
        await _service.AddRuleAsync("repo", new BranchProtectionRule
        {
            BranchPattern = "main",
            RequiredApprovals = 2
        });

        var rule = await _service.GetMatchingRuleAsync("repo", "main");
        Assert.NotNull(rule);
        Assert.Equal(2, rule.RequiredApprovals);
    }

    [Fact]
    public async Task GetMatchingRuleAsync_ReturnsNull_WhenNoMatch()
    {
        var rule = await _service.GetMatchingRuleAsync("repo", "main");
        Assert.Null(rule);
    }
}

public class WorkflowServiceTests
{
    private readonly WorkflowService _service;

    public WorkflowServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        _service = new WorkflowService(factory, NullLogger<WorkflowService>.Instance);
    }

    [Fact]
    public async Task CreateWorkflowRunAsync_CreatesWithQueuedStatus()
    {
        var run = await _service.CreateWorkflowRunAsync("repo", "CI", "main", "abc123", "Initial commit", "alice");

        Assert.Equal("repo", run.RepoName);
        Assert.Equal("CI", run.WorkflowName);
        Assert.Equal("main", run.Branch);
        Assert.Equal("abc123", run.CommitSha);
        Assert.Equal("alice", run.TriggeredBy);
        Assert.Equal(WorkflowStatus.Queued, run.Status);
    }

    [Fact]
    public async Task GetWorkflowRunsAsync_ReturnsAllRuns()
    {
        await _service.CreateWorkflowRunAsync("repo", "CI", "main", "abc", "msg1", "alice");
        await _service.CreateWorkflowRunAsync("repo", "Deploy", "main", "def", "msg2", "bob");

        var runs = await _service.GetWorkflowRunsAsync("repo");
        Assert.Equal(2, runs.Count);
    }

    [Fact]
    public async Task GetWorkflowRunsAsync_IsolatesRepositories()
    {
        await _service.CreateWorkflowRunAsync("repo-a", "CI", "main", "abc", "msg", "alice");
        await _service.CreateWorkflowRunAsync("repo-b", "CI", "main", "def", "msg", "bob");

        var runs = await _service.GetWorkflowRunsAsync("repo-a");
        Assert.Single(runs);
    }

    [Fact]
    public async Task GetWorkflowRunAsync_ReturnsSingleRun()
    {
        var created = await _service.CreateWorkflowRunAsync("repo", "CI", "main", "abc", "msg", "alice");

        var run = await _service.GetWorkflowRunAsync("repo", created.Id);
        Assert.NotNull(run);
        Assert.Equal("CI", run.WorkflowName);
    }

    [Fact]
    public async Task GetWorkflowRunAsync_ReturnsNull_WhenNotFound()
    {
        var run = await _service.GetWorkflowRunAsync("repo", 999);
        Assert.Null(run);
    }

    [Fact]
    public async Task UpdateWorkflowRunAsync_UpdatesViaAction()
    {
        var created = await _service.CreateWorkflowRunAsync("repo", "CI", "main", "abc", "msg", "alice");

        var result = await _service.UpdateWorkflowRunAsync("repo", created.Id, run =>
        {
            run.Status = WorkflowStatus.Success;
            run.CompletedAt = DateTime.UtcNow;
        });

        Assert.True(result);

        var updated = await _service.GetWorkflowRunAsync("repo", created.Id);
        Assert.Equal(WorkflowStatus.Success, updated!.Status);
        Assert.NotNull(updated.CompletedAt);
    }

    [Fact]
    public async Task UpdateWorkflowRunAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.UpdateWorkflowRunAsync("repo", 999, _ => { });
        Assert.False(result);
    }

    [Fact]
    public async Task CreateWebhookAsync_CreatesActiveWebhook()
    {
        var webhook = await _service.CreateWebhookAsync("repo", "https://example.com/hook", "secret123",
            new List<string> { "push", "pull_request" });

        Assert.Equal("repo", webhook.RepoName);
        Assert.Equal("https://example.com/hook", webhook.Url);
        Assert.Equal("secret123", webhook.Secret);
        Assert.True(webhook.IsActive);
        Assert.Equal(2, webhook.Events.Count);
    }

    [Fact]
    public async Task GetWebhooksAsync_ReturnsAllForRepo()
    {
        await _service.CreateWebhookAsync("repo", "https://a.com", "s1", new List<string> { "push" });
        await _service.CreateWebhookAsync("repo", "https://b.com", "s2", new List<string> { "push" });

        var webhooks = await _service.GetWebhooksAsync("repo");
        Assert.Equal(2, webhooks.Count);
    }

    [Fact]
    public async Task ToggleWebhookAsync_TogglesActiveState()
    {
        var webhook = await _service.CreateWebhookAsync("repo", "https://a.com", "s", new List<string> { "push" });
        Assert.True(webhook.IsActive);

        await _service.ToggleWebhookAsync("repo", webhook.Id);
        var webhooks = await _service.GetWebhooksAsync("repo");
        Assert.False(webhooks[0].IsActive);

        await _service.ToggleWebhookAsync("repo", webhook.Id);
        webhooks = await _service.GetWebhooksAsync("repo");
        Assert.True(webhooks[0].IsActive);
    }

    [Fact]
    public async Task ToggleWebhookAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.ToggleWebhookAsync("repo", 999);
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteWebhookAsync_RemovesWebhook()
    {
        var webhook = await _service.CreateWebhookAsync("repo", "https://a.com", "s", new List<string> { "push" });

        var result = await _service.DeleteWebhookAsync("repo", webhook.Id);
        Assert.True(result);

        var remaining = await _service.GetWebhooksAsync("repo");
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task DeleteWebhookAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.DeleteWebhookAsync("repo", 999);
        Assert.False(result);
    }
}

public class SecurityServiceTests
{
    private readonly SecurityService _service;

    public SecurityServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        _service = new SecurityService(factory, NullLogger<SecurityService>.Instance);
    }

    [Fact]
    public async Task CreateAdvisoryAsync_CreatesAsDraft()
    {
        var advisory = await _service.CreateAdvisoryAsync("repo", "XSS Vulnerability", "Found XSS in form",
            SecuritySeverity.High, "1.0-2.0", "alice");

        Assert.Equal("XSS Vulnerability", advisory.Title);
        Assert.Equal(SecuritySeverity.High, advisory.Severity);
        Assert.Equal(SecurityAdvisoryState.Draft, advisory.State);
        Assert.Equal("alice", advisory.Reporter);
    }

    [Fact]
    public async Task GetAdvisoriesAsync_ReturnsAllForRepo()
    {
        await _service.CreateAdvisoryAsync("repo", "Adv 1", "desc", SecuritySeverity.Low, "1.0", "alice");
        await _service.CreateAdvisoryAsync("repo", "Adv 2", "desc", SecuritySeverity.Critical, "2.0", "bob");

        var advisories = await _service.GetAdvisoriesAsync("repo");
        Assert.Equal(2, advisories.Count);
    }

    [Fact]
    public async Task GetAdvisoriesAsync_IsolatesRepositories()
    {
        await _service.CreateAdvisoryAsync("repo-a", "Adv A", "desc", SecuritySeverity.Low, "1.0", "alice");
        await _service.CreateAdvisoryAsync("repo-b", "Adv B", "desc", SecuritySeverity.Low, "1.0", "bob");

        var advisories = await _service.GetAdvisoriesAsync("repo-a");
        Assert.Single(advisories);
    }

    [Fact]
    public async Task PublishAdvisoryAsync_SetsPublishedState()
    {
        var advisory = await _service.CreateAdvisoryAsync("repo", "Advisory", "desc", SecuritySeverity.Medium, "1.0", "alice");

        var result = await _service.PublishAdvisoryAsync("repo", advisory.Id);
        Assert.True(result);

        var advisories = await _service.GetAdvisoriesAsync("repo");
        Assert.Equal(SecurityAdvisoryState.Published, advisories[0].State);
        Assert.NotNull(advisories[0].PublishedAt);
    }

    [Fact]
    public async Task PublishAdvisoryAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.PublishAdvisoryAsync("repo", 999);
        Assert.False(result);
    }

    [Fact]
    public async Task CloseAdvisoryAsync_SetsClosedStateWithPatch()
    {
        var advisory = await _service.CreateAdvisoryAsync("repo", "Advisory", "desc", SecuritySeverity.High, "1.0", "alice");

        var result = await _service.CloseAdvisoryAsync("repo", advisory.Id, "1.0.1");
        Assert.True(result);

        var advisories = await _service.GetAdvisoriesAsync("repo");
        Assert.Equal(SecurityAdvisoryState.Closed, advisories[0].State);
        Assert.NotNull(advisories[0].ClosedAt);
        Assert.Equal("1.0.1", advisories[0].PatchedVersions);
    }

    [Fact]
    public async Task CloseAdvisoryAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.CloseAdvisoryAsync("repo", 999);
        Assert.False(result);
    }

    [Fact]
    public async Task CreateScanAsync_CountsVulnerabilitiesBySeverity()
    {
        var dependencies = new List<Dependency>
        {
            new Dependency
            {
                Name = "vulnerable-lib",
                Version = "1.0",
                Type = "nuget",
                Vulnerabilities = new List<Vulnerability>
                {
                    new Vulnerability { VulnerabilityId = "CVE-1", Title = "Critical Bug", Description = "Bad", Severity = SecuritySeverity.Critical },
                    new Vulnerability { VulnerabilityId = "CVE-2", Title = "High Bug", Description = "Bad", Severity = SecuritySeverity.High }
                }
            },
            new Dependency
            {
                Name = "another-lib",
                Version = "2.0",
                Type = "nuget",
                Vulnerabilities = new List<Vulnerability>
                {
                    new Vulnerability { VulnerabilityId = "CVE-3", Title = "Low Bug", Description = "Minor", Severity = SecuritySeverity.Low }
                }
            }
        };

        var scan = await _service.CreateScanAsync("repo", dependencies);

        Assert.Equal(3, scan.VulnerabilitiesFound);
        Assert.Equal(1, scan.CriticalCount);
        Assert.Equal(1, scan.HighCount);
        Assert.Equal(0, scan.MediumCount);
        Assert.Equal(1, scan.LowCount);
    }

    [Fact]
    public async Task GetLatestScanAsync_ReturnsMostRecent()
    {
        await _service.CreateScanAsync("repo", new List<Dependency>());
        await Task.Delay(10);
        await _service.CreateScanAsync("repo", new List<Dependency>
        {
            new Dependency
            {
                Name = "lib",
                Version = "1.0",
                Type = "nuget",
                Vulnerabilities = new List<Vulnerability>
                {
                    new Vulnerability { VulnerabilityId = "CVE-1", Title = "Bug", Description = "d", Severity = SecuritySeverity.Medium }
                }
            }
        });

        var latest = await _service.GetLatestScanAsync("repo");
        Assert.NotNull(latest);
        Assert.Equal(1, latest.VulnerabilitiesFound);
    }

    [Fact]
    public async Task GetLatestScanAsync_ReturnsNull_WhenNoScans()
    {
        var scan = await _service.GetLatestScanAsync("repo");
        Assert.Null(scan);
    }

    [Fact]
    public async Task GetScansAsync_ReturnsAllScans()
    {
        await _service.CreateScanAsync("repo", new List<Dependency>());
        await _service.CreateScanAsync("repo", new List<Dependency>());

        var scans = await _service.GetScansAsync("repo");
        Assert.Equal(2, scans.Count);
    }
}

public class AdminServiceTests
{
    private readonly AdminService _service;
    private readonly TestDbContextFactory _factory;

    public AdminServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProjectRoot"] = "/tmp/repos",
                ["Git:RequireAuth"] = "true"
            })
            .Build();
        _service = new AdminService(_factory, config, NullLogger<AdminService>.Instance);
    }

    [Fact]
    public async Task GetSystemSettingsAsync_CreatesDefaultWhenNoneExist()
    {
        var settings = await _service.GetSystemSettingsAsync();

        Assert.NotNull(settings);
        Assert.Equal("/tmp/repos", settings.ProjectRoot);
        Assert.True(settings.RequireAuth);
    }

    [Fact]
    public async Task SaveSystemSettingsAsync_PersistsSettings()
    {
        var settings = await _service.GetSystemSettingsAsync();
        settings.ProjectRoot = "/new/path";
        settings.RequireAuth = false;

        await _service.SaveSystemSettingsAsync(settings);

        var loaded = await _service.GetSystemSettingsAsync();
        Assert.Equal("/new/path", loaded.ProjectRoot);
        Assert.False(loaded.RequireAuth);
    }

    [Fact]
    public async Task AddAuditLogAsync_CreatesLogEntry()
    {
        await _service.AddAuditLogAsync("admin", "settings.update", "Changed project root", "127.0.0.1");

        var logs = await _service.GetAuditLogsAsync();
        Assert.Single(logs);
        Assert.Equal("admin", logs[0].Username);
        Assert.Equal("settings.update", logs[0].Action);
        Assert.Equal("Changed project root", logs[0].Details);
        Assert.Equal("127.0.0.1", logs[0].IpAddress);
    }

    [Fact]
    public async Task GetAuditLogsAsync_RespectsLimit()
    {
        for (int i = 0; i < 5; i++)
            await _service.AddAuditLogAsync("admin", $"action-{i}", "details");

        var logs = await _service.GetAuditLogsAsync(limit: 3);
        Assert.Equal(3, logs.Count);
    }

    [Fact]
    public async Task AddUserAsync_CreatesUser()
    {
        var result = await _service.AddUserAsync("newuser", "password123", "new@test.com");
        Assert.True(result);

        var users = await _service.GetAllUsersAsync();
        Assert.Single(users);
        Assert.Equal("newuser", users[0].Username);
        Assert.Equal("new@test.com", users[0].Email);
    }

    [Fact]
    public async Task AddUserAsync_RejectsDuplicateUsername()
    {
        await _service.AddUserAsync("alice", "pass1", "alice@test.com");
        var result = await _service.AddUserAsync("alice", "pass2", "other@test.com");

        Assert.False(result);
    }

    [Fact]
    public async Task AddUserAsync_SupportsAdminFlag()
    {
        await _service.AddUserAsync("superadmin", "pass", "admin@test.com", isAdmin: true);

        var users = await _service.GetAllUsersAsync();
        Assert.True(users[0].IsAdmin);
    }

    [Fact]
    public async Task DeleteUserAsync_RemovesUser()
    {
        await _service.AddUserAsync("alice", "pass", "alice@test.com");

        var result = await _service.DeleteUserAsync("alice");
        Assert.True(result);

        var users = await _service.GetAllUsersAsync();
        Assert.Empty(users);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.DeleteUserAsync("nonexistent");
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesUserFields()
    {
        await _service.AddUserAsync("alice", "pass", "alice@test.com");

        var result = await _service.UpdateUserAsync(new UserManagement
        {
            Username = "alice",
            Email = "newemail@test.com",
            IsAdmin = true,
            IsActive = false
        });

        Assert.True(result);

        var users = await _service.GetAllUsersAsync();
        Assert.Equal("newemail@test.com", users[0].Email);
        Assert.True(users[0].IsAdmin);
        Assert.False(users[0].IsActive);
    }

    [Fact]
    public async Task UpdateUserAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.UpdateUserAsync(new UserManagement
        {
            Username = "nonexistent",
            Email = "test@test.com"
        });

        Assert.False(result);
    }

    [Fact]
    public async Task GetSystemStatisticsAsync_ReturnsCounts()
    {
        using var db = _factory.CreateDbContext();
        db.Users.Add(new User { Username = "alice", Email = "a@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new User { Username = "bob", Email = "b@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow });
        db.Issues.Add(new Issue { RepoName = "repo", Number = 1, Title = "Bug", Author = "alice", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var stats = await _service.GetSystemStatisticsAsync();
        Assert.Equal(2, stats.TotalUsers);
        Assert.Equal(1, stats.TotalIssues);
    }
}
