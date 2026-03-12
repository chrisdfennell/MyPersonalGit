using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IMilestoneService
{
    Task<List<Milestone>> GetMilestonesAsync(string repoName);
    Task<Milestone?> GetMilestoneAsync(string repoName, int number);
    Task<Milestone> CreateMilestoneAsync(string repoName, string title, string? description, DateTime? dueDate, string creator);
    Task<bool> UpdateMilestoneAsync(string repoName, int number, Action<Milestone> updateAction);
    Task<bool> CloseMilestoneAsync(string repoName, int number);
    Task<bool> ReopenMilestoneAsync(string repoName, int number);
    Task<bool> DeleteMilestoneAsync(string repoName, int number);
    Task<(int Open, int Closed)> GetIssueCountsAsync(int milestoneId);
}

public class MilestoneService : IMilestoneService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<MilestoneService> _logger;

    public MilestoneService(IDbContextFactory<AppDbContext> dbFactory, ILogger<MilestoneService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<Milestone>> GetMilestonesAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Milestones
            .Where(m => m.RepoName == repoName)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<Milestone?> GetMilestoneAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Milestones.FirstOrDefaultAsync(m => m.RepoName == repoName && m.Number == number);
    }

    public async Task<Milestone> CreateMilestoneAsync(string repoName, string title, string? description, DateTime? dueDate, string creator)
    {
        using var db = _dbFactory.CreateDbContext();

        var maxNumber = await db.Milestones
            .Where(m => m.RepoName == repoName)
            .MaxAsync(m => (int?)m.Number) ?? 0;

        var milestone = new Milestone
        {
            RepoName = repoName,
            Number = maxNumber + 1,
            Title = title,
            Description = description,
            DueDate = dueDate,
            Creator = creator,
            CreatedAt = DateTime.UtcNow
        };

        db.Milestones.Add(milestone);
        await db.SaveChangesAsync();

        _logger.LogInformation("Milestone #{Number} created in {RepoName}: {Title}", milestone.Number, repoName, title);
        return milestone;
    }

    public async Task<bool> UpdateMilestoneAsync(string repoName, int number, Action<Milestone> updateAction)
    {
        using var db = _dbFactory.CreateDbContext();
        var milestone = await db.Milestones.FirstOrDefaultAsync(m => m.RepoName == repoName && m.Number == number);
        if (milestone == null) return false;
        updateAction(milestone);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CloseMilestoneAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        var milestone = await db.Milestones.FirstOrDefaultAsync(m => m.RepoName == repoName && m.Number == number);
        if (milestone == null) return false;
        milestone.State = MilestoneState.Closed;
        milestone.ClosedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReopenMilestoneAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        var milestone = await db.Milestones.FirstOrDefaultAsync(m => m.RepoName == repoName && m.Number == number);
        if (milestone == null) return false;
        milestone.State = MilestoneState.Open;
        milestone.ClosedAt = null;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMilestoneAsync(string repoName, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        var milestone = await db.Milestones.FirstOrDefaultAsync(m => m.RepoName == repoName && m.Number == number);
        if (milestone == null) return false;

        // Unlink issues from this milestone
        var issues = await db.Issues.Where(i => i.MilestoneId == milestone.Id).ToListAsync();
        foreach (var issue in issues)
            issue.MilestoneId = null;

        db.Milestones.Remove(milestone);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<(int Open, int Closed)> GetIssueCountsAsync(int milestoneId)
    {
        using var db = _dbFactory.CreateDbContext();
        var open = await db.Issues.CountAsync(i => i.MilestoneId == milestoneId && i.State == IssueState.Open);
        var closed = await db.Issues.CountAsync(i => i.MilestoneId == milestoneId && i.State == IssueState.Closed);
        return (open, closed);
    }
}
