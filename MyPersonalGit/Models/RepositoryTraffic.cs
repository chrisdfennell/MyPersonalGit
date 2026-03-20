namespace MyPersonalGit.Models;

public class RepositoryTrafficEvent
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string EventType { get; set; }
    public string? Referrer { get; set; }
    public string? Path { get; set; }
    public string? IpHash { get; set; }
    public DateTime Timestamp { get; set; }
}

public class RepositoryTrafficSummary
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public DateTime Date { get; set; }
    public int Clones { get; set; }
    public int UniqueCloners { get; set; }
    public int PageViews { get; set; }
    public int UniqueVisitors { get; set; }
}
