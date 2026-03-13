using LibGit2Sharp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
[Route("api/v1/repos")]
[EnableRateLimiting("api")]
public class RepositoriesController : ControllerBase
{
    private readonly IRepositoryService _repoService;
    private readonly IReleaseService _releaseService;
    private readonly IArchiveService _archiveService;
    private readonly IBlameService _blameService;
    private readonly ITemplateService _templateService;
    private readonly ICodeOwnersService _codeOwnersService;
    private readonly IConfiguration _config;
    private readonly ILogger<RepositoriesController> _logger;

    public RepositoriesController(IRepositoryService repoService, IReleaseService releaseService, IArchiveService archiveService, IBlameService blameService, ITemplateService templateService, ICodeOwnersService codeOwnersService, IConfiguration config, ILogger<RepositoriesController> logger)
    {
        _repoService = repoService;
        _releaseService = releaseService;
        _archiveService = archiveService;
        _blameService = blameService;
        _templateService = templateService;
        _codeOwnersService = codeOwnersService;
        _config = config;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ListRepositories()
    {
        var repos = await _repoService.GetRepositoriesAsync();
        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";

        // Filter private repos unless the API caller is the owner
        var currentUser = User.Identity?.Name;
        repos = repos.Where(r => !r.IsPrivate || (currentUser != null && r.Owner.Equals(currentUser, StringComparison.OrdinalIgnoreCase))).ToList();

        var result = repos.Select(r =>
        {
            var repoPath = Path.Combine(projectRoot, r.Name);
            string? defaultBranch = null;
            int commitCount = 0;

            if (Repository.IsValid(repoPath))
            {
                try
                {
                    using var repo = new Repository(repoPath);
                    defaultBranch = repo.Head?.FriendlyName;
                    commitCount = repo.Commits.Count();
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to read git repository"); }
            }

            return new
            {
                r.Name,
                r.Description,
                r.Owner,
                r.IsPrivate,
                r.Stars,
                r.Forks,
                r.Topics,
                default_branch = defaultBranch ?? r.DefaultBranch,
                commits = commitCount,
                created_at = r.CreatedAt,
                updated_at = r.UpdatedAt
            };
        });

        return Ok(result);
    }

    [HttpGet("{repoName}")]
    public async Task<IActionResult> GetRepository(string repoName)
    {
        var repo = await _repoService.GetRepositoryAsync(repoName);
        if (repo == null) return NotFound(new { error = $"Repository '{repoName}' not found" });

        // Enforce private repo access
        if (repo.IsPrivate)
        {
            var currentUser = User.Identity?.Name;
            if (currentUser == null || !repo.Owner.Equals(currentUser, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = $"Repository '{repoName}' not found" });
        }

        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        var repoPath = Path.Combine(projectRoot, repoName);

        string? defaultBranch = null;
        int commitCount = 0;
        var branchNames = new List<string>();
        var tagNames = new List<string>();

        if (Repository.IsValid(repoPath))
        {
            try
            {
                using var gitRepo = new Repository(repoPath);
                defaultBranch = gitRepo.Head?.FriendlyName;
                commitCount = gitRepo.Commits.Count();
                branchNames = gitRepo.Branches.Select(b => b.FriendlyName).ToList();
                tagNames = gitRepo.Tags.Select(t => t.FriendlyName).ToList();
            }
            catch { }
        }

        return Ok(new
        {
            repo.Name,
            repo.Description,
            repo.Owner,
            repo.IsPrivate,
            repo.Stars,
            repo.Forks,
            repo.Topics,
            default_branch = defaultBranch ?? repo.DefaultBranch,
            commits = commitCount,
            branches = branchNames,
            tags = tagNames,
            created_at = repo.CreatedAt,
            updated_at = repo.UpdatedAt
        });
    }

    [HttpGet("{repoName}/branches")]
    public IActionResult ListBranches(string repoName)
    {
        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        var repoPath = Path.Combine(projectRoot, repoName);
        if (!Repository.IsValid(repoPath)) return NotFound(new { error = $"Repository '{repoName}' not found" });

        using var repo = new Repository(repoPath);
        var branches = repo.Branches.Select(b => new
        {
            name = b.FriendlyName,
            is_head = b.IsCurrentRepositoryHead,
            commit = b.Tip != null ? new { sha = b.Tip.Sha, message = b.Tip.MessageShort, author = b.Tip.Author.Name, date = b.Tip.Author.When.UtcDateTime } : null
        });

        return Ok(branches);
    }

    [HttpGet("{repoName}/tags")]
    public IActionResult ListTags(string repoName)
    {
        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        var repoPath = Path.Combine(projectRoot, repoName);
        if (!Repository.IsValid(repoPath)) return NotFound(new { error = $"Repository '{repoName}' not found" });

        using var repo = new Repository(repoPath);
        var tags = repo.Tags.Select(t => new
        {
            name = t.FriendlyName,
            target_sha = t.Target.Sha
        });

        return Ok(tags);
    }

    [HttpGet("{repoName}/commits")]
    public IActionResult ListCommits(string repoName, [FromQuery] string? branch = null, [FromQuery] int limit = 30)
    {
        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        var repoPath = Path.Combine(projectRoot, repoName);
        if (!Repository.IsValid(repoPath)) return NotFound(new { error = $"Repository '{repoName}' not found" });

        using var repo = new Repository(repoPath);
        var targetBranch = repo.Branches[branch ?? ""] ?? repo.Branches["main"] ?? repo.Branches["master"] ?? repo.Head;
        if (targetBranch?.Tip == null) return Ok(Array.Empty<object>());

        var commits = repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = targetBranch.Tip })
            .Take(Math.Min(limit, 100))
            .Select(c => new
            {
                sha = c.Sha,
                short_sha = c.Id.ToString(7),
                message = c.MessageShort,
                author = c.Author.Name,
                email = c.Author.Email,
                date = c.Author.When.UtcDateTime
            });

        return Ok(commits);
    }

