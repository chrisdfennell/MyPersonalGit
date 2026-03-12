using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IReactionService
{
    Task<List<Reaction>> GetReactionsForIssueAsync(int issueId);
    Task<List<Reaction>> GetReactionsForIssueCommentAsync(int commentId);
    Task<List<Reaction>> GetReactionsForPullRequestAsync(int pullRequestId);
    Task<List<Reaction>> GetReactionsForReviewCommentAsync(int commentId);
    Task<List<Reaction>> GetReactionsForCommitCommentAsync(int commentId);
    Task<List<Reaction>> GetReactionsForDiscussionAsync(int discussionId);
    Task<List<Reaction>> GetReactionsForDiscussionCommentAsync(int commentId);
    Task<bool> ToggleReactionAsync(string username, string emoji, int? issueId = null, int? issueCommentId = null,
        int? pullRequestId = null, int? reviewCommentId = null, int? commitCommentId = null,
        int? discussionId = null, int? discussionCommentId = null);
    Task<Dictionary<string, List<string>>> GetReactionSummaryAsync(int? issueId = null, int? issueCommentId = null,
        int? pullRequestId = null, int? reviewCommentId = null, int? commitCommentId = null,
        int? discussionId = null, int? discussionCommentId = null);
}

public class ReactionService : IReactionService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ReactionService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Reaction>> GetReactionsForIssueAsync(int issueId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Reactions.Where(r => r.IssueId == issueId).ToListAsync();
    }

    public async Task<List<Reaction>> GetReactionsForIssueCommentAsync(int commentId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Reactions.Where(r => r.IssueCommentId == commentId).ToListAsync();
    }

    public async Task<List<Reaction>> GetReactionsForPullRequestAsync(int pullRequestId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Reactions.Where(r => r.PullRequestId == pullRequestId).ToListAsync();
    }

    public async Task<List<Reaction>> GetReactionsForReviewCommentAsync(int commentId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Reactions.Where(r => r.ReviewCommentId == commentId).ToListAsync();
    }

    public async Task<List<Reaction>> GetReactionsForCommitCommentAsync(int commentId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Reactions.Where(r => r.CommitCommentId == commentId).ToListAsync();
    }

    public async Task<List<Reaction>> GetReactionsForDiscussionAsync(int discussionId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Reactions.Where(r => r.DiscussionId == discussionId).ToListAsync();
    }

    public async Task<List<Reaction>> GetReactionsForDiscussionCommentAsync(int commentId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Reactions.Where(r => r.DiscussionCommentId == commentId).ToListAsync();
    }

    public async Task<bool> ToggleReactionAsync(string username, string emoji, int? issueId = null, int? issueCommentId = null,
        int? pullRequestId = null, int? reviewCommentId = null, int? commitCommentId = null,
        int? discussionId = null, int? discussionCommentId = null)
    {
        using var db = _dbFactory.CreateDbContext();

        var existing = await db.Reactions.FirstOrDefaultAsync(r =>
            r.Username == username && r.Emoji == emoji &&
            r.IssueId == issueId && r.IssueCommentId == issueCommentId &&
            r.PullRequestId == pullRequestId && r.ReviewCommentId == reviewCommentId &&
            r.CommitCommentId == commitCommentId &&
            r.DiscussionId == discussionId && r.DiscussionCommentId == discussionCommentId);

        if (existing != null)
        {
            db.Reactions.Remove(existing);
            await db.SaveChangesAsync();
            return false; // removed
        }

        db.Reactions.Add(new Reaction
        {
            Username = username,
            Emoji = emoji,
            IssueId = issueId,
            IssueCommentId = issueCommentId,
            PullRequestId = pullRequestId,
            ReviewCommentId = reviewCommentId,
            CommitCommentId = commitCommentId,
            DiscussionId = discussionId,
            DiscussionCommentId = discussionCommentId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return true; // added
    }

    public async Task<Dictionary<string, List<string>>> GetReactionSummaryAsync(int? issueId = null, int? issueCommentId = null,
        int? pullRequestId = null, int? reviewCommentId = null, int? commitCommentId = null,
        int? discussionId = null, int? discussionCommentId = null)
    {
        using var db = _dbFactory.CreateDbContext();

        var reactions = await db.Reactions.Where(r =>
            r.IssueId == issueId && r.IssueCommentId == issueCommentId &&
            r.PullRequestId == pullRequestId && r.ReviewCommentId == reviewCommentId &&
            r.CommitCommentId == commitCommentId &&
            r.DiscussionId == discussionId && r.DiscussionCommentId == discussionCommentId)
            .ToListAsync();

        return reactions
            .GroupBy(r => r.Emoji)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Username).ToList());
    }
}
