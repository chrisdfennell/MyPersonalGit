using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface ICommitCommentService
{
    Task<List<CommitComment>> GetCommentsAsync(string repoName, string commitSha);
    Task<CommitComment> AddCommentAsync(string repoName, string commitSha, string author, string body, string? filePath = null, int? lineNumber = null);
    Task<bool> DeleteCommentAsync(int commentId, string username);
    Task<int> GetCommentCountAsync(string repoName, string commitSha);
}

public class CommitCommentService : ICommitCommentService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<CommitCommentService> _logger;
    private readonly INotificationService _notificationService;

    public CommitCommentService(IDbContextFactory<AppDbContext> dbFactory, ILogger<CommitCommentService> logger, INotificationService notificationService)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<List<CommitComment>> GetCommentsAsync(string repoName, string commitSha)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.CommitComments
            .Where(c => c.RepoName == repoName && c.CommitSha == commitSha)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<CommitComment> AddCommentAsync(string repoName, string commitSha, string author, string body, string? filePath = null, int? lineNumber = null)
    {
        using var db = _dbFactory.CreateDbContext();
        var comment = new CommitComment
        {
            RepoName = repoName,
            CommitSha = commitSha,
            Author = author,
            Body = body,
            FilePath = filePath,
            LineNumber = lineNumber,
            CreatedAt = DateTime.UtcNow
        };

        db.CommitComments.Add(comment);
        await db.SaveChangesAsync();

        _logger.LogInformation("Comment added to commit {Sha} in {Repo} by {Author}", commitSha[..7], repoName, author);

        await _notificationService.CreateNotificationAsync(
            "current-user",
            NotificationType.IssueComment,
            $"Comment on commit {commitSha[..7]}",
            $"{author} commented on commit {commitSha[..7]}",
            repoName,
            $"/repo/{repoName}/commit/{commitSha}"
        );

        return comment;
    }

    public async Task<bool> DeleteCommentAsync(int commentId, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        var comment = await db.CommitComments.FindAsync(commentId);
        if (comment == null || comment.Author != username) return false;
        db.CommitComments.Remove(comment);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetCommentCountAsync(string repoName, string commitSha)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.CommitComments.CountAsync(c => c.RepoName == repoName && c.CommitSha == commitSha);
    }
}
