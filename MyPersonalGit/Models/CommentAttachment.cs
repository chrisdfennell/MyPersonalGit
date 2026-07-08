namespace MyPersonalGit.Models;

/// <summary>
/// A file (typically an image pasted or dropped into a comment box) uploaded by a user.
/// Served at /attachments/{uuid}/{filename} — the unguessable UUID acts as the access token,
/// the same model GitHub uses for user-images URLs.
/// </summary>
public class CommentAttachment
{
    public int Id { get; set; }
    public required string Uuid { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public required string UploadedBy { get; set; }
    public string? RepoName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
