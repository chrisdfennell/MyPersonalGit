using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IWorkflowService
{
    Task<List<WorkflowRun>> GetWorkflowRunsAsync(string repoName);
    Task<WorkflowRun?> GetWorkflowRunAsync(string repoName, int runId);
    Task<WorkflowRun> CreateWorkflowRunAsync(string repoName, string workflowName, string branch, string commitSha, string commitMessage, string triggeredBy);
    Task<bool> UpdateWorkflowRunAsync(string repoName, int runId, Action<WorkflowRun> updateAction);
    Task<WorkflowRun> CreateWorkflowRunWithJobsAsync(string repoName, WorkflowDefinition definition, string branch, string sha, string message, string user, Dictionary<string, string>? inputs = null);
    Task TriggerPushWorkflowsAsync(string repoName, string repoPath, string branch, string sha, string commitMessage, string pushedBy);
    Task TriggerPullRequestWorkflowsAsync(string repoName, string repoPath, string sourceBranch, string targetBranch, string sha, string title, string author);
    Task CancelWorkflowRunAsync(string repoName, int runId);
    Task<WorkflowRun?> RerunWorkflowRunAsync(string repoName, int runId);
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
        // Match both "MyRepo" and "MyRepo.git" variants
        var alt = repoName.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? repoName[..^4] : repoName + ".git";
        return await db.WorkflowRuns
            .Include(r => r.Jobs)
                .ThenInclude(j => j.Steps)
            .Where(r => r.RepoName == repoName || r.RepoName == alt)
            .ToListAsync();
    }

    public async Task<WorkflowRun?> GetWorkflowRunAsync(string repoName, int runId)
    {
        using var db = _dbFactory.CreateDbContext();
        var alt = repoName.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? repoName[..^4] : repoName + ".git";
        return await db.WorkflowRuns
            .Include(r => r.Jobs)
                .ThenInclude(j => j.Steps)
            .FirstOrDefaultAsync(r => (r.RepoName == repoName || r.RepoName == alt) && r.Id == runId);
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
        var (name, alt2) = RepoNameVariants(repoName);
        var run = await db.WorkflowRuns
            .Include(r => r.Jobs)
                .ThenInclude(j => j.Steps)
            .FirstOrDefaultAsync(r => (r.RepoName == name || r.RepoName == alt2) && r.Id == runId);

        if (run == null)
            return false;

        updateAction(run);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<WorkflowRun> CreateWorkflowRunWithJobsAsync(
        string repoName, WorkflowDefinition definition, string branch, string sha, string message, string user, Dictionary<string, string>? inputs = null)
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
            InputsJson = inputs != null && inputs.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(inputs) : null,
            Status = WorkflowStatus.Queued,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var (jobName, jobDef) in definition.Jobs)
        {
            // Expand matrix combinations — if no matrix, just one combination (empty dict)
            var combinations = jobDef.Matrix != null && jobDef.Matrix.Count > 0
                ? ExpandMatrix(jobDef.Matrix)
                : new List<Dictionary<string, string>> { new() };

            foreach (var matrixValues in combinations)
            {
                var resolvedRunsOn = SubstituteMatrixVars(jobDef.RunsOn, matrixValues);
                var suffix = matrixValues.Count > 0
                    ? $" ({string.Join(", ", matrixValues.Values)})"
                    : "";

                var job = new WorkflowJob
                {
                    Name = jobName + suffix,
                    RunsOn = resolvedRunsOn,
                    Needs = jobDef.Needs.Count > 0 ? string.Join(";", jobDef.Needs) : null,
                    Condition = jobDef.If,
                    TimeoutMinutes = jobDef.TimeoutMinutes,
                    Environment = jobDef.Environment,
                    Status = WorkflowStatus.Queued
                };

                // If this job uses a reusable workflow, resolve it and inline its steps
                var stepsToProcess = jobDef.Steps;
                if (!string.IsNullOrEmpty(jobDef.Uses) && jobDef.Uses.StartsWith("./"))
                {
                    try
                    {
                        var parser = new WorkflowYamlParser();
                        var repoPath = GetRepoPath(repoName);
                        if (repoPath != null)
                        {
                            var calledWorkflow = parser.ResolveReusableWorkflow(repoPath, jobDef.Uses);
                            if (calledWorkflow != null)
                            {
                                // Inline all jobs from the called workflow as steps
                                stepsToProcess = calledWorkflow.Jobs.Values.SelectMany(j => j.Steps).ToList();
                                _logger.LogInformation("Resolved reusable workflow '{Uses}' with {StepCount} steps", jobDef.Uses, stepsToProcess.Count);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to resolve reusable workflow '{Uses}'", jobDef.Uses);
                    }
                }

                // Resolve the effective working directory for steps
                var defaultWorkDir = definition.DefaultWorkingDirectory;

                foreach (var stepDef in stepsToProcess)
                {
                    var command = stepDef.Run ?? TranslateUsesAction(stepDef);
                    if (command != null)
                        command = SubstituteMatrixVars(command, matrixValues);

                    var stepName = stepDef.Name ?? stepDef.Run ?? stepDef.Uses ?? "Step";
                    if (stepName != null)
                        stepName = SubstituteMatrixVars(stepName, matrixValues);

                    // Apply working-directory (step-level overrides default)
                    var workDir = stepDef.WorkingDirectory ?? defaultWorkDir;
                    if (command != null && !string.IsNullOrEmpty(workDir))
                    {
                        // Prefix command with cd to the working directory relative to /workspace
                        command = $"cd /workspace/{workDir} && {command}";
                    }

                    job.Steps.Add(new WorkflowStep
                    {
                        Name = stepName ?? "Step",
                        Command = command,
                        Condition = stepDef.If,
                        ContinueOnError = stepDef.ContinueOnError,
                        Status = WorkflowStatus.Queued
                    });
                }

                run.Jobs.Add(job);
            }
        }

        db.WorkflowRuns.Add(run);
        await db.SaveChangesAsync();

        _logger.LogInformation("Workflow run {RunId} created with {JobCount} jobs for {RepoName}", run.Id, run.Jobs.Count, repoName);
        return run;
    }

    /// <summary>
    /// Expands a matrix into all combinations. E.g. {os: [a,b], ver: [1,2]} -> [{os:a,ver:1}, {os:a,ver:2}, {os:b,ver:1}, {os:b,ver:2}]
    /// </summary>
    private static List<Dictionary<string, string>> ExpandMatrix(Dictionary<string, List<string>> matrix)
    {
        var results = new List<Dictionary<string, string>> { new() };
        foreach (var (key, values) in matrix)
        {
            var expanded = new List<Dictionary<string, string>>();
            foreach (var existing in results)
                foreach (var val in values)
                {
                    var combo = new Dictionary<string, string>(existing) { [key] = val };
                    expanded.Add(combo);
                }
            results = expanded;
        }
        return results;
    }

    /// <summary>
    /// Replaces ${{ matrix.X }} with the corresponding value from the matrix combination.
    /// </summary>
    private static string SubstituteMatrixVars(string value, Dictionary<string, string> matrixValues)
    {
        if (matrixValues.Count == 0 || string.IsNullOrEmpty(value)) return value;
        return System.Text.RegularExpressions.Regex.Replace(
            value, @"\$\{\{\s*matrix\.(\w+)\s*\}\}",
            m => matrixValues.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);
    }

    public async Task TriggerPushWorkflowsAsync(string repoName, string repoPath, string branch, string sha, string commitMessage, string pushedBy)
    {
        try
        {
            var parser = new WorkflowYamlParser();
            var workflows = parser.ParseFromRepo(repoPath);

            _logger.LogInformation("Found {Count} workflow(s) in {RepoName}", workflows.Count, repoName);

            // Get changed files for path filtering
            var changedFiles = GetChangedFiles(repoPath, sha);

            foreach (var workflow in workflows)
            {
                _logger.LogInformation("Workflow '{Name}': On type={OnType}, value={OnValue}",
                    workflow.Name, workflow.On?.GetType().Name ?? "null", workflow.On?.ToString() ?? "null");

                if (!ShouldTriggerOnPush(workflow, branch)) continue;

                // Check path filters
                var (paths, pathsIgnore) = WorkflowYamlParser.GetPathFilters(workflow, "push");
                if (!MatchesPathFilter(changedFiles, paths, pathsIgnore))
                {
                    _logger.LogInformation("Skipping workflow '{Name}' — no changed files match path filter", workflow.Name);
                    continue;
                }

                _logger.LogInformation("Auto-triggering workflow '{WorkflowName}' on push to {Branch} in {RepoName}",
                    workflow.Name, branch, repoName);

                await CancelPreviousRunsAsync(repoName, workflow.Name);

                await CreateWorkflowRunWithJobsAsync(repoName, workflow, branch, sha, commitMessage, pushedBy);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to trigger push workflows for {RepoName}", repoName);
        }
    }

    /// <summary>Gets list of changed files in the given commit by diffing against its parent.</summary>
    private static List<string> GetChangedFiles(string repoPath, string sha)
    {
        var files = new List<string>();
        try
        {
            if (!LibGit2Sharp.Repository.IsValid(repoPath)) return files;
            using var repo = new LibGit2Sharp.Repository(repoPath);
            var commitObj = repo.Lookup(new LibGit2Sharp.ObjectId(sha));
            if (commitObj is not LibGit2Sharp.Commit commit) return files;

            var parent = commit.Parents.FirstOrDefault();
            var diff = parent != null
                ? repo.Diff.Compare<LibGit2Sharp.TreeChanges>(parent.Tree, commit.Tree)
                : repo.Diff.Compare<LibGit2Sharp.TreeChanges>(null, commit.Tree);

            foreach (var change in diff)
                files.Add(change.Path);
        }
        catch { }
        return files;
    }

    /// <summary>
    /// Returns true if changed files match the path filter.
    /// If no paths/paths-ignore are configured, always returns true.
    /// </summary>
    private static bool MatchesPathFilter(List<string> changedFiles, List<string> paths, List<string> pathsIgnore)
    {
        if (paths.Count == 0 && pathsIgnore.Count == 0) return true;
        if (changedFiles.Count == 0) return paths.Count == 0;

        // paths-ignore: if ALL changed files match ignore patterns, skip
        if (pathsIgnore.Count > 0 && paths.Count == 0)
            return !changedFiles.All(f => pathsIgnore.Any(p => FileMatchesGlob(f, p)));

        // paths: at least one changed file must match a path pattern
        if (paths.Count > 0)
            return changedFiles.Any(f => paths.Any(p => FileMatchesGlob(f, p)));

        return true;
    }

    /// <summary>Simple glob matching: supports ** (any path), * (any segment), and literal matching.</summary>
    private static bool FileMatchesGlob(string filePath, string pattern)
    {
        // Normalize
        filePath = filePath.Replace('\\', '/');
        pattern = pattern.Replace('\\', '/');

        // Convert glob to regex
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace(@"\*\*", ".*")
            .Replace(@"\*", "[^/]*") + "$";

        return System.Text.RegularExpressions.Regex.IsMatch(filePath, regexPattern);
    }

    private async Task CancelPreviousRunsAsync(string repoName, string workflowName)
    {
        try
        {
            using var db = _dbFactory.CreateDbContext();
            var pendingRuns = await db.WorkflowRuns
                .Include(r => r.Jobs).ThenInclude(j => j.Steps)
                .Where(r => r.RepoName == repoName && r.WorkflowName == workflowName &&
                    (r.Status == WorkflowStatus.Queued || r.Status == WorkflowStatus.InProgress))
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
        var (name, alt2) = RepoNameVariants(repoName);
        var run = await db.WorkflowRuns
            .Include(r => r.Jobs).ThenInclude(j => j.Steps)
            .FirstOrDefaultAsync(r => r.Id == runId && (r.RepoName == name || r.RepoName == alt2));

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

    public async Task<WorkflowRun?> RerunWorkflowRunAsync(string repoName, int runId)
    {
        using var db = _dbFactory.CreateDbContext();
        var (name, alt2) = RepoNameVariants(repoName);
        var original = await db.WorkflowRuns
            .Include(r => r.Jobs).ThenInclude(j => j.Steps)
            .FirstOrDefaultAsync(r => r.Id == runId && (r.RepoName == name || r.RepoName == alt2));

        if (original == null) return null;

        // Create a new run cloned from the original
        var newRun = new WorkflowRun
        {
            RepoName = original.RepoName,
            WorkflowName = original.WorkflowName,
            Branch = original.Branch,
            CommitSha = original.CommitSha,
            CommitMessage = original.CommitMessage,
            TriggeredBy = original.TriggeredBy,
            InputsJson = original.InputsJson,
            Status = WorkflowStatus.Queued,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var origJob in original.Jobs)
        {
            var job = new WorkflowJob
            {
                Name = origJob.Name,
                RunsOn = origJob.RunsOn,
                Needs = origJob.Needs,
                Condition = origJob.Condition,
                TimeoutMinutes = origJob.TimeoutMinutes,
                Status = WorkflowStatus.Queued
            };

            foreach (var origStep in origJob.Steps)
            {
                job.Steps.Add(new WorkflowStep
                {
                    Name = origStep.Name,
                    Command = origStep.Command,
                    Condition = origStep.Condition,
                    ContinueOnError = origStep.ContinueOnError,
                    Status = WorkflowStatus.Queued
                });
            }

            newRun.Jobs.Add(job);
        }

        db.WorkflowRuns.Add(newRun);
        await db.SaveChangesAsync();

        _logger.LogInformation("Re-run {NewRunId} created from run {OrigRunId} for {RepoName}", newRun.Id, runId, repoName);
        return newRun;
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

        // Local composite actions (./.github/actions/xxx) — placeholder, expanded at runtime
        if (step.Uses.StartsWith("./"))
            return $"echo 'Composite action: {step.Uses} (expanded inline)'";

        // actions/checkout — already handled by the runner (clones to /workspace)
        if (uses.StartsWith("actions/checkout"))
            return "echo 'Checkout: repo already cloned to /workspace'";

        // actions/setup-dotnet — install .NET SDK via official install script
        if (uses.StartsWith("actions/setup-dotnet"))
        {
            var dotnetVersion = with.GetValueOrDefault("dotnet-version", "8.0.x");
            // Extract channel from version string: "10.0.x" -> "10.0", "8.0.100" -> "8.0"
            var channel = System.Text.RegularExpressions.Regex.Match(dotnetVersion, @"^\d+\.\d+").Value;
            if (string.IsNullOrEmpty(channel)) channel = "8.0";
            return $"curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh && " +
                   $"chmod +x /tmp/dotnet-install.sh && " +
                   $"/tmp/dotnet-install.sh --channel {channel} --install-dir /usr/local/dotnet && " +
                   $"ln -sf /usr/local/dotnet/dotnet /usr/local/bin/dotnet && " +
                   $"dotnet --version";
        }

        // actions/setup-node — install Node.js via nvm
        if (uses.StartsWith("actions/setup-node"))
        {
            var nodeVersion = with.GetValueOrDefault("node-version", "20");
            // Strip .x suffix: "20.x" -> "20"
            nodeVersion = nodeVersion.Replace(".x", "");
            return $"curl -fsSL https://deb.nodesource.com/setup_{nodeVersion}.x | bash - > /dev/null 2>&1 && " +
                   $"apt-get install -y -qq nodejs > /dev/null 2>&1 || " +
                   $"(apk add --no-cache nodejs npm > /dev/null 2>&1) || true && " +
                   $"node --version && npm --version";
        }

        // actions/setup-python — install Python
        if (uses.StartsWith("actions/setup-python"))
        {
            var pyVersion = with.GetValueOrDefault("python-version", "3.12");
            return $"which python3 > /dev/null 2>&1 || " +
                   $"(apt-get update -qq && apt-get install -y -qq python{pyVersion} python3-pip > /dev/null 2>&1) || " +
                   $"(apk add --no-cache python3 py3-pip > /dev/null 2>&1) || true && " +
                   $"python3 --version";
        }

        // actions/setup-java — install JDK
        if (uses.StartsWith("actions/setup-java"))
        {
            var javaVersion = with.GetValueOrDefault("java-version", "17");
            return $"which java > /dev/null 2>&1 || " +
                   $"(apt-get update -qq && apt-get install -y -qq openjdk-{javaVersion}-jdk-headless > /dev/null 2>&1) || " +
                   $"(apk add --no-cache openjdk{javaVersion}-jdk > /dev/null 2>&1) || true && " +
                   $"java -version";
        }

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

        // softprops/action-gh-release — write release metadata for runner to create a real Release
        if (uses.StartsWith("softprops/action-gh-release"))
        {
            var tagName = TranslateExpression(with.GetValueOrDefault("tag_name", ""));
            var name = TranslateExpression(with.GetValueOrDefault("name", tagName));
            var prerelease = with.GetValueOrDefault("prerelease", "false");
            var draft = with.GetValueOrDefault("draft", "false");

            // Write meta fields only — body is constructed by the runner from step outputs
            // (avoids shell escaping issues with backticks/markdown in body content)
            return $"echo \"TAG_NAME={tagName}\" > /tmp/release_meta && " +
                   $"echo \"RELEASE_NAME={name}\" >> /tmp/release_meta && " +
                   $"echo \"PRERELEASE={prerelease}\" >> /tmp/release_meta && " +
                   $"echo \"DRAFT={draft}\" >> /tmp/release_meta && " +
                   $"echo '--- Release metadata written ---' && cat /tmp/release_meta";
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
        var (name, alt2) = RepoNameVariants(repoName);
        return await db.Webhooks.Where(w => w.RepoName == name || w.RepoName == alt2).ToListAsync();
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

    /// <summary>Returns both "name" and "name.git" variants for flexible DB matching.</summary>
    private static (string name, string alt) RepoNameVariants(string repoName)
    {
        var alt = repoName.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? repoName[..^4] : repoName + ".git";
        return (repoName, alt);
    }

    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLower();
    }

    private string? GetRepoPath(string repoName)
    {
        // Resolve repo path from config — matches pattern used by other services
        var projectRoot = "/repos";
        try
        {
            using var db = _dbFactory.CreateDbContext();
            var settings = db.SystemSettings.FirstOrDefault();
            if (settings != null && !string.IsNullOrEmpty(settings.ProjectRoot))
                projectRoot = settings.ProjectRoot;
        }
        catch { }

        var path = Path.Combine(projectRoot, repoName);
        if (LibGit2Sharp.Repository.IsValid(path)) return path;
        if (LibGit2Sharp.Repository.IsValid(path + ".git")) return path + ".git";
        return null;
    }
}
