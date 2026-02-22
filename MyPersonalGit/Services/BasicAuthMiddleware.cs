using System.Security.Claims;
using System.Text;
using MyPersonalGit.Data;

namespace MyPersonalGit.Services;

/// <summary>
/// Very small Basic Auth middleware intended for LAN/self-hosted use.
/// Reads users from configuration (Git:Users section).
///
/// Set credentials via environment variables (recommended):
///   Git__Users__yourname=yourpassword
///
/// Or via docker run:
///   docker run -e Git__Users__fennell=secret ...
///
/// IMPORTANT:
/// - Never put real passwords in appsettings.json — use env vars or user-secrets.
/// - For anything internet-exposed, put this app behind a reverse proxy (Nginx Proxy Manager, Traefik)
///   and use HTTPS + stronger auth (SSO, OAuth, etc.).
/// - This middleware is scoped to /git/* so you can keep the UI open if you want.
/// </summary>
public sealed class BasicAuthMiddleware
{
    private readonly RequestDelegate _next;

    public BasicAuthMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IConfiguration config, IRepositoryService repoService)
    {
        if (!context.Request.Path.StartsWithSegments("/git"))
        {
            await _next(context);
            return;
        }

        var requireAuth = config.GetValue("Git:RequireAuth", true);
        if (!requireAuth)
        {
            await _next(context);
            return;
        }

        // Extract repo name from path: /git/{repoName}.git/...
        var repoName = ExtractRepoName(context.Request.Path);
        var isReadOperation = IsReadOperation(context.Request);

        // For public repos, allow unauthenticated read (clone/fetch)
        if (isReadOperation && !string.IsNullOrEmpty(repoName))
        {
            var repoMeta = await repoService.GetRepositoryAsync(repoName);
            if (repoMeta == null || !repoMeta.IsPrivate)
            {
                // Public repo — allow anonymous read
                await _next(context);
                return;
            }
        }

        // Private repos and push operations always require auth
        var header = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            Challenge(context);
            return;
        }

        string decoded;
        try
        {
            var b64 = header.Substring("Basic ".Length).Trim();
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        }
        catch
        {
            Challenge(context);
            return;
        }

        // decoded = "username:password"
        var idx = decoded.IndexOf(':');
        if (idx <= 0)
        {
            Challenge(context);
            return;
        }

        var user = decoded.Substring(0, idx);
        var pass = decoded.Substring(idx + 1);

        // Load config users
        var usersSection = config.GetSection("Git:Users");
        var expected = usersSection[user];

        if (string.IsNullOrEmpty(expected) || !FixedTimeEquals(expected, pass))
        {
            Challenge(context);
            return;
        }

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, user),
            new Claim(ClaimTypes.AuthenticationMethod, "Basic")
        }, "Basic");

        context.User = new ClaimsPrincipal(identity);

        await _next(context);
    }

    private static string? ExtractRepoName(PathString path)
    {
        // Path format: /git/{repoName}.git/info/refs etc.
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments == null || segments.Length < 2) return null;
        var repoSegment = segments[1]; // e.g., "myrepo.git"
        return repoSegment; // Keep the .git suffix for DB lookup
    }

    private static bool IsReadOperation(HttpRequest request)
    {
        // git-upload-pack = fetch/clone (read), git-receive-pack = push (write)
        var path = request.Path.Value ?? "";
        var query = request.Query["service"].ToString();

        if (path.Contains("git-receive-pack") || query == "git-receive-pack")
            return false;

        // Everything else (info/refs with upload-pack, git-upload-pack) is a read
        return true;
    }

    private static void Challenge(HttpContext context)
    {
        context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"PersonalGit\"";
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        // Prevent trivial timing attacks.
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}

public static class BasicAuthExtensions
{
    public static IApplicationBuilder UseBasicAuthForGit(this IApplicationBuilder app)
        => app.UseMiddleware<BasicAuthMiddleware>();
}
