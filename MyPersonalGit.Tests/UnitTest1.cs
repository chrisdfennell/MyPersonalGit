using Microsoft.EntityFrameworkCore;
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
        _service = new IssueService(factory, NullLogger<IssueService>.Instance, _notifications);
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
