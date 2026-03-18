namespace MyPersonalGit.Models;

public class RepositoryLabel
{
    public int Id { get; set; }
    public string RepoName { get; set; } = "";
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#0075ca";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
