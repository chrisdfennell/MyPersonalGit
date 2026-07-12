using System.Security.Claims;
using MyPersonalGit.Data;

namespace MyPersonalGit.Services;

public sealed class RegistryAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RegistryAuthMiddleware> _logger;

    public RegistryAuthMiddleware(RequestDelegate next, ILogger<RegistryAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IPatTokenService patTokens)
    {
        if (!context.Request.Path.StartsWithSegments("/v2"))
        {
            await _next(context);
            return;
        }

        // /v2/ version check — try to auth but don't require it
        if (context.Request.Path.Value is "/v2/" or "/v2")
        {
            await TryAuthenticateAsync(context, patTokens);
            await _next(context);
            return;
        }

        var isWriteOp = context.Request.Method is "PUT" or "POST" or "PATCH" or "DELETE";

        if (isWriteOp)
        {
            if (!await TryAuthenticateAsync(context, patTokens))
            {
                Challenge(context);
                return;
            }
        }
        else
        {
            // For reads, try to auth but allow anonymous (controller checks repo privacy)
            await TryAuthenticateAsync(context, patTokens);
        }

        await _next(context);
    }

    private static async Task<bool> TryAuthenticateAsync(HttpContext context, IPatTokenService patTokens)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;

        var token = authHeader["Bearer ".Length..].Trim();
        var matched = await patTokens.ValidateAsync(token);
        if (matched == null)
            return false;

        if (matched.ExpiresAt.HasValue && matched.ExpiresAt.Value < DateTime.UtcNow)
            return false;

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, matched.Username),
            new("token_id", matched.Id.ToString())
        };
        foreach (var scope in matched.Scopes ?? Array.Empty<string>())
            claims.Add(new Claim("scope", scope));

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "RegistryToken"));
        return true;
    }

    private static void Challenge(HttpContext context)
    {
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        context.Response.Headers["WWW-Authenticate"] = $"Bearer realm=\"{baseUrl}/v2/token\",service=\"mypersonalgit\"";
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
    }
}

public static class RegistryAuthExtensions
{
    public static IApplicationBuilder UseRegistryAuth(this IApplicationBuilder app)
        => app.UseMiddleware<RegistryAuthMiddleware>();
}