    [HttpGet("{repoName}/tree/{*path}")]
    public IActionResult GetTree(string repoName, string? path = null, [FromQuery] string? branch = null)
    {
        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        var repoPath = Path.Combine(projectRoot, repoName);
        if (!Repository.IsValid(repoPath)) return NotFound(new { error = $"Repository '{repoName}' not found" });

        using var repo = new Repository(repoPath);
        var targetBranch = repo.Branches[branch ?? ""] ?? repo.Branches["main"] ?? repo.Branches["master"] ?? repo.Head;
        if (targetBranch?.Tip == null) return Ok(Array.Empty<object>());

        Tree tree;
        if (string.IsNullOrEmpty(path))
        {
            tree = targetBranch.Tip.Tree;
        }
        else
        {
            var entry = targetBranch.Tip[path];
            if (entry == null) return NotFound(new { error = $"Path '{path}' not found" });

            if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                var blob = (Blob)entry.Target;
                using var reader = new StreamReader(blob.GetContentStream(), System.Text.Encoding.UTF8);
                return Ok(new
                {
                    type = "file",
                    name = entry.Name,
                    path,
                    size = blob.Size,
                    content = reader.ReadToEnd()
                });
            }

            tree = (Tree)entry.Target;
        }

        var items = tree.Select(e => new
        {
            name = e.Name,
            type = e.TargetType == TreeEntryTargetType.Tree ? "dir" : "file",
            path = string.IsNullOrEmpty(path) ? e.Name : $"{path}/{e.Name}",
            size = e.TargetType == TreeEntryTargetType.Blob ? ((Blob)e.Target).Size : (long?)null
        }).OrderByDescending(e => e.type == "dir").ThenBy(e => e.name);

