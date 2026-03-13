namespace MyPersonalGit.Models;

public class DeployKey
{
    public int Id { get; set; }
    public int RepositoryId { get; set; }
    public Repository Repository { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string KeyFingerprint { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public bool ReadOnly { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}
