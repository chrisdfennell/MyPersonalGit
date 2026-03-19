using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Controllers;

/// <summary>
/// OAuth2 Authorization Server endpoints — allows external apps to use
/// "Sign in with MyPersonalGit" for authentication.
/// Implements Authorization Code flow with optional PKCE.
/// </summary>
[ApiController]
[Route("oauth2")]
public class OAuth2ProviderController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAuthService _authService;
    private readonly ILogger<OAuth2ProviderController> _logger;

    public OAuth2ProviderController(IDbContextFactory<AppDbContext> dbFactory, IAuthService authService,
        ILogger<OAuth2ProviderController> logger)
    {
        _dbFactory = dbFactory;
        _authService = authService;
        _logger = logger;
    }

    // GET /oauth2/authorize — Authorization endpoint (user-facing)
    // Redirects user to login, then issues an authorization code
    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize(
        [FromQuery] string response_type,
        [FromQuery] string client_id,
        [FromQuery] string redirect_uri,
        [FromQuery] string? scope,
        [FromQuery] string? state,
        [FromQuery] string? code_challenge,
        [FromQuery] string? code_challenge_method)
    {
        if (response_type != "code")
            return BadRequest(new { error = "unsupported_response_type" });

        using var db = _dbFactory.CreateDbContext();
        var app = await db.OAuth2Apps.FirstOrDefaultAsync(a => a.ClientId == client_id);
        if (app == null)
            return BadRequest(new { error = "invalid_client", error_description = "Unknown client_id" });

        if (!app.RedirectUri.Equals(redirect_uri, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "invalid_request", error_description = "redirect_uri mismatch" });

        // Check if user is logged in via session cookie
        var sessionId = Request.Cookies["oauth2_session"];
        if (string.IsNullOrEmpty(sessionId))
        {
            // Redirect to login page with return URL
            var returnUrl = $"/oauth2/authorize?response_type={response_type}&client_id={client_id}" +
                            $"&redirect_uri={Uri.EscapeDataString(redirect_uri)}" +
                            (scope != null ? $"&scope={Uri.EscapeDataString(scope)}" : "") +
                            (state != null ? $"&state={Uri.EscapeDataString(state)}" : "") +
                            (code_challenge != null ? $"&code_challenge={Uri.EscapeDataString(code_challenge)}" : "") +
                            (code_challenge_method != null ? $"&code_challenge_method={code_challenge_method}" : "");
            return Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        var user = await _authService.GetUserBySessionAsync(sessionId);
        if (user == null)
            return Redirect($"/login?returnUrl={Uri.EscapeDataString(Request.Path + Request.QueryString)}");

        // Issue authorization code
        var code = GenerateSecureToken(32);
        db.OAuth2AuthCodes.Add(new OAuth2AuthCode
        {
            Code = code,
            ClientId = client_id,
            Username = user.Username,
            RedirectUri = redirect_uri,
            Scope = scope,
            CodeChallenge = code_challenge,
            CodeChallengeMethod = code_challenge_method,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });
        await db.SaveChangesAsync();

        var redirectUrl = $"{redirect_uri}?code={Uri.EscapeDataString(code)}";
        if (!string.IsNullOrEmpty(state))
            redirectUrl += $"&state={Uri.EscapeDataString(state)}";

        _logger.LogInformation("OAuth2 auth code issued for user {User} to app {App}", user.Username, app.Name);
        return Redirect(redirectUrl);
    }

    // POST /oauth2/token — Token endpoint
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token([FromForm] string grant_type,
        [FromForm] string? code,
        [FromForm] string? redirect_uri,
        [FromForm] string? client_id,
        [FromForm] string? client_secret,
        [FromForm] string? code_verifier,
        [FromForm] string? refresh_token)
    {
        using var db = _dbFactory.CreateDbContext();

        if (grant_type == "authorization_code")
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest(new { error = "invalid_request", error_description = "Missing code" });

            var authCode = await db.OAuth2AuthCodes.FirstOrDefaultAsync(c => c.Code == code && !c.Used);
            if (authCode == null || authCode.ExpiresAt < DateTime.UtcNow)
                return BadRequest(new { error = "invalid_grant", error_description = "Invalid or expired code" });

            // Validate client
            var resolvedClientId = client_id ?? authCode.ClientId;
            var app = await db.OAuth2Apps.FirstOrDefaultAsync(a => a.ClientId == resolvedClientId);
            if (app == null)
                return BadRequest(new { error = "invalid_client" });

            if (app.IsConfidential && app.ClientSecret != client_secret)
                return Unauthorized(new { error = "invalid_client", error_description = "Invalid client_secret" });

            // Validate PKCE if code_challenge was provided
            if (!string.IsNullOrEmpty(authCode.CodeChallenge))
            {
                if (string.IsNullOrEmpty(code_verifier))
                    return BadRequest(new { error = "invalid_request", error_description = "Missing code_verifier" });

                var expectedChallenge = authCode.CodeChallengeMethod == "S256"
                    ? Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(code_verifier)))
                    : code_verifier; // plain

                if (expectedChallenge != authCode.CodeChallenge)
                    return BadRequest(new { error = "invalid_grant", error_description = "PKCE verification failed" });
            }

            if (!string.IsNullOrEmpty(redirect_uri) &&
                !redirect_uri.Equals(authCode.RedirectUri, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "invalid_grant", error_description = "redirect_uri mismatch" });

            // Mark code as used
            authCode.Used = true;

            // Issue tokens
            var accessToken = GenerateSecureToken(32);
            var refreshTok = GenerateSecureToken(32);
            db.OAuth2Tokens.Add(new OAuth2Token
            {
                AccessToken = accessToken,
                RefreshToken = refreshTok,
                ClientId = authCode.ClientId,
                Username = authCode.Username,
                Scope = authCode.Scope,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });
            await db.SaveChangesAsync();

            _logger.LogInformation("OAuth2 access token issued for user {User}", authCode.Username);
            return Ok(new
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = 3600,
                refresh_token = refreshTok,
                scope = authCode.Scope ?? ""
            });
        }
        else if (grant_type == "refresh_token")
        {
            if (string.IsNullOrEmpty(refresh_token))
                return BadRequest(new { error = "invalid_request" });

            var existing = await db.OAuth2Tokens.FirstOrDefaultAsync(t => t.RefreshToken == refresh_token);
            if (existing == null)
                return BadRequest(new { error = "invalid_grant" });

            // Issue new access token
            var newAccessToken = GenerateSecureToken(32);
            var newRefreshToken = GenerateSecureToken(32);
            db.OAuth2Tokens.Add(new OAuth2Token
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ClientId = existing.ClientId,
                Username = existing.Username,
                Scope = existing.Scope,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });
            db.OAuth2Tokens.Remove(existing);
            await db.SaveChangesAsync();

            return Ok(new
            {
                access_token = newAccessToken,
                token_type = "Bearer",
                expires_in = 3600,
                refresh_token = newRefreshToken,
                scope = existing.Scope ?? ""
            });
        }

        return BadRequest(new { error = "unsupported_grant_type" });
    }

    // GET /oauth2/userinfo — OpenID Connect-style user info endpoint
    [HttpGet("userinfo")]
    public async Task<IActionResult> UserInfo()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return Unauthorized(new { error = "invalid_token" });

        var token = authHeader["Bearer ".Length..];
        using var db = _dbFactory.CreateDbContext();
        var oauth2Token = await db.OAuth2Tokens.FirstOrDefaultAsync(t => t.AccessToken == token);
        if (oauth2Token == null || oauth2Token.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { error = "invalid_token" });

        var user = await _authService.GetUserByUsernameAsync(oauth2Token.Username);
        if (user == null)
            return Unauthorized(new { error = "invalid_token" });

        return Ok(new
        {
            sub = user.Username,
            preferred_username = user.Username,
            name = user.FullName ?? user.Username,
            email = user.Email,
            picture = user.AvatarUrl ?? "",
            updated_at = user.LastLoginAt?.ToUniversalTime().ToString("O") ?? ""
        });
    }

    // GET /oauth2/.well-known/openid-configuration — Discovery document
    [HttpGet(".well-known/openid-configuration")]
    public IActionResult Discovery()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(new
        {
            issuer = baseUrl,
            authorization_endpoint = $"{baseUrl}/oauth2/authorize",
            token_endpoint = $"{baseUrl}/oauth2/token",
            userinfo_endpoint = $"{baseUrl}/oauth2/userinfo",
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code", "refresh_token" },
            subject_types_supported = new[] { "public" },
            scopes_supported = new[] { "openid", "profile", "email" },
            token_endpoint_auth_methods_supported = new[] { "client_secret_post", "none" },
            code_challenge_methods_supported = new[] { "S256", "plain" }
        });
    }

    private static string GenerateSecureToken(int byteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