        return Ok(new { type = "dir", path = path ?? "/", entries = items });
    }

    [HttpGet("{repoName}/archive/zip")]
    public async Task<IActionResult> DownloadZipLegacy(string repoName, [FromQuery] string? @ref = null)
    {
        return await DownloadArchive(repoName, @ref, ArchiveFormat.Zip);
    }

    [HttpGet("{repoName}/archive/{refAndFormat}")]
    public async Task<IActionResult> DownloadArchiveByRef(string repoName, string refAndFormat)
    {
        ArchiveFormat format;
        string gitRef;

        if (refAndFormat.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
        {
            format = ArchiveFormat.TarGz;
            gitRef = refAndFormat[..^7]; // remove ".tar.gz"
        }
        else if (refAndFormat.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            format = ArchiveFormat.Zip;
            gitRef = refAndFormat[..^4]; // remove ".zip"
        }
        else
        {
            return BadRequest(new { error = "Unsupported archive format. Use .zip or .tar.gz" });
        }

        return await DownloadArchive(repoName, gitRef, format);
    }

    private async Task<IActionResult> DownloadArchive(string repoName, string? gitRef, ArchiveFormat format)
    {
        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        var repoPath = Path.Combine(projectRoot, repoName);
        if (!Repository.IsValid(repoPath))
        {
            repoPath = Path.Combine(projectRoot, repoName + ".git");
            if (!Repository.IsValid(repoPath))
                return NotFound(new { error = $"Repository '{repoName}' not found" });
        }

        // Enforce private repo access
        var meta = await _repoService.GetRepositoryAsync(repoName);
        if (meta is { IsPrivate: true })
        {
            var currentUser = User.Identity?.Name;
            if (currentUser == null || !meta.Owner.Equals(currentUser, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = $"Repository '{repoName}' not found" });
        }

        var displayName = repoName.EndsWith(".git") ? repoName[..^4] : repoName;
        var stream = _archiveService.CreateArchive(repoPath, gitRef, format, out var resolvedRef);
        if (stream == null)
            return NotFound(new { error = "Invalid ref or empty repository" });

        var (contentType, extension) = format switch
        {
            ArchiveFormat.TarGz => ("application/gzip", ".tar.gz"),
            _ => ("application/zip", ".zip")
        };

        return File(stream, contentType, $"{displayName}-{resolvedRef}{extension}");
    }

    [HttpGet("{repoName}/releases")]
    public async Task<IActionResult> ListReleases(string repoName)
    {
        var releases = await _releaseService.GetReleasesAsync(repoName);
        return Ok(releases.Select(r => new
        {
            r.Id, r.TagName, r.Title, r.Body, r.Author, r.IsDraft, r.IsPrerelease,
            created_at = r.CreatedAt, published_at = r.PublishedAt,
            assets = r.Assets.Select(a => new { a.Id, a.FileName, a.Size, a.ContentType, a.DownloadCount })
        }));
    }

    [HttpGet("{repoName}/releases/{releaseId}/assets/{assetId}")]
    public async Task<IActionResult> DownloadAsset(string repoName, int releaseId, int assetId)
    {
        var (asset, data) = await _releaseService.GetAssetAsync(assetId);
        if (asset == null || data == null) return NotFound(new { error = "Asset not found" });
        return File(data, asset.ContentType, asset.FileName);
    }

    [HttpGet("templates")]
    public async Task<IActionResult> ListTemplates()
    {
        var templates = await _templateService.GetTemplatesAsync();
        var currentUser = User.Identity?.Name;
        // Filter private templates unless the caller is the owner
        templates = templates.Where(r => !r.IsPrivate || (currentUser != null && r.Owner.Equals(currentUser, StringComparison.OrdinalIgnoreCase))).ToList();

        return Ok(templates.Select(r => new
        {
            r.Id,
            r.Name,
            r.Description,
            r.Owner,
            r.IsPrivate,
            r.DefaultBranch,
            created_at = r.CreatedAt,
            updated_at = r.UpdatedAt
        }));
    }

    [HttpPost("create-from-template")]
    public async Task<IActionResult> CreateFromTemplate([FromBody] CreateFromTemplateRequest request)
    {
        var currentUser = User.Identity?.Name;
        if (string.IsNullOrEmpty(currentUser))
            return Unauthorized(new { error = "Authentication required" });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Repository name is required" });

        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        var repo = await _templateService.CreateFromTemplateAsync(request.TemplateId, currentUser, request.Name, request.Description, request.IsPrivate, projectRoot);

        if (repo == null)
            return BadRequest(new { error = "Failed to create repository from template. Template may not exist or repository name is taken." });

        return Ok(new
        {
            repo.Id,
            repo.Name,
            repo.Description,
            repo.Owner,
            repo.IsPrivate,
            template_id = repo.TemplateRepositoryId,
            created_at = repo.CreatedAt
        });
    }

    public class CreateFromTemplateRequest
    {
        public int TemplateId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public bool IsPrivate { get; set; }
    }

    [HttpGet("{owner}/{repoName}/blame/{branch}/{**path}")]
    public async Task<IActionResult> GetBlame(string owner, string repoName, string branch, string path)
    {
        if (string.IsNullOrEmpty(path))
            return BadRequest(new { error = "File path is required" });

        // Enforce private repo access
        var meta = await _repoService.GetRepositoryAsync(repoName);
        if (meta is { IsPrivate: true })
        {
            var currentUser = User.Identity?.Name;
            if (currentUser == null || !meta.Owner.Equals(currentUser, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = $"Repository '{repoName}' not found" });
        }

        var hunks = _blameService.GetBlame(owner, repoName, branch, path);
        if (!hunks.Any())
            return NotFound(new { error = $"Unable to get blame for '{path}'" });

        return Ok(new
        {
            repository = repoName,
            owner,
            branch,
            path,
            hunks = hunks.Select(h => new
            {
                start_line = h.FinalStartLineNumber + 1,
                line_count = h.LineCount,
                commit_sha = h.CommitSha,
                short_sha = h.ShortSha,
                author_name = h.AuthorName,
                author_email = h.AuthorEmail,
                date = h.Date,
                message = h.MessageSummary,
                lines = h.Lines
            })
        });
    }

    [HttpGet("{owner}/{repoName}/codeowners/{**path}")]
    public async Task<IActionResult> GetCodeOwners(string owner, string repoName, string path, [FromQuery] string? branch = null)
    {
        if (string.IsNullOrEmpty(path))
            return BadRequest(new { error = "File path is required" });

        // Enforce private repo access
        var meta = await _repoService.GetRepositoryAsync(repoName);
        if (meta is { IsPrivate: true })
        {
            var currentUser = User.Identity?.Name;
            if (currentUser == null || !meta.Owner.Equals(currentUser, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = $"Repository '{repoName}' not found" });
        }

        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        var repoPath = Path.Combine(projectRoot, repoName);
        if (!Repository.IsValid(repoPath))
        {
            repoPath = Path.Combine(projectRoot, repoName + ".git");
            if (!Repository.IsValid(repoPath))
                return NotFound(new { error = $"Repository '{repoName}' not found" });
        }

        var targetBranch = branch ?? "main";
        try
        {
            using var repo = new Repository(repoPath);
            var branchRef = repo.Branches[targetBranch] ?? repo.Branches["main"] ?? repo.Branches["master"] ?? repo.Head;
            if (branchRef != null)
                targetBranch = branchRef.FriendlyName;
        }
        catch { }

        var owners = _codeOwnersService.GetCodeOwnersForFile(repoPath, targetBranch, path);

        return Ok(new
        {
            repository = repoName,
            owner,
            branch = targetBranch,
            path,
            owners
        });
    }

}
