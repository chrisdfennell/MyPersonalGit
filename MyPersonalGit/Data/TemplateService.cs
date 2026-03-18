using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;
using Repository = MyPersonalGit.Models.Repository;

namespace MyPersonalGit.Data;

public interface ITemplateService
{
    Task<List<Repository>> GetTemplatesAsync();
    Task<Repository?> CreateFromTemplateAsync(int templateRepoId, string newOwner, string newName, string? description, bool isPrivate, string projectRoot);
}

public class TemplateService : ITemplateService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<TemplateService> _logger;
    private readonly IActivityService _activityService;

    public TemplateService(IDbContextFactory<AppDbContext> dbFactory, ILogger<TemplateService> logger, IActivityService activityService)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _activityService = activityService;
    }

    public async Task<List<Repository>> GetTemplatesAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Repositories.Where(r => r.IsTemplate).ToListAsync();
    }

    public async Task<Repository?> CreateFromTemplateAsync(int templateRepoId, string newOwner, string newName, string? description, bool isPrivate, string projectRoot)
    {
        using var db = _dbFactory.CreateDbContext();

        var template = await db.Repositories.FirstOrDefaultAsync(r => r.Id == templateRepoId && r.IsTemplate);
        if (template == null)
            return null;

        // Check if repo already exists
        var repoFolderName = newName.EndsWith(".git") ? newName : $"{newName}.git";
        if (await db.Repositories.AnyAsync(r => r.Name.ToLower() == repoFolderName.ToLower()))
            return null;

        // Find the template repo on disk
        var templatePath = Path.Combine(projectRoot, template.Name);
        if (!LibGit2Sharp.Repository.IsValid(templatePath))
        {
            templatePath = Path.Combine(projectRoot, template.Name + ".git");
            if (!LibGit2Sharp.Repository.IsValid(templatePath))
                return null;
        }

        var newRepoPath = Path.Combine(projectRoot, repoFolderName);
        if (Directory.Exists(newRepoPath))
            return null;

        // Initialize a new bare repository
        LibGit2Sharp.Repository.Init(newRepoPath, isBare: true);

        try
        {
            // Read all files from the template repo's HEAD and create a fresh initial commit
            using var templateRepo = new LibGit2Sharp.Repository(templatePath);
            var headCommit = templateRepo.Head?.Tip;

            if (headCommit != null)
            {
                using var newRepo = new LibGit2Sharp.Repository(newRepoPath);
                var author = new Signature(newOwner, $"{newOwner}@localhost", DateTimeOffset.Now);
                var treeDef = new TreeDefinition();

                // Recursively add all blobs from the template's tree
                AddTreeEntries(templateRepo, treeDef, headCommit.Tree, "");

                var tree = newRepo.ObjectDatabase.CreateTree(treeDef);
                var commit = newRepo.ObjectDatabase.CreateCommit(
                    author, author,
                    $"Initial commit (from template {template.Name})",
                    tree,
                    Array.Empty<Commit>(),
                    true);

                // Determine default branch name
                var branchName = template.DefaultBranch ?? "main";
                newRepo.Branches.Add(branchName, commit);
                newRepo.Refs.UpdateTarget("HEAD", $"refs/heads/{branchName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy template content from {Template} to {New}", template.Name, repoFolderName);
            // Clean up the partially created repo
            try { Directory.Delete(newRepoPath, true); } catch { }
            return null;
        }

        // Create DB record
        var newRepo2 = new Repository
        {
            Name = repoFolderName,
            Owner = newOwner,
            Description = description ?? template.Description,
            IsPrivate = isPrivate,
            TemplateRepositoryId = template.Id,
            DefaultBranch = template.DefaultBranch ?? "main",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Repositories.Add(newRepo2);
        await db.SaveChangesAsync();

        // Copy repository labels from template
        try
        {
            var templateLabels = await db.RepositoryLabels
                .Where(l => l.RepoName == template.Name)
                .ToListAsync();

            foreach (var label in templateLabels)
            {
                db.RepositoryLabels.Add(new RepositoryLabel
                {
                    RepoName = repoFolderName,
                    Name = label.Name,
                    Color = label.Color,
                    Description = label.Description,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Copy issue templates from template
            var issueTemplates = await db.IssueTemplates
                .Where(t => t.RepoName == template.Name)
                .ToListAsync();

            foreach (var tmpl in issueTemplates)
            {
                db.IssueTemplates.Add(new IssueTemplate
                {
                    RepoName = repoFolderName,
                    Name = tmpl.Name,
                    Description = tmpl.Description,
                    Body = tmpl.Body,
                    Labels = tmpl.Labels,
                    SortOrder = tmpl.SortOrder,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Copy branch protection rules from template
            var branchRules = await db.BranchProtectionRules
                .Where(r => r.RepoName == template.Name)
                .ToListAsync();

            foreach (var rule in branchRules)
            {
                db.BranchProtectionRules.Add(new BranchProtectionRule
                {
                    RepoName = repoFolderName,
                    BranchPattern = rule.BranchPattern,
                    RequirePullRequest = rule.RequirePullRequest,
                    RequiredApprovals = rule.RequiredApprovals,
                    RequireStatusChecks = rule.RequireStatusChecks,
                    RequiredStatusChecks = new List<string>(rule.RequiredStatusChecks),
                    PreventForcePush = rule.PreventForcePush,
                    PreventDeletion = rule.PreventDeletion,
                    RequireLinearHistory = rule.RequireLinearHistory,
                    RestrictPushes = rule.RestrictPushes,
                    AllowedPushUsers = new List<string>(rule.AllowedPushUsers),
                    RequireCodeOwnersApproval = rule.RequireCodeOwnersApproval,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to copy template metadata (labels/templates/rules) from {Template} to {New}", template.Name, repoFolderName);
        }

        _logger.LogInformation("Repository {New} created from template {Template} by {Owner}", repoFolderName, template.Name, newOwner);

        await _activityService.RecordActivityAsync(newOwner, "created_repo", repoFolderName, $"{newOwner} created {repoFolderName} from template {template.Name}", $"/repo/{newName}");

        return newRepo2;
    }

    private void AddTreeEntries(LibGit2Sharp.Repository sourceRepo, TreeDefinition treeDef, Tree tree, string prefix)
    {
        foreach (var entry in tree)
        {
            var entryPath = string.IsNullOrEmpty(prefix) ? entry.Name : $"{prefix}/{entry.Name}";

            if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                var blob = (Blob)entry.Target;
                treeDef.Add(entryPath, blob, entry.Mode);
            }
            else if (entry.TargetType == TreeEntryTargetType.Tree)
            {
                AddTreeEntries(sourceRepo, treeDef, (Tree)entry.Target, entryPath);
            }
        }
    }
}
