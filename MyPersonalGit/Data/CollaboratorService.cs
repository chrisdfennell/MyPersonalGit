using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface ICollaboratorService
{
    Task<List<RepositoryCollaborator>> GetCollaboratorsAsync(string repoName);
    Task<bool> AddCollaboratorAsync(string repoName, string username, CollaboratorPermission permission, string invitedBy);
    Task<bool> RemoveCollaboratorAsync(string repoName, string username);
    Task<bool> UpdatePermissionAsync(string repoName, string username, CollaboratorPermission permission);
    Task<bool> HasPermissionAsync(string repoName, string username, CollaboratorPermission minPermission);
    Task<bool> IsCollaboratorAsync(string repoName, string username);
}

public class CollaboratorService : ICollaboratorService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<CollaboratorService> _logger;

    public CollaboratorService(IDbContextFactory<AppDbContext> dbFactory, ILogger<CollaboratorService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<RepositoryCollaborator>> GetCollaboratorsAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.RepositoryCollaborators
            .Where(c => c.RepoName.ToLower() == repoName.ToLower())
            .ToListAsync();
    }

    public async Task<bool> AddCollaboratorAsync(string repoName, string username, CollaboratorPermission permission, string invitedBy)
    {
        using var db = _dbFactory.CreateDbContext();

        if (await db.RepositoryCollaborators.AnyAsync(c => c.RepoName.ToLower() == repoName.ToLower() && c.Username.ToLower() == username.ToLower()))
            return false;

        db.RepositoryCollaborators.Add(new RepositoryCollaborator
        {
            RepoName = repoName,
            Username = username,
            Permission = permission,
            InvitedBy = invitedBy,
            AddedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        _logger.LogInformation("Collaborator {Username} added to {RepoName} with {Permission} by {InvitedBy}", username, repoName, permission, invitedBy);
        return true;
    }

    public async Task<bool> RemoveCollaboratorAsync(string repoName, string username)
    {
        using var db = _dbFactory.CreateDbContext();

        var collab = await db.RepositoryCollaborators
            .FirstOrDefaultAsync(c => c.RepoName.ToLower() == repoName.ToLower() && c.Username.ToLower() == username.ToLower());

        if (collab == null) return false;

        db.RepositoryCollaborators.Remove(collab);
        await db.SaveChangesAsync();
        _logger.LogInformation("Collaborator {Username} removed from {RepoName}", username, repoName);
        return true;
    }

    public async Task<bool> UpdatePermissionAsync(string repoName, string username, CollaboratorPermission permission)
    {
        using var db = _dbFactory.CreateDbContext();

        var collab = await db.RepositoryCollaborators
            .FirstOrDefaultAsync(c => c.RepoName.ToLower() == repoName.ToLower() && c.Username.ToLower() == username.ToLower());

        if (collab == null) return false;

        collab.Permission = permission;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasPermissionAsync(string repoName, string username, CollaboratorPermission minPermission)
    {
        using var db = _dbFactory.CreateDbContext();

        // Check if user is repo owner first
        var repo = await db.Repositories.FirstOrDefaultAsync(r => r.Name.ToLower() == repoName.ToLower());
        if (repo != null && repo.Owner.Equals(username, StringComparison.OrdinalIgnoreCase))
            return true;

        var collab = await db.RepositoryCollaborators
            .FirstOrDefaultAsync(c => c.RepoName.ToLower() == repoName.ToLower() && c.Username.ToLower() == username.ToLower());

        if (collab == null) return false;

        return collab.Permission >= minPermission;
    }

    public async Task<bool> IsCollaboratorAsync(string repoName, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.RepositoryCollaborators
            .AnyAsync(c => c.RepoName.ToLower() == repoName.ToLower() && c.Username.ToLower() == username.ToLower());
    }
}
