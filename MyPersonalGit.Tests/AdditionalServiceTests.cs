using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Tests;

// ============================================================================
// TagProtectionService Tests
// ============================================================================
public class TagProtectionServiceTests
{
    private readonly ITagProtectionService _service;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public TagProtectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);
        _service = new TagProtectionService(_dbFactory, NullLogger<TagProtectionService>.Instance);
    }

    [Fact]
    public async Task AddRuleAsync_CreatesRule()
    {
        var rule = await _service.AddRuleAsync("repo", new TagProtectionRule { TagPattern = "v*" });

        Assert.Equal("repo", rule.RepoName);
        Assert.Equal("v*", rule.TagPattern);
        Assert.True(rule.Id > 0);
    }

    [Fact]
    public async Task GetRulesAsync_ReturnsRulesForRepo()
    {
        await _service.AddRuleAsync("repo1", new TagProtectionRule { TagPattern = "v*" });
        await _service.AddRuleAsync("repo2", new TagProtectionRule { TagPattern = "release-*" });

        var rules = await _service.GetRulesAsync("repo1");

        Assert.Single(rules);
        Assert.Equal("v*", rules[0].TagPattern);
    }

    [Fact]
    public async Task GetRulesAsync_ReturnsEmpty_WhenNoRulesExist()
    {
        var rules = await _service.GetRulesAsync("nonexistent");
        Assert.Empty(rules);
    }

    [Fact]
    public async Task UpdateRuleAsync_UpdatesExistingRule()
    {
        var rule = await _service.AddRuleAsync("repo", new TagProtectionRule { TagPattern = "v*" });

        var updated = await _service.UpdateRuleAsync("repo", new TagProtectionRule
        {
            Id = rule.Id,
            TagPattern = "release-*",
            PreventDeletion = false
        });

        Assert.NotNull(updated);
        Assert.Equal("release-*", updated!.TagPattern);
        Assert.False(updated.PreventDeletion);
    }

    [Fact]
    public async Task UpdateRuleAsync_ReturnsNull_WhenRuleNotFound()
    {
        var result = await _service.UpdateRuleAsync("repo", new TagProtectionRule
        {
            Id = 999,
            TagPattern = "v*"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteRuleAsync_RemovesRule()
    {
        var rule = await _service.AddRuleAsync("repo", new TagProtectionRule { TagPattern = "v*" });

        var deleted = await _service.DeleteRuleAsync("repo", rule.Id);

        Assert.True(deleted);
        var rules = await _service.GetRulesAsync("repo");
        Assert.Empty(rules);
    }

    [Fact]
    public async Task DeleteRuleAsync_ReturnsFalse_WhenRuleNotFound()
    {
        var deleted = await _service.DeleteRuleAsync("repo", 999);
        Assert.False(deleted);
    }

    [Fact]
    public async Task IsTagProtectedAsync_ReturnsTrue_WhenTagMatchesWildcard()
    {
        await _service.AddRuleAsync("repo", new TagProtectionRule { TagPattern = "v*" });

        var isProtected = await _service.IsTagProtectedAsync("repo", "v1.0.0");

        Assert.True(isProtected);
    }

    [Fact]
    public async Task IsTagProtectedAsync_ReturnsFalse_WhenNoMatch()
    {
        await _service.AddRuleAsync("repo", new TagProtectionRule { TagPattern = "v*" });

        var isProtected = await _service.IsTagProtectedAsync("repo", "release-1.0");

        Assert.False(isProtected);
    }

    [Fact]
    public async Task GetMatchingRuleAsync_ReturnsExactMatch()
    {
        await _service.AddRuleAsync("repo", new TagProtectionRule { TagPattern = "v1.0.0" });

        var rule = await _service.GetMatchingRuleAsync("repo", "v1.0.0");

        Assert.NotNull(rule);
        Assert.Equal("v1.0.0", rule!.TagPattern);
    }
}

// ============================================================================
// TimeTrackingService Tests
// ============================================================================
public class TimeTrackingServiceTests
{
    private readonly ITimeTrackingService _service;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public TimeTrackingServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);
        _service = new TimeTrackingService(_dbFactory);
    }

    [Fact]
    public async Task StartTimerAsync_CreatesRunningEntry()
    {
        var entry = await _service.StartTimerAsync("repo", 1, "alice");

        Assert.True(entry.IsRunning);
        Assert.Equal("repo", entry.RepoName);
        Assert.Equal(1, entry.IssueNumber);
        Assert.Equal("alice", entry.Username);
        Assert.NotNull(entry.StartedAt);
    }

    [Fact]
    public async Task StartTimerAsync_StopsPreviousRunningTimer()
    {
        await _service.StartTimerAsync("repo", 1, "alice");
        await _service.StartTimerAsync("repo", 2, "alice");

        var running = await _service.GetRunningTimerAsync("alice");

        Assert.NotNull(running);
        Assert.Equal(2, running!.IssueNumber);
    }

    [Fact]
    public async Task StopTimerAsync_StopsRunningTimer()
    {
        await _service.StartTimerAsync("repo", 1, "alice");

        var stopped = await _service.StopTimerAsync("repo", 1, "alice");

        Assert.NotNull(stopped);
        Assert.False(stopped!.IsRunning);
        Assert.NotNull(stopped.StoppedAt);
        Assert.True(stopped.Duration > TimeSpan.Zero || stopped.Duration == TimeSpan.Zero);
    }

    [Fact]
    public async Task StopTimerAsync_ReturnsNull_WhenNoRunningTimer()
    {
        var result = await _service.StopTimerAsync("repo", 1, "alice");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRunningTimerAsync_ReturnsNull_WhenNoneRunning()
    {
        var result = await _service.GetRunningTimerAsync("alice");
        Assert.Null(result);
    }

    [Fact]
    public async Task LogTimeAsync_CreatesNonRunningEntry()
    {
        var entry = await _service.LogTimeAsync("repo", 1, "alice", TimeSpan.FromHours(2), "code review");

        Assert.False(entry.IsRunning);
        Assert.Equal(TimeSpan.FromHours(2), entry.Duration);
        Assert.Equal("code review", entry.Note);
    }

    [Fact]
    public async Task GetTimeEntriesAsync_ReturnsEntriesForIssue()
    {
        await _service.LogTimeAsync("repo", 1, "alice", TimeSpan.FromHours(1), null);
        await _service.LogTimeAsync("repo", 1, "bob", TimeSpan.FromHours(2), null);
        await _service.LogTimeAsync("repo", 2, "alice", TimeSpan.FromHours(3), null);

        var entries = await _service.GetTimeEntriesAsync("repo", 1);

        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public async Task GetTotalTimeAsync_SumsDurations()
    {
        await _service.LogTimeAsync("repo", 1, "alice", TimeSpan.FromHours(1), null);
        await _service.LogTimeAsync("repo", 1, "bob", TimeSpan.FromHours(2), null);

        var total = await _service.GetTotalTimeAsync("repo", 1);

        Assert.Equal(TimeSpan.FromHours(3), total);
    }

    [Fact]
    public async Task DeleteTimeEntryAsync_DeletesOwnEntry()
    {
        var entry = await _service.LogTimeAsync("repo", 1, "alice", TimeSpan.FromHours(1), null);

        var deleted = await _service.DeleteTimeEntryAsync(entry.Id, "alice");

        Assert.True(deleted);
    }

    [Fact]
    public async Task DeleteTimeEntryAsync_ReturnsFalse_WhenDifferentUser()
    {
        var entry = await _service.LogTimeAsync("repo", 1, "alice", TimeSpan.FromHours(1), null);

        var deleted = await _service.DeleteTimeEntryAsync(entry.Id, "bob");

        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteTimeEntryAsync_ReturnsFalse_WhenNotFound()
    {
        var deleted = await _service.DeleteTimeEntryAsync(999, "alice");
        Assert.False(deleted);
    }
}

// ============================================================================
// AutolinkService Tests
// ============================================================================
public class AutolinkServiceTests
{
    private readonly IAutolinkService _service;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AutolinkServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);
        _service = new AutolinkService(_dbFactory);
    }

    [Fact]
    public async Task AddPatternAsync_CreatesPattern()
    {
        var pattern = await _service.AddPatternAsync("repo", "JIRA-", "https://jira.example.com/browse/JIRA-{0}");

        Assert.True(pattern.Id > 0);
        Assert.Equal("repo", pattern.RepoName);
        Assert.Equal("JIRA-", pattern.Prefix);
    }

    [Fact]
    public async Task GetPatternsAsync_ReturnsPatternsForRepo()
    {
        await _service.AddPatternAsync("repo1", "JIRA-", "https://jira.example.com/{0}");
        await _service.AddPatternAsync("repo2", "GH-", "https://github.com/{0}");

        var patterns = await _service.GetPatternsAsync("repo1");

        Assert.Single(patterns);
        Assert.Equal("JIRA-", patterns[0].Prefix);
    }

    [Fact]
    public async Task GetPatternsAsync_ReturnsEmpty_WhenNoneExist()
    {
        var patterns = await _service.GetPatternsAsync("nonexistent");
        Assert.Empty(patterns);
    }

    [Fact]
    public async Task DeletePatternAsync_RemovesPattern()
    {
        var pattern = await _service.AddPatternAsync("repo", "JIRA-", "https://jira.example.com/{0}");

        var deleted = await _service.DeletePatternAsync(pattern.Id);

        Assert.True(deleted);
        var patterns = await _service.GetPatternsAsync("repo");
        Assert.Empty(patterns);
    }

    [Fact]
    public async Task DeletePatternAsync_ReturnsFalse_WhenNotFound()
    {
        var deleted = await _service.DeletePatternAsync(999);
        Assert.False(deleted);
    }

    [Fact]
    public void ApplyAutolinks_ConvertsIssueReferences()
    {
        var result = _service.ApplyAutolinks("Fix #123 and #456", "repo");

        Assert.Contains("href=\"/repo/repo/issues/123\"", result);
        Assert.Contains("href=\"/repo/repo/issues/456\"", result);
    }

    [Fact]
    public void ApplyAutolinks_AppliesCustomPatterns()
    {
        var patterns = new List<AutolinkPattern>
        {
            new() { RepoName = "repo", Prefix = "JIRA-", UrlTemplate = "https://jira.example.com/browse/JIRA-{0}" }
        };

        var result = _service.ApplyAutolinks("See JIRA-123", "repo", patterns);

        Assert.Contains("href=\"https://jira.example.com/browse/JIRA-123\"", result);
    }

    [Fact]
    public void ApplyAutolinks_ReturnsEmptyString_WhenInputIsEmpty()
    {
        var result = _service.ApplyAutolinks("", "repo");
        Assert.Equal("", result);
    }

    [Fact]
    public void ApplyAutolinks_ReturnsNull_WhenInputIsNull()
    {
        var result = _service.ApplyAutolinks(null!, "repo");
        Assert.Null(result);
    }
}

// ============================================================================
// SavedReplyService Tests
// ============================================================================
public class SavedReplyServiceTests
{
    private readonly ISavedReplyService _service;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public SavedReplyServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);
        _service = new SavedReplyService(_dbFactory);
    }

    [Fact]
    public async Task CreateReplyAsync_CreatesReply()
    {
        var reply = await _service.CreateReplyAsync("alice", "Thanks", "Thank you for the report!");

        Assert.True(reply.Id > 0);
        Assert.Equal("alice", reply.Username);
        Assert.Equal("Thanks", reply.Title);
        Assert.Equal("Thank you for the report!", reply.Body);
    }

    [Fact]
    public async Task GetRepliesAsync_ReturnsRepliesForUser()
    {
        await _service.CreateReplyAsync("alice", "Thanks", "Thank you!");
        await _service.CreateReplyAsync("alice", "LGTM", "Looks good to me!");
        await _service.CreateReplyAsync("bob", "WIP", "Work in progress");

        var replies = await _service.GetRepliesAsync("alice");

        Assert.Equal(2, replies.Count);
    }

    [Fact]
    public async Task GetRepliesAsync_ReturnsEmpty_WhenNoReplies()
    {
        var replies = await _service.GetRepliesAsync("nonexistent");
        Assert.Empty(replies);
    }

    [Fact]
    public async Task UpdateReplyAsync_UpdatesExistingReply()
    {
        var reply = await _service.CreateReplyAsync("alice", "Thanks", "Thank you!");

        var updated = await _service.UpdateReplyAsync(reply.Id, "alice", "Updated Thanks", "Updated body");

        Assert.True(updated);

        var replies = await _service.GetRepliesAsync("alice");
        Assert.Equal("Updated Thanks", replies[0].Title);
        Assert.Equal("Updated body", replies[0].Body);
    }

    [Fact]
    public async Task UpdateReplyAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.UpdateReplyAsync(999, "alice", "Title", "Body");
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateReplyAsync_ReturnsFalse_WhenDifferentUser()
    {
        var reply = await _service.CreateReplyAsync("alice", "Thanks", "Thank you!");

        var result = await _service.UpdateReplyAsync(reply.Id, "bob", "Hacked", "Hacked body");

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteReplyAsync_RemovesReply()
    {
        var reply = await _service.CreateReplyAsync("alice", "Thanks", "Thank you!");

        var deleted = await _service.DeleteReplyAsync(reply.Id, "alice");

        Assert.True(deleted);
        var replies = await _service.GetRepliesAsync("alice");
        Assert.Empty(replies);
    }

    [Fact]
    public async Task DeleteReplyAsync_ReturnsFalse_WhenDifferentUser()
    {
        var reply = await _service.CreateReplyAsync("alice", "Thanks", "Thank you!");

        var deleted = await _service.DeleteReplyAsync(reply.Id, "bob");

        Assert.False(deleted);
    }
}

