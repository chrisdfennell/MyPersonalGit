namespace MyPersonalGit.Models;

public class OAuthProviderConfig
{
    public int Id { get; set; }
    public required string ProviderName { get; set; }
    public string? DisplayName { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ExternalLogin
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Provider { get; set; }
    public required string ProviderUserId { get; set; }
    public string? ProviderUsername { get; set; }
    public string? Email { get; set; }
    public string? AccessToken { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}
