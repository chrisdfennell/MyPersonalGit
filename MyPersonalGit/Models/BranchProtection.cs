namespace MyPersonalGit.Models;

public class BranchProtectionRule
{
    public int Id { get; set; }
    public string RepoName { get; set; } = string.Empty;
    public required string BranchPattern { get; set; }
    public bool RequirePullRequest { get; set; }
    public int RequiredApprovals { get; set; }
    public bool RequireStatusChecks { get; set; }
    public List<string> RequiredStatusChecks { get; set; } = new();
    public bool PreventForcePush { get; set; } = true;
    public bool PreventDeletion { get; set; } = true;
    public bool RequireLinearHistory { get; set; }
    public bool RestrictPushes { get; set; }
    public List<string> AllowedPushUsers { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
