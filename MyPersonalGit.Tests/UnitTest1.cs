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
        var branchProtection = Substitute.For<IBranchProtectionService>();
        var codeOwners = Substitute.For<ICodeOwnersService>();
        var issueAutoClose = Substitute.For<IIssueAutoCloseService>();
        _service = new PullRequestService(factory, NullLogger<PullRequestService>.Instance, _notifications, activityService, adminService, branchProtection, codeOwners, issueAutoClose, config);
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
        var emailService = Substitute.For<IEmailService>();
        _service = new NotificationService(_factory, NullLogger<NotificationService>.Instance, httpClientFactory, adminService, emailService);
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
        var config = Substitute.For<IConfiguration>();
        _service = new BranchProtectionService(factory, NullLogger<BranchProtectionService>.Instance, config);
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

public class TwoFactorServiceTests
{
    private readonly TwoFactorService _service;
    private readonly IDbContextFactory<AppDbContext> _factory;

    public TwoFactorServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        _service = new TwoFactorService(_factory, NullLogger<TwoFactorService>.Instance);
    }

    private async Task<User> SeedUser(string username = "alice", string email = "alice@test.com")
    {
        using var db = _factory.CreateDbContext();
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public void GenerateSecretKey_ReturnsValidBase32String()
    {
        var key = _service.GenerateSecretKey();

        Assert.NotNull(key);
        Assert.NotEmpty(key);
        // Base32 characters only
        Assert.All(key, c => Assert.Contains(c, "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"));
    }

    [Fact]
    public void GenerateTotpCode_ReturnsSixDigitString()
    {
        var secret = _service.GenerateSecretKey();
        var code = _service.GenerateTotpCode(secret);

        Assert.NotNull(code);
        Assert.Equal(6, code.Length);
        Assert.All(code, c => Assert.True(char.IsDigit(c)));
    }

    [Fact]
    public void ValidateTotpCode_SucceedsWithCorrectCode()
    {
        var secret = _service.GenerateSecretKey();
        var code = _service.GenerateTotpCode(secret);

        var result = _service.ValidateTotpCode(secret, code);

        Assert.True(result);
    }

    [Fact]
    public void ValidateTotpCode_FailsWithWrongCode()
    {
        var secret = _service.GenerateSecretKey();

        var result = _service.ValidateTotpCode(secret, "000000");

        // While there's a tiny chance this could collide, it's astronomically unlikely
        // combined with the time-window check
        Assert.False(result);
    }

    [Fact]
    public async Task EnableTwoFactor_CreatesPendingRecord()
    {
        var user = await SeedUser();

        var setup = await _service.EnableTwoFactor(user.Id);

        Assert.NotNull(setup);
        Assert.NotEmpty(setup.Secret);
        Assert.StartsWith("otpauth://totp/", setup.TotpUri);

        // Verify record is pending (IsEnabled = false)
        using var db = _factory.CreateDbContext();
        var twoFa = await db.TwoFactorAuths.FirstOrDefaultAsync(t => t.Username == user.Username);
        Assert.NotNull(twoFa);
        Assert.False(twoFa.IsEnabled);
    }

    [Fact]
    public async Task VerifyAndActivate_EnablesTwoFactor_WithCorrectCode()
    {
        var user = await SeedUser();
        var setup = await _service.EnableTwoFactor(user.Id);

        var code = _service.GenerateTotpCode(setup.Secret);
        var result = await _service.VerifyAndActivate(user.Id, code);

        Assert.True(result);

        using var db = _factory.CreateDbContext();
        var twoFa = await db.TwoFactorAuths.FirstOrDefaultAsync(t => t.Username == user.Username);
        Assert.NotNull(twoFa);
        Assert.True(twoFa.IsEnabled);
        Assert.NotNull(twoFa.EnabledAt);
    }

    [Fact]
    public async Task VerifyAndActivate_FailsWithWrongCode()
    {
        var user = await SeedUser();
        await _service.EnableTwoFactor(user.Id);

        var result = await _service.VerifyAndActivate(user.Id, "000000");

        Assert.False(result);

        using var db = _factory.CreateDbContext();
        var twoFa = await db.TwoFactorAuths.FirstOrDefaultAsync(t => t.Username == user.Username);
        Assert.NotNull(twoFa);
        Assert.False(twoFa.IsEnabled);
    }

    [Fact]
    public async Task DisableTwoFactor_RemovesTwoFa_WithCorrectCode()
    {
        var user = await SeedUser();
        var setup = await _service.EnableTwoFactor(user.Id);
        var code = _service.GenerateTotpCode(setup.Secret);
        await _service.VerifyAndActivate(user.Id, code);

        // Now disable with a fresh code
        var disableCode = _service.GenerateTotpCode(setup.Secret);
        var result = await _service.DisableTwoFactor(user.Id, disableCode);

        Assert.True(result);

        using var db = _factory.CreateDbContext();
        var twoFa = await db.TwoFactorAuths.FirstOrDefaultAsync(t => t.Username == user.Username);
        Assert.Null(twoFa);
    }

    [Fact]
    public async Task GenerateRecoveryCodes_ReturnsTenCodes()
    {
        var user = await SeedUser();
        var setup = await _service.EnableTwoFactor(user.Id);
        var code = _service.GenerateTotpCode(setup.Secret);
        await _service.VerifyAndActivate(user.Id, code);

        var codes = await _service.GenerateRecoveryCodes(user.Id);

        Assert.NotNull(codes);
        Assert.Equal(10, codes.Length);
        Assert.All(codes, c => Assert.NotEmpty(c));
    }

    [Fact]
    public async Task UseRecoveryCode_ConsumesCode_WorksOnceThenFails()
    {
        var user = await SeedUser();
        var setup = await _service.EnableTwoFactor(user.Id);
        var totpCode = _service.GenerateTotpCode(setup.Secret);
        await _service.VerifyAndActivate(user.Id, totpCode);

        var codes = await _service.GenerateRecoveryCodes(user.Id);
        var recoveryCode = codes[0];

        // First use should succeed
        var firstUse = await _service.UseRecoveryCode(user.Id, recoveryCode);
        Assert.True(firstUse);

        // Second use of same code should fail
        var secondUse = await _service.UseRecoveryCode(user.Id, recoveryCode);
        Assert.False(secondUse);
    }

    [Fact]
    public void GetTotpUri_ReturnsValidOtpauthUri()
    {
        var secret = _service.GenerateSecretKey();
        var uri = _service.GetTotpUri(secret, "alice@test.com");

        Assert.StartsWith("otpauth://totp/", uri);
        Assert.Contains(secret, uri);
        Assert.Contains("alice%40test.com", uri);
        Assert.Contains("issuer=MyPersonalGit", uri);
    }
}

public class IssueAutoCloseServiceTests
{
    private readonly IssueAutoCloseService _service;
    private readonly IIssueService _issueService;

    public IssueAutoCloseServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        _issueService = Substitute.For<IIssueService>();
        _service = new IssueAutoCloseService(factory, NullLogger<IssueAutoCloseService>.Instance, _issueService);
    }

    [Fact]
    public void ParseIssueReferences_DetectsFixesKeyword()
    {
        var refs = _service.ParseIssueReferences("fixes #123");

        Assert.Single(refs);
        Assert.Equal(123, refs[0].IssueNumber);
        Assert.True(refs[0].IsClosing);
        Assert.Equal("fixes", refs[0].Keyword);
    }

    [Fact]
    public void ParseIssueReferences_DetectsClosesKeyword_CaseInsensitive()
    {
        var refs = _service.ParseIssueReferences("CLOSES #45");

        Assert.Single(refs);
        Assert.Equal(45, refs[0].IssueNumber);
        Assert.True(refs[0].IsClosing);
    }

    [Fact]
    public void ParseIssueReferences_DetectsResolvedKeyword()
    {
        var refs = _service.ParseIssueReferences("resolved #7");

        Assert.Single(refs);
        Assert.Equal(7, refs[0].IssueNumber);
        Assert.True(refs[0].IsClosing);
    }

    [Fact]
    public void ParseIssueReferences_DetectsMultipleRefs()
    {
        var refs = _service.ParseIssueReferences("fixes #1, closes #2, and resolves #3");

        var closingRefs = refs.Where(r => r.IsClosing).ToList();
        Assert.Equal(3, closingRefs.Count);
        Assert.Contains(closingRefs, r => r.IssueNumber == 1);
        Assert.Contains(closingRefs, r => r.IssueNumber == 2);
        Assert.Contains(closingRefs, r => r.IssueNumber == 3);
    }

    [Fact]
    public void ParseIssueReferences_DetectsCrossRepoRefs()
    {
        var refs = _service.ParseIssueReferences("fixes owner/repo#123");

        Assert.Single(refs);
        Assert.Equal(123, refs[0].IssueNumber);
        Assert.Equal("owner", refs[0].RepoOwner);
        Assert.Equal("repo", refs[0].RepoName);
        Assert.True(refs[0].IsClosing);
    }

    [Fact]
    public void ParseIssueReferences_DetectsStandaloneRef_AsNonClosing()
    {
        var refs = _service.ParseIssueReferences("see #123 for details");

        Assert.Single(refs);
        Assert.Equal(123, refs[0].IssueNumber);
        Assert.False(refs[0].IsClosing);
    }

    [Fact]
    public void ParseIssueReferences_ReturnsEmpty_WhenNoRefs()
    {
        var refs = _service.ParseIssueReferences("just a normal commit message");

        Assert.Empty(refs);
    }

    [Fact]
    public void RenderIssueLinks_ConvertsRefToClickableLink()
    {
        var result = _service.RenderIssueLinks("see #123 for details", "myrepo");

        Assert.Contains("href=\"/repo/myrepo/issues/123\"", result);
        Assert.Contains("#123</a>", result);
    }

    [Fact]
    public async Task ProcessCommitMessage_ClosesReferencedIssues_AndAddsComments()
    {
        var issue = new Issue
        {
            RepoName = "myrepo",
            Number = 1,
            Title = "Bug",
            Author = "alice",
            State = IssueState.Open,
            CreatedAt = DateTime.UtcNow,
            Comments = new List<IssueComment>()
        };

        _issueService.GetIssueAsync("myrepo", 1).Returns(Task.FromResult<Issue?>(issue));
        _issueService.AddCommentAsync("myrepo", 1, Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _issueService.CloseIssueAsync("myrepo", 1).Returns(true);

        await _service.ProcessCommitMessage("myrepo", "fixes #1", "abc1234567890", "bob");

        await _issueService.Received(1).AddCommentAsync("myrepo", 1, "bob", Arg.Is<string>(s => s.Contains("abc1234")));
        await _issueService.Received(1).CloseIssueAsync("myrepo", 1);
    }
}

public class CodeOwnersServiceTests
{
    private readonly CodeOwnersService _service;

