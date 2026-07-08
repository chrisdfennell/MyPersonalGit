namespace MyPersonalGit.Models;

public class Repository
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? WebsiteUrl { get; set; }
    public required string Owner { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Stars { get; set; }
    public int Forks { get; set; }
    public int Watchers { get; set; }
    public List<string> Topics { get; set; } = new();
    public string? DefaultBranch { get; set; } = "main";
    public bool HasIssues { get; set; } = true;
    public bool HasWiki { get; set; } = true;
    public bool HasProjects { get; set; } = true;
    public string? ForkedFrom { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public bool HasPages { get; set; }
    public string PagesBranch { get; set; } = "gh-pages";
    public bool IsTemplate { get; set; }
    public int? TemplateRepositoryId { get; set; }
    public string? ExternalIssueTrackerUrl { get; set; }
    public string? ExternalIssueTrackerPattern { get; set; } // e.g., "PROJ-{id}"
    public bool UseExternalIssueTracker { get; set; }
}

public class RepositoryStar
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string Username { get; set; }
    public DateTime StarredAt { get; set; } = DateTime.UtcNow;
}

public class RepositoryWatch
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string Username { get; set; }
    public DateTime WatchedAt { get; set; } = DateTime.UtcNow;
    public WatchLevel Level { get; set; } = WatchLevel.All;
}

/// <summary>
/// Notification subscription level for a repository. Users with no RepositoryWatch row
/// get the default behavior: notified only when participating (author, assignee,
/// commenter, reviewer) or @mentioned.
/// </summary>
public enum WatchLevel
{
    /// <summary>Notified of all issue/PR activity in the repository.</summary>
    All = 0,
    /// <summary>Never notified for this repository, not even for mentions.</summary>
    Ignore = 1
}

public class RepositoryFork
{
    public int Id { get; set; }
    public required string OriginalRepo { get; set; }
    public required string ForkedRepo { get; set; }
    public required string Owner { get; set; }
    public DateTime ForkedAt { get; set; } = DateTime.UtcNow;
}
