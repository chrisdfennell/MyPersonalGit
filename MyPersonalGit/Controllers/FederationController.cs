using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
public class FederationController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public FederationController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// NodeInfo discovery document (RFC: https://nodeinfo.diaspora.software/protocol)
    /// </summary>
    [HttpGet("/.well-known/nodeinfo")]
    public IActionResult NodeInfoDiscovery()
    {
        var host = $"{Request.Scheme}://{Request.Host}";
        return Ok(new
        {
            links = new[]
            {
                new
                {
                    rel = "http://nodeinfo.diaspora.software/ns/schema/2.0",
                    href = $"{host}/nodeinfo/2.0"
                }
            }
        });
    }

    /// <summary>
    /// NodeInfo 2.0 document with instance metadata.
    /// </summary>
    [HttpGet("/nodeinfo/2.0")]
    public async Task<IActionResult> NodeInfo()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var userCount = await db.Users.CountAsync();
        var activeMonthCount = await db.Users.CountAsync(u =>
            u.LastLoginAt != null && u.LastLoginAt > DateTime.UtcNow.AddDays(-30));
        var repoCount = await db.Repositories.CountAsync();
        var settings = await db.SystemSettings.FirstOrDefaultAsync();
        var openRegistrations = settings?.AllowUserRegistration ?? false;

        return Ok(new
        {
            version = "2.0",
            software = new { name = "mypersonalgit", version = "1.0.0" },
            protocols = new[] { "activitypub" },
            usage = new
            {
                users = new { total = userCount, activeMonth = activeMonthCount },
                localPosts = repoCount
            },
            openRegistrations
        });
    }

    /// <summary>
    /// WebFinger host-meta (XML) for discovery.
    /// </summary>
    [HttpGet("/.well-known/host-meta")]
    public IActionResult HostMeta()
    {
        var host = $"{Request.Scheme}://{Request.Host}";
        var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<XRD xmlns=""http://docs.oasis-open.org/ns/xri/xrd-1.0"">
  <Link rel=""lrdd"" type=""application/xrd+xml"" template=""{host}/.well-known/webfinger?resource={{uri}}"" />
</XRD>";
        return Content(xml, "application/xrd+xml; charset=utf-8");
    }

    /// <summary>
    /// WebFinger endpoint for user discovery.
    /// </summary>
    [HttpGet("/.well-known/webfinger")]
    public async Task<IActionResult> WebFinger([FromQuery] string resource)
    {
        if (string.IsNullOrEmpty(resource) || !resource.StartsWith("acct:"))
            return BadRequest(new { error = "Invalid resource. Expected acct:user@host" });

        var acct = resource["acct:".Length..];
        var atIndex = acct.IndexOf('@');
        if (atIndex < 0)
            return BadRequest(new { error = "Invalid resource format. Expected acct:user@host" });

        var username = acct[..atIndex];

        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return NotFound(new { error = "User not found" });

        var host = $"{Request.Scheme}://{Request.Host}";
        return Ok(new
        {
            subject = resource,
            links = new[]
            {
                new
                {
                    rel = "self",
                    type = "application/activity+json",
                    href = $"{host}/users/{username}"
                }
            }
        });
    }
}
