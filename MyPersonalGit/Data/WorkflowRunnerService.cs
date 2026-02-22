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

            // Create container
            var createParams = new CreateContainerParameters
            {
                Image = image,
                Cmd = new[] { "sleep", "3600" },
                HostConfig = new HostConfig
                {
                    Memory = 512 * 1024 * 1024, // 512MB
                    NanoCPUs = 1_000_000_000,    // 1 CPU
                    Binds = repoMount != null
                        ? new[] { $"{repoMount}:/repo:ro" }
                        : Array.Empty<string>()
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
