using System.Text;
using LibGit2Sharp;

namespace MyPersonalGit.Data;

// Data models
public record TreeNode(string Name, string Path, string Type, long Size, List<TreeNode>? Children);
public record FileContent(string Path, string Content, string Language);
public record FileChange(string Path, string Content, string Action, string? OldPath = null);
public record CommitRequest(string Branch, string Message, List<FileChange> Changes);
public record SearchResult(string FilePath, int LineNumber, string LineContent, string Context);
public record BlameHunk(int StartLine, int EndLine, string CommitSha, string Author, DateTimeOffset Date, string Message);
public record FileHistoryEntry(string Sha, string ShortSha, string Author, string Email, DateTimeOffset Date, string Message);
public record CommitGraphEntry(string Sha, string ShortSha, string Author, DateTimeOffset Date, string Message, List<string> ParentShas, int Column, List<string> Branches);

public interface IWebIdeService
{
    Task<List<TreeNode>> GetFullTreeAsync(string repoName, string branch);
    Task<FileContent> GetFileContentAsync(string repoName, string branch, string path);
    Task<List<FileContent>> GetMultipleFilesAsync(string repoName, string branch, List<string> paths);
    Task CommitMultipleFilesAsync(string repoName, string branch, string username, string email, string message, List<FileChange> changes);
    Task CreateFileAsync(string repoName, string branch, string path, string content, string username, string email);
    Task DeletePathAsync(string repoName, string branch, string path, string username, string email);
    Task RenamePathAsync(string repoName, string branch, string oldPath, string newPath, string username, string email);
    Task<List<SearchResult>> SearchFilesAsync(string repoName, string branch, string query, string? fileExtFilter = null);
    Task<byte[]?> GetFileRawAsync(string repoName, string branch, string path);
    Task CreateBinaryFileAsync(string repoName, string branch, string path, byte[] content, string username, string email);
    Task<List<BlameHunk>> GetFileBlameAsync(string repoName, string branch, string path);
    Task<List<FileHistoryEntry>> GetFileHistoryAsync(string repoName, string branch, string path);
    Task CreateBranchAsync(string repoName, string sourceBranch, string newBranchName);
    Task<List<CommitGraphEntry>> GetCommitGraphAsync(string repoName, string branch, int maxCount = 100);
    Task<List<StashEntry>> GetStashesAsync(string repoName);
    Task StashAsync(string repoName, string username, string email, string? message = null);
    Task StashPopAsync(string repoName);
    Task StashApplyAsync(string repoName, int index);
    Task StashDropAsync(string repoName, int index);
}

public record StashEntry(int Index, string Message, DateTimeOffset Date);

public class WebIdeService : IWebIdeService
{
    private readonly IConfiguration _config;
    private readonly ILogger<WebIdeService> _logger;
    private readonly IAdminService _adminService;

    public WebIdeService(IConfiguration config, ILogger<WebIdeService> logger, IAdminService adminService)
    {
        _config = config;
        _logger = logger;
        _adminService = adminService;
    }

    private async Task<string> GetRepoPathAsync(string repoName)
    {
        var systemSettings = await _adminService.GetSystemSettingsAsync();
        var projectRoot = !string.IsNullOrEmpty(systemSettings.ProjectRoot)
            ? systemSettings.ProjectRoot
            : _config["Git:ProjectRoot"] ?? _config["Git:ReposPath"] ?? "/repos";
        var path = System.IO.Path.Combine(projectRoot, repoName);
        if (Repository.IsValid(path)) return path;
        var dotGit = path + ".git";
        if (Repository.IsValid(dotGit)) return dotGit;
        var nested = System.IO.Path.Combine(path, repoName + ".git");
        if (Repository.IsValid(nested)) return nested;
        return path;
    }

    private static Branch ResolveBranch(Repository repo, string branch)
    {
        return repo.Branches[branch]
            ?? repo.Branches["main"]
            ?? repo.Branches["master"]
            ?? repo.Head;
    }

    public async Task<List<TreeNode>> GetFullTreeAsync(string repoName, string branch)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        return await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            var targetBranch = ResolveBranch(repo, branch);
            if (targetBranch?.Tip == null)
                return new List<TreeNode>();