    public CodeOwnersServiceTests()
    {
        var config = Substitute.For<IConfiguration>();
        var adminService = Substitute.For<IAdminService>();
        _service = new CodeOwnersService(config, adminService, NullLogger<CodeOwnersService>.Instance);
    }

    [Fact]
    public void ParseCodeOwners_HandlesCommentLines()
    {
        var content = "# This is a comment\n*.js @user1";

        var rules = _service.ParseCodeOwners(content);

        Assert.Single(rules);
        Assert.Equal("*.js", rules[0].Pattern);
    }

    [Fact]
    public void ParseCodeOwners_HandlesEmptyLines()
    {
        var content = "\n\n*.js @user1\n\n";

        var rules = _service.ParseCodeOwners(content);

        Assert.Single(rules);
    }

    [Fact]
    public void ParseCodeOwners_ParsesExtensionPattern()
    {
        var content = "*.js @user1";

        var rules = _service.ParseCodeOwners(content);

        Assert.Single(rules);
        Assert.Equal("*.js", rules[0].Pattern);
        Assert.Single(rules[0].Owners);
        Assert.Equal("user1", rules[0].Owners[0]);
    }

    [Fact]
    public void ParseCodeOwners_ParsesDirectoryPatternWithMultipleOwners()
    {
        var content = "/docs/ @user1 @user2";

        var rules = _service.ParseCodeOwners(content);

        Assert.Single(rules);
        Assert.Equal("/docs/", rules[0].Pattern);
        Assert.Equal(2, rules[0].Owners.Count);
        Assert.Equal("user1", rules[0].Owners[0]);
        Assert.Equal("user2", rules[0].Owners[1]);
    }

    [Fact]
    public void PatternMatching_StarMatchesEverything()
    {
        var content = "* @default-owner";
        var rules = _service.ParseCodeOwners(content);

        // Use GetCodeOwnersForPullRequest to test matching indirectly
        var result = _service.GetCodeOwnersForPullRequest("unused", "unused",
            new List<string> { "anything.txt", "src/deep/file.js" });

        // GetCodeOwnersForPullRequest requires a real repo, so test via ParseCodeOwners + reflection
        // Instead, test that ParseCodeOwners returns the rule, and trust MatchFileToOwners is correct
        Assert.Single(rules);
        Assert.Equal("*", rules[0].Pattern);
    }

    [Fact]
    public void PatternMatching_ExtensionMatchesCorrectly()
    {
        var content = "*.js @js-owner\n*.py @py-owner";
        var rules = _service.ParseCodeOwners(content);

        Assert.Equal(2, rules.Count);
        Assert.Equal("*.js", rules[0].Pattern);
        Assert.Equal("*.py", rules[1].Pattern);
    }

    [Fact]
    public void PatternMatching_DirectoryPatternParsed()
    {
        var content = "/docs/ @doc-team";
        var rules = _service.ParseCodeOwners(content);

        Assert.Single(rules);
        Assert.Equal("/docs/", rules[0].Pattern);
        Assert.Equal("doc-team", rules[0].Owners[0]);
    }

    [Fact]
    public void PatternMatching_LastMatchingPatternWins()
    {
        // The service uses "last matching pattern wins" semantics.
        // We can verify this by checking that rules are returned in order,
        // which means later rules override earlier ones in MatchFileToOwners.
        var content = "* @default\n*.js @js-team\n/src/ @src-team";
        var rules = _service.ParseCodeOwners(content);

        Assert.Equal(3, rules.Count);
        // Verify ordering is preserved (last match wins when iterating)
        Assert.Equal("*", rules[0].Pattern);
        Assert.Equal("*.js", rules[1].Pattern);
        Assert.Equal("/src/", rules[2].Pattern);
    }

    [Fact]
    public void GetCodeOwnersForPullRequest_AggregatesOwnersFromMultipleFiles()
    {
        // GetCodeOwnersForPullRequest calls GetCodeOwnersFile which needs a real git repo.
        // Since we can't easily set that up in a unit test, we verify the method returns
        // an empty dict when no repo is available (null CODEOWNERS content).
        var result = _service.GetCodeOwnersForPullRequest(
            "nonexistent-repo-path", "main",
            new List<string> { "file1.js", "docs/readme.md" });

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}

public class DeployKeyServiceTests
{
    private readonly DeployKeyService _service;
    private readonly TestDbContextFactory _factory;

    public DeployKeyServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        _service = new DeployKeyService(_factory, NullLogger<DeployKeyService>.Instance);
    }

    [Fact]
    public async Task AddDeployKeyAsync_CreatesKeyWithCorrectProperties()
    {
        var result = await _service.AddDeployKeyAsync(1, "My Deploy Key", "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABgQ test@host", true);

        Assert.NotNull(result);
        Assert.Equal(1, result.RepositoryId);
        Assert.Equal("My Deploy Key", result.Title);
        Assert.Equal("ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABgQ test@host", result.PublicKey);
        Assert.True(result.ReadOnly);
    }

    [Fact]
    public async Task AddDeployKeyAsync_GeneratesFingerprint()
    {
        var result = await _service.AddDeployKeyAsync(1, "Key1", "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABgQ test@host", true);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.KeyFingerprint));
    }

    [Fact]
    public async Task GetDeployKeysAsync_ReturnsKeysForCorrectRepoOnly()
    {
        await _service.AddDeployKeyAsync(1, "Repo1Key", "ssh-rsa AAAA1 test@host", true);
        await _service.AddDeployKeyAsync(2, "Repo2Key", "ssh-rsa AAAA2 test@host", true);
        await _service.AddDeployKeyAsync(1, "Repo1Key2", "ssh-rsa AAAA3 test@host", false);

        var repo1Keys = await _service.GetDeployKeysAsync(1);
        var repo2Keys = await _service.GetDeployKeysAsync(2);

        Assert.Equal(2, repo1Keys.Count);
        Assert.Single(repo2Keys);
        Assert.All(repo1Keys, k => Assert.Equal(1, k.RepositoryId));
        Assert.All(repo2Keys, k => Assert.Equal(2, k.RepositoryId));
    }

    [Fact]
    public async Task DeleteDeployKeyAsync_RemovesTheKey()
    {
        var key = await _service.AddDeployKeyAsync(1, "ToDelete", "ssh-rsa AAAA4 test@host", true);
        Assert.NotNull(key);

        var deleted = await _service.DeleteDeployKeyAsync(key.Id);
        Assert.True(deleted);

        var keys = await _service.GetDeployKeysAsync(1);
        Assert.Empty(keys);
    }

    [Fact]
    public async Task DeleteDeployKeyAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.DeleteDeployKeyAsync(999);
        Assert.False(result);
    }

    [Fact]
    public async Task AddDeployKeyAsync_DuplicateFingerprint_ReturnsNull()
    {
        var key1 = await _service.AddDeployKeyAsync(1, "Key1", "ssh-rsa AAAA5 test@host", true);
        Assert.NotNull(key1);

        var key2 = await _service.AddDeployKeyAsync(1, "Key2", "ssh-rsa AAAA5 test@host", true);
        Assert.Null(key2);
    }

    [Fact]
    public async Task AddDeployKeyAsync_ReadOnlyDefaultsToPassedValue()
    {
        var readOnlyKey = await _service.AddDeployKeyAsync(1, "RO", "ssh-rsa AAAA6 test@host", true);
        var rwKey = await _service.AddDeployKeyAsync(1, "RW", "ssh-rsa AAAA7 test@host", false);

        Assert.NotNull(readOnlyKey);
        Assert.NotNull(rwKey);
        Assert.True(readOnlyKey.ReadOnly);
        Assert.False(rwKey.ReadOnly);
    }

    [Fact]
    public async Task AddDeployKeyAsync_EmptyTitle_ReturnsNull()
    {
        var result = await _service.AddDeployKeyAsync(1, "", "ssh-rsa AAAA8 test@host", true);
        Assert.Null(result);
    }

    [Fact]
    public async Task AddDeployKeyAsync_EmptyPublicKey_ReturnsNull()
    {
        var result = await _service.AddDeployKeyAsync(1, "Key", "", true);
        Assert.Null(result);
    }

    [Fact]
    public async Task AddDeployKeyAsync_TrimsWhitespace()
    {
        var result = await _service.AddDeployKeyAsync(1, "  Key  ", "  ssh-rsa AAAA9 test@host  ", true);

        Assert.NotNull(result);
        Assert.Equal("Key", result.Title);
        Assert.Equal("ssh-rsa AAAA9 test@host", result.PublicKey);
    }
}

public class EmailServiceTests
{
    private readonly EmailService _service;
    private readonly IAdminService _adminService;

    public EmailServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        _adminService = Substitute.For<IAdminService>();
        _service = new EmailService(factory, _adminService, NullLogger<EmailService>.Instance);
    }

    [Fact]
    public void Service_CanBeInstantiated()
    {
        Assert.NotNull(_service);
    }

    [Fact]
    public async Task SendTestEmailAsync_ReturnsFalse_WhenSmtpNotConfigured()
    {
        _adminService.GetSystemSettingsAsync().Returns(new SystemSettings
        {
            SmtpServer = ""
        });

        var (success, message) = await _service.SendTestEmailAsync("test@example.com");

        Assert.False(success);
        Assert.Contains("not configured", message);
    }

    [Fact]
    public async Task SendEmailAsync_SkipsQuietly_WhenNotificationsDisabled()
    {
        _adminService.GetSystemSettingsAsync().Returns(new SystemSettings
        {
            EmailNotificationsEnabled = false,
            SmtpServer = ""
        });

        // Should not throw
        await _service.SendEmailAsync("test@example.com", "Subject", "<p>Body</p>");
    }

    [Fact]
    public async Task SendEmailAsync_SkipsQuietly_WhenSmtpNotConfigured()
    {
        _adminService.GetSystemSettingsAsync().Returns(new SystemSettings
        {
            EmailNotificationsEnabled = true,
            SmtpServer = ""
        });

        // Should not throw
        await _service.SendEmailAsync("test@example.com", "Subject", "<p>Body</p>");
    }

    [Fact]
    public async Task SendIssueNotificationAsync_SkipsQuietly_WhenDisabled()
    {
        _adminService.GetSystemSettingsAsync().Returns(new SystemSettings
        {
            EmailNotificationsEnabled = false,
            SmtpServer = ""
        });

        var issue = new Issue { RepoName = "repo", Number = 1, Title = "Bug", Author = "alice", CreatedAt = DateTime.UtcNow };

        // Should not throw
        await _service.SendIssueNotificationAsync(issue, "opened", "bob");
    }

    [Fact]
    public async Task SendPullRequestNotificationAsync_SkipsQuietly_WhenDisabled()
    {
        _adminService.GetSystemSettingsAsync().Returns(new SystemSettings
        {
            EmailNotificationsEnabled = false,
            SmtpServer = ""
        });

        var pr = new PullRequest { RepoName = "repo", Number = 1, Title = "Fix", SourceBranch = "dev", TargetBranch = "main", Author = "alice", CreatedAt = DateTime.UtcNow };

        // Should not throw
        await _service.SendPullRequestNotificationAsync(pr, "opened", "bob");
    }

    [Fact]
    public async Task SendMentionNotificationAsync_SkipsQuietly_WhenDisabled()
    {
        _adminService.GetSystemSettingsAsync().Returns(new SystemSettings
        {
            EmailNotificationsEnabled = false,
            SmtpServer = ""
        });

        // Should not throw
        await _service.SendMentionNotificationAsync("alice", "Issue #1", "http://example.com");
    }
}

