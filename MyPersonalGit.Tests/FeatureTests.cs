using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Tests;

public class TaskListProgressTests
{
    [Fact]
    public void Count_CountsCheckedAndUnchecked()
    {
        var body = "Intro\n- [x] done thing\n- [ ] open thing\n* [X] also done\n+ [ ] also open\n";
        var (done, total) = TaskListProgress.Count(body);
        Assert.Equal(2, done);
        Assert.Equal(4, total);
    }

    [Fact]
    public void Count_IgnoresNonTaskListLines()
    {
        var body = "some text [x] not a task\n-[x] missing space\n- [y] invalid marker\n";
        var (_, total) = TaskListProgress.Count(body);
        Assert.Equal(0, total);
    }

    [Fact]
    public void Count_HandlesNullAndEmpty()
    {
        Assert.Equal((0, 0), TaskListProgress.Count(null));
        Assert.Equal((0, 0), TaskListProgress.Count(""));
    }

    [Fact]
    public void Count_HandlesIndentedNestedTasks()
    {
        var body = "- [ ] parent\n  - [x] nested child\n";
        var (done, total) = TaskListProgress.Count(body);
        Assert.Equal(1, done);
        Assert.Equal(2, total);
    }
}

public class SubIssueTests
{
    private readonly IssueService _service;

