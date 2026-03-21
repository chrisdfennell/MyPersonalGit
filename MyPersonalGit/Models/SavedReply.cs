namespace MyPersonalGit.Models;

public class SavedReply
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public DateTime CreatedAt { get; set; }
}
