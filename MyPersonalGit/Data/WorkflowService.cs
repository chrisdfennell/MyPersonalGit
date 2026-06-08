using System.Text.Json;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public class WorkflowService
{
    private readonly string _dataPath;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(IConfiguration configuration, ILogger<WorkflowService> logger)
    {
        var projectRoot = configuration["ProjectRoot"] ?? throw new InvalidOperationException("ProjectRoot not configured");
        _dataPath = Path.Combine(projectRoot, ".mypersonalgit");
        _logger = logger;
        Directory.CreateDirectory(_dataPath);
    }

    private string GetWorkflowRunsFilePath(string repoName) => Path.Combine(_dataPath, $"{repoName}_workflow_runs.json");
    private string GetWebhooksFilePath(string repoName) => Path.Combine(_dataPath, $"{repoName}_webhooks.json");
    private string GetWebhookDeliveriesFilePath(string repoName) => Path.Combine(_dataPath, $"{repoName}_webhook_deliveries.json");

    public async Task<List<WorkflowRun>> GetWorkflowRunsAsync(string repoName)
    {
        var filePath = GetWorkflowRunsFilePath(repoName);
        if (!File.Exists(filePath))
            return new List<WorkflowRun>();

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<WorkflowRun>>(json) ?? new List<WorkflowRun>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workflow runs for {RepoName}", repoName);
            return new List<WorkflowRun>();
        }
    }

    public async Task<WorkflowRun?> GetWorkflowRunAsync(string repoName, int runId)
    {
        var runs = await GetWorkflowRunsAsync(repoName);
        return runs.FirstOrDefault(r => r.Id == runId);
    }

    public async Task<WorkflowRun> CreateWorkflowRunAsync(
        string repoName, string workflowName, string branch, 
        string commitSha, string commitMessage, string triggeredBy)
    {
        var runs = await GetWorkflowRunsAsync(repoName);
        
        var run = new WorkflowRun
        {
            Id = runs.Count > 0 ? runs.Max(r => r.Id) + 1 : 1,
            RepoName = repoName,
            WorkflowName = workflowName,
            Branch = branch,
            CommitSha = commitSha,
            CommitMessage = commitMessage,
            TriggeredBy = triggeredBy,
            Status = WorkflowStatus.Queued,
            CreatedAt = DateTime.UtcNow
        };

        runs.Add(run);
        await SaveWorkflowRunsAsync(repoName, runs);
        return run;
    }

    public async Task<bool> UpdateWorkflowRunAsync(string repoName, int runId, Action<WorkflowRun> updateAction)
    {
        var runs = await GetWorkflowRunsAsync(repoName);
        var run = runs.FirstOrDefault(r => r.Id == runId);
        
        if (run == null)
            return false;

        updateAction(run);
        await SaveWorkflowRunsAsync(repoName, runs);
        return true;
    }

    public async Task<List<Webhook>> GetWebhooksAsync(string repoName)
    {
        var filePath = GetWebhooksFilePath(repoName);
        if (!File.Exists(filePath))
            return new List<Webhook>();

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<Webhook>>(json) ?? new List<Webhook>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load webhooks for {RepoName}", repoName);
            return new List<Webhook>();
        }
    }

    public async Task<Webhook> CreateWebhookAsync(string repoName, string url, string secret, List<string> events)
    {
        var webhooks = await GetWebhooksAsync(repoName);
        
        var webhook = new Webhook
        {
            Id = webhooks.Count > 0 ? webhooks.Max(w => w.Id) + 1 : 1,
            RepoName = repoName,
            Url = url,
            Secret = secret,
            Events = events,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        webhooks.Add(webhook);
        await SaveWebhooksAsync(repoName, webhooks);
        return webhook;
    }

    public async Task<bool> DeleteWebhookAsync(string repoName, int webhookId)
    {
        var webhooks = await GetWebhooksAsync(repoName);
        var webhook = webhooks.FirstOrDefault(w => w.Id == webhookId);
        
        if (webhook == null)
            return false;

        webhooks.Remove(webhook);
        await SaveWebhooksAsync(repoName, webhooks);
        return true;
    }

    public async Task<bool> ToggleWebhookAsync(string repoName, int webhookId)
    {
        var webhooks = await GetWebhooksAsync(repoName);
        var webhook = webhooks.FirstOrDefault(w => w.Id == webhookId);
        
        if (webhook == null)
            return false;

        webhook.IsActive = !webhook.IsActive;
        await SaveWebhooksAsync(repoName, webhooks);
        return true;
    }

    public async Task<List<WebhookDelivery>> GetWebhookDeliveriesAsync(string repoName, int webhookId)
    {
        var filePath = GetWebhookDeliveriesFilePath(repoName);
        if (!File.Exists(filePath))
            return new List<WebhookDelivery>();

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var deliveries = JsonSerializer.Deserialize<List<WebhookDelivery>>(json) ?? new List<WebhookDelivery>();
            return deliveries.Where(d => d.WebhookId == webhookId).OrderByDescending(d => d.DeliveredAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load webhook deliveries for {RepoName}", repoName);
            return new List<WebhookDelivery>();
        }
    }

    private async Task SaveWorkflowRunsAsync(string repoName, List<WorkflowRun> runs)
    {
        var filePath = GetWorkflowRunsFilePath(repoName);
        var json = JsonSerializer.Serialize(runs, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    private async Task SaveWebhooksAsync(string repoName, List<Webhook> webhooks)
    {
        var filePath = GetWebhooksFilePath(repoName);
        var json = JsonSerializer.Serialize(webhooks, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }
}