    public SubIssueTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        _service = new IssueService(factory, NullLogger<IssueService>.Instance,
            Substitute.For<INotificationService>(), Substitute.For<IActivityService>());
    }

    [Fact]
    public async Task SetParentIssueAsync_LinksAndLists()
    {
        await _service.CreateIssueAsync("repo", "Parent", null, "alice");
        await _service.CreateIssueAsync("repo", "Child", null, "alice");

        var (success, error) = await _service.SetParentIssueAsync("repo", 2, 1);
        Assert.True(success, error);

        var subs = await _service.GetSubIssuesAsync("repo", 1);
        Assert.Single(subs);
        Assert.Equal("Child", subs[0].Title);
        Assert.Equal(1, (await _service.GetIssueAsync("repo", 2))!.ParentIssueNumber);
    }

    [Fact]
    public async Task SetParentIssueAsync_RejectsSelfAndCycles()
    {
        await _service.CreateIssueAsync("repo", "A", null, "alice");
        await _service.CreateIssueAsync("repo", "B", null, "alice");

        var (selfOk, _) = await _service.SetParentIssueAsync("repo", 1, 1);
        Assert.False(selfOk);

        Assert.True((await _service.SetParentIssueAsync("repo", 2, 1)).Success);
        var (cycleOk, cycleError) = await _service.SetParentIssueAsync("repo", 1, 2);
        Assert.False(cycleOk);
        Assert.Contains("sub-issue", cycleError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetParentIssueAsync_NullDetaches()
    {
        await _service.CreateIssueAsync("repo", "Parent", null, "alice");
        await _service.CreateIssueAsync("repo", "Child", null, "alice");
        await _service.SetParentIssueAsync("repo", 2, 1);

        Assert.True((await _service.SetParentIssueAsync("repo", 2, null)).Success);
        Assert.Empty(await _service.GetSubIssuesAsync("repo", 1));
    }

    [Fact]
    public async Task SetParentIssueAsync_RejectsMissingParent()
    {
        await _service.CreateIssueAsync("repo", "Only", null, "alice");
        var (ok, _) = await _service.SetParentIssueAsync("repo", 1, 99);
        Assert.False(ok);
    }
}

public class MergeQueueServiceTests
{
    private readonly MergeQueueService _service;
    private readonly TestDbContextFactory _factory;

    public MergeQueueServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _factory = new TestDbContextFactory(options);
        _service = new MergeQueueService(_factory, NullLogger<MergeQueueService>.Instance);
    }

    private async Task SeedPr(int number, PullRequestState state = PullRequestState.Open, bool draft = false)
    {
        using var db = _factory.CreateDbContext();
        db.PullRequests.Add(new PullRequest
        {
            RepoName = "repo",
            Number = number,
            Title = $"PR {number}",
            Author = "alice",
            SourceBranch = $"feature-{number}",
            TargetBranch = "main",
            State = state,
            IsDraft = draft
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task EnqueueAsync_AddsEntriesInOrder()
    {
        await SeedPr(1);
        await SeedPr(2);

        Assert.True((await _service.EnqueueAsync("repo", 1, "alice")).Success);
        Assert.True((await _service.EnqueueAsync("repo", 2, "bob", MergeStrategy.Squash)).Success);

        var queue = await _service.GetQueueAsync("repo", "main");
        Assert.Equal(2, queue.Count);
        Assert.Equal(1, queue[0].PullRequestNumber);
        Assert.Equal(2, queue[1].PullRequestNumber);
        Assert.Equal("Squash", queue[1].MergeStrategy);
    }

    [Fact]
    public async Task EnqueueAsync_RejectsDuplicatesDraftsAndClosed()
    {
        await SeedPr(1);
        await SeedPr(2, draft: true);
        await SeedPr(3, PullRequestState.Closed);

        Assert.True((await _service.EnqueueAsync("repo", 1, "alice")).Success);
        Assert.False((await _service.EnqueueAsync("repo", 1, "alice")).Success);
        Assert.False((await _service.EnqueueAsync("repo", 2, "alice")).Success);
        Assert.False((await _service.EnqueueAsync("repo", 3, "alice")).Success);
        Assert.False((await _service.EnqueueAsync("repo", 99, "alice")).Success);
    }

    [Fact]
    public async Task DequeueAsync_CancelsAndAllowsRequeue()
    {
        await SeedPr(1);
        await _service.EnqueueAsync("repo", 1, "alice");

        Assert.True(await _service.DequeueAsync("repo", 1));
        Assert.Null(await _service.GetActiveEntryAsync("repo", 1));
        Assert.Empty(await _service.GetQueueAsync("repo"));

        Assert.True((await _service.EnqueueAsync("repo", 1, "alice")).Success);
    }

    [Theory]
    [InlineData("Required status check 'CI' has not run", true)]
    [InlineData("Required status check 'CI' has not passed (status: InProgress)", true)]
    [InlineData("Required status check 'CI' has not passed (status: Queued)", true)]
    [InlineData("Required status check 'CI' has not passed (status: Pending)", true)]
    [InlineData("Required status check 'CI' has not passed (status: Failure)", false)]
    [InlineData("Requires 2 approval(s), has 0", false)]
    [InlineData("Merge conflicts detected - resolve conflicts before merging", false)]
    [InlineData(null, false)]
    public void IsRetryableReason_ClassifiesCorrectly(string? reason, bool expected)
    {
        Assert.Equal(expected, MergeQueueProcessorService.IsRetryableReason(reason));
    }
}

public class RepoInitTemplatesTests
{
    [Fact]
    public void BuildReadme_IncludesNameAndDescription()
    {
        Assert.Equal("# my-repo\n", RepoInitTemplates.BuildReadme("my-repo", null));
        Assert.Contains("A cool project", RepoInitTemplates.BuildReadme("my-repo", "A cool project"));
    }

    [Fact]
    public void GetLicense_SubstitutesOwnerAndYear()
    {
        var mit = RepoInitTemplates.GetLicense("MIT", "Jane Doe", 2026);
        Assert.NotNull(mit);
        Assert.Contains("Copyright (c) 2026 Jane Doe", mit);
        Assert.Null(RepoInitTemplates.GetLicense("NOPE", "x", 2026));
    }

    [Fact]
    public void AllListedLicensesResolve()
    {
        foreach (var (spdxId, _) in RepoInitTemplates.Licenses)
            Assert.False(string.IsNullOrWhiteSpace(RepoInitTemplates.GetLicense(spdxId, "Owner", 2026)), spdxId);
    }

    [Fact]
    public void AllListedGitignoresResolve()
    {
        Assert.NotEmpty(RepoInitTemplates.GitignoreTemplateNames);
        foreach (var name in RepoInitTemplates.GitignoreTemplateNames)
            Assert.False(string.IsNullOrWhiteSpace(RepoInitTemplates.GetGitignore(name)), name);
    }
}
