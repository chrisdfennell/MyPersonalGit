using LibGit2Sharp;

namespace MyPersonalGit.Data;

public class BlameHunkInfo
{
    public int FinalStartLineNumber { get; set; }
    public int LineCount { get; set; }
    public string CommitSha { get; set; } = "";
    public string ShortSha { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public string AuthorEmail { get; set; } = "";
    public DateTime Date { get; set; }
    public string MessageSummary { get; set; } = "";
    public List<string> Lines { get; set; } = new();
}

public interface IBlameService
{
    List<BlameHunkInfo> GetBlame(string owner, string repoName, string branchOrRef, string filePath);
}

public class BlameService : IBlameService
{
    private readonly IConfiguration _config;
    private readonly ILogger<BlameService> _logger;

    public BlameService(IConfiguration config, ILogger<BlameService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public List<BlameHunkInfo> GetBlame(string owner, string repoName, string branchOrRef, string filePath)
    {
        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        var repoPath = ResolveRepoPath(projectRoot, repoName);
        if (repoPath == null)
            return new List<BlameHunkInfo>();

        try
        {
            using var repo = new Repository(repoPath);

            // Resolve branch/ref to a commit
            var commit = ResolveCommit(repo, branchOrRef);
            if (commit == null)
                return new List<BlameHunkInfo>();

            // Verify the file exists at this commit
            var entry = commit[filePath];
            if (entry == null || entry.TargetType != TreeEntryTargetType.Blob)
                return new List<BlameHunkInfo>();

            // Read file content for line data
            var blob = (Blob)entry.Target;
            string[] allLines;
            using (var reader = new StreamReader(blob.GetContentStream(), System.Text.Encoding.UTF8))
            {
                allLines = reader.ReadToEnd().Split('\n');
            }

            var blameResult = repo.Blame(filePath);
            var hunks = new List<BlameHunkInfo>();

            foreach (var hunk in blameResult)
            {
                var lines = new List<string>();
                for (int i = 0; i < hunk.LineCount; i++)
                {
                    var globalLine = hunk.FinalStartLineNumber + i; // 1-based
                    var idx = globalLine - 1; // convert to 0-based array index
                    lines.Add(idx >= 0 && idx < allLines.Length ? allLines[idx] : "");
                }

                hunks.Add(new BlameHunkInfo
                {
                    FinalStartLineNumber = hunk.FinalStartLineNumber,
                    LineCount = hunk.LineCount,
                    CommitSha = hunk.FinalCommit.Sha,
                    ShortSha = hunk.FinalCommit.Sha[..7],
                    AuthorName = hunk.FinalCommit.Author.Name,
                    AuthorEmail = hunk.FinalCommit.Author.Email,
                    Date = hunk.FinalCommit.Author.When.UtcDateTime,
                    MessageSummary = hunk.FinalCommit.MessageShort,
                    Lines = lines
                });
            }

            return hunks;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get blame for {RepoName}/{FilePath}", repoName, filePath);
            return new List<BlameHunkInfo>();
        }
    }

    private static Commit? ResolveCommit(Repository repo, string branchOrRef)
    {
        // Try as branch name first
        var branch = repo.Branches[branchOrRef];
        if (branch?.Tip != null)
            return branch.Tip;

        // Try as tag
        var tag = repo.Tags[branchOrRef];
        if (tag?.Target is Commit tagCommit)
            return tagCommit;

        // Try as SHA
        try
        {
            var obj = repo.Lookup(branchOrRef);
            if (obj is Commit c) return c;
        }
        catch { }

        // Fallback to default branches
        branch = repo.Branches["main"] ?? repo.Branches["master"] ?? repo.Head;
        return branch?.Tip;
    }

    private static string? ResolveRepoPath(string projectRoot, string repoName)
    {
        var path = Path.Combine(projectRoot, repoName);
        if (Repository.IsValid(path)) return path;
        if (Repository.IsValid(path + ".git")) return path + ".git";
        var nested = Path.Combine(path, repoName + ".git");
        if (Repository.IsValid(nested)) return nested;
        return null;
    }
}