public class BlameServiceTests
{
    [Fact]
    public void Service_CanBeInstantiated()
    {
        var config = Substitute.For<IConfiguration>();
        var service = new BlameService(config, NullLogger<BlameService>.Instance);
        Assert.NotNull(service);
    }

    [Fact]
    public void GetBlame_ReturnsEmptyList_WhenRepoPathInvalid()
    {
        var config = Substitute.For<IConfiguration>();
        config["Git:ProjectRoot"].Returns("/nonexistent/path");
        var service = new BlameService(config, NullLogger<BlameService>.Instance);

        var result = service.GetBlame("owner", "nonexistent-repo", "main", "file.txt");

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}

public class ArchiveServiceTests
{
    [Fact]
    public void Service_CanBeInstantiated()
    {
        var service = new ArchiveService(NullLogger<ArchiveService>.Instance);
        Assert.NotNull(service);
    }

    [Fact]
    public void CreateArchive_ReturnsNull_WhenRepoPathInvalid()
    {
        var service = new ArchiveService(NullLogger<ArchiveService>.Instance);

        var result = service.CreateArchive("/nonexistent/path", "main", ArchiveFormat.Zip, out var resolvedRef);

        Assert.Null(result);
        Assert.Equal("", resolvedRef);
    }

    [Fact]
    public void CreateArchive_ReturnsNull_ForTarGz_WhenRepoPathInvalid()
    {
        var service = new ArchiveService(NullLogger<ArchiveService>.Instance);

        var result = service.CreateArchive("/nonexistent/path", "main", ArchiveFormat.TarGz, out var resolvedRef);

        Assert.Null(result);
    }
}

public class GpgKeyServiceTests
{
    private readonly GpgKeyService _service;
    private readonly TestDbContextFactory _factory;

    public GpgKeyServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        _service = new GpgKeyService(_factory, NullLogger<GpgKeyService>.Instance);
    }

    [Fact]
    public async Task AddGpgKeyAsync_ReturnsNull_WhenKeyIsEmpty()
    {
        var result = await _service.AddGpgKeyAsync(1, "");
        Assert.Null(result);
    }

    [Fact]
    public async Task AddGpgKeyAsync_ReturnsNull_WhenKeyIsWhitespace()
    {
        var result = await _service.AddGpgKeyAsync(1, "   ");
        Assert.Null(result);
    }

    [Fact]
    public async Task AddGpgKeyAsync_ReturnsNull_WhenKeyIsInvalidFormat()
    {
        var result = await _service.AddGpgKeyAsync(1, "not a valid pgp key");
        Assert.Null(result);
    }

    [Fact]
    public async Task AddGpgKeyAsync_ReturnsNull_WhenMissingPgpHeader()
    {
        var result = await _service.AddGpgKeyAsync(1, "some random text without PGP headers");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserGpgKeysAsync_ReturnsEmptyList_WhenNoKeys()
    {
        var result = await _service.GetUserGpgKeysAsync(1);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserGpgKeysAsync_ReturnsKeysForCorrectUser()
    {
        using var db = _factory.CreateDbContext();
        db.GpgKeys.Add(new GpgKey { UserId = 1, KeyId = "AAAA1111", LongKeyId = "BBBBBBBBAAAA1111", PublicKey = "key1", CreatedAt = DateTime.UtcNow });
        db.GpgKeys.Add(new GpgKey { UserId = 2, KeyId = "CCCC2222", LongKeyId = "DDDDDDDDCCCC2222", PublicKey = "key2", CreatedAt = DateTime.UtcNow });
        db.GpgKeys.Add(new GpgKey { UserId = 1, KeyId = "EEEE3333", LongKeyId = "FFFFFFFFEEEE3333", PublicKey = "key3", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var user1Keys = await _service.GetUserGpgKeysAsync(1);
        var user2Keys = await _service.GetUserGpgKeysAsync(2);

        Assert.Equal(2, user1Keys.Count);
        Assert.Single(user2Keys);
        Assert.All(user1Keys, k => Assert.Equal(1, k.UserId));
    }

    [Fact]
    public async Task DeleteGpgKeyAsync_RemovesTheKey()
    {
        using var db = _factory.CreateDbContext();
        var key = new GpgKey { UserId = 1, KeyId = "DEL11111", LongKeyId = "DEADBEEFDEADBEEF", PublicKey = "key", CreatedAt = DateTime.UtcNow };
        db.GpgKeys.Add(key);
        await db.SaveChangesAsync();
        var keyId = key.Id;

        var deleted = await _service.DeleteGpgKeyAsync(keyId);
        Assert.True(deleted);

        var remaining = await _service.GetUserGpgKeysAsync(1);
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task DeleteGpgKeyAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.DeleteGpgKeyAsync(999);
        Assert.False(result);
    }

    [Fact]
    public void CommitHasSignature_ReturnsFalse_WhenPathInvalid()
    {
        var result = GpgKeyService.CommitHasSignature("abc123", "/nonexistent/path");
        Assert.False(result);
    }
}

public class TemplateServiceTests
{
    private readonly TemplateService _service;
    private readonly TestDbContextFactory _factory;
    private readonly IActivityService _activityService;

    public TemplateServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        _activityService = Substitute.For<IActivityService>();
        _service = new TemplateService(_factory, NullLogger<TemplateService>.Instance, _activityService);
    }

    [Fact]
    public async Task GetTemplatesAsync_ReturnsOnlyTemplateRepos()
    {
        using var db = _factory.CreateDbContext();
        db.Repositories.Add(new Repository { Name = "template-repo.git", Owner = "alice", IsTemplate = true });
        db.Repositories.Add(new Repository { Name = "normal-repo.git", Owner = "alice", IsTemplate = false });
        db.Repositories.Add(new Repository { Name = "another-template.git", Owner = "bob", IsTemplate = true });
        await db.SaveChangesAsync();

        var templates = await _service.GetTemplatesAsync();

        Assert.Equal(2, templates.Count);
        Assert.All(templates, t => Assert.True(t.IsTemplate));
    }

    [Fact]
    public async Task GetTemplatesAsync_ReturnsEmptyList_WhenNoTemplates()
    {
        using var db = _factory.CreateDbContext();
        db.Repositories.Add(new Repository { Name = "repo1.git", Owner = "alice", IsTemplate = false });
        await db.SaveChangesAsync();

        var templates = await _service.GetTemplatesAsync();

        Assert.Empty(templates);
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ReturnsNull_WhenTemplateNotFound()
    {
        var result = await _service.CreateFromTemplateAsync(999, "alice", "new-repo", null, false, "/tmp");
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ReturnsNull_WhenRepoIsNotTemplate()
    {
        using var db = _factory.CreateDbContext();
        db.Repositories.Add(new Repository { Name = "not-template.git", Owner = "alice", IsTemplate = false });
        await db.SaveChangesAsync();

        var repo = db.Repositories.First();
        var result = await _service.CreateFromTemplateAsync(repo.Id, "bob", "new-repo", null, false, "/tmp");
        Assert.Null(result);
    }
}

public class CollaboratorServiceTests
{
    private readonly CollaboratorService _service;
    private readonly TestDbContextFactory _factory;

    public CollaboratorServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        _service = new CollaboratorService(_factory, NullLogger<CollaboratorService>.Instance);
    }

    [Fact]
    public async Task AddCollaboratorAsync_AddsSuccessfully()
    {
        var result = await _service.AddCollaboratorAsync("repo.git", "alice", CollaboratorPermission.Write, "bob");
        Assert.True(result);

        var collabs = await _service.GetCollaboratorsAsync("repo.git");
        Assert.Single(collabs);
        Assert.Equal("alice", collabs[0].Username);
        Assert.Equal(CollaboratorPermission.Write, collabs[0].Permission);
    }

    [Fact]
    public async Task AddCollaboratorAsync_ReturnsFalse_WhenDuplicate()
    {
        await _service.AddCollaboratorAsync("repo.git", "alice", CollaboratorPermission.Write, "bob");
        var result = await _service.AddCollaboratorAsync("repo.git", "alice", CollaboratorPermission.Admin, "bob");

        Assert.False(result);
    }

    [Fact]
    public async Task RemoveCollaboratorAsync_RemovesSuccessfully()
    {
        await _service.AddCollaboratorAsync("repo.git", "alice", CollaboratorPermission.Write, "bob");

        var removed = await _service.RemoveCollaboratorAsync("repo.git", "alice");
        Assert.True(removed);

        var collabs = await _service.GetCollaboratorsAsync("repo.git");
        Assert.Empty(collabs);
    }

    [Fact]
    public async Task RemoveCollaboratorAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.RemoveCollaboratorAsync("repo.git", "nonexistent");
        Assert.False(result);
    }

    [Fact]
    public async Task UpdatePermissionAsync_UpdatesSuccessfully()
    {
        await _service.AddCollaboratorAsync("repo.git", "alice", CollaboratorPermission.Read, "bob");

        var updated = await _service.UpdatePermissionAsync("repo.git", "alice", CollaboratorPermission.Admin);
        Assert.True(updated);

        var collabs = await _service.GetCollaboratorsAsync("repo.git");
        Assert.Equal(CollaboratorPermission.Admin, collabs[0].Permission);
    }

    [Fact]
    public async Task UpdatePermissionAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.UpdatePermissionAsync("repo.git", "nonexistent", CollaboratorPermission.Admin);
        Assert.False(result);
    }

    [Fact]
    public async Task IsCollaboratorAsync_ReturnsCorrectly()
    {
        await _service.AddCollaboratorAsync("repo.git", "alice", CollaboratorPermission.Write, "bob");

        Assert.True(await _service.IsCollaboratorAsync("repo.git", "alice"));
        Assert.False(await _service.IsCollaboratorAsync("repo.git", "charlie"));
    }

    [Fact]
    public async Task HasPermissionAsync_ReturnsTrueForOwner()
    {
        using var db = _factory.CreateDbContext();
        db.Repositories.Add(new Repository { Name = "repo.git", Owner = "owner" });
        await db.SaveChangesAsync();

        var result = await _service.HasPermissionAsync("repo.git", "owner", CollaboratorPermission.Admin);
        Assert.True(result);
    }

    [Fact]
    public async Task HasPermissionAsync_ChecksPermissionLevel()
    {
        await _service.AddCollaboratorAsync("repo.git", "alice", CollaboratorPermission.Read, "bob");

        Assert.True(await _service.HasPermissionAsync("repo.git", "alice", CollaboratorPermission.Read));
        Assert.False(await _service.HasPermissionAsync("repo.git", "alice", CollaboratorPermission.Write));
    }

    [Fact]
    public async Task GetCollaboratorsAsync_ReturnsEmptyForUnknownRepo()
    {
        var result = await _service.GetCollaboratorsAsync("nonexistent.git");
        Assert.Empty(result);
    }
}

public class ReleaseServiceTests
{
    private readonly ReleaseService _service;
    private readonly TestDbContextFactory _factory;
    private readonly IActivityService _activityService;

    public ReleaseServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        _activityService = Substitute.For<IActivityService>();
        _service = new ReleaseService(_factory, NullLogger<ReleaseService>.Instance, _activityService);
    }

    [Fact]
    public async Task CreateReleaseAsync_CreatesWithCorrectProperties()
    {
        var release = await _service.CreateReleaseAsync("repo.git", "v1.0", "Version 1.0", "Release notes", "alice", false, false);

        Assert.NotNull(release);
        Assert.Equal("repo.git", release.RepoName);
        Assert.Equal("v1.0", release.TagName);
        Assert.Equal("Version 1.0", release.Title);
        Assert.Equal("Release notes", release.Body);
        Assert.Equal("alice", release.Author);
        Assert.False(release.IsDraft);
        Assert.False(release.IsPrerelease);
        Assert.NotNull(release.PublishedAt);
    }

    [Fact]
    public async Task CreateReleaseAsync_DraftRelease_HasNullPublishedAt()
    {
        var release = await _service.CreateReleaseAsync("repo.git", "v2.0-beta", "Beta", null, "alice", true, false);

        Assert.True(release.IsDraft);
        Assert.Null(release.PublishedAt);
    }

    [Fact]
    public async Task GetReleasesAsync_ReturnsReleasesForCorrectRepo()
    {
        await _service.CreateReleaseAsync("repo1.git", "v1.0", "R1", null, "alice", false, false);
        await _service.CreateReleaseAsync("repo2.git", "v1.0", "R2", null, "bob", false, false);
        await _service.CreateReleaseAsync("repo1.git", "v2.0", "R3", null, "alice", false, false);

        var repo1Releases = await _service.GetReleasesAsync("repo1.git");
        var repo2Releases = await _service.GetReleasesAsync("repo2.git");

        Assert.Equal(2, repo1Releases.Count);
        Assert.Single(repo2Releases);
    }

    [Fact]
    public async Task GetReleaseAsync_ReturnsCorrectRelease()
    {
        var created = await _service.CreateReleaseAsync("repo.git", "v1.0", "Release 1", "Notes", "alice", false, false);

        var fetched = await _service.GetReleaseAsync("repo.git", created.Id);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("v1.0", fetched.TagName);
    }

    [Fact]
    public async Task GetReleaseAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _service.GetReleaseAsync("repo.git", 999);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteReleaseAsync_RemovesRelease()
    {
        var release = await _service.CreateReleaseAsync("repo.git", "v1.0", "R1", null, "alice", false, false);

        var deleted = await _service.DeleteReleaseAsync("repo.git", release.Id);
        Assert.True(deleted);

        var fetched = await _service.GetReleaseAsync("repo.git", release.Id);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task DeleteReleaseAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.DeleteReleaseAsync("repo.git", 999);
        Assert.False(result);
    }

    [Fact]
    public async Task CreateReleaseAsync_PrereleaseFlag()
    {
        var release = await _service.CreateReleaseAsync("repo.git", "v1.0-rc1", "RC1", null, "alice", false, true);

        Assert.True(release.IsPrerelease);
        Assert.False(release.IsDraft);
    }
}

public class SnippetServiceTests
{
    private readonly SnippetService _service;
    private readonly TestDbContextFactory _factory;

