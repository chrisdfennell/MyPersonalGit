namespace MyPersonalGit.Models;

public enum CollaboratorPermission
{
    Read,
    Write,
    Admin
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
