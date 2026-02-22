using LibGit2Sharp;
using Microsoft.AspNetCore.StaticFiles;
using MyPersonalGit.Data;

namespace MyPersonalGit.Services;

public sealed class PagesMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PagesMiddleware> _logger;

    public PagesMiddleware(RequestDelegate next, ILogger<PagesMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration config,
        IRepositoryService repoService, IAdminService adminService)
    {
        if (!context.Request.Path.StartsWithSegments("/pages", out var remaining))
        {
            await _next(context);
            return;
        }

        // Parse: /pages/{owner}/{repoName}/path/to/file
        var segments = remaining.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments == null || segments.Length < 2)
        {
            context.Response.StatusCode = 404;
            return;
        }

        var owner = segments[0];
        var repoName = segments[1];
        var filePath = segments.Length > 2
            ? string.Join("/", segments.Skip(2))
            : "index.html";

        if (string.IsNullOrEmpty(filePath) || filePath.EndsWith("/"))
            filePath += "index.html";

        var systemSettings = await adminService.GetSystemSettingsAsync();
        var projectRoot = !string.IsNullOrEmpty(systemSettings.ProjectRoot)
            ? systemSettings.ProjectRoot
            : config["Git:ProjectRoot"] ?? "/repos";

        var repoPath = ResolveRepoPath(projectRoot, repoName);
        if (repoPath == null)
        {
            context.Response.StatusCode = 404;
            return;
        }

        var dirName = Path.GetFileName(repoPath);
        var meta = await repoService.GetRepositoryAsync(dirName);
        if (meta == null || !meta.HasPages)
        {
            context.Response.StatusCode = 404;
            return;
        }

        // Only serve public repos via Pages
        if (meta.IsPrivate)
        {
            context.Response.StatusCode = 404;
            return;
        }

        var pagesBranch = string.IsNullOrEmpty(meta.PagesBranch) ? "gh-pages" : meta.PagesBranch;

        try
        {
            using var repo = new Repository(repoPath);
            var branch = repo.Branches[pagesBranch];
            if (branch?.Tip == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            var entry = branch.Tip[filePath];

            // If entry is a directory, try index.html inside it
            if (entry != null && entry.TargetType == TreeEntryTargetType.Tree)
                entry = branch.Tip[filePath.TrimEnd('/') + "/index.html"];

            if (entry == null || entry.TargetType != TreeEntryTargetType.Blob)
            {
                context.Response.StatusCode = 404;
                return;
            }

            var blob = (Blob)entry.Target;

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out var contentType))
                contentType = "application/octet-stream";

            if (contentType == "image/svg+xml")
                context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'";

            var ms = new MemoryStream();
            using (var blobStream = blob.GetContentStream())
            {
                await blobStream.CopyToAsync(ms);
            }
            ms.Position = 0;

            context.Response.ContentType = contentType;
            context.Response.StatusCode = 200;
            await ms.CopyToAsync(context.Response.Body);
        }
        catch
        {
            context.Response.StatusCode = 404;
        }
    }

    private static string? ResolveRepoPath(string projectRoot, string repoName)
    {
        var path = Path.Combine(projectRoot, repoName);
        if (Repository.IsValid(path)) return path;
        if (Repository.IsValid(path + ".git")) return path + ".git";
        var nested = Path.Combine(path, repoName + ".git");
        if (Repository.IsValid(nested)) return nested;
        return null;
    }
}

public static class PagesMiddlewareExtensions
{
    public static IApplicationBuilder UsePages(this IApplicationBuilder app)
        => app.UseMiddleware<PagesMiddleware>();
}
