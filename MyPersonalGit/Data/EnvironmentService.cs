using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IEnvironmentService
{
    Task<List<DeploymentEnvironment>> GetEnvironmentsAsync(string repoName);
    Task<DeploymentEnvironment?> GetEnvironmentAsync(string repoName, string name);
    Task<DeploymentEnvironment> CreateOrUpdateEnvironmentAsync(string repoName, string name, string? url, int waitTimerMinutes, bool requireApproval, List<string>? requiredReviewers, List<string>? allowedBranches);
    Task<bool> DeleteEnvironmentAsync(string repoName, string name);
    Task<Deployment> CreateDeploymentAsync(string repoName, string environmentName, string commitSha, string @ref, string creator, int? workflowRunId = null, string? description = null);
    Task<bool> ApproveDeploymentAsync(int deploymentId, string reviewer, bool approved, string? comment = null);
    Task<List<Deployment>> GetDeploymentsAsync(string repoName, string? environmentName = null);
    Task<Deployment?> GetDeploymentAsync(int deploymentId);
    Task<List<DeploymentApproval>> GetApprovalsAsync(int deploymentId);
    Task<bool> UpdateDeploymentStatusAsync(int deploymentId, DeploymentStatus status);
    Task ProcessPendingDeploymentsAsync();
}