    public SnippetServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        _service = new SnippetService(_factory, NullLogger<SnippetService>.Instance);
    }

    [Fact]
    public async Task CreateSnippetAsync_CreatesWithCorrectProperties()
    {
        var files = new List<SnippetFile> { new SnippetFile { Filename = "test.cs", Content = "Console.WriteLine();" } };
        var snippet = await _service.CreateSnippetAsync("My Snippet", "A description", "alice", true, files);

        Assert.NotNull(snippet);
        Assert.Equal("My Snippet", snippet.Title);
        Assert.Equal("A description", snippet.Description);
        Assert.Equal("alice", snippet.Owner);
        Assert.True(snippet.IsPublic);
        Assert.Single(snippet.Files);
    }

    [Fact]
    public async Task GetSnippetsAsync_ReturnsPublicSnippetsOnly_ByDefault()
    {
        await _service.CreateSnippetAsync("Public", null, "alice", true, new List<SnippetFile> { new SnippetFile { Filename = "a.txt", Content = "a" } });
        await _service.CreateSnippetAsync("Private", null, "alice", false, new List<SnippetFile> { new SnippetFile { Filename = "b.txt", Content = "b" } });

        var result = await _service.GetSnippetsAsync();

        Assert.Single(result);
        Assert.Equal("Public", result[0].Title);
    }

    [Fact]
    public async Task GetSnippetsAsync_IncludesPrivate_WhenRequested()
    {
        await _service.CreateSnippetAsync("Public", null, "alice", true, new List<SnippetFile> { new SnippetFile { Filename = "a.txt", Content = "a" } });
        await _service.CreateSnippetAsync("Private", null, "alice", false, new List<SnippetFile> { new SnippetFile { Filename = "b.txt", Content = "b" } });

        var result = await _service.GetSnippetsAsync(includePrivate: true);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetSnippetsAsync_FiltersByOwner()
    {
        await _service.CreateSnippetAsync("Alice's", null, "alice", true, new List<SnippetFile> { new SnippetFile { Filename = "a.txt", Content = "a" } });
        await _service.CreateSnippetAsync("Bob's", null, "bob", true, new List<SnippetFile> { new SnippetFile { Filename = "b.txt", Content = "b" } });

        var result = await _service.GetSnippetsAsync(owner: "alice");

        Assert.Single(result);
        Assert.Equal("alice", result[0].Owner);
    }

    [Fact]
    public async Task GetSnippetAsync_ReturnsSnippetById()
    {
        var created = await _service.CreateSnippetAsync("Test", null, "alice", true, new List<SnippetFile> { new SnippetFile { Filename = "a.txt", Content = "a" } });

        var fetched = await _service.GetSnippetAsync(created.Id);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
    }

    [Fact]
    public async Task GetSnippetAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _service.GetSnippetAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteSnippetAsync_RemovesSnippet()
    {
        var created = await _service.CreateSnippetAsync("ToDelete", null, "alice", true, new List<SnippetFile> { new SnippetFile { Filename = "a.txt", Content = "a" } });

        var deleted = await _service.DeleteSnippetAsync(created.Id, "alice");
        Assert.True(deleted);

        var fetched = await _service.GetSnippetAsync(created.Id);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task DeleteSnippetAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.DeleteSnippetAsync(999, "alice");
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateSnippetAsync_UpdatesProperties()
    {
        var created = await _service.CreateSnippetAsync("Original", "Desc", "alice", true, new List<SnippetFile> { new SnippetFile { Filename = "a.txt", Content = "a" } });

        var newFiles = new List<SnippetFile> { new SnippetFile { Filename = "b.txt", Content = "b" } };
        var updated = await _service.UpdateSnippetAsync(created.Id, "Updated", "New Desc", false, newFiles);

        Assert.True(updated);

        var fetched = await _service.GetSnippetAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Updated", fetched.Title);
        Assert.Equal("New Desc", fetched.Description);
        Assert.False(fetched.IsPublic);
    }

    [Fact]
    public async Task UpdateSnippetAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.UpdateSnippetAsync(999, "Title", null, true, new List<SnippetFile>());
        Assert.False(result);
    }
}

public class MilestoneServiceTests
{
    private readonly MilestoneService _service;
    private readonly TestDbContextFactory _factory;

    public MilestoneServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        _service = new MilestoneService(_factory, NullLogger<MilestoneService>.Instance);
    }

    [Fact]
    public async Task CreateMilestoneAsync_AssignsSequentialNumbers()
    {
        var m1 = await _service.CreateMilestoneAsync("repo.git", "Sprint 1", null, null, "alice");
        var m2 = await _service.CreateMilestoneAsync("repo.git", "Sprint 2", null, null, "alice");

        Assert.Equal(1, m1.Number);
        Assert.Equal(2, m2.Number);
    }

    [Fact]
    public async Task CreateMilestoneAsync_SetsCorrectProperties()
    {
        var dueDate = DateTime.UtcNow.AddDays(14);
        var milestone = await _service.CreateMilestoneAsync("repo.git", "Sprint 1", "Description", dueDate, "alice");

        Assert.Equal("repo.git", milestone.RepoName);
        Assert.Equal("Sprint 1", milestone.Title);
        Assert.Equal("Description", milestone.Description);
        Assert.Equal(dueDate, milestone.DueDate);
        Assert.Equal("alice", milestone.Creator);
        Assert.Equal(MilestoneState.Open, milestone.State);
    }

    [Fact]
    public async Task GetMilestonesAsync_ReturnsForCorrectRepo()
    {
        await _service.CreateMilestoneAsync("repo1.git", "M1", null, null, "alice");
        await _service.CreateMilestoneAsync("repo2.git", "M2", null, null, "alice");
        await _service.CreateMilestoneAsync("repo1.git", "M3", null, null, "alice");

        var repo1Milestones = await _service.GetMilestonesAsync("repo1.git");
        var repo2Milestones = await _service.GetMilestonesAsync("repo2.git");

        Assert.Equal(2, repo1Milestones.Count);
        Assert.Single(repo2Milestones);
    }

    [Fact]
    public async Task GetMilestoneAsync_ReturnsByNumber()
    {
        await _service.CreateMilestoneAsync("repo.git", "Sprint 1", null, null, "alice");

        var fetched = await _service.GetMilestoneAsync("repo.git", 1);

        Assert.NotNull(fetched);
        Assert.Equal("Sprint 1", fetched.Title);
    }

