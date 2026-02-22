using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IWorkflowService
{
    Task<List<WorkflowRun>> GetWorkflowRunsAsync(string repoName);
    Task<WorkflowRun?> GetWorkflowRunAsync(string repoName, int runId);
    Task<WorkflowRun> CreateWorkflowRunAsync(string repoName, string workflowName, string branch, string commitSha, string commitMessage, string triggeredBy);
    Task<bool> UpdateWorkflowRunAsync(string repoName, int runId, Action<WorkflowRun> updateAction);
    Task<WorkflowRun> CreateWorkflowRunWithJobsAsync(string repoName, WorkflowDefinition definition, string branch, string sha, string message, string user);
    Task<List<Webhook>> GetWebhooksAsync(string repoName);
    Task<Webhook> CreateWebhookAsync(string repoName, string url, string secret, List<string> events);
    Task<bool> DeleteWebhookAsync(string repoName, int webhookId);
    Task<bool> ToggleWebhookAsync(string repoName, int webhookId);
    Task<List<WebhookDelivery>> GetWebhookDeliveriesAsync(string repoName, int webhookId);
}

public class WorkflowService : IWorkflowService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(IDbContextFactory<AppDbContext> dbFactory, ILogger<WorkflowService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<WorkflowRun>> GetWorkflowRunsAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.WorkflowRuns
            .Include(r => r.Jobs)
                .ThenInclude(j => j.Steps)
            .Where(r => r.RepoName == repoName)
            .ToListAsync();
    }

    public async Task<WorkflowRun?> GetWorkflowRunAsync(string repoName, int runId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.WorkflowRuns
            .Include(r => r.Jobs)
                .ThenInclude(j => j.Steps)
            .FirstOrDefaultAsync(r => r.RepoName == repoName && r.Id == runId);
    }

    public async Task<WorkflowRun> CreateWorkflowRunAsync(
        string repoName, string workflowName, string branch,
        string commitSha, string commitMessage, string triggeredBy)
    {
        using var db = _dbFactory.CreateDbContext();

        var run = new WorkflowRun
        {
            RepoName = repoName,
            WorkflowName = workflowName,
            Branch = branch,
            CommitSha = commitSha,
            CommitMessage = commitMessage,
            TriggeredBy = triggeredBy,
            Status = WorkflowStatus.Queued,
            CreatedAt = DateTime.UtcNow
        };

        db.WorkflowRuns.Add(run);
        await db.SaveChangesAsync();

        _logger.LogInformation("Workflow run {RunId} created for {RepoName} by {TriggeredBy}", run.Id, repoName, triggeredBy);
        return run;
    }

    public async Task<bool> UpdateWorkflowRunAsync(string repoName, int runId, Action<WorkflowRun> updateAction)
    {
        using var db = _dbFactory.CreateDbContext();

        var run = await db.WorkflowRuns
            .Include(r => r.Jobs)
                .ThenInclude(j => j.Steps)
            .FirstOrDefaultAsync(r => r.RepoName == repoName && r.Id == runId);

        if (run == null)
            return false;

        updateAction(run);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<WorkflowRun> CreateWorkflowRunWithJobsAsync(
        string repoName, WorkflowDefinition definition, string branch, string sha, string message, string user)
    {
        using var db = _dbFactory.CreateDbContext();

        var run = new WorkflowRun
        {
            RepoName = repoName,
            WorkflowName = definition.Name,
            Branch = branch,
            CommitSha = sha,
            CommitMessage = message,
            TriggeredBy = user,
            Status = WorkflowStatus.Queued,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var (jobName, jobDef) in definition.Jobs)
        {
            var job = new WorkflowJob
            {
                Name = jobName,
                RunsOn = jobDef.RunsOn,
                Status = WorkflowStatus.Queued
            };

            foreach (var stepDef in jobDef.Steps)
            {
                job.Steps.Add(new WorkflowStep
                {
                    Name = stepDef.Name ?? stepDef.Run ?? stepDef.Uses ?? "Step",
                    Command = stepDef.Run,
                    Status = WorkflowStatus.Queued
                });
            }

            run.Jobs.Add(job);
        }

        db.WorkflowRuns.Add(run);
        await db.SaveChangesAsync();

        _logger.LogInformation("Workflow run {RunId} created with {JobCount} jobs for {RepoName}", run.Id, run.Jobs.Count, repoName);
        return run;
    }

    public async Task<List<Webhook>> GetWebhooksAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Webhooks.Where(w => w.RepoName == repoName).ToListAsync();
    }

    public async Task<Webhook> CreateWebhookAsync(string repoName, string url, string secret, List<string> events)
    {
        using var db = _dbFactory.CreateDbContext();

        var webhook = new Webhook
        {
            RepoName = repoName,
            Url = url,
            Secret = secret,
            Events = events,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Webhooks.Add(webhook);
        await db.SaveChangesAsync();

        _logger.LogInformation("Webhook created for {RepoName}: {Url}", repoName, url);
        return webhook;
    }

    public async Task<bool> DeleteWebhookAsync(string repoName, int webhookId)
    {
        using var db = _dbFactory.CreateDbContext();

        var webhook = await db.Webhooks.FirstOrDefaultAsync(w => w.Id == webhookId && w.RepoName == repoName);
        if (webhook == null)
            return false;

        db.Webhooks.Remove(webhook);
        await db.SaveChangesAsync();

        _logger.LogInformation("Webhook {WebhookId} deleted from {RepoName}", webhookId, repoName);
        return true;
    }

    public async Task<bool> ToggleWebhookAsync(string repoName, int webhookId)
    {
        using var db = _dbFactory.CreateDbContext();

        var webhook = await db.Webhooks.FirstOrDefaultAsync(w => w.Id == webhookId && w.RepoName == repoName);
        if (webhook == null)
            return false;

        webhook.IsActive = !webhook.IsActive;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<WebhookDelivery>> GetWebhookDeliveriesAsync(string repoName, int webhookId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.WebhookDeliveries
            .Where(d => d.WebhookId == webhookId)
            .OrderByDescending(d => d.DeliveredAt)
            .ToListAsync();
    }
}
