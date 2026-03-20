namespace MyPersonalGit.Services;

/// <summary>
/// Adds X-RateLimit-* response headers to API requests so clients
/// can track their remaining quota without hitting the limit.
/// </summary>
public sealed class RateLimitHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimitHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // Only add headers to API responses
        if (!context.Request.Path.StartsWithSegments("/api"))
            return;

        // Fixed window: 100 requests per minute
        const int limit = 100;
        var resetEpoch = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds();

        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = resetEpoch.ToString();

        // If we got a 429, remaining is 0
        if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
        {
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            context.Response.Headers["Retry-After"] = "60";
        }
    }
}

public static class RateLimitHeadersExtensions
{
    public static IApplicationBuilder UseRateLimitHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<RateLimitHeadersMiddleware>();
}
