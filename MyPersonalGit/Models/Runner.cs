namespace MyPersonalGit.Models;

public class Runner
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Token { get; set; }
    public string[] Labels { get; set; } = Array.Empty<string>();
    public bool IsOnline { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
