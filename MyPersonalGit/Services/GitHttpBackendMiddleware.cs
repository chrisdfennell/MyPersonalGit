using System.Diagnostics;
using System.Text;

namespace MyPersonalGit.Services;

/// <summary>
/// Minimal Git "Smart HTTP" server using the stock `git http-backend` CGI.
///
/// Why this approach?
/// - It supports clone/fetch/push exactly like GitHub (Smart HTTP protocol).
/// - It avoids re-implementing Git's protocol in C#.
/// - It works great on NAS boxes as long as `git` is installed.
///
/// Mount point:
///   /git/{repo}.git/...
///
/// Notes:
/// - This middleware is intentionally scoped to /git.
/// - Repo storage is configured with Git:ProjectRoot in appsettings.
/// - For security, you should enable auth on /git (Basic, or reverse-proxy auth).
/// </summary>
public sealed class GitHttpBackendMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GitHttpBackendMiddleware> _logger;

    public GitHttpBackendMiddleware(RequestDelegate next, ILogger<GitHttpBackendMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration config)
    {
        // Only handle /git/* paths; let everything else pass through.
        if (!context.Request.Path.StartsWithSegments("/git", out var remaining))
        {
            await _next(context);
            return;
        }

        // Example:
        //   Request.Path: /git/MyRepo.git/info/refs
        //   remaining:    /MyRepo.git/info/refs
        var projectRoot = config["Git:ProjectRoot"] ?? "/repos";
        var pathInfo = remaining.Value ?? "/";
        if (string.IsNullOrWhiteSpace(pathInfo))
            pathInfo = "/";

        // Basic hardening: reject path traversal.
        if (pathInfo.Contains("..", StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid path.");
            return;
        }

        // git http-backend needs CGI env vars.
        // Docs: https://git-scm.com/docs/git-http-backend
        var env = new Dictionary<string, string?>
        {
            ["GIT_PROJECT_ROOT"] = projectRoot,
            ["GIT_HTTP_EXPORT_ALL"] = "1",

            ["REQUEST_METHOD"] = context.Request.Method,
            ["PATH_INFO"] = pathInfo,
            ["QUERY_STRING"] = context.Request.QueryString.HasValue ? context.Request.QueryString.Value!.TrimStart('?') : "",
            ["CONTENT_TYPE"] = context.Request.ContentType ?? "",
            ["REMOTE_USER"] = context.User?.Identity?.IsAuthenticated == true ? context.User.Identity!.Name : "",
            // Optional, but helpful:
            ["REMOTE_ADDR"] = context.Connection.RemoteIpAddress?.ToString() ?? ""
        };

        if (context.Request.ContentLength.HasValue)
            env["CONTENT_LENGTH"] = context.Request.ContentLength.Value.ToString();

        // Start git http-backend
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "http-backend",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var kv in env)
        {
            if (!string.IsNullOrEmpty(kv.Value))
                psi.Environment[kv.Key] = kv.Value!;
            else
                psi.Environment[kv.Key] = "";
        }

        // Forward HTTP_* headers (Authorization is usually handled by ASP.NET auth, but safe to forward others).
        foreach (var header in context.Request.Headers)
        {
            var key = header.Key;
            if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                continue;

            var envKey = "HTTP_" + key.ToUpperInvariant().Replace('-', '_');
            // Some headers can appear multiple times; join with comma per CGI convention.
            psi.Environment[envKey] = string.Join(",", header.Value.ToArray());
        }

        using var process = Process.Start(psi);
        if (process is null)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Failed to start git backend.");
            return;
        }

        // Pipe request body -> git stdin (for push / RPC calls)
        if (context.Request.Body != Stream.Null)
        {
            try
            {
                await context.Request.Body.CopyToAsync(process.StandardInput.BaseStream, context.RequestAborted);
            }
            catch (OperationCanceledException) { /* request aborted */ }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to copy request body to git http-backend stdin.");
            }
        }
        try { process.StandardInput.Close(); } catch { }

        // git http-backend writes CGI headers then body on stdout.
        // We must parse headers safely.
        context.Response.Headers.Clear();
        context.Response.ContentType = null;

        // Read headers line-by-line until blank line.
        var headerBytes = new List<byte>();
        var stdout = process.StandardOutput.BaseStream;

        // Read until \r\n\r\n (CRLF CRLF) OR \n\n (some environments)
        byte[] buffer = new byte[1];
        int matched = 0;
        byte[] pattern1 = Encoding.ASCII.GetBytes("\r\n\r\n");
        byte[] pattern2 = Encoding.ASCII.GetBytes("\n\n");

        while (true)
        {
            int read = await stdout.ReadAsync(buffer, 0, 1, context.RequestAborted);
            if (read <= 0) break;

            headerBytes.Add(buffer[0]);

            // match \r\n\r\n
            if (buffer[0] == pattern1[matched])
            {
                matched++;
                if (matched == pattern1.Length) break;
            }
            else
            {
                matched = buffer[0] == pattern1[0] ? 1 : 0;
            }

            // also handle \n\n
            if (headerBytes.Count >= pattern2.Length &&
                headerBytes[^2] == pattern2[0] &&
                headerBytes[^1] == pattern2[1])
            {
                break;
            }

            // Hard cap to prevent runaway if backend misbehaves
            if (headerBytes.Count > 64 * 1024)
                break;
        }

        var headerText = Encoding.UTF8.GetString(headerBytes.ToArray());
        var headerLines = headerText
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        int statusCode = 200;

        foreach (var line in headerLines)
        {
            var idx = line.IndexOf(':');
            if (idx <= 0) continue;

            var name = line.Substring(0, idx).Trim();
            var value = line.Substring(idx + 1).Trim();

            if (name.Equals("Status", StringComparison.OrdinalIgnoreCase))
            {
                // e.g. "Status: 200 OK"
                var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && int.TryParse(parts[0], out var sc))
                    statusCode = sc;
                continue;
            }

            // Avoid overriding server-controlled headers.
            if (name.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                continue;

            context.Response.Headers[name] = value;
        }

        context.Response.StatusCode = statusCode;

        // Copy remaining stdout (body) to response.
        try
        {
            await stdout.CopyToAsync(context.Response.Body, context.RequestAborted);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stream git http-backend output to response.");
        }

        // Log stderr if non-empty (useful for debugging on NAS)
        var stderr = await process.StandardError.ReadToEndAsync();
        if (!string.IsNullOrWhiteSpace(stderr))
            _logger.LogWarning("git http-backend stderr: {stderr}", stderr);

        try { await process.WaitForExitAsync(context.RequestAborted); } catch { }
    }
}

public static class GitHttpBackendExtensions
{
    public static IApplicationBuilder UseGitHttpBackend(this IApplicationBuilder app)
        => app.UseMiddleware<GitHttpBackendMiddleware>();
}
