using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IOAuthService
{
    Task<List<OAuthProviderConfig>> GetAllProvidersAsync();
    Task<List<OAuthProviderConfig>> GetEnabledProvidersAsync();
    Task SaveProviderConfigAsync(OAuthProviderConfig config);
    string GetAuthorizationUrl(string providerName, string clientId, string redirectUri, string state);
    Task<string?> ExchangeCodeAsync(string providerName, string clientId, string clientSecret, string code, string redirectUri);
    Task<OAuthUserInfo?> GetUserInfoAsync(string providerName, string accessToken);
    Task<(User user, bool created)> FindOrCreateUserAsync(string provider, string providerUserId, string? email, string? name, string? avatarUrl, string? providerUsername);
    Task<List<ExternalLogin>> GetExternalLoginsAsync(int userId);
    Task LinkExternalLoginAsync(int userId, string provider, string providerUserId, string? email, string? providerUsername, string? accessToken);
    Task<bool> UnlinkExternalLoginAsync(int userId, string provider);
    Task<ExternalLogin?> FindExternalLoginAsync(string provider, string providerUserId);
}

public class OAuthUserInfo
{
    public string? Id { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Username { get; set; }
    public string? AvatarUrl { get; set; }
}

public class OAuthService : IOAuthService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OAuthService> _logger;

