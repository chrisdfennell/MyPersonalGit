namespace MyPersonalGit.Models;

public class Organization
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public required string Owner { get; set; }
    public bool IsPublic { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Team
{
    public int Id { get; set; }
    public required string OrganizationName { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public TeamPermission Permission { get; set; } = TeamPermission.Read;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<TeamMember> Members { get; set; } = new();
    public List<TeamRepository> Repositories { get; set; } = new();
}

public class TeamMember
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public required string Username { get; set; }
    public TeamRole Role { get; set; } = TeamRole.Member;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

public class TeamRepository
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public required string RepoName { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

public class OrganizationMember
{
    public int Id { get; set; }
    public required string OrganizationName { get; set; }
    public required string Username { get; set; }
    public OrgRole Role { get; set; } = OrgRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public enum TeamPermission
{
    Read,
    Write,
    Admin
}

public enum TeamRole
{
    Member,
    Maintainer
}

public enum OrgRole
{
    Member,
    Owner
}