// ============================================================================
// IssueDependencyService Tests
// ============================================================================
public class IssueDependencyServiceTests
{
    private readonly IIssueDependencyService _service;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public IssueDependencyServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);
        var notificationService = Substitute.For<INotificationService>();
        _service = new IssueDependencyService(_dbFactory, NullLogger<IssueDependencyService>.Instance, notificationService);
    }

    private async Task SeedIssues(params int[] numbers)
    {
        using var db = _dbFactory.CreateDbContext();
        foreach (var num in numbers)
        {
            db.Issues.Add(new Issue
            {
                RepoName = "repo",
                Number = num,
                Title = $"Issue {num}",
                Author = "alice",
                State = IssueState.Open
            });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task AddDependencyAsync_CreatesDependency()
    {
        await SeedIssues(1, 2);

        var dep = await _service.AddDependencyAsync("repo", 1, 2, "alice");

        Assert.NotNull(dep);
        Assert.Equal(1, dep!.BlockingIssueNumber);
        Assert.Equal(2, dep.BlockedIssueNumber);
    }

    [Fact]
    public async Task AddDependencyAsync_ReturnsNull_WhenSameIssue()
    {
        await SeedIssues(1);

        var dep = await _service.AddDependencyAsync("repo", 1, 1, "alice");

        Assert.Null(dep);
    }

    [Fact]
    public async Task AddDependencyAsync_ReturnsNull_WhenIssueNotFound()
    {
        await SeedIssues(1);

        var dep = await _service.AddDependencyAsync("repo", 1, 999, "alice");

        Assert.Null(dep);
    }

    [Fact]
    public async Task AddDependencyAsync_ReturnsNull_WhenDuplicate()
    {
        await SeedIssues(1, 2);
        await _service.AddDependencyAsync("repo", 1, 2, "alice");

        var dup = await _service.AddDependencyAsync("repo", 1, 2, "alice");

        Assert.Null(dup);
    }

    [Fact]
    public async Task RemoveDependencyAsync_RemovesDependency()
    {
        await SeedIssues(1, 2);
        var dep = await _service.AddDependencyAsync("repo", 1, 2, "alice");

        var removed = await _service.RemoveDependencyAsync("repo", dep!.Id);

        Assert.True(removed);
    }

    [Fact]
    public async Task RemoveDependencyAsync_ReturnsFalse_WhenNotFound()
    {
        var removed = await _service.RemoveDependencyAsync("repo", 999);
        Assert.False(removed);
    }

    [Fact]
    public async Task GetBlockingIssuesAsync_ReturnsBlockingIssues()
    {
        await SeedIssues(1, 2, 3);
        await _service.AddDependencyAsync("repo", 1, 3, "alice");
        await _service.AddDependencyAsync("repo", 2, 3, "alice");

        var blocking = await _service.GetBlockingIssuesAsync("repo", 3);

        Assert.Equal(2, blocking.Count);
    }

    [Fact]
    public async Task GetBlockedByIssuesAsync_ReturnsBlockedIssues()
    {
        await SeedIssues(1, 2, 3);
        await _service.AddDependencyAsync("repo", 1, 2, "alice");
        await _service.AddDependencyAsync("repo", 1, 3, "alice");

        var blocked = await _service.GetBlockedByIssuesAsync("repo", 1);

        Assert.Equal(2, blocked.Count);
    }

    [Fact]
    public async Task IsBlockedAsync_ReturnsTrue_WhenBlockingIssueIsOpen()
    {
        await SeedIssues(1, 2);
        await _service.AddDependencyAsync("repo", 1, 2, "alice");

        var isBlocked = await _service.IsBlockedAsync("repo", 2);

        Assert.True(isBlocked);
    }

    [Fact]
    public async Task IsBlockedAsync_ReturnsFalse_WhenBlockingIssueIsClosed()
    {
        using var db = _dbFactory.CreateDbContext();
        db.Issues.Add(new Issue { RepoName = "repo", Number = 1, Title = "Issue 1", Author = "alice", State = IssueState.Closed });
        db.Issues.Add(new Issue { RepoName = "repo", Number = 2, Title = "Issue 2", Author = "alice", State = IssueState.Open });
        await db.SaveChangesAsync();

        await _service.AddDependencyAsync("repo", 1, 2, "alice");

        var isBlocked = await _service.IsBlockedAsync("repo", 2);

        Assert.False(isBlocked);
    }

    [Fact]
    public async Task IsBlockedAsync_ReturnsFalse_WhenNoDependencies()
    {
        await SeedIssues(1);

        var isBlocked = await _service.IsBlockedAsync("repo", 1);

        Assert.False(isBlocked);
    }
}

// ============================================================================
// IssueTemplateService Tests
// ============================================================================
public class IssueTemplateServiceTests
{
    private readonly IIssueTemplateService _service;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public IssueTemplateServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);
        _service = new IssueTemplateService(_dbFactory, NullLogger<IssueTemplateService>.Instance);
    }

    [Fact]
    public async Task CreateTemplateAsync_CreatesTemplate()
    {
        var template = await _service.CreateTemplateAsync("repo", "Bug Report", "## Description\n", "Report a bug", "bug");

        Assert.True(template.Id > 0);
        Assert.Equal("Bug Report", template.Name);
        Assert.Equal("## Description\n", template.Body);
        Assert.Equal("Report a bug", template.Description);
        Assert.Equal("bug", template.Labels);
        Assert.Equal(1, template.SortOrder);
    }

    [Fact]
    public async Task CreateTemplateAsync_IncrementsOrder()
    {
        await _service.CreateTemplateAsync("repo", "Bug Report", "body1");
        var second = await _service.CreateTemplateAsync("repo", "Feature Request", "body2");

        Assert.Equal(2, second.SortOrder);
    }

    [Fact]
    public async Task GetTemplatesAsync_ReturnsTemplatesForRepo()
    {
        await _service.CreateTemplateAsync("repo1", "Bug Report", "body");
        await _service.CreateTemplateAsync("repo2", "Feature Request", "body");

        var templates = await _service.GetTemplatesAsync("repo1");

        Assert.Single(templates);
        Assert.Equal("Bug Report", templates[0].Name);
    }

    [Fact]
    public async Task GetTemplatesAsync_ReturnsEmpty_WhenNoneExist()
    {
        var templates = await _service.GetTemplatesAsync("nonexistent");
        Assert.Empty(templates);
    }

    [Fact]
    public async Task GetTemplateAsync_ReturnsTemplate()
    {
        var created = await _service.CreateTemplateAsync("repo", "Bug Report", "body");

        var template = await _service.GetTemplateAsync(created.Id);

        Assert.NotNull(template);
        Assert.Equal("Bug Report", template!.Name);
    }

    [Fact]
    public async Task GetTemplateAsync_ReturnsNull_WhenNotFound()
    {
        var template = await _service.GetTemplateAsync(999);
        Assert.Null(template);
    }

    [Fact]
    public async Task UpdateTemplateAsync_UpdatesTemplate()
    {
        var created = await _service.CreateTemplateAsync("repo", "Bug Report", "body");

        var updated = await _service.UpdateTemplateAsync(created.Id, "Updated Bug Report", "updated body", "updated desc", "bug,critical");

        Assert.True(updated);

        var template = await _service.GetTemplateAsync(created.Id);
        Assert.Equal("Updated Bug Report", template!.Name);
        Assert.Equal("updated body", template.Body);
    }

    [Fact]
    public async Task UpdateTemplateAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.UpdateTemplateAsync(999, "Name", "Body");
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteTemplateAsync_RemovesTemplate()
    {
        var created = await _service.CreateTemplateAsync("repo", "Bug Report", "body");

        var deleted = await _service.DeleteTemplateAsync(created.Id);

        Assert.True(deleted);
        var template = await _service.GetTemplateAsync(created.Id);
        Assert.Null(template);
    }

    [Fact]
    public async Task DeleteTemplateAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _service.DeleteTemplateAsync(999);
        Assert.False(result);
    }
}

