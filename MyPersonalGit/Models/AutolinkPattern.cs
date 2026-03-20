namespace MyPersonalGit.Models;

public class AutolinkPattern
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string Prefix { get; set; }
    public required string UrlTemplate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
