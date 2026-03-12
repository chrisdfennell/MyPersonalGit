using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IDiscussionService
{
    Task<List<Discussion>> GetDiscussionsAsync(string repoName);
    Task<Discussion?> GetDiscussionAsync(string repoName, int number);
    Task<Discussion> CreateDiscussionAsync(string repoName, string title, string body, string author, DiscussionCategory category);
    Task<DiscussionComment> AddCommentAsync(int discussionId, string author, string body, int? parentCommentId = null);
    Task<bool> DeleteCommentAsync(int commentId, string username);
    Task<bool> TogglePinAsync(string repoName, int number);
    Task<bool> ToggleLockAsync(string repoName, int number);
    Task<bool> MarkAsAnswerAsync(int commentId);
    Task<bool> UnmarkAsAnswerAsync(int commentId);
    Task<bool> UpvoteCommentAsync(int commentId, string username);
    Task<bool> DeleteDiscussionAsync(string repoName, int number, string username);
}

public class DiscussionService : IDiscussionService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<DiscussionService> _logger;
    private readonly INotificationService _notificationService;

    public DiscussionService(IDbContextFactory<AppDbContext> dbFactory, ILogger<DiscussionService> logger, INotificationService notificationService)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<List<Discussion>> GetDiscussionsAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Discussions
            .Where(d => d.RepoName == repoName)
            .Include(d => d.Comments.Where(c => c.ParentCommentId == null))
            .OrderByDescending(d => d.IsPinned)
            .ThenByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Discussion?> GetDiscussionAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        var discussion = await db.Discussions
            .Include(d => d.Comments)
            .FirstOrDefaultAsync(d => d.RepoName == repoName && d.Number == number);

        if (discussion != null)
        {
            // Build reply tree
            var topLevel = discussion.Comments.Where(c => c.ParentCommentId == null).OrderBy(c => c.CreatedAt).ToList();
            foreach (var comment in topLevel)
            {
                comment.Replies = discussion.Comments
                    .Where(c => c.ParentCommentId == comment.Id)
                    .OrderBy(c => c.CreatedAt)
                    .ToList();
            }
            discussion.Comments = topLevel;
        }

        return discussion;
    }

    public async Task<Discussion> CreateDiscussionAsync(string repoName, string title, string body, string author, DiscussionCategory category)
    {
        using var db = _dbFactory.CreateDbContext();

        var maxNumber = await db.Discussions
            .Where(d => d.RepoName == repoName)
            .MaxAsync(d => (int?)d.Number) ?? 0;

        var discussion = new Discussion
        {
            RepoName = repoName,
            Number = maxNumber + 1,
            Title = title,
            Body = body,
            Author = author,
            Category = category,
            CreatedAt = DateTime.UtcNow
        };

        db.Discussions.Add(discussion);
        await db.SaveChangesAsync();

        _logger.LogInformation("Discussion #{Number} created in {Repo} by {Author}", discussion.Number, repoName, author);

        await _notificationService.CreateNotificationAsync(
            "current-user",
            NotificationType.IssueComment,
            $"New discussion #{discussion.Number}",
            $"{author} started a discussion: {title}",
            repoName,
            $"/repo/{repoName}/discussions/{discussion.Number}"
        );

        return discussion;
    }

    public async Task<DiscussionComment> AddCommentAsync(int discussionId, string author, string body, int? parentCommentId = null)
    {
        using var db = _dbFactory.CreateDbContext();

        var comment = new DiscussionComment
        {
            DiscussionId = discussionId,
            Author = author,
            Body = body,
            ParentCommentId = parentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        db.DiscussionComments.Add(comment);
        await db.SaveChangesAsync();

        _logger.LogInformation("Comment added to discussion {Id} by {Author}", discussionId, author);
        return comment;
    }

    public async Task<bool> DeleteCommentAsync(int commentId, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        var comment = await db.DiscussionComments.FindAsync(commentId);
        if (comment == null || comment.Author != username) return false;

        // Delete replies too
        var replies = await db.DiscussionComments.Where(c => c.ParentCommentId == commentId).ToListAsync();
        db.DiscussionComments.RemoveRange(replies);
        db.DiscussionComments.Remove(comment);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TogglePinAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        var discussion = await db.Discussions.FirstOrDefaultAsync(d => d.RepoName == repoName && d.Number == number);
        if (discussion == null) return false;
        discussion.IsPinned = !discussion.IsPinned;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleLockAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        var discussion = await db.Discussions.FirstOrDefaultAsync(d => d.RepoName == repoName && d.Number == number);
        if (discussion == null) return false;
        discussion.IsLocked = !discussion.IsLocked;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAsAnswerAsync(int commentId)
    {
        using var db = _dbFactory.CreateDbContext();
        var comment = await db.DiscussionComments.FindAsync(commentId);
        if (comment == null) return false;

        var discussion = await db.Discussions.FindAsync(comment.DiscussionId);
        if (discussion == null || discussion.Category != DiscussionCategory.QAndA) return false;

        // Unmark previous answer
        if (discussion.AnswerCommentId.HasValue)
        {
            var prev = await db.DiscussionComments.FindAsync(discussion.AnswerCommentId.Value);
            if (prev != null) prev.IsAnswer = false;
        }

        comment.IsAnswer = true;
        discussion.IsAnswered = true;
        discussion.AnswerCommentId = commentId;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnmarkAsAnswerAsync(int commentId)
    {
        using var db = _dbFactory.CreateDbContext();
        var comment = await db.DiscussionComments.FindAsync(commentId);
        if (comment == null) return false;

        var discussion = await db.Discussions.FindAsync(comment.DiscussionId);
        if (discussion == null) return false;

        comment.IsAnswer = false;
        discussion.IsAnswered = false;
        discussion.AnswerCommentId = null;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpvoteCommentAsync(int commentId, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        var comment = await db.DiscussionComments.FindAsync(commentId);
        if (comment == null) return false;

        comment.UpvoteCount++;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDiscussionAsync(string repoName, int number, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        var discussion = await db.Discussions
            .Include(d => d.Comments)
            .FirstOrDefaultAsync(d => d.RepoName == repoName && d.Number == number);
        if (discussion == null || discussion.Author != username) return false;

        db.DiscussionComments.RemoveRange(discussion.Comments);
        db.Discussions.Remove(discussion);
        await db.SaveChangesAsync();
        return true;
    }
}
