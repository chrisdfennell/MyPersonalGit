using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    // Auth & Users
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    // Repositories
    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<RepositoryStar> RepositoryStars => Set<RepositoryStar>();
    public DbSet<RepositoryWatch> RepositoryWatches => Set<RepositoryWatch>();
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
    public DbSet<ReviewComment> ReviewComments => Set<ReviewComment>();

    // Commit Statuses
    public DbSet<CommitStatus> CommitStatuses => Set<CommitStatus>();

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
    public DbSet<WorkflowArtifact> WorkflowArtifacts => Set<WorkflowArtifact>();
    public DbSet<WorkflowSchedule> WorkflowSchedules => Set<WorkflowSchedule>();
    public DbSet<GlobalSecret> GlobalSecrets => Set<GlobalSecret>();

    // Security
    public DbSet<SecurityAdvisory> SecurityAdvisories => Set<SecurityAdvisory>();
    public DbSet<SecurityScan> SecurityScans => Set<SecurityScan>();
    public DbSet<Dependency> Dependencies => Set<Dependency>();
    public DbSet<Vulnerability> Vulnerabilities => Set<Vulnerability>();

    // Branch Protection
    public DbSet<BranchProtectionRule> BranchProtectionRules => Set<BranchProtectionRule>();

    // Tag Protection
    public DbSet<TagProtectionRule> TagProtectionRules => Set<TagProtectionRule>();

    // Issue Dependencies
    public DbSet<IssueDependency> IssueDependencies => Set<IssueDependency>();

    // Repository Labels
    public DbSet<RepositoryLabel> RepositoryLabels => Set<RepositoryLabel>();

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

    // Secrets
    public DbSet<RepositorySecret> RepositorySecrets => Set<RepositorySecret>();

    // Container Registry
    public DbSet<ContainerManifest> ContainerManifests => Set<ContainerManifest>();
    public DbSet<ContainerBlob> ContainerBlobs => Set<ContainerBlob>();
    public DbSet<ContainerUploadSession> ContainerUploadSessions => Set<ContainerUploadSession>();

    // Packages
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<PackageVersion> PackageVersions => Set<PackageVersion>();
    public DbSet<PackageFile> PackageFiles => Set<PackageFile>();

    // Organizations & Teams
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TeamRepository> TeamRepositories => Set<TeamRepository>();

    // Milestones
    public DbSet<Milestone> Milestones => Set<Milestone>();

    // Commit Comments
    public DbSet<CommitComment> CommitComments => Set<CommitComment>();

    // Time Tracking
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    // Discussions
    public DbSet<Discussion> Discussions => Set<Discussion>();
    public DbSet<DiscussionComment> DiscussionComments => Set<DiscussionComment>();

    // Reactions
    public DbSet<Reaction> Reactions => Set<Reaction>();

    // Issue Templates
    public DbSet<IssueTemplate> IssueTemplates => Set<IssueTemplate>();

    // Deploy Keys
    public DbSet<DeployKey> DeployKeys => Set<DeployKey>();

    // GPG Keys
    public DbSet<GpgKey> GpgKeys => Set<GpgKey>();

    // OAuth Providers
    public DbSet<OAuthProviderConfig> OAuthProviderConfigs => Set<OAuthProviderConfig>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

    // Migration Tasks
    public DbSet<MigrationTask> MigrationTasks => Set<MigrationTask>();

    // OAuth2 Provider (authorization server)
    public DbSet<OAuth2App> OAuth2Apps => Set<OAuth2App>();
    public DbSet<OAuth2AuthCode> OAuth2AuthCodes => Set<OAuth2AuthCode>();
    public DbSet<OAuth2Token> OAuth2Tokens => Set<OAuth2Token>();

    // WebAuthn / Passkeys
    public DbSet<WebAuthnCredential> WebAuthnCredentials => Set<WebAuthnCredential>();

    // Pinned Repositories
    public DbSet<PinnedRepository> PinnedRepositories => Set<PinnedRepository>();

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
            e.Property(i => i.Assignees).HasConversion(listStringConverter, listStringComparer);
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

        // --- WorkflowArtifact ---
        modelBuilder.Entity<WorkflowArtifact>(e =>
        {
            e.HasIndex(a => a.WorkflowRunId);
        });

        // --- WorkflowSchedule ---
        modelBuilder.Entity<WorkflowSchedule>(e =>
        {
            e.HasIndex(s => new { s.RepoName, s.WorkflowFileName }).IsUnique();
        });

        // --- GlobalSecret ---
        modelBuilder.Entity<GlobalSecret>(e =>
        {
            e.HasIndex(s => s.Name).IsUnique();
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
            e.Property(r => r.ProtectedFilePatterns).HasConversion(listStringConverter, listStringComparer);
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
            e.Property(t => t.AllowedRoutes).HasConversion(arrayStringConverter, arrayStringComparer);
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

        // --- RepositorySecret ---
        modelBuilder.Entity<RepositorySecret>(e =>
        {
            e.HasIndex(s => new { s.RepoName, s.Name }).IsUnique();
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

        // --- ReviewComment (inline diff comments) ---
        modelBuilder.Entity<ReviewComment>(e =>
        {
            e.HasIndex(c => c.PullRequestId);
            e.HasIndex(c => new { c.PullRequestId, c.FilePath });
        });

        // --- CommitStatus ---
        modelBuilder.Entity<CommitStatus>(e =>
        {
            e.HasIndex(s => new { s.RepoName, s.Sha });
            e.HasIndex(s => new { s.RepoName, s.Sha, s.Context }).IsUnique();
        });

        // --- Organization ---
        modelBuilder.Entity<Organization>(e =>
        {
            e.HasIndex(o => o.Name).IsUnique();
        });

        modelBuilder.Entity<OrganizationMember>(e =>
        {
            e.HasIndex(m => new { m.OrganizationName, m.Username }).IsUnique();
        });

        // --- Team ---
        modelBuilder.Entity<Team>(e =>
        {
            e.HasIndex(t => new { t.OrganizationName, t.Name }).IsUnique();
            e.HasMany(t => t.Members)
                .WithOne()
                .HasForeignKey(m => m.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(t => t.Repositories)
                .WithOne()
                .HasForeignKey(r => r.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Milestone ---
        modelBuilder.Entity<Milestone>(e =>
        {
            e.HasIndex(m => new { m.RepoName, m.Number }).IsUnique();
        });

        // --- CommitComment ---
        modelBuilder.Entity<CommitComment>(e =>
        {
            e.HasIndex(c => new { c.RepoName, c.CommitSha });
        });

        // --- Discussion ---
        modelBuilder.Entity<Discussion>(e =>
        {
            e.HasIndex(d => new { d.RepoName, d.Number }).IsUnique();
            e.HasMany(d => d.Comments)
                .WithOne()
                .HasForeignKey(c => c.DiscussionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DiscussionComment>(e =>
        {
            e.HasIndex(c => c.DiscussionId);
        });

        // --- Reaction ---
        modelBuilder.Entity<Reaction>(e =>
        {
            e.HasIndex(r => new { r.Username, r.Emoji, r.IssueId, r.IssueCommentId, r.PullRequestId, r.ReviewCommentId, r.CommitCommentId, r.DiscussionId, r.DiscussionCommentId });
        });

        // --- IssueTemplate ---
        modelBuilder.Entity<IssueTemplate>(e =>
        {
            e.HasIndex(t => new { t.RepoName, t.Name }).IsUnique();
        });

        // --- DeployKey ---
        modelBuilder.Entity<DeployKey>(e =>
        {
            e.HasIndex(d => new { d.RepositoryId, d.KeyFingerprint }).IsUnique();
            e.HasOne(d => d.Repository)
                .WithMany()
                .HasForeignKey(d => d.RepositoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- GpgKey ---
        modelBuilder.Entity<GpgKey>(e =>
        {
            e.HasIndex(k => new { k.UserId, k.LongKeyId }).IsUnique();
            e.Property(k => k.Emails).HasConversion(listStringConverter, listStringComparer);
            e.HasOne(k => k.User)
                .WithMany()
                .HasForeignKey(k => k.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- OAuthProviderConfig ---
        modelBuilder.Entity<OAuthProviderConfig>(e =>
        {
            e.HasIndex(o => o.ProviderName).IsUnique();
        });

        // --- ExternalLogin ---
        modelBuilder.Entity<ExternalLogin>(e =>
        {
            e.HasIndex(l => new { l.Provider, l.ProviderUserId }).IsUnique();
            e.HasIndex(l => l.UserId);
        });

        // --- MigrationTask ---
        modelBuilder.Entity<MigrationTask>(e =>
        {
            e.HasIndex(m => m.Owner);
            e.HasIndex(m => m.Status);
        });

        // --- TagProtectionRule ---
        modelBuilder.Entity<TagProtectionRule>(e =>
        {
            e.Property(r => r.AllowedUsers).HasConversion(listStringConverter, listStringComparer);
            e.HasIndex(r => r.RepoName);
        });

        // --- IssueDependency ---
        modelBuilder.Entity<IssueDependency>(e =>
        {
            e.HasIndex(d => new { d.RepoName, d.BlockingIssueNumber });
            e.HasIndex(d => new { d.RepoName, d.BlockedIssueNumber });
            e.HasIndex(d => new { d.RepoName, d.BlockingIssueNumber, d.BlockedIssueNumber }).IsUnique();
        });

        // --- RepositoryLabel ---
        modelBuilder.Entity<RepositoryLabel>(e =>
        {
            e.HasIndex(l => new { l.RepoName, l.Name }).IsUnique();
        });

        // --- TimeEntry ---
        modelBuilder.Entity<TimeEntry>(e =>
        {
            e.HasIndex(t => new { t.RepoName, t.IssueNumber });
            e.HasIndex(t => new { t.Username, t.IsRunning });
        });

        // --- OAuth2 Provider ---
        modelBuilder.Entity<OAuth2App>(e =>
        {
            e.HasIndex(a => a.ClientId).IsUnique();
        });

        modelBuilder.Entity<OAuth2AuthCode>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
        });

        modelBuilder.Entity<OAuth2Token>(e =>
        {
            e.HasIndex(t => t.AccessToken).IsUnique();
        });

        // --- WebAuthn ---
        modelBuilder.Entity<WebAuthnCredential>(e =>
        {
            e.HasIndex(c => new { c.Username, c.CredentialId }).IsUnique();
        });

        // --- PinnedRepository ---
        modelBuilder.Entity<PinnedRepository>(e =>
        {
            e.HasIndex(p => new { p.Username, p.RepoName }).IsUnique();
        });
    }
}