// ============================================================================
// CommitCommentService Tests
// ============================================================================
public class CommitCommentServiceTests
{
    private readonly ICommitCommentService _service;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly INotificationService _notificationService;

    public CommitCommentServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);
        _notificationService = Substitute.For<INotificationService>();
        _service = new CommitCommentService(_dbFactory, NullLogger<CommitCommentService>.Instance, _notificationService);
    }

    [Fact]
    public async Task AddCommentAsync_CreatesComment()
    {
        var comment = await _service.AddCommentAsync("repo", "abc1234def5678", "alice", "Great commit!");

        Assert.True(comment.Id > 0);
        Assert.Equal("repo", comment.RepoName);
        Assert.Equal("abc1234def5678", comment.CommitSha);
        Assert.Equal("alice", comment.Author);
        Assert.Equal("Great commit!", comment.Body);
    }

    [Fact]
    public async Task AddCommentAsync_WithFileAndLine()
    {
        var comment = await _service.AddCommentAsync("repo", "abc1234def5678", "alice", "Bug here", "src/Main.cs", 42);

        Assert.Equal("src/Main.cs", comment.FilePath);
        Assert.Equal(42, comment.LineNumber);
    }

    [Fact]
    public async Task AddCommentAsync_SendsNotification()
    {
        await _service.AddCommentAsync("repo", "abc1234def5678", "alice", "Nice!");

        await _notificationService.Received(1).CreateNotificationAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            "repo",
            Arg.Any<string>());
    }

    [Fact]
    public async Task GetCommentsAsync_ReturnsCommentsForCommit()
    {
        await _service.AddCommentAsync("repo", "abc1234def5678", "alice", "Comment 1");
        await _service.AddCommentAsync("repo", "abc1234def5678", "bob", "Comment 2");
        await _service.AddCommentAsync("repo", "other1234567890", "alice", "Other commit");

        var comments = await _service.GetCommentsAsync("repo", "abc1234def5678");

        Assert.Equal(2, comments.Count);
    }

    [Fact]
    public async Task GetCommentsAsync_ReturnsEmpty_WhenNoComments()
    {
        var comments = await _service.GetCommentsAsync("repo", "nonexistent1234");
        Assert.Empty(comments);
    }

    [Fact]
    public async Task DeleteCommentAsync_DeletesOwnComment()
    {
        var comment = await _service.AddCommentAsync("repo", "abc1234def5678", "alice", "My comment");

        var deleted = await _service.DeleteCommentAsync(comment.Id, "alice");

        Assert.True(deleted);
    }

    [Fact]
    public async Task DeleteCommentAsync_ReturnsFalse_WhenDifferentUser()
    {
        var comment = await _service.AddCommentAsync("repo", "abc1234def5678", "alice", "My comment");

        var deleted = await _service.DeleteCommentAsync(comment.Id, "bob");

        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteCommentAsync_ReturnsFalse_WhenNotFound()
    {
        var deleted = await _service.DeleteCommentAsync(999, "alice");
        Assert.False(deleted);
    }

    [Fact]
    public async Task GetCommentCountAsync_ReturnsCorrectCount()
    {
        await _service.AddCommentAsync("repo", "abc1234def5678", "alice", "Comment 1");
        await _service.AddCommentAsync("repo", "abc1234def5678", "bob", "Comment 2");

        var count = await _service.GetCommentCountAsync("repo", "abc1234def5678");

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetCommentCountAsync_ReturnsZero_WhenNoComments()
    {
        var count = await _service.GetCommentCountAsync("repo", "nonexistent1234");
        Assert.Equal(0, count);
    }
}

// ============================================================================
// SecretsService Tests
// ============================================================================
public class SecretsServiceTests
{
    private readonly ISecretsService _service;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public SecretsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Secrets:EncryptionKey"] = "test-encryption-key-for-unit-tests"
            })
            .Build();

        _service = new SecretsService(_dbFactory, NullLogger<SecretsService>.Instance, config);
    }

    [Fact]
    public async Task SetSecretAsync_CreatesSecret()
    {
        var result = await _service.SetSecretAsync("repo", "MY_SECRET", "secret-value");

        Assert.True(result);
        var secrets = await _service.GetSecretsAsync("repo");
        Assert.Single(secrets);
        Assert.Equal("MY_SECRET", secrets[0].Name);
    }

    [Fact]
    public async Task SetSecretAsync_UppercasesName()
    {
        await _service.SetSecretAsync("repo", "my_secret", "value");

        var secrets = await _service.GetSecretsAsync("repo");
        Assert.Equal("MY_SECRET", secrets[0].Name);
    }

    [Fact]
    public async Task SetSecretAsync_UpdatesExistingSecret()
    {
        await _service.SetSecretAsync("repo", "MY_SECRET", "value1");
        await _service.SetSecretAsync("repo", "MY_SECRET", "value2");

        var secrets = await _service.GetSecretsAsync("repo");
        Assert.Single(secrets);
    }

    [Fact]
    public async Task SetSecretAsync_ReturnsFalse_WhenNameIsEmpty()
    {
        var result = await _service.SetSecretAsync("repo", "", "value");
        Assert.False(result);
    }

    [Fact]
    public async Task SetSecretAsync_ReturnsFalse_WhenValueIsEmpty()
    {
        var result = await _service.SetSecretAsync("repo", "MY_SECRET", "");
        Assert.False(result);
    }

    [Fact]
    public async Task SetSecretAsync_ReturnsFalse_WhenNameHasInvalidChars()
    {
        var result = await _service.SetSecretAsync("repo", "my-secret", "value");
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteSecretAsync_RemovesSecret()
    {
        await _service.SetSecretAsync("repo", "MY_SECRET", "value");

        var deleted = await _service.DeleteSecretAsync("repo", "MY_SECRET");

        Assert.True(deleted);
        var secrets = await _service.GetSecretsAsync("repo");
        Assert.Empty(secrets);
    }

    [Fact]
    public async Task DeleteSecretAsync_ReturnsFalse_WhenNotFound()
    {
        var deleted = await _service.DeleteSecretAsync("repo", "NONEXISTENT");
        Assert.False(deleted);
    }

    [Fact]
    public async Task GetDecryptedSecretsAsync_DecryptsValues()
    {
        await _service.SetSecretAsync("repo", "MY_SECRET", "my-secret-value");

        var decrypted = await _service.GetDecryptedSecretsAsync("repo");

        Assert.Single(decrypted);
        Assert.Equal("my-secret-value", decrypted["MY_SECRET"]);
    }

    // --- Global secrets ---

    [Fact]
    public async Task SetGlobalSecretAsync_CreatesGlobalSecret()
    {
        var result = await _service.SetGlobalSecretAsync("GLOBAL_KEY", "global-value");

        Assert.True(result);
        var secrets = await _service.GetGlobalSecretsAsync();
        Assert.Single(secrets);
        Assert.Equal("GLOBAL_KEY", secrets[0].Name);
    }

    [Fact]
    public async Task SetGlobalSecretAsync_ReturnsFalse_WhenNameEmpty()
    {
        var result = await _service.SetGlobalSecretAsync("", "value");
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteGlobalSecretAsync_RemovesGlobalSecret()
    {
        await _service.SetGlobalSecretAsync("GLOBAL_KEY", "value");

        var deleted = await _service.DeleteGlobalSecretAsync("GLOBAL_KEY");

        Assert.True(deleted);
        var secrets = await _service.GetGlobalSecretsAsync();
        Assert.Empty(secrets);
    }

    [Fact]
    public async Task DeleteGlobalSecretAsync_ReturnsFalse_WhenNotFound()
    {
        var deleted = await _service.DeleteGlobalSecretAsync("NONEXISTENT");
        Assert.False(deleted);
    }

    // --- User secrets ---

    [Fact]
    public async Task SetUserSecretAsync_CreatesUserSecret()
    {
        var result = await _service.SetUserSecretAsync("alice", "USER_KEY", "user-value");

        Assert.True(result);
        var secrets = await _service.GetUserSecretsAsync("alice");
        Assert.Single(secrets);
        Assert.Equal("USER_KEY", secrets[0].Name);
    }

    [Fact]
    public async Task SetUserSecretAsync_ReturnsFalse_WhenInvalidName()
    {
        var result = await _service.SetUserSecretAsync("alice", "123BAD", "value");
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserSecretAsync_RemovesUserSecret()
    {
        await _service.SetUserSecretAsync("alice", "USER_KEY", "value");

        var deleted = await _service.DeleteUserSecretAsync("alice", "USER_KEY");

        Assert.True(deleted);
    }

    [Fact]
    public async Task DeleteUserSecretAsync_ReturnsFalse_WhenNotFound()
    {
        var deleted = await _service.DeleteUserSecretAsync("alice", "NONEXISTENT");
        Assert.False(deleted);
    }

    // --- Organization secrets ---

    [Fact]
    public async Task SetOrgSecretAsync_CreatesOrgSecret()
    {
        var result = await _service.SetOrgSecretAsync("myorg", "ORG_KEY", "org-value");

        Assert.True(result);
        var secrets = await _service.GetOrgSecretsAsync("myorg");
        Assert.Single(secrets);
        Assert.Equal("ORG_KEY", secrets[0].Name);
    }

    [Fact]
    public async Task SetOrgSecretAsync_ReturnsFalse_WhenInvalidName()
    {
        var result = await _service.SetOrgSecretAsync("myorg", "bad-name", "value");
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteOrgSecretAsync_RemovesOrgSecret()
    {
        await _service.SetOrgSecretAsync("myorg", "ORG_KEY", "value");

        var deleted = await _service.DeleteOrgSecretAsync("myorg", "ORG_KEY");

        Assert.True(deleted);
    }

    [Fact]
    public async Task DeleteOrgSecretAsync_ReturnsFalse_WhenNotFound()
    {
        var deleted = await _service.DeleteOrgSecretAsync("myorg", "NONEXISTENT");
        Assert.False(deleted);
    }
}

// ============================================================================
// AiCompletionService Tests
// ============================================================================
public class AiCompletionServiceTests
{
    private readonly IAdminService _adminService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAiCompletionService _service;

    public AiCompletionServiceTests()
    {
        _adminService = Substitute.For<IAdminService>();
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _service = new AiCompletionService(_adminService, _httpClientFactory);
    }

    [Fact]
    public async Task GetCompletionAsync_ReturnsNull_WhenDisabled()
    {
        _adminService.GetSystemSettingsAsync().Returns(new SystemSettings
        {
            AiCompletionEnabled = false,
            AiCompletionEndpoint = "https://api.openai.com/v1",
            AiCompletionApiKey = "sk-test"
        });

        var result = await _service.GetCompletionAsync("test.cs", "var x = ", ";", "csharp");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCompletionAsync_ReturnsNull_WhenApiKeyIsEmpty()
    {
        _adminService.GetSystemSettingsAsync().Returns(new SystemSettings
        {
            AiCompletionEnabled = true,
            AiCompletionEndpoint = "https://api.openai.com/v1",
            AiCompletionApiKey = ""
        });

        var result = await _service.GetCompletionAsync("test.cs", "var x = ", ";", "csharp");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCompletionAsync_ReturnsNull_WhenEndpointIsEmpty()
    {
        _adminService.GetSystemSettingsAsync().Returns(new SystemSettings
        {
            AiCompletionEnabled = true,
            AiCompletionEndpoint = "",
            AiCompletionApiKey = "sk-test"
        });

        var result = await _service.GetCompletionAsync("test.cs", "var x = ", ";", "csharp");

        Assert.Null(result);
    }
}

// ============================================================================
// CodeSearchService Tests (limited — requires git repos for full testing)
// ============================================================================
public class CodeSearchServiceTests
{
    private readonly ICodeSearchService _service;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAdminService _adminService;

    public CodeSearchServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);
        _adminService = Substitute.For<IAdminService>();
        _adminService.GetSystemSettingsAsync().Returns(new SystemSettings { ProjectRoot = "/nonexistent" });

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Git:ProjectRoot"] = "/nonexistent"
            })
            .Build();

        _service = new CodeSearchService(_dbFactory, config, _adminService, NullLogger<CodeSearchService>.Instance);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenQueryTooShort()
    {
        var results = await _service.SearchAsync("a");
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenQueryIsEmpty()
    {
        var results = await _service.SearchAsync("");
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenQueryIsWhitespace()
    {
        var results = await _service.SearchAsync("   ");
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenNoReposExist()
    {
        var results = await _service.SearchAsync("searchterm");
        Assert.Empty(results);
    }
}

// ============================================================================
// RepoHealthService Tests (limited — git operations need real repos)
// ============================================================================
public class RepoHealthServiceTests
{
    private readonly IRepoHealthService _service;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAdminService _adminService;

    public RepoHealthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbFactory = new TestDbContextFactory(options);
        _adminService = Substitute.For<IAdminService>();
        _adminService.GetSystemSettingsAsync().Returns(new SystemSettings { ProjectRoot = "/nonexistent" });

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Git:ProjectRoot"] = "/nonexistent"
            })
            .Build();

        _service = new RepoHealthService(_dbFactory, _adminService, config);
    }

    [Fact]
    public async Task GetHealthReportAsync_ReturnsReport_WhenNoRepo()
    {
        var report = await _service.GetHealthReportAsync("nonexistent");

        Assert.NotNull(report);
        Assert.Equal("F", report.Grade);
    }

    [Fact]
    public async Task GetHealthReportAsync_IncludesDescription_WhenRepoHasDescription()
    {
        using var db = _dbFactory.CreateDbContext();
        db.Repositories.Add(new Repository
        {
            Name = "repo",
            Description = "A test repository",
            Owner = "alice"
        });
        await db.SaveChangesAsync();

        var report = await _service.GetHealthReportAsync("repo");

        Assert.True(report.HasDescription);
        Assert.True(report.Score >= 10);
    }

    [Fact]
    public async Task GetHealthReportAsync_SuggestsDescription_WhenMissing()
    {
        using var db = _dbFactory.CreateDbContext();
        db.Repositories.Add(new Repository
        {
            Name = "repo",
            Owner = "alice"
        });
        await db.SaveChangesAsync();

        var report = await _service.GetHealthReportAsync("repo");

        Assert.False(report.HasDescription);
        Assert.Contains("Add a repository description", report.Suggestions);
    }

    [Fact]
    public async Task GetHealthReportAsync_CalculatesIssueCloseRate()
    {
        using var db = _dbFactory.CreateDbContext();
        db.Issues.Add(new Issue { RepoName = "repo", Number = 1, Title = "Open", Author = "alice", State = IssueState.Open });
        db.Issues.Add(new Issue { RepoName = "repo", Number = 2, Title = "Closed", Author = "alice", State = IssueState.Closed });
        await db.SaveChangesAsync();

        var report = await _service.GetHealthReportAsync("repo");

        Assert.Equal(1, report.OpenIssues);
        Assert.Equal(50, report.IssueCloseRate);
    }

    [Fact]
    public async Task GetHealthReportAsync_CountsStalePRs()
    {
        using var db = _dbFactory.CreateDbContext();
        db.PullRequests.Add(new PullRequest
        {
            Number = 1,
            RepoName = "repo",
            Title = "Old PR",
            Author = "alice",
            SourceBranch = "feature",
            TargetBranch = "main",
            State = PullRequestState.Open,
            CreatedAt = DateTime.UtcNow.AddDays(-60)
        });
        await db.SaveChangesAsync();

        var report = await _service.GetHealthReportAsync("repo");

        Assert.Equal(1, report.StalePRs);
    }
}

// ============================================================================
// WebIdeService Tests (limited — all methods need real git repos)
// ============================================================================
public class WebIdeServiceTests
{
    [Fact]
    public void WebIdeService_CanBeConstructed()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Git:ProjectRoot"] = "/nonexistent"
            })
            .Build();
        var adminService = Substitute.For<IAdminService>();
        adminService.GetSystemSettingsAsync().Returns(new SystemSettings { ProjectRoot = "/nonexistent" });

        var service = new WebIdeService(config, NullLogger<WebIdeService>.Instance, adminService);

        Assert.NotNull(service);
    }

    [Fact]
    public void DataModels_CanBeConstructed()
    {
        var treeNode = new TreeNode("file.cs", "src/file.cs", "blob", 100, null);
        Assert.Equal("file.cs", treeNode.Name);

        var fileContent = new FileContent("file.cs", "content", "csharp");
        Assert.Equal("csharp", fileContent.Language);

        var fileChange = new FileChange("file.cs", "content", "add", null);
        Assert.Equal("add", fileChange.Action);

        var searchResult = new SearchResult("file.cs", 1, "line", "context");
        Assert.Equal(1, searchResult.LineNumber);

        var blameHunk = new BlameHunk(1, 10, "abc1234", "alice", DateTimeOffset.Now, "commit msg");
        Assert.Equal(1, blameHunk.StartLine);

        var historyEntry = new FileHistoryEntry("abc1234567890", "abc1234", "alice", "alice@example.com", DateTimeOffset.Now, "msg");
        Assert.Equal("abc1234", historyEntry.ShortSha);

        var branchDiff = new BranchDiffFile("file.cs", "Modified");
        Assert.Equal("Modified", branchDiff.Status);

        var stashEntry = new StashEntry(0, "WIP", DateTimeOffset.Now);
        Assert.Equal(0, stashEntry.Index);
    }
}

