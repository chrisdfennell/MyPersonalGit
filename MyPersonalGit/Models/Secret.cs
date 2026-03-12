namespace MyPersonalGit.Models;

public class RepositorySecret
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string Name { get; set; }
    public required string EncryptedValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
