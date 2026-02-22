namespace MyPersonalGit.Models;

public class Release
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string TagName { get; set; }
    public required string Title { get; set; }
    public string? Body { get; set; }
    public required string Author { get; set; }
    public bool IsDraft { get; set; }
    public bool IsPrerelease { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public List<ReleaseAsset> Assets { get; set; } = new();
}

public class ReleaseAsset
{
    public int Id { get; set; }
    public int ReleaseId { get; set; }
    public required string FileName { get; set; }
    public long Size { get; set; }
    public required string ContentType { get; set; }
    public int DownloadCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
