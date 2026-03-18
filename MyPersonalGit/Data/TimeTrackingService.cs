using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface ITimeTrackingService
{
    Task<TimeEntry> StartTimerAsync(string repoName, int issueNumber, string username);
    Task<TimeEntry?> StopTimerAsync(string repoName, int issueNumber, string username);
    Task<TimeEntry?> GetRunningTimerAsync(string username);
    Task<TimeEntry> LogTimeAsync(string repoName, int issueNumber, string username, TimeSpan duration, string? note);
    Task<List<TimeEntry>> GetTimeEntriesAsync(string repoName, int issueNumber);
    Task<TimeSpan> GetTotalTimeAsync(string repoName, int issueNumber);
    Task<bool> DeleteTimeEntryAsync(int id, string username);
}

public class TimeTrackingService : ITimeTrackingService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public TimeTrackingService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<TimeEntry> StartTimerAsync(string repoName, int issueNumber, string username)
    {
        using var db = _dbFactory.CreateDbContext();

        // Stop any running timer for this user first
        var running = await db.TimeEntries.FirstOrDefaultAsync(t => t.Username == username && t.IsRunning);
        if (running != null)
        {
            running.IsRunning = false;
            running.StoppedAt = DateTime.UtcNow;
            running.Duration = running.StoppedAt.Value - (running.StartedAt ?? running.CreatedAt);
        }

        var entry = new TimeEntry
        {
            RepoName = repoName,
            IssueNumber = issueNumber,
            Username = username,
            IsRunning = true,
            StartedAt = DateTime.UtcNow,
            Duration = TimeSpan.Zero
        };
        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    public async Task<TimeEntry?> StopTimerAsync(string repoName, int issueNumber, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        var running = await db.TimeEntries.FirstOrDefaultAsync(
            t => t.RepoName == repoName && t.IssueNumber == issueNumber && t.Username == username && t.IsRunning);
        if (running == null) return null;

        running.IsRunning = false;
        running.StoppedAt = DateTime.UtcNow;
        running.Duration = running.StoppedAt.Value - (running.StartedAt ?? running.CreatedAt);
        await db.SaveChangesAsync();
        return running;
    }

    public async Task<TimeEntry?> GetRunningTimerAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.TimeEntries.FirstOrDefaultAsync(t => t.Username == username && t.IsRunning);
    }

    public async Task<TimeEntry> LogTimeAsync(string repoName, int issueNumber, string username, TimeSpan duration, string? note)
    {
        using var db = _dbFactory.CreateDbContext();
        var entry = new TimeEntry
        {
            RepoName = repoName,
            IssueNumber = issueNumber,
            Username = username,
            Duration = duration,
            IsRunning = false,
            Note = note,
        };
        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    public async Task<List<TimeEntry>> GetTimeEntriesAsync(string repoName, int issueNumber)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.TimeEntries
            .Where(t => t.RepoName == repoName && t.IssueNumber == issueNumber)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<TimeSpan> GetTotalTimeAsync(string repoName, int issueNumber)
    {
        using var db = _dbFactory.CreateDbContext();
        var entries = await db.TimeEntries
            .Where(t => t.RepoName == repoName && t.IssueNumber == issueNumber)
            .ToListAsync();

        var total = TimeSpan.Zero;
        foreach (var e in entries)
        {
            if (e.IsRunning && e.StartedAt.HasValue)
                total += DateTime.UtcNow - e.StartedAt.Value;
            else
                total += e.Duration;
        }
        return total;
    }

    public async Task<bool> DeleteTimeEntryAsync(int id, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        var entry = await db.TimeEntries.FindAsync(id);
        if (entry == null || entry.Username != username) return false;
        db.TimeEntries.Remove(entry);
        await db.SaveChangesAsync();
        return true;
    }
}
