namespace MyPersonalGit.Models;

public class IssueDependency
{
    public int Id { get; set; }
    public string RepoName { get; set; } = "";
    public int BlockingIssueNumber { get; set; }
    public int BlockedIssueNumber { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
