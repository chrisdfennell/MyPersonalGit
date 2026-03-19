using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/feeds")]
public class FeedController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IRepositoryService _repoService;
    private readonly IReleaseService _releaseService;
    private readonly IConfiguration _config;

    public FeedController(IDbContextFactory<AppDbContext> dbFactory, IRepositoryService repoService,
        IReleaseService releaseService, IConfiguration config)
    {
        _dbFactory = dbFactory;
        _repoService = repoService;
        _releaseService = releaseService;
        _config = config;
    }

    private string BaseUrl => $"{Request.Scheme}://{Request.Host}";

    // Repository commits feed
    [HttpGet("{repoName}/commits.atom")]
    public async Task<IActionResult> CommitsFeed(string repoName)
    {
        var repo = await _repoService.GetRepositoryAsync(repoName);
        if (repo == null) return NotFound();

        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        var repoPath = Path.Combine(projectRoot, repoName + ".git");
        if (!Directory.Exists(repoPath))
            repoPath = Path.Combine(projectRoot, repoName);
        if (!Directory.Exists(repoPath))
            return NotFound();

        var entries = new List<AtomEntry>();
        try
        {
            using var gitRepo = new LibGit2Sharp.Repository(repoPath);
            foreach (var commit in gitRepo.Commits.Take(30))
            {
                entries.Add(new AtomEntry
                {
                    Id = commit.Sha,
                    Title = commit.MessageShort,
                    Content = commit.Message,
                    Link = $"{BaseUrl}/repo/{repoName}/commit/{commit.Sha}",
                    AuthorName = commit.Author.Name,
                    AuthorEmail = commit.Author.Email,
                    Updated = commit.Committer.When
                });
            }
        }
        catch { return NotFound(); }

        return WriteAtomFeed(
            $"{repoName} — Commits",
            $"{BaseUrl}/repo/{repoName}",
            $"{BaseUrl}/api/feeds/{repoName}/commits.atom",
            entries);
    }

    // Repository releases feed
    [HttpGet("{repoName}/releases.atom")]
    public async Task<IActionResult> ReleasesFeed(string repoName)
    {
        var releases = await _releaseService.GetReleasesAsync(repoName);
        if (releases == null || !releases.Any()) return NotFound();

        var entries = releases.Take(30).Select(r => new AtomEntry
        {
            Id = r.Id.ToString(),
            Title = $"{r.TagName} — {r.Title}",
            Content = r.Body ?? "",
            Link = $"{BaseUrl}/repo/{repoName}/releases",
            AuthorName = r.Author ?? "",
            Updated = r.CreatedAt
        }).ToList();

        return WriteAtomFeed(
            $"{repoName} — Releases",
            $"{BaseUrl}/repo/{repoName}/releases",
            $"{BaseUrl}/api/feeds/{repoName}/releases.atom",
            entries);
    }

    // User activity feed
    [HttpGet("users/{username}/activity.atom")]
    public async Task<IActionResult> UserActivityFeed(string username)
    {
        using var db = _dbFactory.CreateDbContext();
        var activities = await db.UserActivities
            .Where(a => a.Username == username)
            .OrderByDescending(a => a.Timestamp)
            .Take(30)
            .ToListAsync();

        if (!activities.Any()) return NotFound();

        var entries = activities.Select(a => new AtomEntry
        {
            Id = a.Id.ToString(),
            Title = a.Description,
            Content = $"{a.ActivityType} in {a.Repository}",
            Link = string.IsNullOrEmpty(a.Url) ? null : $"{BaseUrl}{a.Url}",
            AuthorName = a.Username,
            Updated = a.Timestamp
        }).ToList();

        return WriteAtomFeed(
            $"{username} — Activity",
            $"{BaseUrl}/user/{username}",
            $"{BaseUrl}/api/feeds/users/{username}/activity.atom",
            entries);
    }

    // Global recent activity feed
    [HttpGet("global/activity.atom")]
    public async Task<IActionResult> GlobalActivityFeed()
    {
        using var db = _dbFactory.CreateDbContext();
        var activities = await db.UserActivities
            .OrderByDescending(a => a.Timestamp)
            .Take(50)
            .ToListAsync();

        if (!activities.Any()) return NotFound();

        var entries = activities.Select(a => new AtomEntry
        {
            Id = a.Id.ToString(),
            Title = $"[{a.Username}] {a.Description}",
            Content = $"{a.ActivityType} in {a.Repository} by {a.Username}",
            Link = string.IsNullOrEmpty(a.Url) ? null : $"{BaseUrl}{a.Url}",
            AuthorName = a.Username,
            Updated = a.Timestamp
        }).ToList();

        return WriteAtomFeed(
            "MyPersonalGit — Global Activity",
            BaseUrl,
            $"{BaseUrl}/api/feeds/global/activity.atom",
            entries);
    }

    // Repository tags feed
    [HttpGet("{repoName}/tags.atom")]
    public async Task<IActionResult> TagsFeed(string repoName)
    {
        var repo = await _repoService.GetRepositoryAsync(repoName);
        if (repo == null) return NotFound();

        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        var repoPath = Path.Combine(projectRoot, repoName + ".git");
        if (!Directory.Exists(repoPath))
            repoPath = Path.Combine(projectRoot, repoName);
        if (!Directory.Exists(repoPath))
            return NotFound();

        var entries = new List<AtomEntry>();
        try
        {
            using var gitRepo = new LibGit2Sharp.Repository(repoPath);
            foreach (var tag in gitRepo.Tags.OrderByDescending(t => (t.Target as LibGit2Sharp.Commit)?.Author.When ?? DateTimeOffset.MinValue).Take(30))
            {
                var commit = tag.Target as LibGit2Sharp.Commit;
                if (commit == null && tag.Target is LibGit2Sharp.TagAnnotation annotation)
                    commit = annotation.Target as LibGit2Sharp.Commit;
                if (commit == null) continue;

                entries.Add(new AtomEntry
                {
                    Id = tag.FriendlyName,
                    Title = tag.FriendlyName,
                    Content = commit.MessageShort,
                    Link = $"{BaseUrl}/repo/{repoName}?tab=tags",
                    Updated = commit.Author.When
                });
            }
        }
        catch { return NotFound(); }

        return WriteAtomFeed(
            $"{repoName} — Tags",
            $"{BaseUrl}/repo/{repoName}",
            $"{BaseUrl}/api/feeds/{repoName}/tags.atom",
            entries);
    }

    private ContentResult WriteAtomFeed(string title, string linkHref, string selfHref, List<AtomEntry> entries)
    {
        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8, OmitXmlDeclaration = false });

        writer.WriteStartElement("feed", "http://www.w3.org/2005/Atom");
        writer.WriteElementString("title", title);
        writer.WriteStartElement("link");
        writer.WriteAttributeString("href", linkHref);
        writer.WriteEndElement();
        writer.WriteStartElement("link");
        writer.WriteAttributeString("rel", "self");
        writer.WriteAttributeString("href", selfHref);
        writer.WriteEndElement();
        writer.WriteElementString("id", selfHref);
        writer.WriteElementString("updated", (entries.FirstOrDefault()?.Updated ?? DateTimeOffset.UtcNow).ToString("O"));

        foreach (var entry in entries)
        {
            writer.WriteStartElement("entry");
            writer.WriteElementString("title", entry.Title);
            writer.WriteElementString("id", entry.Id);
            writer.WriteElementString("updated", entry.Updated.ToString("O"));
            if (!string.IsNullOrEmpty(entry.Link))
            {
                writer.WriteStartElement("link");
                writer.WriteAttributeString("href", entry.Link);
                writer.WriteEndElement();
            }
            if (!string.IsNullOrEmpty(entry.AuthorName))
            {
                writer.WriteStartElement("author");
                writer.WriteElementString("name", entry.AuthorName);
                if (!string.IsNullOrEmpty(entry.AuthorEmail))
                    writer.WriteElementString("email", entry.AuthorEmail);
                writer.WriteEndElement();
            }
            if (!string.IsNullOrEmpty(entry.Content))
            {
                writer.WriteStartElement("content");
                writer.WriteAttributeString("type", "text");
                writer.WriteString(entry.Content);
                writer.WriteEndElement();
            }
            writer.WriteEndElement(); // entry
        }

        writer.WriteEndElement(); // feed
        writer.Flush();
        return Content(sb.ToString(), "application/atom+xml; charset=utf-8");
    }

    private class AtomEntry
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Content { get; set; }
        public string? Link { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorEmail { get; set; }
        public DateTimeOffset Updated { get; set; }
    }
}