    // Well-known OAuth2 endpoints for each provider
    private static readonly Dictionary<string, OAuthEndpoints> ProviderEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["github"] = new(
            "https://github.com/login/oauth/authorize",
            "https://github.com/login/oauth/access_token",
            "https://api.github.com/user",
            "read:user user:email"),
        ["google"] = new(
            "https://accounts.google.com/o/oauth2/v2/auth",
            "https://oauth2.googleapis.com/token",
            "https://www.googleapis.com/oauth2/v2/userinfo",
            "openid email profile"),
        ["microsoft"] = new(
            "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
            "https://login.microsoftonline.com/common/oauth2/v2.0/token",
            "https://graph.microsoft.com/v1.0/me",
            "openid email profile User.Read"),
        ["gitlab"] = new(
            "https://gitlab.com/oauth/authorize",
            "https://gitlab.com/oauth/token",
            "https://gitlab.com/api/v4/user",
            "read_user"),
        ["bitbucket"] = new(
            "https://bitbucket.org/site/oauth2/authorize",
            "https://bitbucket.org/site/oauth2/access_token",
            "https://api.bitbucket.org/2.0/user",
            "account"),
        ["facebook"] = new(
            "https://www.facebook.com/v18.0/dialog/oauth",
            "https://graph.facebook.com/v18.0/oauth/access_token",
            "https://graph.facebook.com/v18.0/me?fields=id,name,email,picture",
            "email public_profile"),
        ["discord"] = new(
            "https://discord.com/api/oauth2/authorize",
            "https://discord.com/api/oauth2/token",
            "https://discord.com/api/users/@me",
            "identify email"),
        ["twitter"] = new(
            "https://twitter.com/i/oauth2/authorize",
            "https://api.twitter.com/2/oauth2/token",
            "https://api.twitter.com/2/users/me?user.fields=profile_image_url",
            "users.read tweet.read offline.access")
    };

    public OAuthService(IDbContextFactory<AppDbContext> dbFactory, IHttpClientFactory httpClientFactory, ILogger<OAuthService> logger)
    {
        _dbFactory = dbFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<OAuthProviderConfig>> GetAllProvidersAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        var saved = await db.OAuthProviderConfigs.ToListAsync();

        // Ensure all known providers exist in the DB
        foreach (var providerName in ProviderEndpoints.Keys)
        {
            if (!saved.Any(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase)))
            {
                var config = new OAuthProviderConfig
                {
                    ProviderName = providerName,
                    DisplayName = GetDisplayName(providerName)
                };
                db.OAuthProviderConfigs.Add(config);
                saved.Add(config);
            }
        }

        await db.SaveChangesAsync();
        return saved.OrderBy(p => p.ProviderName).ToList();
    }

    public async Task<List<OAuthProviderConfig>> GetEnabledProvidersAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.OAuthProviderConfigs
            .Where(p => p.IsEnabled && p.ClientId != "" && p.ClientSecret != "")
            .OrderBy(p => p.ProviderName)
            .ToListAsync();
    }

    public async Task SaveProviderConfigAsync(OAuthProviderConfig config)
    {
        using var db = _dbFactory.CreateDbContext();
        var existing = await db.OAuthProviderConfigs
            .FirstOrDefaultAsync(p => p.ProviderName == config.ProviderName);

        if (existing != null)
        {
            existing.ClientId = config.ClientId;
            existing.ClientSecret = config.ClientSecret;
            existing.DisplayName = config.DisplayName;
            existing.IsEnabled = !string.IsNullOrWhiteSpace(config.ClientId) && !string.IsNullOrWhiteSpace(config.ClientSecret) && config.IsEnabled;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            config.IsEnabled = !string.IsNullOrWhiteSpace(config.ClientId) && !string.IsNullOrWhiteSpace(config.ClientSecret) && config.IsEnabled;
            config.UpdatedAt = DateTime.UtcNow;
            db.OAuthProviderConfigs.Add(config);
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("OAuth provider config saved for {Provider}", config.ProviderName);
    }

    public string GetAuthorizationUrl(string providerName, string clientId, string redirectUri, string state)
    {
        if (!ProviderEndpoints.TryGetValue(providerName, out var endpoints))
            throw new ArgumentException($"Unknown OAuth provider: {providerName}");

        var url = $"{endpoints.AuthorizeUrl}?client_id={Uri.EscapeDataString(clientId)}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&response_type=code" +
                  $"&scope={Uri.EscapeDataString(endpoints.Scopes)}" +
                  $"&state={Uri.EscapeDataString(state)}";

        // Twitter uses PKCE — for now we use a basic flow
        if (providerName.Equals("twitter", StringComparison.OrdinalIgnoreCase))
            url += "&code_challenge=challenge&code_challenge_method=plain";

        return url;
    }

    public async Task<string?> ExchangeCodeAsync(string providerName, string clientId, string clientSecret, string code, string redirectUri)
    {
        if (!ProviderEndpoints.TryGetValue(providerName, out var endpoints))
            return null;

        using var client = _httpClientFactory.CreateClient();

        HttpResponseMessage response;

        if (providerName.Equals("github", StringComparison.OrdinalIgnoreCase))
        {
            // GitHub expects JSON response
            var request = new HttpRequestMessage(HttpMethod.Post, endpoints.TokenUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            });
            response = await client.SendAsync(request);
        }
        else if (providerName.Equals("bitbucket", StringComparison.OrdinalIgnoreCase))
        {
            // Bitbucket uses Basic Auth for token exchange
            var request = new HttpRequestMessage(HttpMethod.Post, endpoints.TokenUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            });
            response = await client.SendAsync(request);
        }
        else if (providerName.Equals("twitter", StringComparison.OrdinalIgnoreCase))
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoints.TokenUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["code_verifier"] = "challenge"
            });
            response = await client.SendAsync(request);
        }
        else
        {
            // Standard OAuth2 token exchange (Google, Microsoft, GitLab, Facebook, Discord)
            response = await client.PostAsync(endpoints.TokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            }));
        }

        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OAuth token exchange failed for {Provider}: {Body}", providerName, body);
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("access_token").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse token response from {Provider}: {Body}", providerName, body);
            return null;
        }
    }

    public async Task<OAuthUserInfo?> GetUserInfoAsync(string providerName, string accessToken)
    {
        if (!ProviderEndpoints.TryGetValue(providerName, out var endpoints))
            return null;

        using var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, endpoints.UserInfoUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("MyPersonalGit/1.0");

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OAuth user info request failed for {Provider}: {Status}", providerName, response.StatusCode);
            return null;
        }

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        return providerName.ToLowerInvariant() switch
        {
            "github" => new OAuthUserInfo
            {
                Id = root.GetProperty("id").GetRawText(),
                Email = root.TryGetProperty("email", out var ghEmail) ? ghEmail.GetString() : await GetGitHubPrimaryEmail(client, accessToken),
                Name = root.TryGetProperty("name", out var ghName) ? ghName.GetString() : null,
                Username = root.TryGetProperty("login", out var ghLogin) ? ghLogin.GetString() : null,
                AvatarUrl = root.TryGetProperty("avatar_url", out var ghAvatar) ? ghAvatar.GetString() : null
            },
            "google" => new OAuthUserInfo
            {
                Id = root.TryGetProperty("id", out var gId) ? gId.GetString() : null,
                Email = root.TryGetProperty("email", out var gEmail) ? gEmail.GetString() : null,
                Name = root.TryGetProperty("name", out var gName) ? gName.GetString() : null,
                Username = root.TryGetProperty("email", out var gUser) ? gUser.GetString()?.Split('@')[0] : null,
                AvatarUrl = root.TryGetProperty("picture", out var gPic) ? gPic.GetString() : null
            },
            "microsoft" => new OAuthUserInfo
            {
                Id = root.TryGetProperty("id", out var msId) ? msId.GetString() : null,
                Email = root.TryGetProperty("mail", out var msMail) ? msMail.GetString() :
                        root.TryGetProperty("userPrincipalName", out var msUpn) ? msUpn.GetString() : null,
                Name = root.TryGetProperty("displayName", out var msName) ? msName.GetString() : null,
                Username = root.TryGetProperty("userPrincipalName", out var msUser) ? msUser.GetString()?.Split('@')[0] : null
            },
            "gitlab" => new OAuthUserInfo
            {
                Id = root.GetProperty("id").GetRawText(),
                Email = root.TryGetProperty("email", out var glEmail) ? glEmail.GetString() : null,
                Name = root.TryGetProperty("name", out var glName) ? glName.GetString() : null,
                Username = root.TryGetProperty("username", out var glUser) ? glUser.GetString() : null,
                AvatarUrl = root.TryGetProperty("avatar_url", out var glAvatar) ? glAvatar.GetString() : null
            },
            "bitbucket" => new OAuthUserInfo
            {
                Id = root.TryGetProperty("uuid", out var bbId) ? bbId.GetString() : null,
                Email = root.TryGetProperty("email", out var bbEmail) ? bbEmail.GetString() : null,
                Name = root.TryGetProperty("display_name", out var bbName) ? bbName.GetString() : null,
                Username = root.TryGetProperty("username", out var bbUser) ? bbUser.GetString() : null,
                AvatarUrl = root.TryGetProperty("links", out var bbLinks) && bbLinks.TryGetProperty("avatar", out var bbAvLink) && bbAvLink.TryGetProperty("href", out var bbHref) ? bbHref.GetString() : null
            },
            "facebook" => new OAuthUserInfo
            {
                Id = root.TryGetProperty("id", out var fbId) ? fbId.GetString() : null,
                Email = root.TryGetProperty("email", out var fbEmail) ? fbEmail.GetString() : null,
                Name = root.TryGetProperty("name", out var fbName) ? fbName.GetString() : null,
                Username = root.TryGetProperty("id", out var fbUser) ? fbUser.GetString() : null,
                AvatarUrl = root.TryGetProperty("picture", out var fbPic) && fbPic.TryGetProperty("data", out var fbData) && fbData.TryGetProperty("url", out var fbUrl) ? fbUrl.GetString() : null
            },
            "discord" => new OAuthUserInfo
            {
                Id = root.TryGetProperty("id", out var dcId) ? dcId.GetString() : null,
                Email = root.TryGetProperty("email", out var dcEmail) ? dcEmail.GetString() : null,
                Name = root.TryGetProperty("global_name", out var dcName) ? dcName.GetString() : null,
                Username = root.TryGetProperty("username", out var dcUser) ? dcUser.GetString() : null,
                AvatarUrl = root.TryGetProperty("avatar", out var dcAvatar) && dcAvatar.GetString() != null
                    ? $"https://cdn.discordapp.com/avatars/{root.GetProperty("id").GetString()}/{dcAvatar.GetString()}.png"
                    : null
            },
            "twitter" => new OAuthUserInfo
            {
                Id = root.TryGetProperty("data", out var twData) && twData.TryGetProperty("id", out var twId) ? twId.GetString() : null,
                Name = root.TryGetProperty("data", out var twD2) && twD2.TryGetProperty("name", out var twName) ? twName.GetString() : null,
                Username = root.TryGetProperty("data", out var twD3) && twD3.TryGetProperty("username", out var twUser) ? twUser.GetString() : null,
                AvatarUrl = root.TryGetProperty("data", out var twD4) && twD4.TryGetProperty("profile_image_url", out var twAvatar) ? twAvatar.GetString() : null
            },
            _ => null
        };
    }

    public async Task<(User user, bool created)> FindOrCreateUserAsync(string provider, string providerUserId, string? email, string? name, string? avatarUrl, string? providerUsername)
    {
        using var db = _dbFactory.CreateDbContext();

        // Check if there's already a linked external login
        var existingLogin = await db.ExternalLogins
            .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderUserId == providerUserId);

        if (existingLogin != null)
        {
            var existingUser = await db.Users.FindAsync(existingLogin.UserId);
            if (existingUser != null)
                return (existingUser, false);
        }

        // Try to find user by email
        User? user = null;
        if (!string.IsNullOrEmpty(email))
        {
            user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        bool created = false;
        if (user == null)
        {
            // Create a new user
            var username = await GenerateUniqueUsername(db, providerUsername ?? name ?? provider + "_user");

            user = new User
            {
                Username = username,
                Email = email ?? $"{username}@oauth.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), workFactor: 12),
                FullName = name,
                AvatarUrl = avatarUrl,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();
            created = true;
            _logger.LogInformation("Created new user {Username} via OAuth {Provider}", user.Username, provider);
        }

        // Link the external login
        if (existingLogin == null)
        {
            db.ExternalLogins.Add(new ExternalLogin
            {
                UserId = user.Id,
                Provider = provider,
                ProviderUserId = providerUserId,
                ProviderUsername = providerUsername,
                Email = email,
                LinkedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        return (user, created);
    }

    public async Task<List<ExternalLogin>> GetExternalLoginsAsync(int userId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.ExternalLogins
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.Provider)
            .ToListAsync();
    }

    public async Task LinkExternalLoginAsync(int userId, string provider, string providerUserId, string? email, string? providerUsername, string? accessToken)
    {
        using var db = _dbFactory.CreateDbContext();

        var existing = await db.ExternalLogins
            .FirstOrDefaultAsync(l => l.UserId == userId && l.Provider == provider);

        if (existing != null)
        {
            existing.ProviderUserId = providerUserId;
            existing.Email = email;
            existing.ProviderUsername = providerUsername;
            existing.AccessToken = accessToken;
        }
        else
        {
            db.ExternalLogins.Add(new ExternalLogin
            {
                UserId = userId,
                Provider = provider,
                ProviderUserId = providerUserId,
                ProviderUsername = providerUsername,
                Email = email,
                AccessToken = accessToken,
                LinkedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task<bool> UnlinkExternalLoginAsync(int userId, string provider)
    {
        using var db = _dbFactory.CreateDbContext();
        var login = await db.ExternalLogins
            .FirstOrDefaultAsync(l => l.UserId == userId && l.Provider == provider);

        if (login == null) return false;

        db.ExternalLogins.Remove(login);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<ExternalLogin?> FindExternalLoginAsync(string provider, string providerUserId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.ExternalLogins
            .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderUserId == providerUserId);
    }

    private static string GetDisplayName(string providerName) => providerName.ToLowerInvariant() switch
    {
        "github" => "GitHub",
        "google" => "Google",
        "microsoft" => "Microsoft",
        "gitlab" => "GitLab",
        "bitbucket" => "Bitbucket",
        "facebook" => "Facebook",
        "discord" => "Discord",
        "twitter" => "X (Twitter)",
        _ => providerName
    };

    public static string GetProviderIcon(string providerName) => providerName.ToLowerInvariant() switch
    {
        "github" => "bi-github",
        "google" => "bi-google",
        "microsoft" => "bi-microsoft",
        "gitlab" => "bi-gitlab",
        "bitbucket" => "bi-bucket",
        "facebook" => "bi-facebook",
        "discord" => "bi-discord",
        "twitter" => "bi-twitter-x",
        _ => "bi-globe"
    };

    public static string GetProviderColor(string providerName) => providerName.ToLowerInvariant() switch
    {
        "github" => "#24292e",
        "google" => "#4285F4",
        "microsoft" => "#00a4ef",
        "gitlab" => "#FC6D26",
        "bitbucket" => "#0052CC",
        "facebook" => "#1877F2",
        "discord" => "#5865F2",
        "twitter" => "#000000",
        _ => "#6c757d"
    };

    private async Task<string?> GetGitHubPrimaryEmail(HttpClient client, string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.UserAgent.ParseAdd("MyPersonalGit/1.0");

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);

            foreach (var email in doc.RootElement.EnumerateArray())
            {
                if (email.TryGetProperty("primary", out var primary) && primary.GetBoolean())
                    return email.GetProperty("email").GetString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch GitHub primary email");
        }
        return null;
    }

    private static async Task<string> GenerateUniqueUsername(AppDbContext db, string baseUsername)
    {
        // Clean the username to only allow valid characters
        var clean = new string(baseUsername.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
        if (string.IsNullOrEmpty(clean)) clean = "user";
        if (clean.Length > 30) clean = clean[..30];

        var candidate = clean;
        var counter = 1;

        while (await db.Users.AnyAsync(u => u.Username.ToLower() == candidate.ToLower()))
        {
            candidate = $"{clean}{counter}";
            counter++;
        }

        return candidate;
    }

    private record OAuthEndpoints(string AuthorizeUrl, string TokenUrl, string UserInfoUrl, string Scopes);
}