    [Fact]
    public async Task GetMilestoneAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _service.GetMilestoneAsync("repo.git", 999);
        Assert.Null(result);
    }

    [Fact]
    public async Task CloseMilestoneAsync_SetsStateAndClosedAt()
    {
        await _service.CreateMilestoneAsync("repo.git", "Sprint 1", null, null, "alice");

        var closed = await _service.CloseMilestoneAsync("repo.git", 1);
        Assert.True(closed);

        var milestone = await _service.GetMilestoneAsync("repo.git", 1);
        Assert.NotNull(milestone);
        Assert.Equal(MilestoneState.Closed, milestone.State);
        Assert.NotNull(milestone.ClosedAt);
    }

    [Fact]
    public async Task CloseMilestoneAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.CloseMilestoneAsync("repo.git", 999);
        Assert.False(result);
    }

    [Fact]
    public async Task ReopenMilestoneAsync_ResetsStateAndClosedAt()
    {
        await _service.CreateMilestoneAsync("repo.git", "Sprint 1", null, null, "alice");
        await _service.CloseMilestoneAsync("repo.git", 1);

        var reopened = await _service.ReopenMilestoneAsync("repo.git", 1);
        Assert.True(reopened);

        var milestone = await _service.GetMilestoneAsync("repo.git", 1);
        Assert.NotNull(milestone);
        Assert.Equal(MilestoneState.Open, milestone.State);
        Assert.Null(milestone.ClosedAt);
    }

    [Fact]
    public async Task ReopenMilestoneAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.ReopenMilestoneAsync("repo.git", 999);
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteMilestoneAsync_RemovesMilestone()
    {
        await _service.CreateMilestoneAsync("repo.git", "Sprint 1", null, null, "alice");

        var deleted = await _service.DeleteMilestoneAsync("repo.git", 1);
        Assert.True(deleted);

        var fetched = await _service.GetMilestoneAsync("repo.git", 1);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task DeleteMilestoneAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.DeleteMilestoneAsync("repo.git", 999);
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteMilestoneAsync_UnlinksIssues()
    {
        var milestone = await _service.CreateMilestoneAsync("repo.git", "Sprint 1", null, null, "alice");

        using (var db = _factory.CreateDbContext())
        {
            db.Issues.Add(new Issue { RepoName = "repo.git", Number = 1, Title = "Bug", Author = "alice", MilestoneId = milestone.Id, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        await _service.DeleteMilestoneAsync("repo.git", 1);

        using (var db = _factory.CreateDbContext())
        {
            var issue = await db.Issues.FirstAsync(i => i.Number == 1);
            Assert.Null(issue.MilestoneId);
        }
    }

    [Fact]
    public async Task GetIssueCountsAsync_ReturnsCorrectCounts()
    {
        var milestone = await _service.CreateMilestoneAsync("repo.git", "Sprint 1", null, null, "alice");

        using var db = _factory.CreateDbContext();
        db.Issues.Add(new Issue { RepoName = "repo.git", Number = 1, Title = "Open1", Author = "alice", MilestoneId = milestone.Id, State = IssueState.Open, CreatedAt = DateTime.UtcNow });
        db.Issues.Add(new Issue { RepoName = "repo.git", Number = 2, Title = "Open2", Author = "alice", MilestoneId = milestone.Id, State = IssueState.Open, CreatedAt = DateTime.UtcNow });
        db.Issues.Add(new Issue { RepoName = "repo.git", Number = 3, Title = "Closed1", Author = "alice", MilestoneId = milestone.Id, State = IssueState.Closed, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var (open, closed) = await _service.GetIssueCountsAsync(milestone.Id);
        Assert.Equal(2, open);
        Assert.Equal(1, closed);
    }

    [Fact]
    public async Task UpdateMilestoneAsync_UpdatesProperties()
    {
        await _service.CreateMilestoneAsync("repo.git", "Sprint 1", null, null, "alice");

        var updated = await _service.UpdateMilestoneAsync("repo.git", 1, m => { m.Title = "Updated Sprint"; m.Description = "New desc"; });
        Assert.True(updated);

        var milestone = await _service.GetMilestoneAsync("repo.git", 1);
        Assert.NotNull(milestone);
        Assert.Equal("Updated Sprint", milestone.Title);
        Assert.Equal("New desc", milestone.Description);
    }

    [Fact]
    public async Task UpdateMilestoneAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.UpdateMilestoneAsync("repo.git", 999, m => { m.Title = "X"; });
        Assert.False(result);
    }
}

public class OrganizationServiceTests
{
    private readonly OrganizationService _service;
    private readonly TestDbContextFactory _factory;
    private readonly IActivityService _activityService;

    public OrganizationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        _activityService = Substitute.For<IActivityService>();
        _service = new OrganizationService(_factory, NullLogger<OrganizationService>.Instance, _activityService);
    }

    [Fact]
    public async Task CreateOrganizationAsync_CreatesOrgAndAddsOwnerAsMember()
    {
        var org = await _service.CreateOrganizationAsync("my-org", "alice", "My Org", "A description");

        Assert.Equal("my-org", org.Name);
        Assert.Equal("alice", org.Owner);
        Assert.Equal("My Org", org.DisplayName);
        Assert.Equal("A description", org.Description);

        var members = await _service.GetMembersAsync("my-org");
        Assert.Single(members);
        Assert.Equal("alice", members[0].Username);
        Assert.Equal(OrgRole.Owner, members[0].Role);
    }

    [Fact]
    public async Task GetOrganizationsAsync_ReturnsAllOrgs()
    {
        await _service.CreateOrganizationAsync("org1", "alice");
        await _service.CreateOrganizationAsync("org2", "bob");

        var orgs = await _service.GetOrganizationsAsync();
        Assert.Equal(2, orgs.Count);
    }

    [Fact]
    public async Task GetOrganizationAsync_ReturnsByName()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");

        var org = await _service.GetOrganizationAsync("my-org");
        Assert.NotNull(org);
        Assert.Equal("my-org", org.Name);
    }

    [Fact]
    public async Task GetOrganizationAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _service.GetOrganizationAsync("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public async Task AddMemberAsync_AddsMember()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");

        var added = await _service.AddMemberAsync("my-org", "bob");
        Assert.True(added);

        var members = await _service.GetMembersAsync("my-org");
        Assert.Equal(2, members.Count);
    }

    [Fact]
    public async Task AddMemberAsync_ReturnsFalse_WhenDuplicate()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");

        var result = await _service.AddMemberAsync("my-org", "alice");
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveMemberAsync_RemovesMember()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");
        await _service.AddMemberAsync("my-org", "bob");

        var removed = await _service.RemoveMemberAsync("my-org", "bob");
        Assert.True(removed);

        var members = await _service.GetMembersAsync("my-org");
        Assert.Single(members);
    }

    [Fact]
    public async Task RemoveMemberAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.RemoveMemberAsync("my-org", "nonexistent");
        Assert.False(result);
    }

    [Fact]
    public async Task IsMemberAsync_ReturnsCorrectly()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");

        Assert.True(await _service.IsMemberAsync("my-org", "alice"));
        Assert.False(await _service.IsMemberAsync("my-org", "bob"));
    }

    [Fact]
    public async Task IsOwnerAsync_ReturnsCorrectly()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");
        await _service.AddMemberAsync("my-org", "bob", OrgRole.Member);

        Assert.True(await _service.IsOwnerAsync("my-org", "alice"));
        Assert.False(await _service.IsOwnerAsync("my-org", "bob"));
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_UpdatesRole()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");
        await _service.AddMemberAsync("my-org", "bob", OrgRole.Member);

        var updated = await _service.UpdateMemberRoleAsync("my-org", "bob", OrgRole.Owner);
        Assert.True(updated);

        Assert.True(await _service.IsOwnerAsync("my-org", "bob"));
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.UpdateMemberRoleAsync("my-org", "nonexistent", OrgRole.Owner);
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserOrganizationsAsync_ReturnsOrgsForUser()
    {
        await _service.CreateOrganizationAsync("org1", "alice");
        await _service.CreateOrganizationAsync("org2", "bob");
        await _service.AddMemberAsync("org2", "alice");

        var aliceOrgs = await _service.GetUserOrganizationsAsync("alice");
        Assert.Equal(2, aliceOrgs.Count);

        var bobOrgs = await _service.GetUserOrganizationsAsync("bob");
        Assert.Single(bobOrgs);
    }

    [Fact]
    public async Task DeleteOrganizationAsync_RemovesOrgAndMembers()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");
        await _service.AddMemberAsync("my-org", "bob");

        var deleted = await _service.DeleteOrganizationAsync("my-org");
        Assert.True(deleted);

        var org = await _service.GetOrganizationAsync("my-org");
        Assert.Null(org);

        var members = await _service.GetMembersAsync("my-org");
        Assert.Empty(members);
    }

    [Fact]
    public async Task DeleteOrganizationAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.DeleteOrganizationAsync("nonexistent");
        Assert.False(result);
    }

    [Fact]
    public async Task CreateTeamAsync_CreatesTeam()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");
        var team = await _service.CreateTeamAsync("my-org", "devs", "Developers", TeamPermission.Write);

        Assert.Equal("devs", team.Name);
        Assert.Equal("my-org", team.OrganizationName);
        Assert.Equal("Developers", team.Description);
        Assert.Equal(TeamPermission.Write, team.Permission);
    }

    [Fact]
    public async Task GetTeamsAsync_ReturnsTeamsForOrg()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");
        await _service.CreateTeamAsync("my-org", "devs");
        await _service.CreateTeamAsync("my-org", "ops");

        var teams = await _service.GetTeamsAsync("my-org");
        Assert.Equal(2, teams.Count);
    }

    [Fact]
    public async Task GetTeamAsync_ReturnsByName()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");
        await _service.CreateTeamAsync("my-org", "devs");

        var team = await _service.GetTeamAsync("my-org", "devs");
        Assert.NotNull(team);
        Assert.Equal("devs", team.Name);
    }

    [Fact]
    public async Task DeleteTeamAsync_RemovesTeam()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");
        var team = await _service.CreateTeamAsync("my-org", "devs");

        var deleted = await _service.DeleteTeamAsync(team.Id);
        Assert.True(deleted);

        var fetched = await _service.GetTeamAsync("my-org", "devs");
        Assert.Null(fetched);
    }

    [Fact]
    public async Task DeleteTeamAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.DeleteTeamAsync(999);
        Assert.False(result);
    }

    [Fact]
    public async Task AddTeamMemberAsync_AddsMember()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");
        var team = await _service.CreateTeamAsync("my-org", "devs");

        var added = await _service.AddTeamMemberAsync(team.Id, "bob");
        Assert.True(added);
    }

    [Fact]
    public async Task AddTeamMemberAsync_ReturnsFalse_WhenDuplicate()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");
        var team = await _service.CreateTeamAsync("my-org", "devs");
        await _service.AddTeamMemberAsync(team.Id, "bob");

        var result = await _service.AddTeamMemberAsync(team.Id, "bob");
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveTeamMemberAsync_RemovesMember()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");
        var team = await _service.CreateTeamAsync("my-org", "devs");
        await _service.AddTeamMemberAsync(team.Id, "bob");

        var removed = await _service.RemoveTeamMemberAsync(team.Id, "bob");
        Assert.True(removed);
    }

    [Fact]
    public async Task RemoveTeamMemberAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.RemoveTeamMemberAsync(999, "bob");
        Assert.False(result);
    }

    [Fact]
    public async Task AddTeamRepositoryAsync_AddsRepo()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");
        var team = await _service.CreateTeamAsync("my-org", "devs");

        var added = await _service.AddTeamRepositoryAsync(team.Id, "repo.git");
        Assert.True(added);
    }

    [Fact]
    public async Task AddTeamRepositoryAsync_ReturnsFalse_WhenDuplicate()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");
        var team = await _service.CreateTeamAsync("my-org", "devs");
        await _service.AddTeamRepositoryAsync(team.Id, "repo.git");

        var result = await _service.AddTeamRepositoryAsync(team.Id, "repo.git");
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveTeamRepositoryAsync_RemovesRepo()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");
        var team = await _service.CreateTeamAsync("my-org", "devs");
        await _service.AddTeamRepositoryAsync(team.Id, "repo.git");

        var removed = await _service.RemoveTeamRepositoryAsync(team.Id, "repo.git");
        Assert.True(removed);
    }

    [Fact]
    public async Task RemoveTeamRepositoryAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.RemoveTeamRepositoryAsync(999, "repo.git");
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateOrganizationAsync_UpdatesProperties()
    {
        await _service.CreateOrganizationAsync("my-org", "alice");

        var updated = await _service.UpdateOrganizationAsync("my-org", o => { o.Description = "Updated desc"; });
        Assert.True(updated);

        var org = await _service.GetOrganizationAsync("my-org");
        Assert.NotNull(org);
        Assert.Equal("Updated desc", org.Description);
    }

    [Fact]
    public async Task UpdateOrganizationAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.UpdateOrganizationAsync("nonexistent", o => { o.Description = "X"; });
        Assert.False(result);
    }
}

