using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public class WorkflowRunnerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkflowRunnerService> _logger;
    private readonly DockerClient? _docker;

    public WorkflowRunnerService(IServiceScopeFactory scopeFactory, ILogger<WorkflowRunnerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        try
        {
            var dockerUri = OperatingSystem.IsWindows()
                ? new Uri("npipe://./pipe/docker_engine")
                : new Uri("unix:///var/run/docker.sock");
            _docker = new DockerClientConfiguration(dockerUri).CreateClient();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Docker client initialization failed — workflow runner will be disabled");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_docker == null)
        {
            _logger.LogWarning("Docker not available — workflow runner disabled");
            return;
        }

        _logger.LogInformation("Workflow runner started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueuedRuns(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing workflow queue");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }

    private async Task ProcessQueuedRuns(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();

        var queuedRuns = await db.WorkflowRuns
            .Include(r => r.Jobs)
                .ThenInclude(j => j.Steps)
            .Where(r => r.Status == WorkflowStatus.Queued)
            .OrderBy(r => r.CreatedAt)
            .Take(1)
            .ToListAsync(ct);

        foreach (var run in queuedRuns)
        {
            await ExecuteRun(db, run, ct);
        }
    }

    private async Task ExecuteRun(AppDbContext db, WorkflowRun run, CancellationToken ct)
    {
        _logger.LogInformation("Starting workflow run {RunId} for {RepoName}", run.Id, run.RepoName);

        run.Status = WorkflowStatus.InProgress;
        run.StartedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var pendingContext = $"ci/{run.WorkflowName.ToLowerInvariant().Replace(' ', '-')}";
        db.CommitStatuses.Add(new CommitStatus
        {
            RepoName = run.RepoName,
            Sha = run.CommitSha,
            State = CommitStatusState.Pending,
            Context = pendingContext,
            Description = "Workflow running...",
            Creator = "ci"
        });
        try { await db.SaveChangesAsync(ct); } catch { }

        var runSuccess = await ExecuteJobsAsync(run, ct);

        // Re-fetch run so we have fresh job/step data for tag creation
        db.ChangeTracker.Clear();
        var freshRun = await db.WorkflowRuns
            .Include(r => r.Jobs).ThenInclude(j => j.Steps)
            .FirstOrDefaultAsync(r => r.Id == run.Id, ct) ?? run;

        // Check if this run was cancelled by a newer push while we were executing
        if (freshRun.Status == WorkflowStatus.Cancelled)
        {
            _logger.LogInformation("Workflow run {RunId} was cancelled by a newer run — skipping completion", run.Id);
            return;
        }

        freshRun.Status = runSuccess ? WorkflowStatus.Success : WorkflowStatus.Failure;
        freshRun.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Workflow run {RunId} completed with status {Status}", run.Id, freshRun.Status);

        await SetCommitStatus(db, freshRun, runSuccess);

        if (runSuccess)
        {
            await TryAutoMerge(run.RepoName, ct);
            await TryCreateTagFromWorkflow(freshRun, ct);
        }

        // Trigger on.workflow_run workflows
        await TriggerWorkflowRunWorkflows(freshRun, ct);
    }

    /// <summary>
    /// Scans for workflows with on: workflow_run trigger and fires them if they match
    /// the completed workflow's name and conclusion.
    /// </summary>
    private async Task TriggerWorkflowRunWorkflows(WorkflowRun completedRun, CancellationToken ct)
    {
        try
        {
            var repoPath = GetRepoPath(completedRun.RepoName);
            if (repoPath == null) return;

            var parser = new WorkflowYamlParser();
            var workflows = parser.ParseFromRepo(repoPath);
            var conclusion = completedRun.Status == WorkflowStatus.Success ? "success" : "failure";

            foreach (var wf in workflows)
            {
                if (wf.On is not Dictionary<object, object> onDict) continue;

                var wrKey = onDict.Keys.FirstOrDefault(k =>
                    k.ToString()?.Equals("workflow_run", StringComparison.OrdinalIgnoreCase) == true);
                if (wrKey == null) continue;

                if (onDict[wrKey] is not Dictionary<object, object> wrConfig) continue;

                // Check workflows: [name1, name2] filter
                bool workflowMatches = true;
                var wfKey = wrConfig.Keys.FirstOrDefault(k => k.ToString() == "workflows");
                if (wfKey != null && wrConfig[wfKey] is List<object> wfNames)
                    workflowMatches = wfNames.Any(n =>
                        n?.ToString()?.Equals(completedRun.WorkflowName, StringComparison.OrdinalIgnoreCase) == true);

                if (!workflowMatches) continue;

                // Check types: [completed] filter (default: completed)
                bool typeMatches = true;
                var typeKey = wrConfig.Keys.FirstOrDefault(k => k.ToString() == "types");
                if (typeKey != null && wrConfig[typeKey] is List<object> types)
                    typeMatches = types.Any(t =>
                        t?.ToString()?.Equals("completed", StringComparison.OrdinalIgnoreCase) == true);

                if (!typeMatches) continue;

                _logger.LogInformation("Triggering workflow '{Name}' via workflow_run from '{Source}'",
                    wf.Name, completedRun.WorkflowName);

                using var scope = _scopeFactory.CreateScope();
                var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();
                await workflowService.CreateWorkflowRunWithJobsAsync(
                    completedRun.RepoName, wf, completedRun.Branch, completedRun.CommitSha,
                    $"Triggered by {completedRun.WorkflowName} ({conclusion})", "workflow_run");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to trigger workflow_run workflows for run {RunId}", completedRun.Id);
        }
    }

    /// <summary>
    /// Executes all jobs in the run, respecting needs: dependencies, job-level if:,
    /// and running independent jobs in parallel. Collects job outputs for downstream consumption.
    /// </summary>
    private async Task<bool> ExecuteJobsAsync(WorkflowRun run, CancellationToken ct)
    {
        var jobs = run.Jobs.ToList();

        // Determine max-parallel from YAML (limits concurrent matrix jobs)
        int? maxParallel = null;
        try
        {
            var mpParser = new WorkflowYamlParser();
            var mpDefs = mpParser.ParseFromRepo(GetRepoPath(run.RepoName) ?? "");
            var mpWf = mpDefs.FirstOrDefault(d => d.Name == run.WorkflowName);
            if (mpWf != null)
                maxParallel = mpWf.Jobs.Values.Max(j => j.MaxParallel);
        }
        catch { }
        var parallelSemaphore = maxParallel.HasValue ? new SemaphoreSlim(maxParallel.Value) : null;

        // Each job gets a TCS so dependent jobs can await it
        var completions = jobs.ToDictionary(j => j.Name, _ => new TaskCompletionSource<bool>());
        // Collect job outputs: jobName -> {key: value}
        var jobOutputs = new System.Collections.Concurrent.ConcurrentDictionary<string, Dictionary<string, string>>();

        var jobTasks = jobs.Select(job =>
        {
            var jobId = job.Id;
            var jobName = job.Name;
            var needs = ParseJobNeeds(job.Needs);
            var jobCondition = job.Condition;

            return Task.Run(async () =>
            {
                // Wait for all declared dependencies to finish
                bool anyDepFailed = false;
                if (needs.Count > 0)
                {
                    var depTasks = needs
                        .Where(n => completions.ContainsKey(n))
                        .Select(n => completions[n].Task);

                    var depResults = await Task.WhenAll(depTasks);
                    anyDepFailed = !depResults.All(r => r);

                    if (anyDepFailed && string.IsNullOrEmpty(jobCondition))
                    {
                        await CancelJobAsync(jobId, ct);
                        completions[jobName].SetResult(false);
                        return false;
                    }
                }

                // Evaluate job-level if: condition
                if (!string.IsNullOrEmpty(jobCondition) && !EvaluateCondition(jobCondition, anyDepFailed))
                {
                    await CancelJobAsync(jobId, ct);
                    completions[jobName].SetResult(true); // skipped jobs don't fail the run
                    return true;
                }

                // Build env vars from upstream job outputs for this job
                var upstreamOutputs = new Dictionary<string, string>();
                foreach (var depName in needs)
                {
                    if (jobOutputs.TryGetValue(depName, out var depOutputs))
                        foreach (var (k, v) in depOutputs)
                            upstreamOutputs[k] = v;
                }

                if (parallelSemaphore != null) await parallelSemaphore.WaitAsync(ct);
                try
                {
                    var result = await ExecuteJobById(jobId, run, upstreamOutputs, ct);
                    completions[jobName].SetResult(result);

                    // Collect this job's outputs for downstream jobs
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
                        var finishedJob = await db.WorkflowJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
                        if (finishedJob?.OutputsJson != null)
                        {
                            var outputs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(finishedJob.OutputsJson);
                            if (outputs != null)
                                jobOutputs[jobName] = outputs;
                        }
                    }
                    catch { }

                    return result;
                }
                finally { parallelSemaphore?.Release(); }
            }, ct);
        }).ToList();

        var results = await Task.WhenAll(jobTasks);
        return results.All(r => r);
    }

    private async Task CancelJobAsync(int jobId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        var job = await db.WorkflowJobs.Include(j => j.Steps).FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job == null) return;

        job.Status = WorkflowStatus.Cancelled;
        job.CompletedAt = DateTime.UtcNow;
        foreach (var step in job.Steps)
        {
            step.Status = WorkflowStatus.Cancelled;
            step.CompletedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<bool> ExecuteJobById(int jobId, WorkflowRun run, Dictionary<string, string>? upstreamOutputs, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        var job = await db.WorkflowJobs.Include(j => j.Steps).FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job == null) return false;

        var timeoutMinutes = job.TimeoutMinutes ?? 360;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(timeoutMinutes));

        try
        {
            return await ExecuteJob(db, run, job, upstreamOutputs, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Job {JobName} in run {RunId} timed out after {Timeout} minutes", job.Name, run.Id, timeoutMinutes);
            job.Status = WorkflowStatus.Failure;
            job.CompletedAt = DateTime.UtcNow;
            foreach (var step in job.Steps.Where(s => s.Status == WorkflowStatus.Queued || s.Status == WorkflowStatus.InProgress))
            {
                step.Status = WorkflowStatus.Failure;
                step.Output = (step.Output ?? "") + $"\n\nJob timed out after {timeoutMinutes} minutes";
                step.CompletedAt = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(ct);
            return false;
        }
    }

    private async Task<bool> ExecuteJob(AppDbContext db, WorkflowRun run, WorkflowJob job, Dictionary<string, string>? upstreamOutputs, CancellationToken ct)
    {
        job.Status = WorkflowStatus.InProgress;
        job.StartedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var image = MapImage(job.RunsOn);
        string? containerId = null;

        try
        {
            _logger.LogInformation("Pulling image {Image} for job {JobName}", image, job.Name);
            await _docker!.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = image },
                null,
                new Progress<JSONMessage>(),
                ct);

            var repoMount = GetRepoPath(run.RepoName);

            var envVars = new List<string>();
            try
            {
                var parser = new WorkflowYamlParser();
                var definitions = parser.ParseFromRepo(repoMount ?? "");
                var wfDef = definitions.FirstOrDefault(d => d.Name == run.WorkflowName);
                if (wfDef != null)
                {
                    if (wfDef.Env != null)
                        foreach (var (k, v) in wfDef.Env)
                            envVars.Add($"{k}={v}");
                    if (wfDef.Jobs.TryGetValue(job.Name, out var jobDef) && jobDef.Env != null)
                        foreach (var (k, v) in jobDef.Env)
                            envVars.Add($"{k}={v}");
                }
            }
            catch { }

            try
            {
                using var secretsScope = _scopeFactory.CreateScope();
                var secretsService = secretsScope.ServiceProvider.GetRequiredService<ISecretsService>();
                var secrets = await secretsService.GetAllSecretsForRunAsync(run.RepoName);
                foreach (var (name, value) in secrets)
                    envVars.Add($"{name}={value}");
                _logger.LogInformation("Loaded {Count} secret(s) for repo '{RepoName}'", secrets.Count, run.RepoName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load secrets for {RepoName}", run.RepoName);
            }

            // Inject github.* context as env vars
            var repoDisplayName = run.RepoName.EndsWith(".git") ? run.RepoName[..^4] : run.RepoName;
            envVars.Add($"GITHUB_SHA={run.CommitSha}");
            envVars.Add($"GITHUB_REF=refs/heads/{run.Branch}");
            envVars.Add($"GITHUB_REF_NAME={run.Branch}");
            envVars.Add($"GITHUB_ACTOR={run.TriggeredBy}");
            envVars.Add($"GITHUB_REPOSITORY={repoDisplayName}");
            envVars.Add($"GITHUB_EVENT_NAME={(run.TriggeredBy == "Manual trigger" ? "workflow_dispatch" : "push")}");
            envVars.Add($"GITHUB_WORKSPACE=/workspace");
            envVars.Add($"GITHUB_RUN_ID={run.Id}");
            envVars.Add($"GITHUB_RUN_NUMBER={run.Id}");
            envVars.Add($"GITHUB_JOB={job.Name}");
            envVars.Add($"GITHUB_WORKFLOW={run.WorkflowName}");
            envVars.Add($"CI=true");

            // Inject upstream job outputs as env vars (for needs.X.outputs.Y -> $Y)
            if (upstreamOutputs != null)
                foreach (var (k, v) in upstreamOutputs)
                    envVars.Add($"{k}={v}");

            // Inject workflow_dispatch inputs as INPUT_* env vars
            if (!string.IsNullOrEmpty(run.InputsJson))
            {
                try
                {
                    var inputs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(run.InputsJson);
                    if (inputs != null)
                        foreach (var (k, v) in inputs)
                            envVars.Add($"INPUT_{k.ToUpperInvariant()}={v}");
                }
                catch { }
            }

            string? artifactHostDir = null;
            try
            {
                using var artifactScope = _scopeFactory.CreateScope();
                var artifactService = artifactScope.ServiceProvider.GetRequiredService<IArtifactService>();
                artifactHostDir = Path.Combine(artifactService.GetArtifactsDirectory(), run.Id.ToString());
                if (!Directory.Exists(artifactHostDir))
                    Directory.CreateDirectory(artifactHostDir);
            }
            catch { }

            var binds = new List<string>();
            if (File.Exists("/var/run/docker.sock"))
                binds.Add("/var/run/docker.sock:/var/run/docker.sock");
            if (artifactHostDir != null)
                binds.Add($"{artifactHostDir}:/artifacts");

            var createParams = new CreateContainerParameters
            {
                Image = image,
                Cmd = new[] { "sleep", "3600" },
                Env = envVars,
                HostConfig = new HostConfig
                {
                    Memory = 512 * 1024 * 1024,
                    NanoCPUs = 1_000_000_000,
                    Binds = binds
                },
                WorkingDir = "/workspace"
            };

            var container = await _docker.Containers.CreateContainerAsync(createParams, ct);
            containerId = container.ID;

            await _docker.Containers.StartContainerAsync(containerId, null, ct);

            await ExecInContainer(containerId, new[] { "sh", "-c",
                "which git > /dev/null 2>&1 || (apt-get update -qq && apt-get install -y -qq git curl ca-certificates > /dev/null 2>&1) || " +
                "(apk add --no-cache git curl ca-certificates > /dev/null 2>&1) || true"
            }, null, ct);

            await ExecInContainer(containerId, new[] { "sh", "-c",
                "which docker > /dev/null 2>&1 || " +
                "(curl -fsSL https://get.docker.com | sh > /dev/null 2>&1) || " +
                "(apk add --no-cache docker-cli > /dev/null 2>&1) || true"
            }, null, ct);

            if (repoMount != null)
            {
                try
                {
                    _logger.LogInformation("Copying repo from {RepoPath} into workflow container", repoMount);

                    var tempClone = Path.Combine(Path.GetTempPath(), $"wf-{run.Id}-{Guid.NewGuid():N}");
                    try
                    {
                        var cloneProc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "git",
                            Arguments = $"clone \"{repoMount}\" \"{tempClone}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                        if (cloneProc != null)
                            await cloneProc.WaitForExitAsync(ct);

                        if (Directory.Exists(tempClone))
                        {
                            using var tarStream = new MemoryStream();
                            await CreateTarFromDirectory(tempClone, tarStream);
                            tarStream.Position = 0;
                            await _docker!.Containers.ExtractArchiveToContainerAsync(
                                containerId,
                                new ContainerPathStatParameters { Path = "/workspace", AllowOverwriteDirWithFile = true },
                                tarStream, ct);
                            _logger.LogInformation("Repo copied to workflow container successfully");
                        }
                    }
                    finally
                    {
                        try { if (Directory.Exists(tempClone)) Directory.Delete(tempClone, true); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to copy repo into workflow container");
                }
            }

            // Initialize files used for step outputs
            await ExecInContainer(containerId, new[] { "sh", "-c",
                "touch /tmp/github_output" }, null, ct);

            // Execute steps, tracking outputs and respecting if: conditions
            var anyStepFailed = false;
            var stepOutputs = new Dictionary<string, string>();

            foreach (var step in job.Steps)
            {
                if (ct.IsCancellationRequested) break;

                // Check if run was cancelled by a newer push
                try
                {
                    using var cancelCheckScope = _scopeFactory.CreateScope();
                    var cancelDb = cancelCheckScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
                    var runStatus = await cancelDb.WorkflowRuns.Where(r => r.Id == run.Id).Select(r => r.Status).FirstOrDefaultAsync(ct);
                    if (runStatus == WorkflowStatus.Cancelled)
                    {
                        _logger.LogInformation("Run {RunId} cancelled mid-execution — aborting remaining steps", run.Id);
                        break;
                    }
                }
                catch { }

                // Evaluate if: condition before running
                if (!EvaluateCondition(step.Condition, anyStepFailed))
                {
                    step.Status = WorkflowStatus.Cancelled;
                    step.CompletedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                    continue;
                }

                step.Status = WorkflowStatus.InProgress;
                step.StartedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);

                var command = step.Command ?? step.Name ?? "echo 'No command'";
                // Determine shell: step-level overrides job-level overrides default (sh)
                var shell = "sh";
                try
                {
                    var shellParser = new WorkflowYamlParser();
                    var shellDefs = shellParser.ParseFromRepo(GetRepoPath(run.RepoName) ?? "");
                    var shellWf = shellDefs.FirstOrDefault(d => d.Name == run.WorkflowName);
                    if (shellWf != null)
                    {
                        if (!string.IsNullOrEmpty(shellWf.DefaultShell)) shell = shellWf.DefaultShell;
                        var baseJobName = job.Name.Contains(" (") ? job.Name[..job.Name.IndexOf(" (")] : job.Name;
                        if (shellWf.Jobs.TryGetValue(baseJobName, out var jd))
                        {
                            var stepIdx = job.Steps.IndexOf(step);
                            if (stepIdx >= 0 && stepIdx < jd.Steps.Count && !string.IsNullOrEmpty(jd.Steps[stepIdx].Shell))
                                shell = jd.Steps[stepIdx].Shell;
                        }
                    }
                }
                catch { }
                var wrappedCommand = $"cd /workspace 2>/dev/null; {command}";

                // Inject GITHUB_OUTPUT path plus any outputs from previous steps
                var execEnv = new Dictionary<string, string>(stepOutputs)
                {
                    ["GITHUB_OUTPUT"] = "/tmp/github_output"
                };

                var (exitCode, output) = await ExecInContainer(containerId,
                    new[] { shell, "-c", wrappedCommand }, execEnv, ct);

                step.Output = output;
                step.CompletedAt = DateTime.UtcNow;

                // Read and process $GITHUB_OUTPUT entries written by the step
                // Supports both key=value and key<<DELIMITER\nvalue\nDELIMITER formats
                var (_, outputFileContent) = await ExecInContainer(containerId,
                    new[] { "sh", "-c", "cat /tmp/github_output 2>/dev/null; : > /tmp/github_output" }, null, ct);

                ParseGitHubOutput(outputFileContent, stepOutputs);

                if (exitCode != 0)
                {
                    step.Status = WorkflowStatus.Failure;
                    step.Output += $"\n\nProcess exited with code {exitCode}";
                    if (!step.ContinueOnError)
                        anyStepFailed = true;
                    else
                        step.Output += "\n(continue-on-error: true — job not failed)";
                }
                else
                {
                    step.Status = WorkflowStatus.Success;
                }

                await db.SaveChangesAsync(ct);
            }

            // Mark any steps that never ran as cancelled
            foreach (var remaining in job.Steps.Where(s => s.Status == WorkflowStatus.Queued))
            {
                remaining.Status = WorkflowStatus.Cancelled;
                remaining.CompletedAt = DateTime.UtcNow;
            }

            // Collect artifacts
            if (artifactHostDir != null && Directory.Exists(artifactHostDir))
            {
                try
                {
                    using var artScope = _scopeFactory.CreateScope();
                    var artService = artScope.ServiceProvider.GetRequiredService<IArtifactService>();
                    foreach (var file in Directory.GetFiles(artifactHostDir))
                    {
                        using var fs = File.OpenRead(file);
                        await artService.SaveArtifactAsync(run.Id, Path.GetFileName(file), fs);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to collect artifacts for run {RunId}", run.Id);
                }
            }

            // Resolve job outputs from step outputs (outputs: { key: ${{ steps.X.outputs.Y }} })
            try
            {
                var parser = new WorkflowYamlParser();
                var defs = parser.ParseFromRepo(GetRepoPath(run.RepoName) ?? "");
                var wfDef = defs.FirstOrDefault(d => d.Name == run.WorkflowName);
                // Find the base job name (strip matrix suffix)
                var baseJobName = job.Name.Contains(" (") ? job.Name[..job.Name.IndexOf(" (")] : job.Name;
                if (wfDef?.Jobs.TryGetValue(baseJobName, out var jobDef) == true && jobDef.Outputs != null)
                {
                    var resolvedOutputs = new Dictionary<string, string>();
                    foreach (var (key, expr) in jobDef.Outputs)
                    {
                        // Resolve ${{ steps.X.outputs.Y }} to the actual value from stepOutputs
                        var match = System.Text.RegularExpressions.Regex.Match(expr, @"\$\{\{\s*steps\.\w+\.outputs\.(\w+)\s*\}\}");
                        if (match.Success && stepOutputs.TryGetValue(match.Groups[1].Value, out var val))
                            resolvedOutputs[key] = val;
                        else
                            resolvedOutputs[key] = expr; // pass through unresolved
                    }
                    if (resolvedOutputs.Count > 0)
                        job.OutputsJson = System.Text.Json.JsonSerializer.Serialize(resolvedOutputs);
                }
            }
            catch { }

            // Check for release metadata written by softprops/action-gh-release translation
            if (containerId != null)
                await TryCreateReleaseFromContainer(containerId, run, stepOutputs, ct);

            job.Status = anyStepFailed ? WorkflowStatus.Failure : WorkflowStatus.Success;
            job.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return !anyStepFailed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobName} failed in run {RunId}", job.Name, run.Id);
            job.Status = WorkflowStatus.Failure;
            job.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return false;
        }
        finally
        {
            if (containerId != null)
            {
                try
                {
                    await _docker!.Containers.StopContainerAsync(containerId, new ContainerStopParameters { WaitBeforeKillSeconds = 5 }, ct);
                    await _docker.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true }, ct);
                }
                catch { }
            }
        }
    }

    /// <summary>
    /// Returns true if a step should run given its if: condition and whether any previous step failed.
    /// Supports: always(), success(), failure(), cancelled(), true, false.
    /// Default (no condition) behaves like success() — only runs if no prior failures.
    /// </summary>
    private static bool EvaluateCondition(string? condition, bool anyStepFailed)
    {
        if (string.IsNullOrWhiteSpace(condition))
            return !anyStepFailed;

        return condition.Trim().ToLowerInvariant() switch
        {
            "always()" or "always"       => true,
            "success()" or "success"     => !anyStepFailed,
            "failure()" or "failure"     => anyStepFailed,
            "cancelled()" or "cancelled" => false,
            "true" or "1"                => true,
            "false" or "0"               => false,
            _                            => !anyStepFailed
        };
    }

    /// <summary>
    /// Parses GITHUB_OUTPUT content supporting both key=value and key&lt;&lt;DELIMITER multiline formats.
    /// </summary>
    private static void ParseGitHubOutput(string content, Dictionary<string, string> outputs)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        var lines = content.Split('\n');
        int i = 0;
        while (i < lines.Length)
        {
            var line = lines[i];
            // Check for multiline: key<<DELIMITER
            var heredocMatch = System.Text.RegularExpressions.Regex.Match(line, @"^(\w+)<<(.+)$");
            if (heredocMatch.Success)
            {
                var key = heredocMatch.Groups[1].Value;
                var delimiter = heredocMatch.Groups[2].Value.Trim();
                var valueLines = new List<string>();
                i++;
                while (i < lines.Length && lines[i].Trim() != delimiter)
                {
                    valueLines.Add(lines[i]);
                    i++;
                }
                outputs[key] = string.Join("\n", valueLines);
                i++; // skip delimiter line
                continue;
            }
            // Simple key=value
            var eqIdx = line.IndexOf('=');
            if (eqIdx > 0)
                outputs[line[..eqIdx].Trim()] = line[(eqIdx + 1)..];
            i++;
        }
    }

    private static List<string> ParseJobNeeds(string? needs) =>
        string.IsNullOrEmpty(needs)
            ? new List<string>()
            : needs.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

    private async Task<(long ExitCode, string Output)> ExecInContainer(
        string containerId, string[] cmd, Dictionary<string, string>? extraEnv, CancellationToken ct)
    {
        var execParams = new ContainerExecCreateParameters
        {
            Cmd = cmd,
            AttachStdout = true,
            AttachStderr = true,
            Env = extraEnv?.Select(kv => $"{kv.Key}={kv.Value}").ToList()
        };

        var exec = await _docker!.Exec.ExecCreateContainerAsync(containerId, execParams, ct);
        using var stream = await _docker.Exec.StartAndAttachContainerExecAsync(exec.ID, false, ct);

        var (stdout, stderr) = await stream.ReadOutputToEndAsync(ct);
        var output = stdout + (string.IsNullOrEmpty(stderr) ? "" : $"\nSTDERR:\n{stderr}");

        var inspect = await _docker.Exec.InspectContainerExecAsync(exec.ID, ct);
        return (inspect.ExitCode, output);
    }

    private async Task<bool> TryCreateTagFromWorkflow(WorkflowRun run, CancellationToken ct)
    {
        try
        {
            string? newTag = null;
            foreach (var job in run.Jobs)
            {
                foreach (var step in job.Steps)
                {
                    if (string.IsNullOrEmpty(step.Output)) continue;
                    var match = System.Text.RegularExpressions.Regex.Match(
                        step.Output, @"New tag:\s*(v[\d.]+)");
                    if (match.Success) { newTag = match.Groups[1].Value; break; }
                }
                if (newTag != null) break;
            }

            if (string.IsNullOrEmpty(newTag)) return false;

            var repoPath = GetRepoPath(run.RepoName);
            if (repoPath == null) return false;

            using var repo = new LibGit2Sharp.Repository(repoPath);
            if (repo.Tags[newTag] != null) return false;

            var commitObj = repo.Lookup(new LibGit2Sharp.ObjectId(run.CommitSha));
            var commit = (commitObj as LibGit2Sharp.Commit) ?? repo.Head?.Tip;
            if (commit == null) return false;

            var tagger = new LibGit2Sharp.Signature("MyPersonalGit CI", "ci@localhost", DateTimeOffset.UtcNow);
            repo.Tags.Add(newTag, commit, tagger, $"Release {newTag}");
            _logger.LogInformation("Created tag {Tag} in {RepoName}", newTag, run.RepoName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create tag from workflow run {RunId}", run.Id);
            return false;
        }
    }

    /// <summary>
    /// Reads /tmp/release_meta from the container (written by softprops/action-gh-release translation)
    /// and creates a real Release entity in the database.
    /// </summary>
    private async Task TryCreateReleaseFromContainer(string containerId, WorkflowRun run, Dictionary<string, string> stepOutputs, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Checking for release metadata in container for run {RunId}", run.Id);
            var (exitCode, metaContent) = await ExecInContainer(containerId,
                new[] { "sh", "-c", "cat /tmp/release_meta 2>/dev/null" }, null, ct);
            _logger.LogInformation("Release meta check: exit={ExitCode}, content='{Content}'", exitCode, metaContent?.Trim());
            if (exitCode != 0 || string.IsNullOrWhiteSpace(metaContent)) return;

            var meta = new Dictionary<string, string>();
            foreach (var line in metaContent.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var eqIdx = line.IndexOf('=');
                if (eqIdx > 0)
                    meta[line[..eqIdx].Trim()] = line[(eqIdx + 1)..].Trim();
            }

            if (!meta.TryGetValue("TAG_NAME", out var tagName) || string.IsNullOrEmpty(tagName))
            {
                _logger.LogWarning("No TAG_NAME found in release meta for run {RunId}. Keys: {Keys}", run.Id, string.Join(", ", meta.Keys));
                return;
            }
            var releaseName = meta.GetValueOrDefault("RELEASE_NAME", tagName);
            var isPrerelease = meta.GetValueOrDefault("PRERELEASE", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
            var isDraft = meta.GetValueOrDefault("DRAFT", "false").Equals("true", StringComparison.OrdinalIgnoreCase);

            // Construct release body from step outputs (avoids shell escaping issues)
            string? body = null;
            if (stepOutputs.TryGetValue("changelog", out var changelog) && !string.IsNullOrWhiteSpace(changelog))
            {
                body = $"## Changes\n{changelog}\n\n## Docker\n```bash\ndocker pull fennch/mypersonalgit:{tagName}\n```";
            }

            // Use the DB repo name (may have .git suffix) to match the Repositories table
            var repoNameForRelease = run.RepoName;
            if (!repoNameForRelease.EndsWith(".git"))
                repoNameForRelease += ".git";

            using var scope = _scopeFactory.CreateScope();
            var releaseService = scope.ServiceProvider.GetRequiredService<IReleaseService>();

            var existing = await releaseService.GetReleasesAsync(repoNameForRelease);
            if (existing.Any(r => r.TagName == tagName))
            {
                _logger.LogDebug("Release for tag {Tag} already exists in {RepoName}", tagName, repoNameForRelease);
                return;
            }

            _logger.LogInformation("Creating release: tag={Tag}, name={Name}, repo={Repo}, hasBody={HasBody}",
                tagName, releaseName, repoNameForRelease, body != null);
            await releaseService.CreateReleaseAsync(repoNameForRelease, tagName, releaseName, body, "ci", isDraft, isPrerelease);
            _logger.LogInformation("Created release {Tag} for {RepoName}", tagName, repoNameForRelease);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create release from workflow run {RunId}", run.Id);
        }
    }

    private async Task SetCommitStatus(AppDbContext db, WorkflowRun run, bool success)
    {
        try
        {
            var context = $"ci/{run.WorkflowName.ToLowerInvariant().Replace(' ', '-')}";
            var existing = await db.CommitStatuses
                .FirstOrDefaultAsync(s => s.RepoName == run.RepoName && s.Sha == run.CommitSha && s.Context == context);

            if (existing != null)
            {
                existing.State = success ? CommitStatusState.Success : CommitStatusState.Failure;
                existing.Description = success ? "Workflow passed" : "Workflow failed";
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                db.CommitStatuses.Add(new CommitStatus
                {
                    RepoName = run.RepoName,
                    Sha = run.CommitSha,
                    State = success ? CommitStatusState.Success : CommitStatusState.Failure,
                    Context = context,
                    Description = success ? "Workflow passed" : "Workflow failed",
                    Creator = "ci"
                });
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set commit status for run {RunId}", run.Id);
        }
    }

    private async Task TryAutoMerge(string repoName, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var prService = scope.ServiceProvider.GetRequiredService<IPullRequestService>();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = dbFactory.CreateDbContext();

            var autoMergePrs = await db.PullRequests
                .Where(p => p.RepoName == repoName &&
                            p.State == PullRequestState.Open &&
                            p.AutoMergeEnabled &&
                            !p.IsDraft)
                .ToListAsync(ct);

            foreach (var pr in autoMergePrs)
            {
                var (canMerge, _) = await prService.CanMergeAsync(repoName, pr.Number);
                if (!canMerge) continue;

                var strategy = Enum.TryParse<MergeStrategy>(pr.AutoMergeStrategy, out var s)
                    ? s : MergeStrategy.MergeCommit;

                var (success, error) = await prService.MergePullRequestAsync(
                    repoName, pr.Number, "auto-merge", strategy);

                if (success)
                    _logger.LogInformation("Auto-merged PR #{Number} in {RepoName}", pr.Number, repoName);
                else
                    _logger.LogWarning("Auto-merge failed for PR #{Number} in {RepoName}: {Error}", pr.Number, repoName, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking auto-merge for {RepoName}", repoName);
        }
    }

    private static string MapImage(string runsOn)
    {
        if (runsOn.Contains('/') || (runsOn.Contains(':') && !runsOn.StartsWith("ubuntu") && !runsOn.StartsWith("node") && !runsOn.StartsWith("python") && !runsOn.StartsWith("dotnet")))
            return runsOn;

        return runsOn.ToLowerInvariant() switch
        {
            "ubuntu-latest" or "ubuntu-22.04" => "ubuntu:22.04",
            "ubuntu-20.04"                    => "ubuntu:20.04",
            "node" or "node-latest" or "node-20" => "node:20",
            "node-18"                         => "node:18",
            "python" or "python-latest" or "python-3.12" => "python:3.12",
            "python-3.11"                     => "python:3.11",
            "dotnet" or "dotnet-8" or "dotnet-latest" => "mcr.microsoft.com/dotnet/sdk:8.0",
            "dotnet-6"                        => "mcr.microsoft.com/dotnet/sdk:6.0",
            _                                 => "ubuntu:22.04"
        };
    }

    private static async Task CreateTarFromDirectory(string sourceDir, Stream outputStream)
    {
        var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(sourceDir, file).Replace('\\', '/');
            var fileInfo = new FileInfo(file);
            var content = await File.ReadAllBytesAsync(file);

            var header = new byte[512];
            var nameBytes = System.Text.Encoding.ASCII.GetBytes(relativePath);
            Array.Copy(nameBytes, header, Math.Min(nameBytes.Length, 100));

            System.Text.Encoding.ASCII.GetBytes("0100644\0").CopyTo(header, 100);
            System.Text.Encoding.ASCII.GetBytes("0000000\0").CopyTo(header, 108);
            System.Text.Encoding.ASCII.GetBytes("0000000\0").CopyTo(header, 116);
            System.Text.Encoding.ASCII.GetBytes(Convert.ToString(content.Length, 8).PadLeft(11, '0') + "\0").CopyTo(header, 124);
            var mtime = (long)(fileInfo.LastWriteTimeUtc - new DateTime(1970, 1, 1)).TotalSeconds;
            System.Text.Encoding.ASCII.GetBytes(Convert.ToString(mtime, 8).PadLeft(11, '0') + "\0").CopyTo(header, 136);
            header[156] = (byte)'0';
            System.Text.Encoding.ASCII.GetBytes("ustar\0").CopyTo(header, 257);
            System.Text.Encoding.ASCII.GetBytes("00").CopyTo(header, 263);

            for (int i = 148; i < 156; i++) header[i] = (byte)' ';
            var checksum = 0;
            for (int i = 0; i < 512; i++) checksum += header[i];
            System.Text.Encoding.ASCII.GetBytes(Convert.ToString(checksum, 8).PadLeft(6, '0') + "\0 ").CopyTo(header, 148);

            await outputStream.WriteAsync(header);
            await outputStream.WriteAsync(content);

            var padding = 512 - (content.Length % 512);
            if (padding < 512)
                await outputStream.WriteAsync(new byte[padding]);
        }

        await outputStream.WriteAsync(new byte[1024]);
    }

    private static string? GetRepoPath(string repoName)
    {
        var basePath = "/repos";
        var path = Path.Combine(basePath, repoName);
        if (Directory.Exists(path)) return path;
        if (Directory.Exists(path + ".git")) return path + ".git";
        return null;
    }
}
