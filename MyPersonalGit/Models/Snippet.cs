namespace MyPersonalGit.Models;

public class Snippet
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string Owner { get; set; }
    public bool IsPublic { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<SnippetFile> Files { get; set; } = new();
}

public class SnippetFile
{
    public int Id { get; set; }
    public int SnippetId { get; set; }
    public required string Filename { get; set; }
    public required string Content { get; set; }
    public string? Language { get; set; }
}