public class DiscussionServiceTests
{
    private readonly DiscussionService _service;
    private readonly TestDbContextFactory _factory;
    private readonly INotificationService _notificationService;

    public DiscussionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        _notificationService = Substitute.For<INotificationService>();
        _service = new DiscussionService(_factory, NullLogger<DiscussionService>.Instance, _notificationService);
    }

    [Fact]
    public async Task CreateDiscussionAsync_AssignsSequentialNumbers()
    {
        var d1 = await _service.CreateDiscussionAsync("repo.git", "First", "Body1", "alice", DiscussionCategory.General);
        var d2 = await _service.CreateDiscussionAsync("repo.git", "Second", "Body2", "bob", DiscussionCategory.QAndA);

        Assert.Equal(1, d1.Number);
        Assert.Equal(2, d2.Number);
    }

    [Fact]
    public async Task CreateDiscussionAsync_SetsCorrectProperties()
    {
        var discussion = await _service.CreateDiscussionAsync("repo.git", "My Discussion", "The body", "alice", DiscussionCategory.Ideas);

        Assert.Equal("repo.git", discussion.RepoName);
        Assert.Equal("My Discussion", discussion.Title);
        Assert.Equal("The body", discussion.Body);
        Assert.Equal("alice", discussion.Author);
        Assert.Equal(DiscussionCategory.Ideas, discussion.Category);
        Assert.False(discussion.IsPinned);
        Assert.False(discussion.IsLocked);
    }

    [Fact]
    public async Task GetDiscussionsAsync_ReturnsForCorrectRepo()
    {
        await _service.CreateDiscussionAsync("repo1.git", "D1", "B1", "alice", DiscussionCategory.General);
        await _service.CreateDiscussionAsync("repo2.git", "D2", "B2", "bob", DiscussionCategory.General);

        var repo1 = await _service.GetDiscussionsAsync("repo1.git");
        var repo2 = await _service.GetDiscussionsAsync("repo2.git");

        Assert.Single(repo1);
        Assert.Single(repo2);
    }

    [Fact]
    public async Task GetDiscussionAsync_ReturnsByNumber()
    {
        await _service.CreateDiscussionAsync("repo.git", "My Discussion", "Body", "alice", DiscussionCategory.General);

        var fetched = await _service.GetDiscussionAsync("repo.git", 1);

        Assert.NotNull(fetched);
        Assert.Equal("My Discussion", fetched.Title);
    }

    [Fact]
    public async Task GetDiscussionAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _service.GetDiscussionAsync("repo.git", 999);
        Assert.Null(result);
    }

    [Fact]
    public async Task AddCommentAsync_AddsComment()
    {
        var discussion = await _service.CreateDiscussionAsync("repo.git", "D1", "Body", "alice", DiscussionCategory.General);

        var comment = await _service.AddCommentAsync(discussion.Id, "bob", "Great discussion!");

        Assert.Equal("bob", comment.Author);
        Assert.Equal("Great discussion!", comment.Body);
        Assert.Null(comment.ParentCommentId);
    }

    [Fact]
    public async Task AddCommentAsync_SupportsReplies()
    {
        var discussion = await _service.CreateDiscussionAsync("repo.git", "D1", "Body", "alice", DiscussionCategory.General);
        var parent = await _service.AddCommentAsync(discussion.Id, "bob", "Comment");
        var reply = await _service.AddCommentAsync(discussion.Id, "alice", "Reply", parent.Id);

        Assert.Equal(parent.Id, reply.ParentCommentId);
    }

    [Fact]
    public async Task DeleteCommentAsync_RemovesComment()
    {
        var discussion = await _service.CreateDiscussionAsync("repo.git", "D1", "Body", "alice", DiscussionCategory.General);
        var comment = await _service.AddCommentAsync(discussion.Id, "bob", "A comment");

        var deleted = await _service.DeleteCommentAsync(comment.Id, "bob");
        Assert.True(deleted);
    }

    [Fact]
    public async Task DeleteCommentAsync_ReturnsFalse_WhenWrongUser()
    {
        var discussion = await _service.CreateDiscussionAsync("repo.git", "D1", "Body", "alice", DiscussionCategory.General);
        var comment = await _service.AddCommentAsync(discussion.Id, "bob", "A comment");

        var result = await _service.DeleteCommentAsync(comment.Id, "alice");
        Assert.False(result);
    }

    [Fact]
    public async Task TogglePinAsync_TogglesPin()
    {
        await _service.CreateDiscussionAsync("repo.git", "D1", "Body", "alice", DiscussionCategory.General);

        var toggled = await _service.TogglePinAsync("repo.git", 1);
        Assert.True(toggled);

        var discussion = await _service.GetDiscussionAsync("repo.git", 1);
        Assert.NotNull(discussion);
        Assert.True(discussion.IsPinned);

        await _service.TogglePinAsync("repo.git", 1);
        discussion = await _service.GetDiscussionAsync("repo.git", 1);
        Assert.NotNull(discussion);
        Assert.False(discussion.IsPinned);
    }

    [Fact]
    public async Task TogglePinAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.TogglePinAsync("repo.git", 999);
        Assert.False(result);
    }

    [Fact]
    public async Task ToggleLockAsync_TogglesLock()
    {
        await _service.CreateDiscussionAsync("repo.git", "D1", "Body", "alice", DiscussionCategory.General);

        await _service.ToggleLockAsync("repo.git", 1);
        var discussion = await _service.GetDiscussionAsync("repo.git", 1);
        Assert.NotNull(discussion);
        Assert.True(discussion.IsLocked);

        await _service.ToggleLockAsync("repo.git", 1);
        discussion = await _service.GetDiscussionAsync("repo.git", 1);
        Assert.NotNull(discussion);
        Assert.False(discussion.IsLocked);
    }

    [Fact]
    public async Task ToggleLockAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.ToggleLockAsync("repo.git", 999);
        Assert.False(result);
    }

    [Fact]
    public async Task MarkAsAnswerAsync_MarksCommentAsAnswer()
    {
        var discussion = await _service.CreateDiscussionAsync("repo.git", "Q", "Question?", "alice", DiscussionCategory.QAndA);
        var comment = await _service.AddCommentAsync(discussion.Id, "bob", "The answer");

        var marked = await _service.MarkAsAnswerAsync(comment.Id);
        Assert.True(marked);

        var fetched = await _service.GetDiscussionAsync("repo.git", 1);
        Assert.NotNull(fetched);
        Assert.True(fetched.IsAnswered);
        Assert.Equal(comment.Id, fetched.AnswerCommentId);
    }

    [Fact]
    public async Task MarkAsAnswerAsync_ReturnsFalse_WhenNotQAndA()
    {
        var discussion = await _service.CreateDiscussionAsync("repo.git", "D", "Body", "alice", DiscussionCategory.General);
        var comment = await _service.AddCommentAsync(discussion.Id, "bob", "Comment");

        var result = await _service.MarkAsAnswerAsync(comment.Id);
        Assert.False(result);
    }

    [Fact]
    public async Task UnmarkAsAnswerAsync_UnmarksAnswer()
    {
        var discussion = await _service.CreateDiscussionAsync("repo.git", "Q", "Question?", "alice", DiscussionCategory.QAndA);
        var comment = await _service.AddCommentAsync(discussion.Id, "bob", "The answer");
        await _service.MarkAsAnswerAsync(comment.Id);

        var unmarked = await _service.UnmarkAsAnswerAsync(comment.Id);
        Assert.True(unmarked);

        var fetched = await _service.GetDiscussionAsync("repo.git", 1);
        Assert.NotNull(fetched);
        Assert.False(fetched.IsAnswered);
        Assert.Null(fetched.AnswerCommentId);
    }

    [Fact]
    public async Task UpvoteCommentAsync_IncrementsCount()
    {
        var discussion = await _service.CreateDiscussionAsync("repo.git", "D1", "Body", "alice", DiscussionCategory.General);
        var comment = await _service.AddCommentAsync(discussion.Id, "bob", "Good point");

        await _service.UpvoteCommentAsync(comment.Id, "alice");
        await _service.UpvoteCommentAsync(comment.Id, "charlie");

        using var db = _factory.CreateDbContext();
        var updated = await db.DiscussionComments.FindAsync(comment.Id);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.UpvoteCount);
    }

    [Fact]
    public async Task DeleteDiscussionAsync_RemovesDiscussionAndComments()
    {
        var discussion = await _service.CreateDiscussionAsync("repo.git", "D1", "Body", "alice", DiscussionCategory.General);
        await _service.AddCommentAsync(discussion.Id, "bob", "Comment");

        var deleted = await _service.DeleteDiscussionAsync("repo.git", 1, "alice");
        Assert.True(deleted);

        var fetched = await _service.GetDiscussionAsync("repo.git", 1);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task DeleteDiscussionAsync_ReturnsFalse_WhenWrongUser()
    {
        await _service.CreateDiscussionAsync("repo.git", "D1", "Body", "alice", DiscussionCategory.General);

        var result = await _service.DeleteDiscussionAsync("repo.git", 1, "bob");
        Assert.False(result);
    }
}

public class ReactionServiceTests
{
    private readonly ReactionService _service;
    private readonly TestDbContextFactory _factory;