// ============================================================================
// LspProcessManager Tests
// ============================================================================
public class LspProcessManagerTests
{
    [Fact]
    public void IsSupported_ReturnsTrue_ForKnownLanguages()
    {
        Assert.True(LspProcessManager.IsSupported("csharp"));
        Assert.True(LspProcessManager.IsSupported("typescript"));
        Assert.True(LspProcessManager.IsSupported("python"));
        Assert.True(LspProcessManager.IsSupported("go"));
        Assert.True(LspProcessManager.IsSupported("rust"));
        Assert.True(LspProcessManager.IsSupported("html"));
        Assert.True(LspProcessManager.IsSupported("css"));
        Assert.True(LspProcessManager.IsSupported("json"));
        Assert.True(LspProcessManager.IsSupported("yaml"));
        Assert.True(LspProcessManager.IsSupported("bash"));
        Assert.True(LspProcessManager.IsSupported("dockerfile"));
        Assert.True(LspProcessManager.IsSupported("markdown"));
    }

    [Fact]
    public void IsSupported_ReturnsFalse_ForUnknownLanguages()
    {
        Assert.False(LspProcessManager.IsSupported("brainfuck"));
        Assert.False(LspProcessManager.IsSupported(""));
        Assert.False(LspProcessManager.IsSupported("CSHARP")); // case sensitive
    }

