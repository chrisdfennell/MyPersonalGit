namespace MyPersonalGit.Models;

/// <summary>
/// An OAuth2 client application registered to use MyPersonalGit as an identity provider.
/// </summary>
public class OAuth2App
{
    public int Id { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string RedirectUri { get; set; }
    public required string Owner { get; set; }
    public bool IsConfidential { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A short-lived authorization code issued during the OAuth2 authorization code flow.
/// </summary>
public class OAuth2AuthCode
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string ClientId { get; set; }
    public required string Username { get; set; }
    public required string RedirectUri { get; set; }
    public string? Scope { get; set; }
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// An OAuth2 access/refresh token issued by the authorization server.
/// </summary>
public class OAuth2Token
{
    public int Id { get; set; }
    public required string AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public required string ClientId { get; set; }
    public required string Username { get; set; }
    public string? Scope { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