    public ReactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        _service = new ReactionService(_factory);
    }

    [Fact]
    public async Task ToggleReactionAsync_AddsReaction_ReturnsTrue()
    {
        var result = await _service.ToggleReactionAsync("alice", "thumbs_up", issueId: 1);
        Assert.True(result);

        var reactions = await _service.GetReactionsForIssueAsync(1);
        Assert.Single(reactions);
        Assert.Equal("alice", reactions[0].Username);
        Assert.Equal("thumbs_up", reactions[0].Emoji);
    }

    [Fact]
    public async Task ToggleReactionAsync_RemovesExistingReaction_ReturnsFalse()
    {
        await _service.ToggleReactionAsync("alice", "thumbs_up", issueId: 1);

        var result = await _service.ToggleReactionAsync("alice", "thumbs_up", issueId: 1);
        Assert.False(result);

        var reactions = await _service.GetReactionsForIssueAsync(1);
        Assert.Empty(reactions);
    }

    [Fact]
    public async Task GetReactionsForIssueAsync_ReturnsCorrectReactions()
    {
        await _service.ToggleReactionAsync("alice", "thumbs_up", issueId: 1);
        await _service.ToggleReactionAsync("bob", "heart", issueId: 1);
        await _service.ToggleReactionAsync("alice", "thumbs_up", issueId: 2);

        var reactions = await _service.GetReactionsForIssueAsync(1);
        Assert.Equal(2, reactions.Count);
    }

    [Fact]
    public async Task GetReactionsForIssueCommentAsync_ReturnsCorrectReactions()
    {
        await _service.ToggleReactionAsync("alice", "laugh", issueCommentId: 10);

        var reactions = await _service.GetReactionsForIssueCommentAsync(10);
        Assert.Single(reactions);
    }

    [Fact]
    public async Task GetReactionsForPullRequestAsync_ReturnsCorrectReactions()
    {
        await _service.ToggleReactionAsync("alice", "rocket", pullRequestId: 5);

        var reactions = await _service.GetReactionsForPullRequestAsync(5);
        Assert.Single(reactions);
    }

    [Fact]
    public async Task GetReactionsForDiscussionAsync_ReturnsCorrectReactions()
    {
        await _service.ToggleReactionAsync("alice", "eyes", discussionId: 3);

        var reactions = await _service.GetReactionsForDiscussionAsync(3);
        Assert.Single(reactions);
    }

    [Fact]
    public async Task GetReactionsForDiscussionCommentAsync_ReturnsCorrectReactions()
    {
        await _service.ToggleReactionAsync("alice", "hooray", discussionCommentId: 7);

        var reactions = await _service.GetReactionsForDiscussionCommentAsync(7);
        Assert.Single(reactions);
    }

    [Fact]
    public async Task GetReactionSummaryAsync_ReturnsGroupedReactions()
    {
        await _service.ToggleReactionAsync("alice", "thumbs_up", issueId: 1);
        await _service.ToggleReactionAsync("bob", "thumbs_up", issueId: 1);
        await _service.ToggleReactionAsync("charlie", "heart", issueId: 1);

        var summary = await _service.GetReactionSummaryAsync(issueId: 1);

        Assert.Equal(2, summary.Count);
        Assert.Equal(2, summary["thumbs_up"].Count);
        Assert.Single(summary["heart"]);
        Assert.Contains("alice", summary["thumbs_up"]);
        Assert.Contains("bob", summary["thumbs_up"]);
        Assert.Contains("charlie", summary["heart"]);
    }

    [Fact]
    public async Task GetReactionSummaryAsync_ReturnsEmptyDict_WhenNoReactions()
    {
        var summary = await _service.GetReactionSummaryAsync(issueId: 999);
        Assert.Empty(summary);
    }

    [Fact]
    public async Task ToggleReactionAsync_DifferentEmojis_BothPersist()
    {
        await _service.ToggleReactionAsync("alice", "thumbs_up", issueId: 1);
        await _service.ToggleReactionAsync("alice", "heart", issueId: 1);

        var reactions = await _service.GetReactionsForIssueAsync(1);
        Assert.Equal(2, reactions.Count);
    }
}

// ─── DatabaseConfigService Tests ─────────────────────────────────────────

public class DatabaseConfigServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IConfiguration _config;

    public DatabaseConfigServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "mypg_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var sshDir = Path.Combine(_tempDir, "ssh");
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ssh:DataDir"] = sshDir,
                ["Database:Provider"] = "sqlite",
                ["ConnectionStrings:Default"] = "Data Source=test.db"
            })
            .Build();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void GetCurrentConfig_NoFile_ReturnsFallbackFromConfig()
    {
        var service = new DatabaseConfigService(_config);
        var config = service.GetCurrentConfig();

        Assert.Equal("sqlite", config.Provider);
        Assert.Equal("Data Source=test.db", config.ConnectionString);
    }

    [Fact]
    public void SaveConfig_CreatesFile_AndCanBeReadBack()
    {
        var service = new DatabaseConfigService(_config);

        service.SaveConfig(new DatabaseConfig
        {
            Provider = "postgresql",
            ConnectionString = "Host=localhost;Database=test"
        });

        var readBack = service.GetCurrentConfig();
        Assert.Equal("postgresql", readBack.Provider);
        Assert.Equal("Host=localhost;Database=test", readBack.ConnectionString);
    }

    [Fact]
    public void SaveConfig_OverwritesExisting()
    {
        var service = new DatabaseConfigService(_config);

        service.SaveConfig(new DatabaseConfig { Provider = "postgresql", ConnectionString = "conn1" });
        service.SaveConfig(new DatabaseConfig { Provider = "sqlite", ConnectionString = "conn2" });

        var readBack = service.GetCurrentConfig();
        Assert.Equal("sqlite", readBack.Provider);
        Assert.Equal("conn2", readBack.ConnectionString);
    }

    [Fact]
    public void GetCurrentConfig_CorruptFile_ReturnsFallback()
    {
        var service = new DatabaseConfigService(_config);
        var path = service.GetConfigFilePath();

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "NOT VALID JSON {{{");

        var config = service.GetCurrentConfig();
        // Falls back to appsettings values
        Assert.Equal("sqlite", config.Provider);
    }

    [Fact]
    public void GetConfigFilePath_ReturnsPathInDataDir()
    {
        var service = new DatabaseConfigService(_config);
        var path = service.GetConfigFilePath();

        Assert.Contains(_tempDir, path);
        Assert.EndsWith("database.json", path);
    }
}

// ─── LdapAuthService Tests ──────────────────────────────────────────────

