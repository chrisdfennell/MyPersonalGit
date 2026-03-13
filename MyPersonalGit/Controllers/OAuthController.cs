using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[Route("oauth")]
public class OAuthController : Controller
{
    private readonly IOAuthService _oauthService;
    private readonly IAuthService _authService;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(IOAuthService oauthService, IAuthService authService, ILogger<OAuthController> logger)
    {
        _oauthService = oauthService;
        _authService = authService;
        _logger = logger;
    }

    [HttpGet("login/{provider}")]
    public async Task<IActionResult> Login(string provider)
    {
        var providers = await _oauthService.GetEnabledProvidersAsync();
        var config = providers.FirstOrDefault(p => p.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));

        if (config == null)
            return Redirect("/login?error=OAuth+provider+not+configured");

        var state = Guid.NewGuid().ToString();
        var redirectUri = $"{Request.Scheme}://{Request.Host}/oauth/callback/{provider}";

        // Store state in a secure cookie for CSRF protection
        Response.Cookies.Append($"oauth_state_{provider}", state, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10)
        });

        var authUrl = _oauthService.GetAuthorizationUrl(provider, config.ClientId, redirectUri, state);
        return Redirect(authUrl);
    }

    [HttpGet("callback/{provider}")]
    public async Task<IActionResult> Callback(string provider, [FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("OAuth callback error from {Provider}: {Error}", provider, error);
            return Redirect($"/login?error=OAuth+login+cancelled");
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Redirect("/login?error=Invalid+OAuth+response");

        // Validate state for CSRF protection
        var stateCookieName = $"oauth_state_{provider}";
        if (!Request.Cookies.TryGetValue(stateCookieName, out var savedState) || savedState != state)
        {
            _logger.LogWarning("OAuth state mismatch for {Provider}", provider);
            return Redirect("/login?error=OAuth+state+mismatch");
        }

        // Clear the state cookie
        Response.Cookies.Delete(stateCookieName);

        // Get provider config
        var providers = await _oauthService.GetEnabledProvidersAsync();
        var config = providers.FirstOrDefault(p => p.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));

        if (config == null)
            return Redirect("/login?error=OAuth+provider+not+configured");

        var redirectUri = $"{Request.Scheme}://{Request.Host}/oauth/callback/{provider}";

        // Exchange code for access token
        var accessToken = await _oauthService.ExchangeCodeAsync(provider, config.ClientId, config.ClientSecret, code, redirectUri);
        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("OAuth token exchange failed for {Provider}", provider);
            return Redirect("/login?error=OAuth+authentication+failed");
        }

        // Get user info from provider
        var userInfo = await _oauthService.GetUserInfoAsync(provider, accessToken);
        if (userInfo == null || string.IsNullOrEmpty(userInfo.Id))
        {
            _logger.LogWarning("OAuth user info retrieval failed for {Provider}", provider);
            return Redirect("/login?error=Could+not+retrieve+user+info");
        }

        // Find or create local user
        var (user, created) = await _oauthService.FindOrCreateUserAsync(
            provider, userInfo.Id, userInfo.Email, userInfo.Name, userInfo.AvatarUrl, userInfo.Username);

        // Store access token for linking
        await _oauthService.LinkExternalLoginAsync(user.Id, provider, userInfo.Id, userInfo.Email, userInfo.Username, accessToken);

        // Create session
        var session = await _authService.CreateSessionAsync(user);
        if (session == null)
            return Redirect("/login?error=Session+creation+failed");

        // Set session cookie (same way the Blazor app reads it via JS interop)
        // We write it as a cookie that the JS sessionHelper can read
        Response.Cookies.Append("oauth_session", session.SessionId, new CookieOptions
        {
            HttpOnly = false, // JS needs to read this
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(30),
            Path = "/"
        });

        _logger.LogInformation("User {Username} authenticated via OAuth {Provider} (created: {Created})", user.Username, provider, created);

        // Redirect to a page that transfers the session to localStorage and then redirects to home
        return Redirect("/oauth/complete");
    }

    [HttpGet("complete")]
    public IActionResult Complete()
    {
        // This page reads the oauth_session cookie, stores it in localStorage, and redirects to /
        return Content(@"<!DOCTYPE html>
<html>
<head><title>Completing sign in...</title></head>
<body>
<script>
    var cookies = document.cookie.split(';');
    for (var i = 0; i < cookies.length; i++) {
        var cookie = cookies[i].trim();
        if (cookie.startsWith('oauth_session=')) {
            var sessionId = cookie.substring('oauth_session='.length);
            localStorage.setItem('sessionId', sessionId);
            // Clear the cookie
            document.cookie = 'oauth_session=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT';
            break;
        }
    }
    window.location.href = '/';
</script>
<noscript><p>Sign in complete. <a href='/'>Click here</a> to continue.</p></noscript>
</body>
</html>", "text/html");
    }
}
