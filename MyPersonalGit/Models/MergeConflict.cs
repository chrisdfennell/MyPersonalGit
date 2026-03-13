namespace MyPersonalGit.Models;

public class MergeConflict
{
    public required string FilePath { get; set; }
    public string? BaseContent { get; set; }
    public required string OursContent { get; set; }
    public required string TheirsContent { get; set; }
    public required string ConflictMarkerContent { get; set; }
}
