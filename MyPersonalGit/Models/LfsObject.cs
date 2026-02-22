namespace MyPersonalGit.Models;

public class LfsObject
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string Oid { get; set; }
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
