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
    }

    /// <summary>
    /// Executes all jobs in the run, respecting needs: dependencies and running
    /// independent jobs in parallel.
    /// </summary>
    private async Task<bool> ExecuteJobsAsync(WorkflowRun run, CancellationToken ct)
    {
        var jobs = run.Jobs.ToList();

        // Each job gets a TCS so dependent jobs can await it
        var completions = jobs.ToDictionary(j => j.Name, _ => new TaskCompletionSource<bool>());

        var jobTasks = jobs.Select(job =>
        {
            var jobId = job.Id;
            var jobName = job.Name;
            var needs = ParseJobNeeds(job.Needs);

            return Task.Run(async () =>
            {
                // Wait for all declared dependencies to finish
                if (needs.Count > 0)
                {
                    var depTasks = needs
                        .Where(n => completions.ContainsKey(n))
                        .Select(n => completions[n].Task);

                    var depResults = await Task.WhenAll(depTasks);

                    if (!depResults.All(r => r))
                    {
                        // A required dependency failed — skip this job
                        await CancelJobAsync(jobId, ct);
                        completions[jobName].SetResult(false);
                        return false;
                    }
                }

                var result = await ExecuteJobById(jobId, run, ct);
                completions[jobName].SetResult(result);
                return result;
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

    private async Task<bool> ExecuteJobById(int jobId, WorkflowRun run, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        var job = await db.WorkflowJobs.Include(j => j.Steps).FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job == null) return false;

        return await ExecuteJob(db, run, job, ct);
    }

    private async Task<bool> ExecuteJob(AppDbContext db, WorkflowRun run, WorkflowJob job, CancellationToken ct)
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
                var wrappedCommand = $"cd /workspace 2>/dev/null; {command}";

                // Inject GITHUB_OUTPUT path plus any outputs from previous steps
                var execEnv = new Dictionary<string, string>(stepOutputs)
                {
                    ["GITHUB_OUTPUT"] = "/tmp/github_output"
                };

                var (exitCode, output) = await ExecInContainer(containerId,
                    new[] { "sh", "-c", wrappedCommand }, execEnv, ct);

                step.Output = output;
                step.CompletedAt = DateTime.UtcNow;

                // Read and process $GITHUB_OUTPUT entries written by the step
                var (_, outputFileContent) = await ExecInContainer(containerId,
                    new[] { "sh", "-c", "cat /tmp/github_output 2>/dev/null; : > /tmp/github_output" }, null, ct);

                foreach (var line in outputFileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var eqIdx = line.IndexOf('=');
                    if (eqIdx > 0)
                        stepOutputs[line[..eqIdx].Trim()] = line[(eqIdx + 1)..];
                }

                if (exitCode != 0)
                {
                    step.Status = WorkflowStatus.Failure;
                    step.Output += $"\n\nProcess exited with code {exitCode}";
                    anyStepFailed = true;
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
