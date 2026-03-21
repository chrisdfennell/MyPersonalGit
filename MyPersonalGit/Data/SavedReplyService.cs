using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface ISavedReplyService
{
    Task<List<SavedReply>> GetRepliesAsync(string username);
    Task<SavedReply> CreateReplyAsync(string username, string title, string body);
    Task<bool> UpdateReplyAsync(int id, string username, string title, string body);
    Task<bool> DeleteReplyAsync(int id, string username);
}

public class SavedReplyService : ISavedReplyService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public SavedReplyService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<SavedReply>> GetRepliesAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.SavedReplies
            .Where(r => r.Username == username)
            .OrderBy(r => r.Title)
            .ToListAsync();
    }

    public async Task<SavedReply> CreateReplyAsync(string username, string title, string body)
    {
        using var db = _dbFactory.CreateDbContext();
        var reply = new SavedReply
        {
            Username = username,
            Title = title,
            Body = body,
            CreatedAt = DateTime.UtcNow
        };
        db.SavedReplies.Add(reply);
        await db.SaveChangesAsync();
        return reply;
    }

    public async Task<bool> UpdateReplyAsync(int id, string username, string title, string body)
    {
        using var db = _dbFactory.CreateDbContext();
        var reply = await db.SavedReplies.FirstOrDefaultAsync(r => r.Id == id && r.Username == username);
        if (reply == null) return false;
        reply.Title = title;
        reply.Body = body;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteReplyAsync(int id, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        var reply = await db.SavedReplies.FirstOrDefaultAsync(r => r.Id == id && r.Username == username);
        if (reply == null) return false;
        db.SavedReplies.Remove(reply);
        await db.SaveChangesAsync();
        return true;
    }
}