            return BuildTreeRecursive(targetBranch.Tip.Tree, "");
        });
    }

    private static List<TreeNode> BuildTreeRecursive(Tree tree, string basePath)
    {
        var nodes = new List<TreeNode>();
        foreach (var entry in tree)
        {
            var fullPath = string.IsNullOrEmpty(basePath) ? entry.Name : $"{basePath}/{entry.Name}";
            if (entry.TargetType == TreeEntryTargetType.Tree)
            {
                var children = BuildTreeRecursive((Tree)entry.Target, fullPath);
                nodes.Add(new TreeNode(entry.Name, fullPath, "tree", 0, children));
            }
            else if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                var blob = (Blob)entry.Target;
                nodes.Add(new TreeNode(entry.Name, fullPath, "blob", blob.Size, null));
            }
        }
        return nodes.OrderByDescending(n => n.Type == "tree").ThenBy(n => n.Name).ToList();
    }

    public async Task<byte[]?> GetFileRawAsync(string repoName, string branch, string path)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        return await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            var targetBranch = ResolveBranch(repo, branch);
            if (targetBranch?.Tip == null) return null;

            var entry = targetBranch.Tip[path];
            if (entry == null || entry.TargetType != TreeEntryTargetType.Blob) return null;

            var blob = (Blob)entry.Target;
            using var ms = new MemoryStream();
            blob.GetContentStream().CopyTo(ms);
            return ms.ToArray();
        });
    }

    public async Task<FileContent> GetFileContentAsync(string repoName, string branch, string path)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        return await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            var targetBranch = ResolveBranch(repo, branch);
            if (targetBranch?.Tip == null)
                throw new FileNotFoundException($"Branch '{branch}' not found or has no commits.");

            var entry = targetBranch.Tip[path];
            if (entry == null || entry.TargetType != TreeEntryTargetType.Blob)
                throw new FileNotFoundException($"File '{path}' not found in branch '{branch}'.");

            var blob = (Blob)entry.Target;
            using var reader = new StreamReader(blob.GetContentStream(), Encoding.UTF8);
            var content = reader.ReadToEnd();
            var language = DetectLanguage(path);
            return new FileContent(path, content, language);
        });
    }

    public async Task<List<FileContent>> GetMultipleFilesAsync(string repoName, string branch, List<string> paths)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        return await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            var targetBranch = ResolveBranch(repo, branch);
            if (targetBranch?.Tip == null)
                return new List<FileContent>();

            var results = new List<FileContent>();
            foreach (var path in paths)
            {
                try
                {
                    var entry = targetBranch.Tip[path];
                    if (entry != null && entry.TargetType == TreeEntryTargetType.Blob)
                    {
                        var blob = (Blob)entry.Target;
                        using var reader = new StreamReader(blob.GetContentStream(), Encoding.UTF8);
                        var content = reader.ReadToEnd();
                        results.Add(new FileContent(path, content, DetectLanguage(path)));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read file {Path} in {Repo}/{Branch}", path, repoName, branch);
                }
            }
            return results;
        });
    }

    public async Task CommitMultipleFilesAsync(string repoName, string branch, string username, string email, string message, List<FileChange> changes)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            var targetBranch = ResolveBranch(repo, branch);
            var tipCommit = targetBranch?.Tip;

            var treeDef = tipCommit != null
                ? TreeDefinition.From(tipCommit.Tree)
                : new TreeDefinition();

            foreach (var change in changes)
            {
                switch (change.Action.ToLowerInvariant())
                {
                    case "add":
                    case "modify":
                        var blob = repo.ObjectDatabase.CreateBlob(new MemoryStream(Encoding.UTF8.GetBytes(change.Content ?? "")));
                        treeDef.Add(change.Path, blob, Mode.NonExecutableFile);
                        break;

                    case "delete":
                        treeDef.Remove(change.Path);
                        break;

                    case "rename":
                        if (!string.IsNullOrEmpty(change.OldPath))
                        {
                            // Copy content from old path if no new content provided
                            if (string.IsNullOrEmpty(change.Content) && tipCommit != null)
                            {
                                var oldEntry = tipCommit[change.OldPath];
                                if (oldEntry != null && oldEntry.TargetType == TreeEntryTargetType.Blob)
                                {
                                    var oldBlob = (Blob)oldEntry.Target;
                                    treeDef.Add(change.Path, oldBlob, Mode.NonExecutableFile);
                                }
                            }
                            else
                            {
                                var renameBlob = repo.ObjectDatabase.CreateBlob(new MemoryStream(Encoding.UTF8.GetBytes(change.Content ?? "")));
                                treeDef.Add(change.Path, renameBlob, Mode.NonExecutableFile);
                            }
                            treeDef.Remove(change.OldPath);
                        }
                        break;
                }
            }

            var tree = repo.ObjectDatabase.CreateTree(treeDef);
            var author = new Signature(username, email, DateTimeOffset.Now);
            var parents = tipCommit != null ? new[] { tipCommit } : Array.Empty<Commit>();
            var commit = repo.ObjectDatabase.CreateCommit(author, author, message, tree, parents, true);

            var targetBranchName = string.IsNullOrEmpty(branch) ? "main" : branch;
            var branchRef = repo.Refs[$"refs/heads/{targetBranchName}"];
            if (branchRef == null)
            {
                repo.Branches.Add(targetBranchName, commit);
                repo.Refs.UpdateTarget("HEAD", $"refs/heads/{targetBranchName}");
            }
            else
            {
                repo.Refs.UpdateTarget(branchRef.CanonicalName, commit.Id.Sha);
            }
        });
    }

    public async Task CreateBinaryFileAsync(string repoName, string branch, string path, byte[] content, string username, string email)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            var targetBranch = ResolveBranch(repo, branch);
            var tipCommit = targetBranch?.Tip;

            var treeDef = tipCommit != null
                ? TreeDefinition.From(tipCommit.Tree)
                : new TreeDefinition();

            var blob = repo.ObjectDatabase.CreateBlob(new MemoryStream(content));
            treeDef.Add(path, blob, Mode.NonExecutableFile);

            var tree = repo.ObjectDatabase.CreateTree(treeDef);
            var author = new Signature(username, email, DateTimeOffset.Now);
            var parents = tipCommit != null ? new[] { tipCommit } : Array.Empty<Commit>();
            var commit = repo.ObjectDatabase.CreateCommit(author, author, $"Upload {path}", tree, parents, true);

            var targetBranchName = string.IsNullOrEmpty(branch) ? "main" : branch;
            var branchRef = repo.Refs[$"refs/heads/{targetBranchName}"];
            if (branchRef == null)
            {
                repo.Branches.Add(targetBranchName, commit);
                repo.Refs.UpdateTarget("HEAD", $"refs/heads/{targetBranchName}");
            }
            else
            {
                repo.Refs.UpdateTarget(branchRef.CanonicalName, commit.Id.Sha);
            }
        });
    }

    public Task CreateFileAsync(string repoName, string branch, string path, string content, string username, string email)
    {
        return CommitMultipleFilesAsync(repoName, branch, username, email, $"Create {path}",
            new List<FileChange> { new FileChange(path, content, "add") });
    }

    public async Task DeletePathAsync(string repoName, string branch, string path, string username, string email)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            var targetBranch = ResolveBranch(repo, branch);
            var tipCommit = targetBranch?.Tip;
            if (tipCommit == null)
                throw new InvalidOperationException($"Branch '{branch}' has no commits.");

            var treeDef = TreeDefinition.From(tipCommit.Tree);

            // Check if the path is a tree (folder) or blob (file)
            var entry = tipCommit[path];
            if (entry == null)
                throw new FileNotFoundException($"Path '{path}' not found in branch '{branch}'.");

            if (entry.TargetType == TreeEntryTargetType.Tree)
            {
                // Recursively remove all entries under this folder
                RemoveTreeEntries(tipCommit.Tree, path, treeDef);
            }
            else
            {
                treeDef.Remove(path);
            }

            var tree = repo.ObjectDatabase.CreateTree(treeDef);
            var author = new Signature(username, email, DateTimeOffset.Now);
            var commit = repo.ObjectDatabase.CreateCommit(author, author, $"Delete {path}", tree, new[] { tipCommit }, true);

            var targetBranchName = string.IsNullOrEmpty(branch) ? "main" : branch;
            var branchRef = repo.Refs[$"refs/heads/{targetBranchName}"];
            if (branchRef != null)
                repo.Refs.UpdateTarget(branchRef.CanonicalName, commit.Id.Sha);
        });
    }

    private static void RemoveTreeEntries(Tree rootTree, string folderPath, TreeDefinition treeDef)
    {
        CollectBlobPaths(rootTree, folderPath, treeDef);
    }

    private static void CollectBlobPaths(Tree tree, string targetPrefix, TreeDefinition treeDef)
    {
        foreach (var entry in tree)
        {
            var entryPath = entry.Path;
            if (entryPath.StartsWith(targetPrefix + "/") || entryPath == targetPrefix)
            {
                if (entry.TargetType == TreeEntryTargetType.Blob)
                {
                    treeDef.Remove(entryPath);
                }
                else if (entry.TargetType == TreeEntryTargetType.Tree)
                {
                    CollectBlobPaths((Tree)entry.Target, targetPrefix, treeDef);
                }
            }
        }
    }

    public async Task RenamePathAsync(string repoName, string branch, string oldPath, string newPath, string username, string email)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            var targetBranch = ResolveBranch(repo, branch);
            var tipCommit = targetBranch?.Tip;
            if (tipCommit == null)
                throw new InvalidOperationException($"Branch '{branch}' has no commits.");

            var treeDef = TreeDefinition.From(tipCommit.Tree);
            var entry = tipCommit[oldPath];
            if (entry == null)
                throw new FileNotFoundException($"Path '{oldPath}' not found.");

            if (entry.TargetType == TreeEntryTargetType.Tree)
            {
                // Rename folder: move all blobs under oldPath to newPath
                RenameFolderEntries(tipCommit.Tree, oldPath, newPath, treeDef);
            }
            else
            {
                // Single file rename
                var blob = (Blob)entry.Target;
                treeDef.Add(newPath, blob, Mode.NonExecutableFile);
                treeDef.Remove(oldPath);
            }

            var tree = repo.ObjectDatabase.CreateTree(treeDef);
            var author = new Signature(username, email, DateTimeOffset.Now);
            var commit = repo.ObjectDatabase.CreateCommit(author, author, $"Rename {oldPath} to {newPath}", tree, new[] { tipCommit }, true);

            var targetBranchName = string.IsNullOrEmpty(branch) ? "main" : branch;
            var branchRef = repo.Refs[$"refs/heads/{targetBranchName}"];
            if (branchRef != null)
                repo.Refs.UpdateTarget(branchRef.CanonicalName, commit.Id.Sha);
        });
    }

    private static void RenameFolderEntries(Tree rootTree, string oldPrefix, string newPrefix, TreeDefinition treeDef)
    {
        foreach (var entry in rootTree)
        {
            if (entry.Path.StartsWith(oldPrefix + "/") || entry.Path == oldPrefix)
            {
                if (entry.TargetType == TreeEntryTargetType.Blob)
                {
                    var relativePath = entry.Path.Substring(oldPrefix.Length);
                    var newEntryPath = newPrefix + relativePath;
                    treeDef.Add(newEntryPath, (Blob)entry.Target, Mode.NonExecutableFile);
                    treeDef.Remove(entry.Path);
                }
                else if (entry.TargetType == TreeEntryTargetType.Tree)
                {
                    RenameFolderEntries((Tree)entry.Target, oldPrefix, newPrefix, treeDef);
                }
            }
        }
    }

    public async Task<List<SearchResult>> SearchFilesAsync(string repoName, string branch, string query, string? fileExtFilter = null)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        return await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            var targetBranch = ResolveBranch(repo, branch);
            if (targetBranch?.Tip == null)
                return new List<SearchResult>();

            var results = new List<SearchResult>();
            SearchTreeRecursive(targetBranch.Tip.Tree, "", query, fileExtFilter, results);
            return results;
        });
    }

    private static void SearchTreeRecursive(Tree tree, string basePath, string query, string? fileExtFilter, List<SearchResult> results)
    {
        foreach (var entry in tree)
        {
            var fullPath = string.IsNullOrEmpty(basePath) ? entry.Name : $"{basePath}/{entry.Name}";

            if (entry.TargetType == TreeEntryTargetType.Tree)
            {
                SearchTreeRecursive((Tree)entry.Target, fullPath, query, fileExtFilter, results);
            }
            else if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                if (!string.IsNullOrEmpty(fileExtFilter))
                {
                    var ext = System.IO.Path.GetExtension(entry.Name);
                    if (!ext.Equals(fileExtFilter, StringComparison.OrdinalIgnoreCase)
                        && !ext.Equals("." + fileExtFilter, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                var blob = (Blob)entry.Target;
                // Skip large or binary files
                if (blob.Size > 1_048_576 || blob.IsBinary)
                    continue;

                try
                {
                    using var reader = new StreamReader(blob.GetContentStream(), Encoding.UTF8);
                    var content = reader.ReadToEnd();
                    var lines = content.Split('\n');

                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                        {
                            // Build context: 1 line before and after
                            var contextLines = new List<string>();
                            if (i > 0) contextLines.Add(lines[i - 1].TrimEnd('\r'));
                            contextLines.Add(lines[i].TrimEnd('\r'));
                            if (i < lines.Length - 1) contextLines.Add(lines[i + 1].TrimEnd('\r'));

                            results.Add(new SearchResult(
                                fullPath,
                                i + 1,
                                lines[i].TrimEnd('\r'),
                                string.Join("\n", contextLines)
                            ));
                        }
                    }
                }
                catch
                {
                    // Skip files that can't be read as text
                }
            }
        }
    }

    public async Task<List<BlameHunk>> GetFileBlameAsync(string repoName, string branch, string path)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        return await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            var targetBranch = ResolveBranch(repo, branch);
            if (targetBranch?.Tip == null)
                return new List<BlameHunk>();

            try
            {
                var blameResult = repo.Blame(path, new BlameOptions
                {
                    StartingAt = targetBranch.Tip
                });

                return blameResult.Select(h => new BlameHunk(
                    h.FinalStartLineNumber,
                    h.FinalStartLineNumber + h.LineCount - 1,
                    h.FinalCommit.Sha[..7],
                    h.FinalCommit.Author.Name,
                    h.FinalCommit.Author.When,
                    h.FinalCommit.MessageShort
                )).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to blame {Path} in {Repo}/{Branch}", path, repoName, branch);
                return new List<BlameHunk>();
            }
        });
    }

    public async Task<List<FileHistoryEntry>> GetFileHistoryAsync(string repoName, string branch, string path)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        return await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            var targetBranch = ResolveBranch(repo, branch);
            if (targetBranch?.Tip == null)
                return new List<FileHistoryEntry>();

            var results = new List<FileHistoryEntry>();
            foreach (var commit in repo.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = targetBranch.Tip,
                SortBy = CommitSortStrategies.Time
            }))
            {
                if (results.Count >= 50) break;

                var entry = commit[path];
                if (entry == null) continue;

                var parent = commit.Parents.FirstOrDefault();
                if (parent == null)
                {
                    // Initial commit that contains this file
                    results.Add(new FileHistoryEntry(
                        commit.Sha, commit.Sha[..7], commit.Author.Name,
                        commit.Author.Email, commit.Author.When, commit.MessageShort));
                }
                else
                {
                    var parentEntry = parent[path];
                    if (parentEntry == null || parentEntry.Target.Sha != entry.Target.Sha)
                    {
                        results.Add(new FileHistoryEntry(
                            commit.Sha, commit.Sha[..7], commit.Author.Name,
                            commit.Author.Email, commit.Author.When, commit.MessageShort));
                    }
                }
            }
            return results;
        });
    }

    private static string DetectLanguage(string path)
    {
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".cs" => "csharp",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".tsx" => "tsx",
            ".jsx" => "jsx",
            ".py" => "python",
            ".rb" => "ruby",
            ".go" => "go",
            ".rs" => "rust",
            ".java" => "java",
            ".kt" => "kotlin",
            ".swift" => "swift",
            ".c" => "c",
            ".cpp" or ".cc" or ".cxx" => "cpp",
            ".h" or ".hpp" => "cpp",
            ".html" or ".htm" => "html",
            ".css" => "css",
            ".scss" => "scss",
            ".less" => "less",
            ".json" => "json",
            ".xml" => "xml",
            ".yaml" or ".yml" => "yaml",
            ".md" => "markdown",
            ".sql" => "sql",
            ".sh" or ".bash" => "shell",
            ".ps1" => "powershell",
            ".dockerfile" => "dockerfile",
            ".razor" => "razor",
            ".cshtml" => "razor",
            ".csproj" or ".sln" or ".props" or ".targets" => "xml",
            ".toml" => "toml",
            ".ini" or ".cfg" => "ini",
            ".php" => "php",
            ".r" => "r",
            ".lua" => "lua",
            ".dart" => "dart",
            ".vue" => "vue",
            ".svelte" => "svelte",
            ".tf" => "hcl",
            ".proto" => "protobuf",
            ".graphql" or ".gql" => "graphql",
            _ => "plaintext"
        };
    }

    public async Task<List<CommitGraphEntry>> GetCommitGraphAsync(string repoName, string branch, int maxCount = 100)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        return await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            var targetBranch = ResolveBranch(repo, branch);
            if (targetBranch?.Tip == null) return new List<CommitGraphEntry>();

            // Map branch tips for labeling
            var branchTips = new Dictionary<string, List<string>>();
            foreach (var b in repo.Branches.Where(b => !b.IsRemote))
            {
                if (b.Tip != null)
                {
                    if (!branchTips.ContainsKey(b.Tip.Sha))
                        branchTips[b.Tip.Sha] = new List<string>();
                    branchTips[b.Tip.Sha].Add(b.FriendlyName);
                }
            }

            // Simple column assignment: track active lanes
            var activeLanes = new List<string>(); // SHA of commits expected in each lane
            var entries = new List<CommitGraphEntry>();

            foreach (var commit in targetBranch.Commits.Take(maxCount))
            {
                var parentShas = commit.Parents.Select(p => p.Sha).ToList();
                var branches = branchTips.GetValueOrDefault(commit.Sha) ?? new List<string>();

                // Find or assign lane
                var column = activeLanes.IndexOf(commit.Sha);
                if (column == -1)
                {
                    column = activeLanes.Count;
                    activeLanes.Add(commit.Sha);
                }

                // Replace this lane with first parent, close lane if no parents
                if (parentShas.Count > 0)
                    activeLanes[column] = parentShas[0];
                else
                    activeLanes[column] = "";

                // Additional parents get new lanes
                for (int i = 1; i < parentShas.Count; i++)
                {
                    if (!activeLanes.Contains(parentShas[i]))
                        activeLanes.Add(parentShas[i]);
                }

                entries.Add(new CommitGraphEntry(
                    commit.Sha,
                    commit.Sha[..7],
                    commit.Author.Name,
                    commit.Author.When,
                    commit.MessageShort,
                    parentShas,
                    column,
                    branches
                ));
            }

            return entries;
        });
    }

    public async Task CreateBranchAsync(string repoName, string sourceBranch, string newBranchName)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            var source = ResolveBranch(repo, sourceBranch);
            if (source?.Tip == null)
                throw new InvalidOperationException($"Source branch '{sourceBranch}' not found.");

            if (repo.Branches[newBranchName] != null)
                throw new InvalidOperationException($"Branch '{newBranchName}' already exists.");

            repo.CreateBranch(newBranchName, source.Tip);
        });
    }

    public async Task<List<StashEntry>> GetStashesAsync(string repoName)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        return await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            return repo.Stashes.Select((s, i) => new StashEntry(i, s.Message, s.Index.Committer.When)).ToList();
        });
    }

    public async Task StashAsync(string repoName, string username, string email, string? message = null)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            var sig = new Signature(username, email, DateTimeOffset.Now);
            repo.Stashes.Add(sig, message ?? "WIP");
        });
    }

    public async Task StashPopAsync(string repoName)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            if (repo.Stashes.Count() == 0) throw new InvalidOperationException("No stashes to pop.");
            repo.Stashes.Pop(0);
        });
    }

    public async Task StashApplyAsync(string repoName, int index)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            if (index >= repo.Stashes.Count()) throw new InvalidOperationException("Stash index out of range.");
            repo.Stashes.Apply(index);
        });
    }

    public async Task StashDropAsync(string repoName, int index)
    {
        var repoPath = await GetRepoPathAsync(repoName);
        await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            if (index >= repo.Stashes.Count()) throw new InvalidOperationException("Stash index out of range.");
            repo.Stashes.Remove(index);
        });
    }
}
