using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IOrganizationService
{
    Task<List<Organization>> GetOrganizationsAsync();
    Task<Organization?> GetOrganizationAsync(string name);
    Task<Organization> CreateOrganizationAsync(string name, string owner, string? displayName = null, string? description = null);
    Task<bool> UpdateOrganizationAsync(string name, Action<Organization> updateAction);
    Task<bool> DeleteOrganizationAsync(string name);
    Task<List<OrganizationMember>> GetMembersAsync(string orgName);
    Task<bool> AddMemberAsync(string orgName, string username, OrgRole role = OrgRole.Member);
    Task<bool> RemoveMemberAsync(string orgName, string username);
    Task<bool> UpdateMemberRoleAsync(string orgName, string username, OrgRole role);
    Task<bool> IsMemberAsync(string orgName, string username);
    Task<bool> IsOwnerAsync(string orgName, string username);
    Task<List<Organization>> GetUserOrganizationsAsync(string username);
    Task<List<Team>> GetTeamsAsync(string orgName);
    Task<Team?> GetTeamAsync(string orgName, string teamName);
    Task<Team> CreateTeamAsync(string orgName, string name, string? description = null, TeamPermission permission = TeamPermission.Read);
    Task<bool> DeleteTeamAsync(int teamId);
    Task<bool> AddTeamMemberAsync(int teamId, string username, TeamRole role = TeamRole.Member);
    Task<bool> RemoveTeamMemberAsync(int teamId, string username);
    Task<bool> AddTeamRepositoryAsync(int teamId, string repoName);
    Task<bool> RemoveTeamRepositoryAsync(int teamId, string repoName);
}

