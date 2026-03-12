namespace MyPersonalGit.Models;

public class Reaction
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Emoji { get; set; } // thumbs_up, thumbs_down, heart, laugh, hooray, confused, rocket, eyes
    public int? IssueCommentId { get; set; }
    public int? ReviewCommentId { get; set; }
    public int? CommitCommentId { get; set; }
    public int? DiscussionCommentId { get; set; }
    public int? IssueId { get; set; }
    public int? PullRequestId { get; set; }
    public int? DiscussionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class ReactionEmojis
{
    public static readonly Dictionary<string, string> All = new()
    {
        ["thumbs_up"] = "\ud83d\udc4d",
        ["thumbs_down"] = "\ud83d\udc4e",
        ["heart"] = "\u2764\ufe0f",
        ["laugh"] = "\ud83d\ude04",
        ["hooray"] = "\ud83c\udf89",
        ["confused"] = "\ud83d\ude15",
        ["rocket"] = "\ud83d\ude80",
        ["eyes"] = "\ud83d\udc40"
    };
}
