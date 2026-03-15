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
    Task TriggerPushWorkflowsAsync(string repoName, string repoPath, string branch, string sha, string commitMessage, string pushedBy);
    Task<List<Webhook>> GetWebhooksAsync(string repoName);
    Task<Webhook> CreateWebhookAsync(string repoName, string url, string secret, List<string> events);
    Task<bool> DeleteWebhookAsync(string repoName, int webhookId);
    Task<bool> ToggleWebhookAsync(string repoName, int webhookId);
    Task<List<WebhookDelivery>> GetWebhookDeliveriesAsync(string repoName, int webhookId);
    Task<bool> UpdateWebhookAsync(string repoName, int webhookId, string url, string secret, List<string> events);
    Task<bool> RedeliverWebhookAsync(string repoName, int deliveryId);
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

    public async Task TriggerPushWorkflowsAsync(string repoName, string repoPath, string branch, string sha, string commitMessage, string pushedBy)
    {
        try
        {
            var parser = new WorkflowYamlParser();
            var workflows = parser.ParseFromRepo(repoPath);

            _logger.LogInformation("Found {Count} workflow(s) in {RepoName}", workflows.Count, repoName);

            foreach (var workflow in workflows)
            {
                _logger.LogInformation("Workflow '{Name}': On type={OnType}, value={OnValue}",
                    workflow.Name, workflow.On?.GetType().Name ?? "null", workflow.On?.ToString() ?? "null");

                if (!ShouldTriggerOnPush(workflow, branch)) continue;

                _logger.LogInformation("Auto-triggering workflow '{WorkflowName}' on push to {Branch} in {RepoName}",
                    workflow.Name, branch, repoName);

                await CreateWorkflowRunWithJobsAsync(repoName, workflow, branch, sha, commitMessage, pushedBy);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to trigger push workflows for {RepoName}", repoName);
        }
    }

    private static bool ShouldTriggerOnPush(WorkflowDefinition workflow, string branch)
    {
        if (workflow.On == null) return false;

        // on: push
        if (workflow.On is string onStr)
            return onStr.Equals("push", StringComparison.OrdinalIgnoreCase);

        // on: [push, pull_request]
        if (workflow.On is List<object> onList)
            return onList.Any(o => o?.ToString()?.Equals("push", StringComparison.OrdinalIgnoreCase) == true);

        // on: { push: { branches: [main] } }
        if (workflow.On is Dictionary<object, object> onDict)
        {
            var pushKey = onDict.Keys.FirstOrDefault(k =>
                k.ToString()?.Equals("push", StringComparison.OrdinalIgnoreCase) == true);

            if (pushKey == null) return false;

            var pushValue = onDict[pushKey];

            // on: { push: null } — trigger on all branches
            if (pushValue == null) return true;

            // on: { push: { branches: [main, develop] } }
            if (pushValue is Dictionary<object, object> pushConfig)
            {
                var branchesKey = pushConfig.Keys.FirstOrDefault(k =>
                    k.ToString()?.Equals("branches", StringComparison.OrdinalIgnoreCase) == true);

                if (branchesKey == null) return true; // no branch filter = all branches

                if (pushConfig[branchesKey] is List<object> branches)
                    return branches.Any(b => b?.ToString()?.Equals(branch, StringComparison.OrdinalIgnoreCase) == true);
            }

            return true;
        }

        return false;
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

    public async Task<bool> UpdateWebhookAsync(string repoName, int webhookId, string url, string secret, List<string> events)
    {
        using var db = _dbFactory.CreateDbContext();
        var webhook = await db.Webhooks.FirstOrDefaultAsync(w => w.Id == webhookId && w.RepoName == repoName);
        if (webhook == null) return false;

        webhook.Url = url;
        if (!string.IsNullOrEmpty(secret))
            webhook.Secret = secret;
        webhook.Events = events;
        await db.SaveChangesAsync();

        _logger.LogInformation("Webhook {WebhookId} updated in {RepoName}", webhookId, repoName);
        return true;
    }

    public async Task<bool> RedeliverWebhookAsync(string repoName, int deliveryId)
    {
        using var db = _dbFactory.CreateDbContext();
        var delivery = await db.WebhookDeliveries.FirstOrDefaultAsync(d => d.Id == deliveryId);
        if (delivery == null) return false;

        var webhook = await db.Webhooks.FirstOrDefaultAsync(w => w.Id == delivery.WebhookId && w.RepoName == repoName);
        if (webhook == null) return false;

        // Re-create delivery by firing it again with the same payload
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var signature = ComputeSignature(delivery.Payload, webhook.Secret);

        var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url);
        request.Headers.Add("X-PersonalGit-Event", delivery.Event);
        request.Headers.Add("X-PersonalGit-Signature", $"sha256={signature}");
        request.Headers.Add("X-PersonalGit-Delivery", Guid.NewGuid().ToString());
        request.Content = new StringContent(delivery.Payload, System.Text.Encoding.UTF8, "application/json");

        int statusCode = 0;
        string? responseBody = null;
        bool success = false;

        try
        {
            var response = await client.SendAsync(request);
            statusCode = (int)response.StatusCode;
            responseBody = await response.Content.ReadAsStringAsync();
            success = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            responseBody = ex.Message;
        }

        db.WebhookDeliveries.Add(new WebhookDelivery
        {
            WebhookId = webhook.Id,
            Event = delivery.Event,
            Payload = delivery.Payload,
            StatusCode = statusCode,
            Response = responseBody?.Length > 5000 ? responseBody[..5000] : responseBody,
            DeliveredAt = DateTime.UtcNow,
            Success = success
        });

        webhook.LastTriggeredAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("Webhook delivery {DeliveryId} redelivered to {Url}", deliveryId, webhook.Url);
        return true;
    }

    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLower();
    }
}
