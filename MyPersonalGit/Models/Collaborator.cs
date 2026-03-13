namespace MyPersonalGit.Models;

public enum CollaboratorPermission
{
    Read = 0,
    Triage = 1,
    Write = 2,
    Maintain = 3,
    Admin = 4
}

public class RepositoryCollaborator
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string Username { get; set; }
    public CollaboratorPermission Permission { get; set; } = CollaboratorPermission.Read;
    public string? InvitedBy { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
