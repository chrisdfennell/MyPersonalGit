namespace MyPersonalGit.Models;

public class Package
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }  // "nuget", "npm", "generic"
    public required string Owner { get; set; }
    public string? Description { get; set; }
    public string? RepositoryName { get; set; }
    public int Downloads { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<PackageVersion> Versions { get; set; } = new();
}

public class PackageVersion
{
    public int Id { get; set; }
    public int PackageId { get; set; }
    public required string Version { get; set; }
    public string? Description { get; set; }
    public long Size { get; set; }
    public string? Metadata { get; set; }  // JSON for nuspec, package.json, etc.
    public int Downloads { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<PackageFile> Files { get; set; } = new();
}

public class PackageFile
{
    public int Id { get; set; }
    public int PackageVersionId { get; set; }
    public required string Filename { get; set; }
    public long Size { get; set; }
    public string? Sha256 { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
