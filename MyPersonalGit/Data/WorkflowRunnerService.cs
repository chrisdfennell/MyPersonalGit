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
            // Connect to Docker socket
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

        var runSuccess = true;

        foreach (var job in run.Jobs)
        {
            if (ct.IsCancellationRequested) break;

            var jobSuccess = await ExecuteJob(db, run, job, ct);
            if (!jobSuccess) runSuccess = false;
        }

        run.Status = runSuccess ? WorkflowStatus.Success : WorkflowStatus.Failure;
        run.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Workflow run {RunId} completed with status {Status}", run.Id, run.Status);

        // Auto-merge: if the run succeeded, check for PRs with auto-merge enabled
        if (runSuccess)
        {
            await TryAutoMerge(run.RepoName, ct);
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

    private async Task<bool> ExecuteJob(AppDbContext db, WorkflowRun run, WorkflowJob job, CancellationToken ct)
    {
        job.Status = WorkflowStatus.InProgress;
        job.StartedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var image = MapImage(job.RunsOn);
        string? containerId = null;

        try
        {
            // Pull image
            _logger.LogInformation("Pulling image {Image} for job {JobName}", image, job.Name);
            await _docker!.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = image },
                null,
                new Progress<JSONMessage>(),
                ct);

            // Resolve repo path
            var repoMount = GetRepoPath(run.RepoName);

            // Load repository secrets and inject as environment variables
            var envVars = new List<string>();
            try
            {
                using var secretsScope = _scopeFactory.CreateScope();
                var secretsService = secretsScope.ServiceProvider.GetRequiredService<ISecretsService>();
                var secrets = await secretsService.GetAllSecretsForRunAsync(run.RepoName);
                foreach (var (name, value) in secrets)
                {
                    envVars.Add($"{name}={value}");
                }
                if (secrets.Count > 0)
                    _logger.LogInformation("Injecting {Count} secret(s) into job {JobName}", secrets.Count, job.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load secrets for {RepoName}", run.RepoName);
            }

            // Setup artifact directory for this run
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

            // Create container
            var binds = new List<string>();
            if (repoMount != null) binds.Add($"{repoMount}:/repo:ro");
            if (artifactHostDir != null) binds.Add($"{artifactHostDir}:/artifacts");
            // Mount Docker socket so workflows can build/push images
            if (File.Exists("/var/run/docker.sock"))
                binds.Add("/var/run/docker.sock:/var/run/docker.sock");

            var createParams = new CreateContainerParameters
            {
                Image = image,
                Cmd = new[] { "sleep", "3600" },
                Env = envVars,
                HostConfig = new HostConfig
                {
                    Memory = 512 * 1024 * 1024, // 512MB
                    NanoCPUs = 1_000_000_000,    // 1 CPU
                    Binds = binds
                },
                WorkingDir = "/workspace"
            };

            var container = await _docker.Containers.CreateContainerAsync(createParams, ct);
            containerId = container.ID;

            await _docker.Containers.StartContainerAsync(containerId, null, ct);

            // Clone bare repo into /workspace inside container
            if (repoMount != null)
            {
                await ExecInContainer(containerId, new[] { "sh", "-c", "git clone /repo /workspace 2>&1 || true" }, ct);
            }

            // Execute each step
            foreach (var step in job.Steps)
            {
                if (ct.IsCancellationRequested) break;

                step.Status = WorkflowStatus.InProgress;
                step.StartedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);

                var command = step.Command ?? step.Name ?? "echo 'No command'";
                var (exitCode, output) = await ExecInContainer(containerId, new[] { "sh", "-c", command }, ct);

                step.Output = output;
                step.CompletedAt = DateTime.UtcNow;

                if (exitCode != 0)
                {
                    step.Status = WorkflowStatus.Failure;
                    step.Output += $"\n\nProcess exited with code {exitCode}";
                    await db.SaveChangesAsync(ct);

                    // Mark remaining steps as cancelled
                    foreach (var remaining in job.Steps.Where(s => s.Status == WorkflowStatus.Queued))
                        remaining.Status = WorkflowStatus.Cancelled;

                    job.Status = WorkflowStatus.Failure;
                    job.CompletedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                    return false;
                }

                step.Status = WorkflowStatus.Success;
                await db.SaveChangesAsync(ct);
            }

            // Collect artifacts from /artifacts directory
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

            job.Status = WorkflowStatus.Success;
            job.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return true;
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
            // Cleanup container
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

    private async Task<(long ExitCode, string Output)> ExecInContainer(string containerId, string[] cmd, CancellationToken ct)
    {
        var execParams = new ContainerExecCreateParameters
        {
            Cmd = cmd,
            AttachStdout = true,
            AttachStderr = true
        };

        var exec = await _docker!.Exec.ExecCreateContainerAsync(containerId, execParams, ct);
        using var stream = await _docker.Exec.StartAndAttachContainerExecAsync(exec.ID, false, ct);

        var (stdout, stderr) = await stream.ReadOutputToEndAsync(ct);
        var output = stdout + (string.IsNullOrEmpty(stderr) ? "" : $"\nSTDERR:\n{stderr}");

        var inspect = await _docker.Exec.InspectContainerExecAsync(exec.ID, ct);
        return (inspect.ExitCode, output);
    }

    private static string MapImage(string runsOn)
    {
        // If it looks like a full image name (contains / or :), use it directly
        if (runsOn.Contains('/') || (runsOn.Contains(':') && !runsOn.StartsWith("ubuntu") && !runsOn.StartsWith("node") && !runsOn.StartsWith("python") && !runsOn.StartsWith("dotnet")))
            return runsOn;

        return runsOn.ToLowerInvariant() switch
        {
            "ubuntu-latest" or "ubuntu-22.04" => "ubuntu:22.04",
            "ubuntu-20.04" => "ubuntu:20.04",
            "node" or "node-latest" or "node-20" => "node:20",
            "node-18" => "node:18",
            "python" or "python-latest" or "python-3.12" => "python:3.12",
            "python-3.11" => "python:3.11",
            "dotnet" or "dotnet-8" or "dotnet-latest" => "mcr.microsoft.com/dotnet/sdk:8.0",
            "dotnet-6" => "mcr.microsoft.com/dotnet/sdk:6.0",
            _ => "ubuntu:22.04"
        };
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