public class EnvironmentService : IEnvironmentService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<EnvironmentService> _logger;
    private readonly INotificationService _notificationService;

    public EnvironmentService(IDbContextFactory<AppDbContext> dbFactory, ILogger<EnvironmentService> logger, INotificationService notificationService)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<List<DeploymentEnvironment>> GetEnvironmentsAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.DeploymentEnvironments.Where(e => e.RepoName == repoName).OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<DeploymentEnvironment?> GetEnvironmentAsync(string repoName, string name)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.DeploymentEnvironments.FirstOrDefaultAsync(e => e.RepoName == repoName && e.Name == name);
    }

    public async Task<DeploymentEnvironment> CreateOrUpdateEnvironmentAsync(
        string repoName, string name, string? url, int waitTimerMinutes, bool requireApproval,
        List<string>? requiredReviewers, List<string>? allowedBranches)
    {
        using var db = _dbFactory.CreateDbContext();
        var env = await db.DeploymentEnvironments.FirstOrDefaultAsync(e => e.RepoName == repoName && e.Name == name);

        if (env == null)
        {
            env = new DeploymentEnvironment
            {
                RepoName = repoName,
                Name = name,
                Url = url,
                WaitTimerMinutes = waitTimerMinutes,
                RequireApproval = requireApproval,
                RequiredReviewers = requiredReviewers ?? new(),
                AllowedBranches = allowedBranches ?? new(),
                CreatedAt = DateTime.UtcNow
            };
            db.DeploymentEnvironments.Add(env);
        }
        else
        {
            env.Url = url;
            env.WaitTimerMinutes = waitTimerMinutes;
            env.RequireApproval = requireApproval;
            env.RequiredReviewers = requiredReviewers ?? new();
            env.AllowedBranches = allowedBranches ?? new();
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("Environment '{Name}' configured for {RepoName}", name, repoName);
        return env;
    }

    public async Task<bool> DeleteEnvironmentAsync(string repoName, string name)
    {
        using var db = _dbFactory.CreateDbContext();
        var env = await db.DeploymentEnvironments.FirstOrDefaultAsync(e => e.RepoName == repoName && e.Name == name);
        if (env == null) return false;

        db.DeploymentEnvironments.Remove(env);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<Deployment> CreateDeploymentAsync(
        string repoName, string environmentName, string commitSha, string @ref, string creator,
        int? workflowRunId = null, string? description = null)
    {
        using var db = _dbFactory.CreateDbContext();
        var env = await db.DeploymentEnvironments.FirstOrDefaultAsync(e => e.RepoName == repoName && e.Name == environmentName);

        var status = DeploymentStatus.Pending;
        if (env != null)
        {
            // Check branch restrictions
            if (env.AllowedBranches.Any() && !env.AllowedBranches.Any(b =>
                b == @ref || b == "*" || (@ref.StartsWith("refs/heads/") && env.AllowedBranches.Contains(@ref.Replace("refs/heads/", "")))))
            {
                throw new InvalidOperationException($"Branch '{@ref}' is not allowed for environment '{environmentName}'");
            }

            if (env.RequireApproval)
                status = DeploymentStatus.WaitingApproval;
            else if (env.WaitTimerMinutes > 0)
                status = DeploymentStatus.WaitingTimer;
        }

        var deployment = new Deployment
        {
            EnvironmentId = env?.Id ?? 0,
            RepoName = repoName,
            EnvironmentName = environmentName,
            WorkflowRunId = workflowRunId,
            CommitSha = commitSha,
            Ref = @ref,
            Creator = creator,
            Status = status,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        db.Deployments.Add(deployment);
        await db.SaveChangesAsync();

        // Notify required reviewers
        if (status == DeploymentStatus.WaitingApproval && env != null)
        {
            foreach (var reviewer in env.RequiredReviewers)
            {
                await _notificationService.CreateNotificationAsync(
                    reviewer,
                    NotificationType.DeploymentApproval,
                    $"Deployment approval needed: {environmentName}",
                    $"{creator} requests deployment to {environmentName} for {repoName}",
                    repoName,
                    $"/repo/{repoName}/environments");
            }
        }

        _logger.LogInformation("Deployment created for {Env} in {Repo} (status: {Status})", environmentName, repoName, status);
        return deployment;
    }

    public async Task<bool> ApproveDeploymentAsync(int deploymentId, string reviewer, bool approved, string? comment = null)
    {
        using var db = _dbFactory.CreateDbContext();
        var deployment = await db.Deployments.FindAsync(deploymentId);
        if (deployment == null || deployment.Status != DeploymentStatus.WaitingApproval) return false;

        db.DeploymentApprovals.Add(new DeploymentApproval
        {
            DeploymentId = deploymentId,
            Reviewer = reviewer,
            Approved = approved,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        });

        if (!approved)
        {
            deployment.Status = DeploymentStatus.Cancelled;
            deployment.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            // Check if all required reviewers have approved
            var env = await db.DeploymentEnvironments.FindAsync(deployment.EnvironmentId);
            var approvals = await db.DeploymentApprovals
                .Where(a => a.DeploymentId == deploymentId && a.Approved)
                .Select(a => a.Reviewer)
                .Distinct()
                .ToListAsync();
            approvals.Add(reviewer);

            var allApproved = env == null || env.RequiredReviewers.All(r => approvals.Contains(r, StringComparer.OrdinalIgnoreCase));

            if (allApproved)
            {
                if (env?.WaitTimerMinutes > 0)
                    deployment.Status = DeploymentStatus.WaitingTimer;
                else
                {
                    deployment.Status = DeploymentStatus.InProgress;
                    deployment.StartedAt = DateTime.UtcNow;
                }
            }
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("Deployment {Id} {Action} by {Reviewer}", deploymentId, approved ? "approved" : "rejected", reviewer);
        return true;
    }

    public async Task<List<Deployment>> GetDeploymentsAsync(string repoName, string? environmentName = null)
    {
        using var db = _dbFactory.CreateDbContext();
        var query = db.Deployments.Where(d => d.RepoName == repoName);
        if (!string.IsNullOrEmpty(environmentName))
            query = query.Where(d => d.EnvironmentName == environmentName);
        return await query.OrderByDescending(d => d.CreatedAt).Take(50).ToListAsync();
    }

    public async Task<Deployment?> GetDeploymentAsync(int deploymentId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Deployments.FindAsync(deploymentId);
    }

    public async Task<List<DeploymentApproval>> GetApprovalsAsync(int deploymentId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.DeploymentApprovals.Where(a => a.DeploymentId == deploymentId).ToListAsync();
    }

    public async Task<bool> UpdateDeploymentStatusAsync(int deploymentId, DeploymentStatus status)
    {
        using var db = _dbFactory.CreateDbContext();
        var deployment = await db.Deployments.FindAsync(deploymentId);
        if (deployment == null) return false;

        deployment.Status = status;
        if (status == DeploymentStatus.InProgress && deployment.StartedAt == null)
            deployment.StartedAt = DateTime.UtcNow;
        if (status is DeploymentStatus.Success or DeploymentStatus.Failure or DeploymentStatus.Cancelled)
            deployment.CompletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task ProcessPendingDeploymentsAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        var waitingTimer = await db.Deployments
            .Where(d => d.Status == DeploymentStatus.WaitingTimer)
            .ToListAsync();

        foreach (var deployment in waitingTimer)
        {
            var env = await db.DeploymentEnvironments.FindAsync(deployment.EnvironmentId);
            if (env == null)
            {
                deployment.Status = DeploymentStatus.InProgress;
                deployment.StartedAt = DateTime.UtcNow;
                continue;
            }

            var waitUntil = deployment.CreatedAt.AddMinutes(env.WaitTimerMinutes);
            if (DateTime.UtcNow >= waitUntil)
            {
                deployment.Status = DeploymentStatus.InProgress;
                deployment.StartedAt = DateTime.UtcNow;
                _logger.LogInformation("Deployment {Id} timer elapsed, now in progress", deployment.Id);
            }
        }

        await db.SaveChangesAsync();
    }
}
