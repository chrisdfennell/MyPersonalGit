namespace MyPersonalGit.Models;

public class WikiPage
{
    public int Id { get; set; }
    public required string RepoName { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public required string Content { get; set; }
    public required string Author { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<WikiPageRevision> Revisions { get; set; } = new();
}

public class WikiPageRevision
{
    public int Id { get; set; }
    public int WikiPageId { get; set; }
    public required string Content { get; set; }
    public required string Author { get; set; }
    public required string Message { get; set; }
    public DateTime CreatedAt { get; set; }
}
