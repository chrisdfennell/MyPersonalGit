namespace MyPersonalGit.Models;

public class TagProtectionRule
{
    public int Id { get; set; }
    public string RepoName { get; set; } = string.Empty;
    public required string TagPattern { get; set; }
    public bool PreventDeletion { get; set; } = true;
    public bool PreventForcePush { get; set; } = true;
    public bool RestrictCreation { get; set; }
    public List<string> AllowedUsers { get; set; } = new();
    public bool RequireSignedTags { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
