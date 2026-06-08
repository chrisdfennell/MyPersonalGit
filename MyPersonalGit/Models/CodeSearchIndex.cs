using System;

namespace MyPersonalGit.Models;

public class CodeSearchIndex
{
    public int Id { get; set; }
    public string RepoName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime IndexedAt { get; set; }
}
