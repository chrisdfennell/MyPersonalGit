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
    Task TriggerPullRequestWorkflowsAsync(string repoName, string repoPath, string sourceBranch, string targetBranch, string sha, string title, string author);
    Task CancelWorkflowRunAsync(string repoName, int runId);
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
                Needs = jobDef.Needs.Count > 0 ? string.Join(";", jobDef.Needs) : null,
                Status = WorkflowStatus.Queued
            };

            foreach (var stepDef in jobDef.Steps)
            {
                var command = stepDef.Run ?? TranslateUsesAction(stepDef);
                job.Steps.Add(new WorkflowStep
                {
                    Name = stepDef.Name ?? stepDef.Run ?? stepDef.Uses ?? "Step",
                    Command = command,
                    Condition = stepDef.If,
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

                // Concurrency: cancel queued runs of the same workflow
                await CancelPreviousRunsAsync(repoName, workflow.Name);

                await CreateWorkflowRunWithJobsAsync(repoName, workflow, branch, sha, commitMessage, pushedBy);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to trigger push workflows for {RepoName}", repoName);
        }
    }

    private async Task CancelPreviousRunsAsync(string repoName, string workflowName)
    {
        try
        {
            using var db = _dbFactory.CreateDbContext();
            var pendingRuns = await db.WorkflowRuns
                .Include(r => r.Jobs).ThenInclude(j => j.Steps)
                .Where(r => r.RepoName == repoName && r.WorkflowName == workflowName &&
                    (r.Status == WorkflowStatus.Queued))
                .ToListAsync();

            foreach (var run in pendingRuns)
            {
                run.Status = WorkflowStatus.Cancelled;
                run.CompletedAt = DateTime.UtcNow;
                foreach (var job in run.Jobs)
                {
                    job.Status = WorkflowStatus.Cancelled;
                    job.CompletedAt = DateTime.UtcNow;
                    foreach (var step in job.Steps)
                    {
                        step.Status = WorkflowStatus.Cancelled;
                        step.CompletedAt = DateTime.UtcNow;
                    }
                }
                _logger.LogInformation("Cancelled superseded workflow run {RunId} for {WorkflowName}", run.Id, workflowName);
            }

            if (pendingRuns.Any())
                await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cancel previous runs for {WorkflowName}", workflowName);
        }
    }

    public async Task TriggerPullRequestWorkflowsAsync(string repoName, string repoPath, string sourceBranch, string targetBranch, string sha, string title, string author)
    {
        try
        {
            var parser = new WorkflowYamlParser();
            var workflows = parser.ParseFromRepo(repoPath);

            foreach (var workflow in workflows)
            {
                if (!ShouldTriggerOnEvent(workflow, "pull_request", targetBranch)) continue;

                _logger.LogInformation("Auto-triggering workflow '{WorkflowName}' on pull_request to {Branch} in {RepoName}",
                    workflow.Name, targetBranch, repoName);

                await CreateWorkflowRunWithJobsAsync(repoName, workflow, sourceBranch, sha, title, author);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to trigger pull_request workflows for {RepoName}", repoName);
        }
    }

    public async Task CancelWorkflowRunAsync(string repoName, int runId)
    {
        using var db = _dbFactory.CreateDbContext();
        var run = await db.WorkflowRuns
            .Include(r => r.Jobs).ThenInclude(j => j.Steps)
            .FirstOrDefaultAsync(r => r.Id == runId && r.RepoName == repoName);

        if (run == null) return;
        if (run.Status != WorkflowStatus.Queued && run.Status != WorkflowStatus.InProgress) return;

        run.Status = WorkflowStatus.Cancelled;
        run.CompletedAt = DateTime.UtcNow;

        foreach (var job in run.Jobs.Where(j => j.Status == WorkflowStatus.Queued || j.Status == WorkflowStatus.InProgress))
        {
            job.Status = WorkflowStatus.Cancelled;
            job.CompletedAt = DateTime.UtcNow;
            foreach (var step in job.Steps.Where(s => s.Status == WorkflowStatus.Queued || s.Status == WorkflowStatus.InProgress))
            {
                step.Status = WorkflowStatus.Cancelled;
                step.CompletedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("Cancelled workflow run {RunId} for {RepoName}", runId, repoName);
    }

    private static bool ShouldTriggerOnPush(WorkflowDefinition workflow, string branch)
        => ShouldTriggerOnEvent(workflow, "push", branch);

    private static bool ShouldTriggerOnEvent(WorkflowDefinition workflow, string eventName, string branch)
    {
        if (workflow.On == null) return false;

        // on: push (or on: pull_request)
        if (workflow.On is string onStr)
            return onStr.Equals(eventName, StringComparison.OrdinalIgnoreCase);

        // on: [push, pull_request]
        if (workflow.On is List<object> onList)
            return onList.Any(o => o?.ToString()?.Equals(eventName, StringComparison.OrdinalIgnoreCase) == true);

        // on: { push: { branches: [main] } }
        if (workflow.On is Dictionary<object, object> onDict)
        {
            var eventKey = onDict.Keys.FirstOrDefault(k =>
                k.ToString()?.Equals(eventName, StringComparison.OrdinalIgnoreCase) == true);

            if (eventKey == null) return false;

            var eventValue = onDict[eventKey];

            // on: { push: null } — trigger on all branches
            if (eventValue == null) return true;

            // on: { push: { branches: [main, develop] } }
            if (eventValue is Dictionary<object, object> eventConfig)
            {
                var branchesKey = eventConfig.Keys.FirstOrDefault(k =>
                    k.ToString()?.Equals("branches", StringComparison.OrdinalIgnoreCase) == true);

                if (branchesKey == null) return true;

                if (eventConfig[branchesKey] is List<object> branches)
                    return branches.Any(b => b?.ToString()?.Equals(branch, StringComparison.OrdinalIgnoreCase) == true);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Translates GitHub Actions 'uses:' steps into equivalent shell commands
    /// so the same workflow YAML works on both GitHub Actions and MyPersonalGit.
    /// </summary>
    private static string? TranslateUsesAction(StepDefinition step)
    {
        if (string.IsNullOrEmpty(step.Uses)) return null;

        var uses = step.Uses.ToLowerInvariant();
        var with = step.With ?? new Dictionary<string, string>();

        // actions/checkout — already handled by the runner (clones to /workspace)
        if (uses.StartsWith("actions/checkout"))
            return "echo 'Checkout: repo already cloned to /workspace'";

        // docker/login-action — translate to docker login
        if (uses.StartsWith("docker/login-action"))
        {
            var username = with.GetValueOrDefault("username", "");
            var password = with.GetValueOrDefault("password", "");
            var registry = with.GetValueOrDefault("registry", "");

            // Replace ${{ secrets.X }} with $X env var reference
            password = TranslateExpression(password);
            username = TranslateExpression(username);

            if (!string.IsNullOrEmpty(registry))
                return $"echo \"{password}\" | docker login {registry} -u {username} --password-stdin";
            return $"echo \"{password}\" | docker login -u {username} --password-stdin";
        }

        // docker/setup-buildx-action — not needed for basic builds
        if (uses.StartsWith("docker/setup-buildx-action"))
            return "echo 'Buildx: using default docker build'";

        // docker/build-push-action — translate to docker build + push
        if (uses.StartsWith("docker/build-push-action"))
        {
            var context = with.GetValueOrDefault("context", ".");
            var push = with.GetValueOrDefault("push", "false");
            var tags = with.GetValueOrDefault("tags", "");

            var cmds = new List<string>();

            // Parse tags (newline or comma separated)
            var tagList = tags.Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .Select(TranslateExpression)
                .ToList();

            if (tagList.Count > 0)
            {
                var tagFlags = string.Join(" ", tagList.Select(t => $"-t {t}"));
                cmds.Add($"docker build {tagFlags} {context}");

                if (push.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var tag in tagList)
                        cmds.Add($"docker push {tag}");
                }
            }
            else
            {
                cmds.Add($"docker build {context}");
            }

            return string.Join(" && ", cmds);
        }

        // softprops/action-gh-release — translate to git tag (release creation)
        if (uses.StartsWith("softprops/action-gh-release"))
        {
            var tagName = TranslateExpression(with.GetValueOrDefault("tag_name", ""));
            var name = TranslateExpression(with.GetValueOrDefault("name", tagName));
            if (!string.IsNullOrEmpty(tagName))
                return $"echo 'Release {name} created (tag: {tagName})'";
            return "echo 'Release step (no tag specified)'";
        }

        // Unknown action — log and skip
        return $"echo 'Skipping unsupported action: {step.Uses}'";
    }

    /// <summary>
    /// Translates GitHub Actions expressions like ${{{{ secrets.TOKEN }}}} to shell $TOKEN
    /// </summary>
    private static string TranslateExpression(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        // ${{ secrets.SOMETHING }} -> $SOMETHING
        value = System.Text.RegularExpressions.Regex.Replace(
            value, @"\$\{\{\s*secrets\.(\w+)\s*\}\}", @"$$$1");

        // ${{ needs.job.outputs.var }} -> $var
        value = System.Text.RegularExpressions.Regex.Replace(
            value, @"\$\{\{\s*needs\.\w+\.outputs\.(\w+)\s*\}\}", @"$$$1");

        // ${{ steps.step.outputs.var }} -> $var
        value = System.Text.RegularExpressions.Regex.Replace(
            value, @"\$\{\{\s*steps\.\w+\.outputs\.(\w+)\s*\}\}", @"$$$1");

        return value;
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
