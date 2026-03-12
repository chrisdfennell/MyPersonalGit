namespace MyPersonalGit.Models;

public class Discussion
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public int Number { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public required string Author { get; set; }
    public DiscussionCategory Category { get; set; } = DiscussionCategory.General;
    public bool IsPinned { get; set; }
    public bool IsLocked { get; set; }
    public bool IsAnswered { get; set; }
    public int? AnswerCommentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public List<DiscussionComment> Comments { get; set; } = new();
}

public enum DiscussionCategory
{
    General,
    QAndA,
    Announcements,
    Ideas,
    ShowAndTell,
    Polls
}

public class DiscussionComment
{
    public int Id { get; set; }
    public int DiscussionId { get; set; }
    public required string Author { get; set; }
    public required string Body { get; set; }
    public int? ParentCommentId { get; set; }
    public bool IsAnswer { get; set; }
    public int UpvoteCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public List<DiscussionComment> Replies { get; set; } = new();
}
