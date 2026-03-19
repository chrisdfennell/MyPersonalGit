using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Services;

/// <summary>
/// SSPI (Windows Integrated Authentication) middleware.
/// Enables transparent Single Sign-On for users on a Windows domain via Negotiate/NTLM.
/// Only active when enabled in Admin > System Settings and running on Windows.
/// Falls through silently on non-Windows platforms or when SSPI is disabled.
/// </summary>
public sealed class SspiAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SspiAuthMiddleware> _logger;

    public SspiAuthMiddleware(RequestDelegate next, ILogger<SspiAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IDbContextFactory<AppDbContext> dbFactory, IAuthService authService)
    {
        // Only intercept the SSPI login endpoint
        if (!context.Request.Path.StartsWithSegments("/auth/sspi"))
        {
            await _next(context);
            return;
        }

        // SSPI only works on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            context.Response.StatusCode = 501;
            await context.Response.WriteAsync("SSPI authentication is only available on Windows");
            return;
        }

        // Check if SSPI is enabled in system settings
        using var db = dbFactory.CreateDbContext();
        var settings = await db.SystemSettings.FirstOrDefaultAsync();
        if (settings == null || !settings.SspiEnabled)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("SSPI authentication is not enabled");
            return;
        }

        // Check if the request has a Windows identity from IIS/HTTP.sys Negotiate auth
        var windowsIdentity = context.User.Identity as WindowsIdentity;
        if (windowsIdentity == null || !windowsIdentity.IsAuthenticated)
        {
            // Challenge the client with Negotiate
            context.Response.StatusCode = 401;
            context.Response.Headers.Append("WWW-Authenticate", "Negotiate");
            return;
        }

        var domainUser = windowsIdentity.Name; // DOMAIN\username
        var username = domainUser.Contains('\\')
            ? domainUser.Split('\\', 2)[1]
            : domainUser;

        _logger.LogInformation("SSPI authenticated: {DomainUser} -> {Username}", domainUser, username);

        // Find or create local user
        var user = await authService.GetUserByUsernameAsync(username);
        if (user == null)
        {
            // Auto-provision from Windows identity
            var email = $"{username}@{(domainUser.Contains('\\') ? domainUser.Split('\\')[0].ToLower() : "localhost")}";
            await authService.RegisterAsync(username, email, Guid.NewGuid().ToString());
            user = await authService.GetUserByUsernameAsync(username);
        }

        if (user == null || !user.IsActive)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Account is disabled");
            return;
        }

        // Create a session for the user
        var session = await authService.CreateSessionAsync(user);
        if (session == null)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Failed to create session");
            return;
        }

        // Return the session ID as JSON so the client-side JS can store it
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            sessionId = session.SessionId,
            username = user.Username,
            email = user.Email
        });
    }
}

public static class SspiAuthExtensions
{
    public static IApplicationBuilder UseSspiAuth(this IApplicationBuilder app)
        => app.UseMiddleware<SspiAuthMiddleware>();
}
