using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Services;

public sealed class ApiAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiAuthMiddleware> _logger;

    public ApiAuthMiddleware(RequestDelegate next, ILogger<ApiAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration config, ICollaboratorService collaboratorService, IAuthService authService, IDbContextFactory<AppDbContext> dbFactory)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // Allow unauthenticated downloads of release assets
        if (context.Request.Method == "GET" && context.Request.Path.Value != null
            && context.Request.Path.Value.Contains("/releases/") && context.Request.Path.Value.Contains("/assets/"))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Missing or invalid Authorization header. Use: Bearer mypg_<token>" }));
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();
        var projectRoot = config["Git:ProjectRoot"] ?? "/repos";
        var dataPath = Path.Combine(projectRoot, ".mypersonalgit");

        // Search user token files for a match
        PersonalAccessToken? matchedToken = null;
        if (Directory.Exists(dataPath))
        {
        foreach (var file in Directory.GetFiles(dataPath, "*_tokens.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var tokens = JsonSerializer.Deserialize<List<PersonalAccessToken>>(json);
                if (tokens == null) continue;

                matchedToken = tokens.FirstOrDefault(t => t.Token == token);
                if (matchedToken != null) break;
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to read token file {File}", file); }
        }
        }

        // Fallback: check database for tokens
        if (matchedToken == null)
        {
            try
            {
                using var db = dbFactory.CreateDbContext();
                matchedToken = await db.PersonalAccessTokens.FirstOrDefaultAsync(t => t.Token == token);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to check database for token"); }
        }

        if (matchedToken == null)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Invalid token" }));
            return;
        }

        if (matchedToken.ExpiresAt.HasValue && matchedToken.ExpiresAt.Value < DateTime.UtcNow)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Token has expired" }));
            return;
        }

        // Enforce route-level restrictions if configured
        var allowedRoutes = matchedToken.AllowedRoutes ?? Array.Empty<string>();
        if (allowedRoutes.Length > 0)
        {
            var requestPath = context.Request.Path.Value ?? "/";
            var routeAllowed = allowedRoutes.Any(pattern => MatchRoute(requestPath, pattern));
            if (!routeAllowed)
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Token is not authorized for this route" }));
                return;
            }
        }

        // Set the user identity from the token
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, matchedToken.Username),
            new("token_id", matchedToken.Id.ToString())
        };

        foreach (var scope in matchedToken.Scopes ?? Array.Empty<string>())
        {
            claims.Add(new Claim("scope", scope));
        }

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiToken"));

        // Check repository permissions for repo-scoped API requests
        // Path format: /api/v1/repos/{repoName}/...
        if (context.Request.Path.StartsWithSegments("/api/v1/repos"))
        {
            var segments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments?.Length >= 4) // ["api", "v1", "repos", "{repoName}", ...]
            {
                var repoName = segments[3];
                var isWriteMethod = context.Request.Method != "GET" && context.Request.Method != "HEAD";
                var requiredPermission = isWriteMethod ? CollaboratorPermission.Write : CollaboratorPermission.Read;

                var dbUser = await authService.GetUserByUsernameAsync(matchedToken.Username);
                if (dbUser?.IsAdmin != true)
                {
                    var hasPermission = await collaboratorService.HasPermissionAsync(repoName, matchedToken.Username, requiredPermission);
                    if (!hasPermission && isWriteMethod)
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "You do not have write access to this repository" }));
                        return;
                    }
                }
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Match a request path against a glob-style route pattern.
    /// Supports ** (any path segments) and * (single segment).
    /// </summary>
    private static bool MatchRoute(string path, string pattern)
    {
        // Exact match
        if (path.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            return true;

        // Prefix match with ** wildcard (e.g. "/api/packages/**" matches "/api/packages/pypi/upload")
        if (pattern.EndsWith("/**"))
        {
            var prefix = pattern[..^3]; // remove /**
            return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        // Prefix match with trailing * (e.g. "/api/packages/*")
        if (pattern.EndsWith("/*"))
        {
            var prefix = pattern[..^2]; // remove /*
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;
            var remainder = path[prefix.Length..].TrimStart('/');
            return !remainder.Contains('/'); // single segment only
        }

        // StartsWith for patterns without wildcards but with trailing /
        if (pattern.EndsWith("/"))
            return path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase);

        return false;
    }
}

public static class ApiAuthExtensions
{
    public static IApplicationBuilder UseApiAuth(this IApplicationBuilder app)
        => app.UseMiddleware<ApiAuthMiddleware>();
}
