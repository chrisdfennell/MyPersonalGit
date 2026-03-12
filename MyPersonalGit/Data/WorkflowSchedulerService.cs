using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;
using GitRepository = LibGit2Sharp.Repository;

namespace MyPersonalGit.Data;

/// <summary>
/// Background service that evaluates cron-based workflow schedules
/// and triggers workflow runs when they're due.
/// Supports standard cron expressions: minute hour day-of-month month day-of-week
/// </summary>
public class WorkflowSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkflowSchedulerService> _logger;

    public WorkflowSchedulerService(IServiceScopeFactory scopeFactory, ILogger<WorkflowSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Workflow scheduler started");

        // Wait a bit on startup for other services to initialize
        await Task.Delay(10_000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanAndTriggerSchedules(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in workflow scheduler");
            }

            // Check every 60 seconds
            await Task.Delay(60_000, stoppingToken);
        }
    }

    private async Task ScanAndTriggerSchedules(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();
        var parser = scope.ServiceProvider.GetRequiredService<WorkflowYamlParser>();
        var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var now = DateTime.UtcNow;

        // Get all enabled schedules that are due
        var schedules = await db.WorkflowSchedules
            .Where(s => s.IsEnabled && (s.NextRunAt == null || s.NextRunAt <= now))
            .ToListAsync(ct);

        if (!schedules.Any()) return;

        var systemSettings = await adminService.GetSystemSettingsAsync();
        var projectRoot = !string.IsNullOrEmpty(systemSettings.ProjectRoot)
            ? systemSettings.ProjectRoot
            : config["Git:ProjectRoot"] ?? "/repos";

        foreach (var schedule in schedules)
        {
            try
            {
                if (!CronMatches(schedule.CronExpression, now) && schedule.NextRunAt != null)
                {
                    // Update next run time and skip
                    schedule.NextRunAt = GetNextCronTime(schedule.CronExpression, now);
                    continue;
                }

                // Find repo and parse workflow
                var repoPath = FindRepoPath(projectRoot, schedule.RepoName);
                if (repoPath == null) continue;

                var workflows = parser.ParseFromRepo(repoPath);
                var workflow = workflows.FirstOrDefault(w => w.FileName == schedule.WorkflowFileName);
                if (workflow == null) continue;

                // Get the HEAD commit info
                string branch = "main", sha = "HEAD", message = "Scheduled run";
                try
                {
                    using var repo = new GitRepository(repoPath);
                    if (repo.Head?.Tip != null)
                    {
                        branch = repo.Head.FriendlyName;
                        sha = repo.Head.Tip.Sha;
                        message = repo.Head.Tip.MessageShort;
                    }
                }
                catch { }

                // Create the workflow run
                await workflowService.CreateWorkflowRunWithJobsAsync(
                    schedule.RepoName, workflow, branch, sha, message, "scheduler");

                schedule.LastRunAt = now;
                schedule.NextRunAt = GetNextCronTime(schedule.CronExpression, now);

                _logger.LogInformation("Scheduled workflow '{WorkflowFile}' triggered for {RepoName} (cron: {Cron})",
                    schedule.WorkflowFileName, schedule.RepoName, schedule.CronExpression);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to trigger scheduled workflow for {RepoName}", schedule.RepoName);
                schedule.NextRunAt = GetNextCronTime(schedule.CronExpression, now);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Parse cron expressions from workflow YAML and register/update schedules in DB.
    /// Called when workflows are detected or repos are scanned.
    /// </summary>
    public static async Task SyncSchedulesFromYamlAsync(
        AppDbContext db, WorkflowYamlParser parser, string repoName, string repoPath)
    {
        var workflows = parser.ParseFromRepo(repoPath);
        var existingSchedules = await db.WorkflowSchedules
            .Where(s => s.RepoName == repoName)
            .ToListAsync();

        var seenFiles = new HashSet<string>();

        foreach (var workflow in workflows)
        {
            var cronExpressions = ExtractScheduleCrons(workflow);
            foreach (var cron in cronExpressions)
            {
                var key = $"{workflow.FileName}|{cron}";
                seenFiles.Add(key);

                var existing = existingSchedules.FirstOrDefault(s =>
                    s.WorkflowFileName == workflow.FileName && s.CronExpression == cron);

                if (existing == null)
                {
                    db.WorkflowSchedules.Add(new WorkflowSchedule
                    {
                        RepoName = repoName,
                        WorkflowFileName = workflow.FileName,
                        CronExpression = cron,
                        IsEnabled = true,
                        NextRunAt = GetNextCronTime(cron, DateTime.UtcNow)
                    });
                }
            }
        }

        // Remove schedules for workflows that no longer have cron triggers
        foreach (var old in existingSchedules)
        {
            var key = $"{old.WorkflowFileName}|{old.CronExpression}";
            if (!seenFiles.Contains(key))
                db.WorkflowSchedules.Remove(old);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Extract cron expressions from the 'on.schedule' trigger in a workflow definition.
    /// Supports: on: schedule or on: { schedule: [{ cron: '...' }] }
    /// </summary>
    private static List<string> ExtractScheduleCrons(WorkflowDefinition workflow)
    {
        var crons = new List<string>();
        if (workflow.On == null) return crons;

        var onStr = workflow.On.ToString() ?? "";

        // Simple string check: "schedule"
        if (onStr.Equals("schedule", StringComparison.OrdinalIgnoreCase))
            return crons; // Need actual cron expression from the dict form

        // Dictionary form: on: { schedule: [{ cron: '...' }] }
        if (workflow.On is Dictionary<object, object> onDict)
        {
            if (onDict.TryGetValue("schedule", out var scheduleObj))
            {
                if (scheduleObj is List<object> scheduleList)
                {
                    foreach (var item in scheduleList)
                    {
                        if (item is Dictionary<object, object> cronDict &&
                            cronDict.TryGetValue("cron", out var cronValue))
                        {
                            var cronStr = cronValue?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(cronStr))
                                crons.Add(cronStr);
                        }
                    }
                }
            }
        }

        return crons;
    }

    /// <summary>
    /// Simple cron matching: minute hour day-of-month month day-of-week
    /// Supports: *, specific values, ranges (1-5), steps (*/5), and lists (1,3,5)
    /// </summary>
    public static bool CronMatches(string cron, DateTime time)
    {
        var parts = cron.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5) return false;

        return FieldMatches(parts[0], time.Minute, 0, 59) &&
               FieldMatches(parts[1], time.Hour, 0, 23) &&
               FieldMatches(parts[2], time.Day, 1, 31) &&
               FieldMatches(parts[3], time.Month, 1, 12) &&
               FieldMatches(parts[4], (int)time.DayOfWeek, 0, 6);
    }

    private static bool FieldMatches(string field, int value, int min, int max)
    {
        if (field == "*") return true;

        // Handle lists: "1,3,5"
        foreach (var part in field.Split(','))
        {
            var trimmed = part.Trim();

            // Handle step: "*/5" or "1-10/2"
            if (trimmed.Contains('/'))
            {
                var stepParts = trimmed.Split('/');
                if (!int.TryParse(stepParts[1], out var step) || step <= 0) continue;

                int rangeStart = min, rangeEnd = max;
                if (stepParts[0] != "*")
                {
                    if (stepParts[0].Contains('-'))
                    {
                        var rangeParts = stepParts[0].Split('-');
                        int.TryParse(rangeParts[0], out rangeStart);
                        int.TryParse(rangeParts[1], out rangeEnd);
                    }
                    else if (int.TryParse(stepParts[0], out var start))
                    {
                        rangeStart = start;
                    }
                }

                for (int i = rangeStart; i <= rangeEnd; i += step)
                {
                    if (i == value) return true;
                }
            }
            // Handle range: "1-5"
            else if (trimmed.Contains('-'))
            {
                var rangeParts = trimmed.Split('-');
                if (int.TryParse(rangeParts[0], out var start) && int.TryParse(rangeParts[1], out var end))
                {
                    if (value >= start && value <= end) return true;
                }
            }
            // Handle exact value
            else if (int.TryParse(trimmed, out var exact))
            {
                if (exact == value) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Calculate the next time a cron expression will match after the given time.
    /// Simple brute-force: checks each minute for the next 48 hours.
    /// </summary>
    public static DateTime GetNextCronTime(string cron, DateTime after)
    {
        var candidate = after.AddMinutes(1);
        candidate = new DateTime(candidate.Year, candidate.Month, candidate.Day, candidate.Hour, candidate.Minute, 0, DateTimeKind.Utc);

        // Check up to 48 hours ahead
        for (int i = 0; i < 2880; i++)
        {
            if (CronMatches(cron, candidate))
                return candidate;
            candidate = candidate.AddMinutes(1);
        }

        // Fallback: 1 hour from now
        return after.AddHours(1);
    }

    private static string? FindRepoPath(string projectRoot, string repoName)
    {
        var path = Path.Combine(projectRoot, repoName);
        if (GitRepository.IsValid(path)) return path;
        if (GitRepository.IsValid(path + ".git")) return path + ".git";
        return null;
    }
}
