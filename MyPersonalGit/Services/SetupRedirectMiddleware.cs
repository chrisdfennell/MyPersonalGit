using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Data;

namespace MyPersonalGit.Services;

/// <summary>
/// First-run gate: until an admin account exists, redirect browser page requests to the
/// /setup wizard. Once an admin exists the check is short-circuited for the process lifetime,
/// so there's no per-request DB hit in normal operation.
/// </summary>
public sealed class SetupRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private static volatile bool _setupComplete;

    public SetupRedirectMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IDbContextFactory<AppDbContext> dbFactory)
    {
        if (_setupComplete)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "/";

        // Let the setup page itself, framework/Blazor assets, and static files through.
        if (path.StartsWith("/setup", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_", StringComparison.OrdinalIgnoreCase) ||   // _blazor, _framework, _content
            path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
            path.Contains('.'))   // favicon.svg, app.css, *.js, etc.
        {
            await _next(context);
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Users.AnyAsync(u => u.IsAdmin))
        {
            _setupComplete = true;   // latch — never re-check once an admin exists
            await _next(context);
            return;
        }

        context.Response.Redirect("/setup");
    }
}

public static class SetupRedirectExtensions
{
    public static IApplicationBuilder UseSetupRedirect(this IApplicationBuilder app)
        => app.UseMiddleware<SetupRedirectMiddleware>();
}
