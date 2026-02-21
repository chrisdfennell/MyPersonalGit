using System.Security.Claims;
using System.Text.Json;
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

    public async Task InvokeAsync(HttpContext context, IConfiguration config)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
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

        // Search all user token files for a match
        if (!Directory.Exists(dataPath))
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Invalid token" }));
            return;
        }

        PersonalAccessToken? matchedToken = null;
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

        await _next(context);
    }
}

public static class ApiAuthExtensions
{
    public static IApplicationBuilder UseApiAuth(this IApplicationBuilder app)
        => app.UseMiddleware<ApiAuthMiddleware>();
}