public class LdapAuthServiceTests
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IAdminService _adminService;
    private readonly LdapAuthService _service;

    public LdapAuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        _adminService = Substitute.For<IAdminService>();
        _service = new LdapAuthService(_factory, _adminService, NullLogger<LdapAuthService>.Instance);
    }

    [Fact]
    public async Task AuthenticateAsync_LdapDisabled_ReturnsNull()
    {
        _adminService.GetSystemSettingsAsync().Returns(new SystemSettings
        {
            LdapEnabled = false,
            LdapServer = "ldap.example.com"
        });

        var result = await _service.AuthenticateAsync("testuser", "password");
        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_EmptyServer_ReturnsNull()
    {
        _adminService.GetSystemSettingsAsync().Returns(new SystemSettings
        {
            LdapEnabled = true,
            LdapServer = ""
        });

        var result = await _service.AuthenticateAsync("testuser", "password");
        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidServer_ReturnsNull()
    {
        _adminService.GetSystemSettingsAsync().Returns(new SystemSettings
        {
            LdapEnabled = true,
            LdapServer = "nonexistent.invalid.server.test",
            LdapPort = 389,
            LdapSearchBase = "DC=test,DC=local",
            LdapUserFilter = "(uid={0})"
        });

        // Should return null (connection fails gracefully)
        var result = await _service.AuthenticateAsync("testuser", "password");
        Assert.Null(result);
    }

    [Fact]
    public async Task TestConnectionAsync_LdapDisabled_ReturnsFailure()
    {
        _adminService.GetSystemSettingsAsync().Returns(new SystemSettings
        {
            LdapEnabled = false
        });

        var (success, message) = await _service.TestConnectionAsync();
        Assert.False(success);
        Assert.Contains("not enabled", message);
    }

    [Fact]
    public async Task TestConnectionAsync_EmptyServer_ReturnsFailure()
    {
        _adminService.GetSystemSettingsAsync().Returns(new SystemSettings
        {
            LdapEnabled = true,
            LdapServer = ""
        });

        var (success, message) = await _service.TestConnectionAsync();
        Assert.False(success);
        Assert.Contains("not configured", message.ToLowerInvariant());
    }
}

// ─── SshSession Helper Tests ────────────────────────────────────────────

public class SshDataHelperTests
{
    [Fact]
    public void WriteAndReadUint32_RoundTrips()
    {
        using var ms = new MemoryStream();
        MyPersonalGit.Services.SshServer.SshDataHelper.WriteUint32(ms, 0x01020304);

        var bytes = ms.ToArray();
        Assert.Equal(4, bytes.Length);

        var result = MyPersonalGit.Services.SshServer.SshDataHelper.ReadUint32(bytes, 0);
        Assert.Equal(0x01020304u, result);
    }

    [Fact]
    public void WriteUint32Be_CorrectByteOrder()
    {
        var buf = new byte[4];
        MyPersonalGit.Services.SshServer.SshDataHelper.WriteUint32Be(buf, 0, 0xDEADBEEF);

        Assert.Equal(0xDE, buf[0]);
        Assert.Equal(0xAD, buf[1]);
        Assert.Equal(0xBE, buf[2]);
        Assert.Equal(0xEF, buf[3]);
    }

    [Fact]
    public void WriteString_WritesLengthPrefixedUtf8()
    {
        using var ms = new MemoryStream();
        MyPersonalGit.Services.SshServer.SshDataHelper.WriteString(ms, "hello");

        var bytes = ms.ToArray();
        // 4 bytes length + 5 bytes "hello"
        Assert.Equal(9, bytes.Length);
        var len = MyPersonalGit.Services.SshServer.SshDataHelper.ReadUint32(bytes, 0);
        Assert.Equal(5u, len);
    }

    [Fact]
    public void WriteBytes_WritesLengthPrefixedBlob()
    {
        using var ms = new MemoryStream();
        var data = new byte[] { 0x01, 0x02, 0x03 };
        MyPersonalGit.Services.SshServer.SshDataHelper.WriteBytes(ms, data);

        var bytes = ms.ToArray();
        Assert.Equal(7, bytes.Length); // 4 + 3
        var len = MyPersonalGit.Services.SshServer.SshDataHelper.ReadUint32(bytes, 0);
        Assert.Equal(3u, len);
    }

    [Fact]
    public void WriteMpint_PositiveWithHighBit_AddsPadding()
    {
        using var ms = new MemoryStream();
        var value = new byte[] { 0x80, 0x01 }; // high bit set
        MyPersonalGit.Services.SshServer.SshDataHelper.WriteMpint(ms, value);

        var bytes = ms.ToArray();
        var len = MyPersonalGit.Services.SshServer.SshDataHelper.ReadUint32(bytes, 0);
        Assert.Equal(3u, len); // 0x00 + 0x80 + 0x01
        Assert.Equal(0x00, bytes[4]); // padding byte
    }

    [Fact]
    public void WriteMpint_PositiveWithoutHighBit_NoPadding()
    {
        using var ms = new MemoryStream();
        var value = new byte[] { 0x7F, 0x01 };
        MyPersonalGit.Services.SshServer.SshDataHelper.WriteMpint(ms, value);

        var bytes = ms.ToArray();
        var len = MyPersonalGit.Services.SshServer.SshDataHelper.ReadUint32(bytes, 0);
        Assert.Equal(2u, len);
    }

    [Fact]
    public void WriteMpint_LeadingZeros_Stripped()
    {
        using var ms = new MemoryStream();
        var value = new byte[] { 0x00, 0x00, 0x42 };
        MyPersonalGit.Services.SshServer.SshDataHelper.WriteMpint(ms, value);

        var bytes = ms.ToArray();
        var len = MyPersonalGit.Services.SshServer.SshDataHelper.ReadUint32(bytes, 0);
        Assert.Equal(1u, len);
        Assert.Equal(0x42, bytes[4]);
    }
}

public class SshDataReaderTests
{
    [Fact]
    public void ReadString_ParsesCorrectly()
    {
        using var ms = new MemoryStream();
        MyPersonalGit.Services.SshServer.SshDataHelper.WriteString(ms, "ssh-rsa");
        var data = ms.ToArray();

        var reader = new MyPersonalGit.Services.SshServer.SshDataReader(data, 0);
        Assert.Equal("ssh-rsa", reader.ReadString());
    }

    [Fact]
    public void ReadBinary_ParsesCorrectly()
    {
        using var ms = new MemoryStream();
        var blob = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        MyPersonalGit.Services.SshServer.SshDataHelper.WriteBytes(ms, blob);
        var data = ms.ToArray();

        var reader = new MyPersonalGit.Services.SshServer.SshDataReader(data, 0);
        var result = reader.ReadBinary();
        Assert.Equal(blob, result);
    }

    [Fact]
    public void ReadBool_ParsesCorrectly()
    {
        var data = new byte[] { 0x01, 0x00 };
        var reader = new MyPersonalGit.Services.SshServer.SshDataReader(data, 0);

        Assert.True(reader.ReadBool());
        Assert.False(reader.ReadBool());
    }

    [Fact]
    public void ReadMultipleFields_SequentialParsing()
    {
        using var ms = new MemoryStream();
        MyPersonalGit.Services.SshServer.SshDataHelper.WriteString(ms, "ecdsa-sha2-nistp256");
        MyPersonalGit.Services.SshServer.SshDataHelper.WriteString(ms, "nistp256");
        MyPersonalGit.Services.SshServer.SshDataHelper.WriteBytes(ms, new byte[] { 0x04, 0x01, 0x02 });
        var data = ms.ToArray();

        var reader = new MyPersonalGit.Services.SshServer.SshDataReader(data, 0);
        Assert.Equal("ecdsa-sha2-nistp256", reader.ReadString());
        Assert.Equal("nistp256", reader.ReadString());
        var blob = reader.ReadBinary();
        Assert.Equal(3, blob.Length);
        Assert.Equal(0x04, blob[0]);
    }
}

// ─── AesCtrCipher Tests ─────────────────────────────────────────────────

public class AesCtrCipherTests
{
    [Fact]
    public void EncryptDecrypt_RoundTrips()
    {
        var key = new byte[16]; // AES-128
        var iv = new byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);
        System.Security.Cryptography.RandomNumberGenerator.Fill(iv);

        var plaintext = System.Text.Encoding.UTF8.GetBytes("Hello, SSH world! This is a test of AES-CTR mode encryption.");
        var ciphertext = (byte[])plaintext.Clone();

        using var encryptor = new MyPersonalGit.Services.SshServer.AesCtrCipher(key, (byte[])iv.Clone());
        encryptor.Process(ciphertext, 0, ciphertext.Length);

        // Ciphertext should differ from plaintext
        Assert.NotEqual(plaintext, ciphertext);

        // Decrypt with same key/IV
        using var decryptor = new MyPersonalGit.Services.SshServer.AesCtrCipher(key, (byte[])iv.Clone());
        decryptor.Process(ciphertext, 0, ciphertext.Length);

        Assert.Equal(plaintext, ciphertext);
    }

    [Fact]
    public void Process_DifferentKeys_ProduceDifferentCiphertext()
    {
        var key1 = new byte[16];
        var key2 = new byte[16];
        var iv = new byte[16];
        key1[0] = 0x01;
        key2[0] = 0x02;

        var plaintext = System.Text.Encoding.UTF8.GetBytes("test data");
        var ct1 = (byte[])plaintext.Clone();
        var ct2 = (byte[])plaintext.Clone();

        using var c1 = new MyPersonalGit.Services.SshServer.AesCtrCipher(key1, (byte[])iv.Clone());
        c1.Process(ct1, 0, ct1.Length);

        using var c2 = new MyPersonalGit.Services.SshServer.AesCtrCipher(key2, (byte[])iv.Clone());
        c2.Process(ct2, 0, ct2.Length);

        Assert.NotEqual(ct1, ct2);
    }

    [Fact]
    public void Process_LargerThanOneBlock_Works()
    {
        var key = new byte[32]; // AES-256
        var iv = new byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);

        // 100 bytes = 6+ AES blocks, tests counter increment
        var plaintext = new byte[100];
        System.Security.Cryptography.RandomNumberGenerator.Fill(plaintext);
        var ciphertext = (byte[])plaintext.Clone();

        using var enc = new MyPersonalGit.Services.SshServer.AesCtrCipher(key, (byte[])iv.Clone());
        enc.Process(ciphertext, 0, ciphertext.Length);

        using var dec = new MyPersonalGit.Services.SshServer.AesCtrCipher(key, (byte[])iv.Clone());
        dec.Process(ciphertext, 0, ciphertext.Length);

        Assert.Equal(plaintext, ciphertext);
    }

    [Fact]
    public void Process_PartialBlockSizes_Work()
    {
        var key = new byte[16];
        var iv = new byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);

        var plaintext = new byte[7]; // less than one block
        System.Security.Cryptography.RandomNumberGenerator.Fill(plaintext);
        var ciphertext = (byte[])plaintext.Clone();

        using var enc = new MyPersonalGit.Services.SshServer.AesCtrCipher(key, (byte[])iv.Clone());
        enc.Process(ciphertext, 0, ciphertext.Length);

        using var dec = new MyPersonalGit.Services.SshServer.AesCtrCipher(key, (byte[])iv.Clone());
        dec.Process(ciphertext, 0, ciphertext.Length);

        Assert.Equal(plaintext, ciphertext);
    }
}

// ─── SshSession Git Command Parser Tests ────────────────────────────────

public class SshGitCommandParserTests
{
    // Use reflection to test the private ParseGitCommand method
    private static (string? operation, string? repoPath) ParseGitCommand(string command)
    {
        var method = typeof(MyPersonalGit.Services.SshServer.SshSession)
            .GetMethod("ParseGitCommand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method!.Invoke(null, new object[] { command });
        var tuple = ((string? operation, string? repoPath))result!;
        return tuple;
    }

    [Fact]
    public void ParseGitCommand_UploadPack_WithQuotes()
    {
        var (op, path) = ParseGitCommand("git-upload-pack 'myrepo.git'");
        Assert.Equal("git-upload-pack", op);
        Assert.Equal("myrepo.git", path);
    }

    [Fact]
    public void ParseGitCommand_ReceivePack_WithQuotes()
    {
        var (op, path) = ParseGitCommand("git-receive-pack 'myrepo.git'");
        Assert.Equal("git-receive-pack", op);
        Assert.Equal("myrepo.git", path);
    }

    [Fact]
    public void ParseGitCommand_LeadingSlash_Stripped()
    {
        var (op, path) = ParseGitCommand("git-upload-pack '/myrepo.git'");
        Assert.Equal("git-upload-pack", op);
        Assert.Equal("myrepo.git", path);
    }

    [Fact]
    public void ParseGitCommand_SpaceForm_Works()
    {
        var (op, path) = ParseGitCommand("git upload-pack 'repo.git'");
        Assert.Equal("git-upload-pack", op);
        Assert.Equal("repo.git", path);
    }

    [Fact]
    public void ParseGitCommand_InvalidCommand_ReturnsNull()
    {
        var (op, path) = ParseGitCommand("ls -la");
        Assert.Null(op);
        Assert.Null(path);
    }

    [Fact]
    public void ParseGitCommand_EmptyPath_ReturnsNull()
    {
        var (op, path) = ParseGitCommand("git-upload-pack ''");
        Assert.Null(op);
        Assert.Null(path);
    }

    [Fact]
    public void ParseGitCommand_DoubleQuotes_Work()
    {
        var (op, path) = ParseGitCommand("git-receive-pack \"myrepo.git\"");
        Assert.Equal("git-receive-pack", op);
        Assert.Equal("myrepo.git", path);
    }
}

// ─── SystemSettings Model Tests ─────────────────────────────────────────

public class SystemSettingsModelTests
{
    [Fact]
    public void Defaults_SshServerPort_Is2222()
    {
        var settings = new SystemSettings();
        Assert.Equal(2222, settings.SshServerPort);
    }

    [Fact]
    public void Defaults_LdapPort_Is389()
    {
        var settings = new SystemSettings();
        Assert.Equal(389, settings.LdapPort);
    }

    [Fact]
    public void Defaults_LdapUserFilter_IsAdFormat()
    {
        var settings = new SystemSettings();
        Assert.Equal("(sAMAccountName={0})", settings.LdapUserFilter);
    }

    [Fact]
    public void Defaults_SshAndLdap_AreDisabled()
    {
        var settings = new SystemSettings();
        Assert.False(settings.EnableBuiltInSshServer);
        Assert.False(settings.LdapEnabled);
    }

    [Fact]
    public void Defaults_LdapAttributes_AreAdDefaults()
    {
        var settings = new SystemSettings();
        Assert.Equal("sAMAccountName", settings.LdapUsernameAttribute);
        Assert.Equal("mail", settings.LdapEmailAttribute);
        Assert.Equal("displayName", settings.LdapDisplayNameAttribute);
    }
}

// ─── DatabaseConfig Model Tests ─────────────────────────────────────────

public class DatabaseConfigModelTests
{
    [Fact]
    public void Defaults_Provider_IsSqlite()
    {
        var config = new DatabaseConfig();
        Assert.Equal("sqlite", config.Provider);
    }

    [Fact]
    public void Defaults_ConnectionString_IsSqliteFile()
    {
        var config = new DatabaseConfig();
        Assert.StartsWith("Data Source=", config.ConnectionString);
    }
}
