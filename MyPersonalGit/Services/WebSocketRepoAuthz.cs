using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Services;

/// <summary>
/// Shared repository authorization for the Web IDE WebSocket handlers
/// (terminal, task runner, LSP, DAP). These handlers spawn processes and serve
/// repository contents, but previously only verified that a session existed —
/// any authenticated user could open a shell in, or read the source of, any
/// repository (including private ones they had no access to).
///
/// This mirrors <c>WebIdeController</c>'s owner / collaborator / private-repo
/// gating. Requiring the repo to resolve via <see cref="IRepositoryService"/>
/// also closes the path-traversal hole, since a "<c>..</c>"-laden repoName
/// won't match a real repository.
/// </summary>
internal static class WebSocketRepoAuthz
{
    /// <summary>
    /// Returns <c>true</c> if <paramref name="user"/> may access
    /// <paramref name="repoName"/>. On failure, writes the status code/body and
    /// returns <c>false</c> (the caller should then return without accepting the
    /// socket).
    /// </summary>
    /// <param name="requireWrite">
    /// <c>true</c> for command-executing handlers (terminal, task runner): demands
    /// ownership or Write collaborator permission. <c>false</c> for source-serving
    /// handlers (LSP, DAP): public repo, ownership, or Read collaborator suffices.
    /// </param>
    public static async Task<bool> AuthorizeAsync(HttpContext context, User user, string repoName, bool requireWrite)
    {
        var repoService = context.RequestServices.GetRequiredService<IRepositoryService>();
        var repo = await repoService.GetRepositoryAsync(repoName);
        if (repo == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Repository not found.");
            return false;
        }

        // Admins bypass per-repo permission checks (consistent with BasicAuthMiddleware).
        if (user.IsAdmin)
            return true;

        var isOwner = repo.Owner.Equals(user.Username, StringComparison.OrdinalIgnoreCase);
        var collaboratorService = context.RequestServices.GetRequiredService<ICollaboratorService>();

        if (requireWrite)
        {
            if (isOwner || await collaboratorService.HasPermissionAsync(repoName, user.Username, CollaboratorPermission.Write))
                return true;

            // Don't disclose the existence of private repos to users without access.
            if (repo.IsPrivate)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("Repository not found.");
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("You do not have write access to this repository.");
            }
            return false;
        }

        // Read gate: anyone may read a public repo; private repos need owner or Read+ collaborator.
        if (!repo.IsPrivate || isOwner ||
            await collaboratorService.HasPermissionAsync(repoName, user.Username, CollaboratorPermission.Read))
            return true;

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("Repository not found.");
        return false;
    }
}