public class OrganizationService : IOrganizationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<OrganizationService> _logger;
    private readonly IActivityService _activityService;

    public OrganizationService(IDbContextFactory<AppDbContext> dbFactory, ILogger<OrganizationService> logger, IActivityService activityService)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _activityService = activityService;
    }

    public async Task<List<Organization>> GetOrganizationsAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Organizations.OrderBy(o => o.Name).ToListAsync();
    }

    public async Task<Organization?> GetOrganizationAsync(string name)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Organizations.FirstOrDefaultAsync(o => o.Name.ToLower() == name.ToLower());
    }

    public async Task<Organization> CreateOrganizationAsync(string name, string owner, string? displayName = null, string? description = null)
    {
        using var db = _dbFactory.CreateDbContext();

        var org = new Organization
        {
            Name = name,
            Owner = owner,
            DisplayName = displayName ?? name,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        db.Organizations.Add(org);

        // Add owner as org member
        db.OrganizationMembers.Add(new OrganizationMember
        {
            OrganizationName = name,
            Username = owner,
            Role = OrgRole.Owner,
            JoinedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        _logger.LogInformation("Organization {Name} created by {Owner}", name, owner);

        await _activityService.RecordActivityAsync(owner, "created_org", name, $"{owner} created organization {name}", $"/org/{name}");

        return org;
    }

    public async Task<bool> UpdateOrganizationAsync(string name, Action<Organization> updateAction)
    {
        using var db = _dbFactory.CreateDbContext();
        var org = await db.Organizations.FirstOrDefaultAsync(o => o.Name.ToLower() == name.ToLower());
        if (org == null) return false;
        updateAction(org);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteOrganizationAsync(string name)
    {
        using var db = _dbFactory.CreateDbContext();
        var org = await db.Organizations.FirstOrDefaultAsync(o => o.Name.ToLower() == name.ToLower());
        if (org == null) return false;

        // Remove all members, teams, team members, team repos
        var members = await db.OrganizationMembers.Where(m => m.OrganizationName == name).ToListAsync();
        db.OrganizationMembers.RemoveRange(members);

        var teams = await db.Teams.Include(t => t.Members).Include(t => t.Repositories)
            .Where(t => t.OrganizationName == name).ToListAsync();
        db.Teams.RemoveRange(teams);

        db.Organizations.Remove(org);
        await db.SaveChangesAsync();
        _logger.LogInformation("Organization {Name} deleted", name);
        return true;
    }

    public async Task<List<OrganizationMember>> GetMembersAsync(string orgName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.OrganizationMembers.Where(m => m.OrganizationName == orgName).ToListAsync();
    }

    public async Task<bool> AddMemberAsync(string orgName, string username, OrgRole role = OrgRole.Member)
    {
        using var db = _dbFactory.CreateDbContext();
        if (await db.OrganizationMembers.AnyAsync(m => m.OrganizationName == orgName && m.Username == username))
            return false;

        db.OrganizationMembers.Add(new OrganizationMember
        {
            OrganizationName = orgName,
            Username = username,
            Role = role,
            JoinedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveMemberAsync(string orgName, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        var member = await db.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationName == orgName && m.Username == username);
        if (member == null) return false;
        db.OrganizationMembers.Remove(member);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateMemberRoleAsync(string orgName, string username, OrgRole role)
    {
        using var db = _dbFactory.CreateDbContext();
        var member = await db.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationName == orgName && m.Username == username);
        if (member == null) return false;
        member.Role = role;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsMemberAsync(string orgName, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.OrganizationMembers.AnyAsync(m => m.OrganizationName == orgName && m.Username == username);
    }

    public async Task<bool> IsOwnerAsync(string orgName, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.OrganizationMembers.AnyAsync(m => m.OrganizationName == orgName && m.Username == username && m.Role == OrgRole.Owner);
    }

    public async Task<List<Organization>> GetUserOrganizationsAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();
        var orgNames = await db.OrganizationMembers
            .Where(m => m.Username == username)
            .Select(m => m.OrganizationName)
            .ToListAsync();

        return await db.Organizations.Where(o => orgNames.Contains(o.Name)).ToListAsync();
    }

    public async Task<List<Team>> GetTeamsAsync(string orgName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Teams
            .Include(t => t.Members)
            .Include(t => t.Repositories)
            .Where(t => t.OrganizationName == orgName)
            .ToListAsync();
    }

    public async Task<Team?> GetTeamAsync(string orgName, string teamName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Teams
            .Include(t => t.Members)
            .Include(t => t.Repositories)
            .FirstOrDefaultAsync(t => t.OrganizationName == orgName && t.Name.ToLower() == teamName.ToLower());
    }

    public async Task<Team> CreateTeamAsync(string orgName, string name, string? description = null, TeamPermission permission = TeamPermission.Read)
    {
        using var db = _dbFactory.CreateDbContext();
        var team = new Team
        {
            OrganizationName = orgName,
            Name = name,
            Description = description,
            Permission = permission,
            CreatedAt = DateTime.UtcNow
        };
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        _logger.LogInformation("Team {Name} created in org {Org}", name, orgName);
        return team;
    }

    public async Task<bool> DeleteTeamAsync(int teamId)
    {
        using var db = _dbFactory.CreateDbContext();
        var team = await db.Teams.FindAsync(teamId);
        if (team == null) return false;
        db.Teams.Remove(team);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddTeamMemberAsync(int teamId, string username, TeamRole role = TeamRole.Member)
    {
        using var db = _dbFactory.CreateDbContext();
        if (await db.TeamMembers.AnyAsync(m => m.TeamId == teamId && m.Username == username))
            return false;

        db.TeamMembers.Add(new TeamMember { TeamId = teamId, Username = username, Role = role, AddedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveTeamMemberAsync(int teamId, string username)
    {
        using var db = _dbFactory.CreateDbContext();
        var member = await db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.Username == username);
        if (member == null) return false;
        db.TeamMembers.Remove(member);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddTeamRepositoryAsync(int teamId, string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        if (await db.TeamRepositories.AnyAsync(r => r.TeamId == teamId && r.RepoName == repoName))
            return false;

        db.TeamRepositories.Add(new TeamRepository { TeamId = teamId, RepoName = repoName, AddedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveTeamRepositoryAsync(int teamId, string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        var repo = await db.TeamRepositories.FirstOrDefaultAsync(r => r.TeamId == teamId && r.RepoName == repoName);
        if (repo == null) return false;
        db.TeamRepositories.Remove(repo);
        await db.SaveChangesAsync();
        return true;
    }
}
