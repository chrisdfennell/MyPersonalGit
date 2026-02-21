namespace MyPersonalGit.Models;

public class Notification
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Type { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required string RepoName { get; set; }
    public string? Url { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public static class NotificationType
{
    public const string IssueCreated = "issue_created";
    public const string IssueComment = "issue_comment";
    public const string IssueClosed = "issue_closed";
    public const string PullRequestCreated = "pr_created";
    public const string PullRequestReview = "pr_review";
    public const string PullRequestMerged = "pr_merged";
    public const string Mention = "mention";
    public const string RepositoryStarred = "repo_starred";
}
