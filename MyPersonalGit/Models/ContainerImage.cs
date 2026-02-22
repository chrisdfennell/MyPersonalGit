namespace MyPersonalGit.Models;

public class ContainerManifest
{
    public int Id { get; set; }
    public required string RepositoryName { get; set; }
    public required string Tag { get; set; }
    public required string Digest { get; set; }
    public required string MediaType { get; set; }
    public required string Content { get; set; }
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ContainerBlob
{
    public int Id { get; set; }
    public required string RepositoryName { get; set; }
    public required string Digest { get; set; }
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ContainerUploadSession
{
    public int Id { get; set; }
    public required string Uuid { get; set; }
    public required string RepositoryName { get; set; }
    public long BytesReceived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
