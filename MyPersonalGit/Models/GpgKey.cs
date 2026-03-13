namespace MyPersonalGit.Models;

public class GpgKey
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string KeyId { get; set; } = "";           // Short key ID (last 8 hex chars)
    public string LongKeyId { get; set; } = "";       // Full key ID (16 hex chars)
    public string PublicKey { get; set; } = "";        // ASCII-armored public key
    public string PrimaryEmail { get; set; } = "";
    public List<string> Emails { get; set; } = new();  // All UIDs/emails
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsVerified { get; set; }
}