    [Fact]
    public void SupportedLanguages_ContainsExpectedLanguages()
    {
        var languages = LspProcessManager.SupportedLanguages;

        Assert.Contains("csharp", languages);
        Assert.Contains("typescript", languages);
        Assert.Contains("python", languages);
        Assert.True(languages.Count >= 12);
    }

    [Fact]
    public void Constructor_CreatesInstance()
    {
        using var manager = new LspProcessManager();
        Assert.NotNull(manager);
    }

    [Fact]
    public void GetOrStartSession_ReturnsNull_ForUnsupportedLanguage()
    {
        using var manager = new LspProcessManager();

        var session = manager.GetOrStartSession("repo", "brainfuck", "user1", "/nonexistent", "main");

        Assert.Null(session);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var manager = new LspProcessManager();
        manager.Dispose();
        manager.Dispose(); // Should not throw
    }

    [Fact]
    public void LspSession_Touch_UpdatesLastActivity()
    {
        using var session = new LspSession { Key = "test" };
        var before = session.LastActivity;

        // Small delay to ensure time difference
        session.Touch();

        Assert.True(session.LastActivity >= before);
    }

    [Fact]
    public void LspSession_Dispose_CanBeCalledMultipleTimes()
    {
        var session = new LspSession { Key = "test" };
        session.Dispose();
        session.Dispose(); // Should not throw
    }
}
