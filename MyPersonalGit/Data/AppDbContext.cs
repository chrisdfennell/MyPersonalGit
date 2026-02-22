using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Auth & Users
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    // Repositories
    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<RepositoryStar> RepositoryStars => Set<RepositoryStar>();
    public DbSet<RepositoryFork> RepositoryForks => Set<RepositoryFork>();
    public DbSet<RepositoryCollaborator> RepositoryCollaborators => Set<RepositoryCollaborator>();

    // Releases
    public DbSet<Release> Releases => Set<Release>();
    public DbSet<ReleaseAsset> ReleaseAssets => Set<ReleaseAsset>();

    // Issues
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueComment> IssueComments => Set<IssueComment>();

    // Pull Requests
    public DbSet<PullRequest> PullRequests => Set<PullRequest>();
    public DbSet<PullRequestReview> PullRequestReviews => Set<PullRequestReview>();

    // Projects
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectColumn> ProjectColumns => Set<ProjectColumn>();
    public DbSet<ProjectCard> ProjectCards => Set<ProjectCard>();

    // Wiki
    public DbSet<WikiPage> WikiPages => Set<WikiPage>();
    public DbSet<WikiPageRevision> WikiPageRevisions => Set<WikiPageRevision>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();

    // Workflows
    public DbSet<WorkflowRun> WorkflowRuns => Set<WorkflowRun>();
    public DbSet<WorkflowJob> WorkflowJobs => Set<WorkflowJob>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<Webhook> Webhooks => Set<Webhook>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    // Security
    public DbSet<SecurityAdvisory> SecurityAdvisories => Set<SecurityAdvisory>();
    public DbSet<SecurityScan> SecurityScans => Set<SecurityScan>();
    public DbSet<Dependency> Dependencies => Set<Dependency>();
    public DbSet<Vulnerability> Vulnerabilities => Set<Vulnerability>();

    // Branch Protection
    public DbSet<BranchProtectionRule> BranchProtectionRules => Set<BranchProtectionRule>();

    // Admin
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Snippets
    public DbSet<Snippet> Snippets => Set<Snippet>();
    public DbSet<SnippetFile> SnippetFiles => Set<SnippetFile>();

    // Repository Mirrors
    public DbSet<RepositoryMirror> RepositoryMirrors => Set<RepositoryMirror>();

    // Git LFS
    public DbSet<LfsObject> LfsObjects => Set<LfsObject>();

    // User Profiles
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserActivity> UserActivities => Set<UserActivity>();
    public DbSet<SshKey> SshKeys => Set<SshKey>();
    public DbSet<PersonalAccessToken> PersonalAccessTokens => Set<PersonalAccessToken>();
    public DbSet<ActiveUserSession> ActiveUserSessions => Set<ActiveUserSession>();
    public DbSet<TwoFactorAuth> TwoFactorAuths => Set<TwoFactorAuth>();

    // Container Registry
    public DbSet<ContainerManifest> ContainerManifests => Set<ContainerManifest>();
    public DbSet<ContainerBlob> ContainerBlobs => Set<ContainerBlob>();
    public DbSet<ContainerUploadSession> ContainerUploadSessions => Set<ContainerUploadSession>();

    // Packages
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<PackageVersion> PackageVersions => Set<PackageVersion>();
    public DbSet<PackageFile> PackageFiles => Set<PackageFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Shared JSON value converters + comparers for List<string> and string[]
        var listStringConverter = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());

        var listStringComparer = new ValueComparer<List<string>>(
            (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
            v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            v => v.ToList());

        var arrayStringConverter = new ValueConverter<string[], string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<string>());

        var arrayStringComparer = new ValueComparer<string[]>(
            (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
            v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            v => v.ToArray());

        // --- User ---
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<UserSession>(e =>
        {
            e.HasIndex(s => s.SessionId).IsUnique();
        });

        // --- Repository ---
        modelBuilder.Entity<Repository>(e =>
        {
            e.HasIndex(r => r.Name).IsUnique();
            e.Property(r => r.Topics).HasConversion(listStringConverter, listStringComparer);
        });

        modelBuilder.Entity<RepositoryStar>(e =>
        {
            e.HasIndex(s => new { s.RepoName, s.Username }).IsUnique();
        });

        modelBuilder.Entity<RepositoryFork>(e =>
        {
            e.HasIndex(f => new { f.OriginalRepo, f.Owner }).IsUnique();
        });

        modelBuilder.Entity<RepositoryCollaborator>(e =>
        {
            e.HasIndex(c => new { c.RepoName, c.Username }).IsUnique();
        });

        // --- Release ---
        modelBuilder.Entity<Release>(e =>
        {
            e.HasMany(r => r.Assets)
                .WithOne()
                .HasForeignKey(a => a.ReleaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Issue ---
        modelBuilder.Entity<Issue>(e =>
        {
            e.HasIndex(i => new { i.RepoName, i.Number }).IsUnique();
            e.Property(i => i.Labels).HasConversion(listStringConverter, listStringComparer);
            e.HasMany(i => i.Comments)
                .WithOne()
                .HasForeignKey(c => c.IssueId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- PullRequest ---
        modelBuilder.Entity<PullRequest>(e =>
        {
            e.HasIndex(p => new { p.RepoName, p.Number }).IsUnique();
            e.Property(p => p.Labels).HasConversion(listStringConverter, listStringComparer);
            e.Property(p => p.Reviewers).HasConversion(listStringConverter, listStringComparer);
            e.HasMany(p => p.Reviews)
                .WithOne()
                .HasForeignKey(r => r.PullRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Comments)
                .WithOne()
                .HasForeignKey(c => c.PullRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Project ---
        modelBuilder.Entity<Project>(e =>
        {
            e.HasMany(p => p.Columns)
                .WithOne()
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectColumn>(e =>
        {
            e.HasMany(c => c.Cards)
                .WithOne()
                .HasForeignKey(c => c.ProjectColumnId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- WikiPage ---
        modelBuilder.Entity<WikiPage>(e =>
        {
            e.HasIndex(w => new { w.RepoName, w.Slug }).IsUnique();
            e.HasMany(w => w.Revisions)
                .WithOne()
                .HasForeignKey(r => r.WikiPageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Notification ---
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasIndex(n => n.Username);
        });

        // --- Workflow ---
        modelBuilder.Entity<WorkflowRun>(e =>
        {
            e.HasMany(r => r.Jobs)
                .WithOne()
                .HasForeignKey(j => j.WorkflowRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowJob>(e =>
        {
            e.HasMany(j => j.Steps)
                .WithOne()
                .HasForeignKey(s => s.WorkflowJobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Webhook>(e =>
        {
            e.Property(w => w.Events).HasConversion(listStringConverter, listStringComparer);
        });

        // --- Security ---
        modelBuilder.Entity<SecurityScan>(e =>
        {
            e.HasMany(s => s.Dependencies)
                .WithOne()
                .HasForeignKey(d => d.SecurityScanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Dependency>(e =>
        {
            e.HasMany(d => d.Vulnerabilities)
                .WithOne()
                .HasForeignKey(v => v.DependencyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- BranchProtection ---
        modelBuilder.Entity<BranchProtectionRule>(e =>
        {
            e.Property(r => r.RequiredStatusChecks).HasConversion(listStringConverter, listStringComparer);
            e.Property(r => r.AllowedPushUsers).HasConversion(listStringConverter, listStringComparer);
        });

        // --- UserProfile ---
        modelBuilder.Entity<UserProfile>(e =>
        {
            e.HasIndex(p => p.Username).IsUnique();
        });

        modelBuilder.Entity<TwoFactorAuth>(e =>
        {
            e.HasIndex(t => t.Username).IsUnique();
            e.Property(t => t.BackupCodes).HasConversion(arrayStringConverter, arrayStringComparer);
        });

        modelBuilder.Entity<PersonalAccessToken>(e =>
        {
            e.HasIndex(t => t.Token).IsUnique();
            e.Property(t => t.Scopes).HasConversion(arrayStringConverter, arrayStringComparer);
        });

        // --- Snippet ---
        modelBuilder.Entity<Snippet>(e =>
        {
            e.HasMany(s => s.Files)
                .WithOne()
                .HasForeignKey(f => f.SnippetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- RepositoryMirror ---
        modelBuilder.Entity<RepositoryMirror>(e =>
        {
            e.HasIndex(m => new { m.RepoName, m.RemoteUrl }).IsUnique();
        });

        // --- LfsObject ---
        modelBuilder.Entity<LfsObject>(e =>
        {
            e.HasIndex(o => new { o.RepoName, o.Oid }).IsUnique();
        });

        // --- Container Registry ---
        modelBuilder.Entity<ContainerManifest>(e =>
        {
            e.HasIndex(m => new { m.RepositoryName, m.Tag }).IsUnique();
            e.HasIndex(m => new { m.RepositoryName, m.Digest });
        });

        modelBuilder.Entity<ContainerBlob>(e =>
        {
            e.HasIndex(b => new { b.RepositoryName, b.Digest }).IsUnique();
        });

        modelBuilder.Entity<ContainerUploadSession>(e =>
        {
            e.HasIndex(s => s.Uuid).IsUnique();
        });

        // --- Packages ---
        modelBuilder.Entity<Package>(e =>
        {
            e.HasIndex(p => new { p.Name, p.Type }).IsUnique();
            e.HasMany(p => p.Versions)
                .WithOne()
                .HasForeignKey(v => v.PackageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PackageVersion>(e =>
        {
            e.HasIndex(v => new { v.PackageId, v.Version }).IsUnique();
            e.HasMany(v => v.Files)
                .WithOne()
                .HasForeignKey(f => f.PackageVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
