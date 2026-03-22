using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;
using MyPersonalGit.Services;
using MyPersonalGit.Services.SshServer;
using MyPersonalGit.Components;
using MyPersonalGit.Data;
using MyPersonalGit.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Remove request body size limit so large git pushes can succeed
// Also configure HTTPS if enabled in system settings
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null; // unlimited

    // Try to read TLS settings from the database for HTTPS configuration
    try
    {
        var tlsSettings = TlsBootstrap.ReadTlsSettings(builder.Configuration);
        if (tlsSettings.Enabled)
        {
            var cert = TlsBootstrap.LoadCertificate(tlsSettings);
            if (cert != null)
            {
                options.ListenAnyIP(tlsSettings.HttpsPort, listenOptions =>
                {
                    listenOptions.UseHttps(cert);
                });
                // Keep HTTP on the default port as well
                options.ListenAnyIP(8080);
                Console.WriteLine($"==> HTTPS enabled on port {tlsSettings.HttpsPort} ({tlsSettings.CertSource})");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Failed to configure HTTPS: {ex.Message}");
    }
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = null; // unlimited
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MyPersonalGit API", Version = "v1", Description = "REST API for MyPersonalGit — self-hosted Git server" });
});

// Rate limiting for API endpoints
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global fixed-window policy for /api endpoints: 100 requests/min per IP
    options.AddPolicy("api", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Stricter policy for auth-related endpoints: 10 requests/min per IP
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Core infrastructure — supports SQLite (default) and PostgreSQL
// Priority: environment variables > database.json > appsettings.json
var dbConfigService = new DatabaseConfigService(builder.Configuration);
var dbConfig = dbConfigService.GetCurrentConfig();
var dbProvider = (builder.Configuration["Database:Provider"]
    ?? dbConfig.Provider
    ?? "sqlite").ToLowerInvariant();
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? dbConfig.ConnectionString
    ?? "Data Source=mypersonalgit.db";

builder.Services.AddSingleton<IDatabaseConfigService>(dbConfigService);
builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    if (dbProvider is "postgresql" or "postgres" or "npgsql")
        options.UseNpgsql(connectionString);
    else
        options.UseSqlite(connectionString);
});

// Domain services (registered as interfaces for testability)
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddSingleton<IIssueService, IssueService>();
builder.Services.AddSingleton<IPullRequestService, PullRequestService>();
builder.Services.AddSingleton<IRepositoryService, RepositoryService>();
builder.Services.AddSingleton<ICollaboratorService, CollaboratorService>();
builder.Services.AddSingleton<IWikiService, WikiService>();
builder.Services.AddSingleton<IProjectService, ProjectService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IWorkflowService, WorkflowService>();
builder.Services.AddSingleton<IWebhookDeliveryService, WebhookDeliveryService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IReleaseService, ReleaseService>();
builder.Services.AddSingleton<IActivityService, ActivityService>();
builder.Services.AddSingleton<WorkflowYamlParser>();
builder.Services.AddHostedService<WorkflowRunnerService>();
builder.Services.AddSingleton<ISecurityService, SecurityService>();
builder.Services.AddSingleton<IAdminService, AdminService>();
builder.Services.AddSingleton<IUserProfileService, UserProfileService>();
builder.Services.AddSingleton<IBranchProtectionService, BranchProtectionService>();
builder.Services.AddSingleton<ISnippetService, SnippetService>();
builder.Services.AddSingleton<IMirrorService, MirrorService>();
builder.Services.AddHostedService<MirrorSyncService>();
builder.Services.AddSingleton<IPackageService, PackageService>();
builder.Services.AddSingleton<ISecretsService, SecretsService>();
builder.Services.AddSingleton<ISshAuthService, SshAuthService>();
builder.Services.AddHostedService<AuthorizedKeysSyncService>();
builder.Services.AddHostedService<BuiltInSshServer>();
builder.Services.AddSingleton<ILdapAuthService, LdapAuthService>();
builder.Services.AddSingleton<IArtifactService, ArtifactService>();
builder.Services.AddSingleton<IMarkdownService, MarkdownService>();
builder.Services.AddSingleton<IOrganizationService, OrganizationService>();
builder.Services.AddSingleton<IMilestoneService, MilestoneService>();
builder.Services.AddSingleton<ICommitCommentService, CommitCommentService>();
builder.Services.AddSingleton<IDiscussionService, DiscussionService>();
builder.Services.AddSingleton<IReactionService, ReactionService>();
builder.Services.AddSingleton<IIssueTemplateService, IssueTemplateService>();
builder.Services.AddSingleton<IIssueAutoCloseService, IssueAutoCloseService>();
builder.Services.AddSingleton<IBlameService, BlameService>();
builder.Services.AddSingleton<IArchiveService, ArchiveService>();
builder.Services.AddSingleton<IDeployKeyService, DeployKeyService>();
builder.Services.AddSingleton<IGpgKeyService, GpgKeyService>();
builder.Services.AddSingleton<ITemplateService, TemplateService>();
builder.Services.AddSingleton<ICodeOwnersService, CodeOwnersService>();
builder.Services.AddSingleton<ITwoFactorService, TwoFactorService>();
builder.Services.AddHostedService<WorkflowSchedulerService>();
builder.Services.AddSingleton<IOAuthService, OAuthService>();
builder.Services.AddSingleton<IMigrationService, MigrationService>();
builder.Services.AddSingleton<MigrationChannel>();
builder.Services.AddHostedService<MigrationWorkerService>();
builder.Services.AddSingleton<IConflictResolutionService, ConflictResolutionService>();
builder.Services.AddSingleton<IIssueDependencyService, IssueDependencyService>();
builder.Services.AddSingleton<IRepositoryLabelService, RepositoryLabelService>();
builder.Services.AddSingleton<ITagProtectionService, TagProtectionService>();
builder.Services.AddSingleton<IAutoMergeService, AutoMergeService>();
builder.Services.AddSingleton<ICodeSearchService, CodeSearchService>();
builder.Services.AddSingleton<ITimeTrackingService, TimeTrackingService>();
builder.Services.AddSingleton<IAGitFlowService, AGitFlowService>();
builder.Services.AddSingleton<IWebAuthnService, WebAuthnService>();
builder.Services.AddSingleton<IGitHooksService, GitHooksService>();
builder.Services.AddSingleton<IAutolinkService, AutolinkService>();
builder.Services.AddSingleton<ISavedReplyService, SavedReplyService>();
builder.Services.AddSingleton<IRepoHealthService, RepoHealthService>();
builder.Services.AddSingleton<ISecretScanService, SecretScanService>();
builder.Services.AddSingleton<ICherryPickRevertService, CherryPickRevertService>();
builder.Services.AddSingleton<IRepositoryTrafficService, RepositoryTrafficService>();
builder.Services.AddHostedService<TrafficAggregationService>();
builder.Services.AddSingleton<IEnvironmentService, EnvironmentService>();
builder.Services.AddSingleton<IDependencyUpdateService, DependencyUpdateService>();
builder.Services.AddSingleton<IWebIdeService, WebIdeService>();
builder.Services.AddHostedService<DependencyUpdateSchedulerService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLocalization();
builder.Services.AddScoped<CurrentUserService>();

var app = builder.Build();

// Auto-migrate database and seed default admin on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Ensure all schema additions exist (idempotent — safe to re-run)
    // From 20260317200000: IssueDependencies, RepositoryLabels, TagProtectionRules, BranchProtection.RequireCodeOwnersApproval
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""IssueDependencies"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""RepoName"" TEXT NOT NULL,
            ""BlockingIssueNumber"" INTEGER NOT NULL,
            ""BlockedIssueNumber"" INTEGER NOT NULL,
            ""CreatedBy"" TEXT NOT NULL,
            ""CreatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_IssueDependencies_RepoName_BlockedIssueNumber"" ON ""IssueDependencies"" (""RepoName"", ""BlockedIssueNumber"");");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_IssueDependencies_RepoName_BlockingIssueNumber"" ON ""IssueDependencies"" (""RepoName"", ""BlockingIssueNumber"");");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_IssueDependencies_RepoName_BlockingIssueNumber_BlockedIssueNumber"" ON ""IssueDependencies"" (""RepoName"", ""BlockingIssueNumber"", ""BlockedIssueNumber"");");
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""RepositoryLabels"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""RepoName"" TEXT NOT NULL,
            ""Name"" TEXT NOT NULL,
            ""Color"" TEXT NOT NULL,
            ""Description"" TEXT NULL,
            ""CreatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_RepositoryLabels_RepoName_Name"" ON ""RepositoryLabels"" (""RepoName"", ""Name"");");
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""TagProtectionRules"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""RepoName"" TEXT NOT NULL,
            ""TagPattern"" TEXT NOT NULL,
            ""PreventDeletion"" INTEGER NOT NULL,
            ""PreventForcePush"" INTEGER NOT NULL,
            ""RestrictCreation"" INTEGER NOT NULL,
            ""AllowedUsers"" TEXT NOT NULL,
            ""RequireSignedTags"" INTEGER NOT NULL,
            ""CreatedAt"" TEXT NOT NULL,
            ""UpdatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_TagProtectionRules_RepoName"" ON ""TagProtectionRules"" (""RepoName"");");
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""BranchProtectionRules"" ADD COLUMN ""RequireCodeOwnersApproval"" INTEGER NOT NULL DEFAULT 0;"); } catch { }

    // From 20260317210000: Issue assignees/due date/pin/lock, PR pin/lock
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Issues"" ADD COLUMN ""Assignees"" TEXT NOT NULL DEFAULT '[]';"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Issues"" ADD COLUMN ""DueDate"" TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Issues"" ADD COLUMN ""IsPinned"" INTEGER NOT NULL DEFAULT 0;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Issues"" ADD COLUMN ""IsLocked"" INTEGER NOT NULL DEFAULT 0;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Issues"" ADD COLUMN ""LockReason"" TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""PullRequests"" ADD COLUMN ""IsPinned"" INTEGER NOT NULL DEFAULT 0;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""PullRequests"" ADD COLUMN ""IsLocked"" INTEGER NOT NULL DEFAULT 0;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""PullRequests"" ADD COLUMN ""LockReason"" TEXT NULL;"); } catch { }

    // From 20260318200000: TimeEntries, SystemSettings signing columns
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""TimeEntries"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""RepoName"" TEXT NOT NULL,
            ""IssueNumber"" INTEGER NOT NULL,
            ""Username"" TEXT NOT NULL,
            ""Duration"" TEXT NOT NULL,
            ""StartedAt"" TEXT NULL,
            ""StoppedAt"" TEXT NULL,
            ""IsRunning"" INTEGER NOT NULL,
            ""Note"" TEXT NULL,
            ""CreatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_TimeEntries_RepoName_IssueNumber"" ON ""TimeEntries"" (""RepoName"", ""IssueNumber"");");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_TimeEntries_Username_IsRunning"" ON ""TimeEntries"" (""Username"", ""IsRunning"");");
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SystemSettings"" ADD COLUMN ""SignMergeCommits"" INTEGER NOT NULL DEFAULT 0;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SystemSettings"" ADD COLUMN ""ServerGpgKeyId"" TEXT NOT NULL DEFAULT '';"); } catch { }

    // From 20260322: TLS / HTTPS settings
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SystemSettings"" ADD COLUMN ""EnableHttps"" INTEGER NOT NULL DEFAULT 0;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SystemSettings"" ADD COLUMN ""HttpsPort"" INTEGER NOT NULL DEFAULT 8443;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SystemSettings"" ADD COLUMN ""HttpsExternalPort"" INTEGER NOT NULL DEFAULT 8443;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SystemSettings"" ADD COLUMN ""HttpsRedirect"" INTEGER NOT NULL DEFAULT 0;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SystemSettings"" ADD COLUMN ""TlsCertSource"" TEXT NOT NULL DEFAULT 'none';"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SystemSettings"" ADD COLUMN ""TlsCertPath"" TEXT NOT NULL DEFAULT '';"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SystemSettings"" ADD COLUMN ""TlsKeyPath"" TEXT NOT NULL DEFAULT '';"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SystemSettings"" ADD COLUMN ""TlsPfxPath"" TEXT NOT NULL DEFAULT '';"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SystemSettings"" ADD COLUMN ""TlsPfxPassword"" TEXT NOT NULL DEFAULT '';"); } catch { }

    // From 20260318210000: Route-level scoped access tokens
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""PersonalAccessTokens"" ADD COLUMN ""AllowedRoutes"" TEXT NOT NULL DEFAULT '[]';"); } catch { }

    // From 20260318220000: OAuth2 Provider, WebAuthn, SSPI, AGit Flow
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""OAuth2Apps"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""ClientId"" TEXT NOT NULL,
            ""ClientSecret"" TEXT NOT NULL,
            ""Name"" TEXT NOT NULL,
            ""Description"" TEXT NULL,
            ""RedirectUri"" TEXT NOT NULL,
            ""Owner"" TEXT NOT NULL,
            ""IsConfidential"" INTEGER NOT NULL DEFAULT 1,
            ""CreatedAt"" TEXT NOT NULL,
            ""UpdatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_OAuth2Apps_ClientId"" ON ""OAuth2Apps"" (""ClientId"");");
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""OAuth2AuthCodes"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""Code"" TEXT NOT NULL,
            ""ClientId"" TEXT NOT NULL,
            ""Username"" TEXT NOT NULL,
            ""RedirectUri"" TEXT NOT NULL,
            ""Scope"" TEXT NULL,
            ""CodeChallenge"" TEXT NULL,
            ""CodeChallengeMethod"" TEXT NULL,
            ""ExpiresAt"" TEXT NOT NULL,
            ""Used"" INTEGER NOT NULL DEFAULT 0,
            ""CreatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_OAuth2AuthCodes_Code"" ON ""OAuth2AuthCodes"" (""Code"");");
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""OAuth2Tokens"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""AccessToken"" TEXT NOT NULL,
            ""RefreshToken"" TEXT NULL,
            ""ClientId"" TEXT NOT NULL,
            ""Username"" TEXT NOT NULL,
            ""Scope"" TEXT NULL,
            ""ExpiresAt"" TEXT NOT NULL,
            ""CreatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_OAuth2Tokens_AccessToken"" ON ""OAuth2Tokens"" (""AccessToken"");");
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""WebAuthnCredentials"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""Username"" TEXT NOT NULL,
            ""Name"" TEXT NOT NULL,
            ""CredentialId"" TEXT NOT NULL,
            ""PublicKey"" TEXT NOT NULL,
            ""SignCount"" INTEGER NOT NULL DEFAULT 0,
            ""AaGuid"" TEXT NULL,
            ""IsPlatform"" INTEGER NOT NULL DEFAULT 0,
            ""CreatedAt"" TEXT NOT NULL,
            ""LastUsedAt"" TEXT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_WebAuthnCredentials_Username_CredentialId"" ON ""WebAuthnCredentials"" (""Username"", ""CredentialId"");");
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""PinnedRepositories"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""Username"" TEXT NOT NULL,
            ""RepoName"" TEXT NOT NULL,
            ""SortOrder"" INTEGER NOT NULL DEFAULT 0,
            ""CreatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PinnedRepositories_Username_RepoName"" ON ""PinnedRepositories"" (""Username"", ""RepoName"");");
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SystemSettings"" ADD COLUMN ""SspiEnabled"" INTEGER NOT NULL DEFAULT 0;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SystemSettings"" ADD COLUMN ""AGitFlowEnabled"" INTEGER NOT NULL DEFAULT 1;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""BranchProtectionRules"" ADD COLUMN ""RequireSignedCommits"" INTEGER NOT NULL DEFAULT 0;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""BranchProtectionRules"" ADD COLUMN ""ProtectedFilePatterns"" TEXT NOT NULL DEFAULT '[]';"); } catch { }

    // From 20260320200000: AutolinkPatterns table, External issue tracker columns
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""AutolinkPatterns"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""RepoName"" TEXT NOT NULL,
            ""Prefix"" TEXT NOT NULL,
            ""UrlTemplate"" TEXT NOT NULL,
            ""CreatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_AutolinkPatterns_RepoName_Prefix"" ON ""AutolinkPatterns"" (""RepoName"", ""Prefix"");");
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Repositories"" ADD COLUMN ""ExternalIssueTrackerUrl"" TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Repositories"" ADD COLUMN ""ExternalIssueTrackerPattern"" TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Repositories"" ADD COLUMN ""UseExternalIssueTracker"" INTEGER NOT NULL DEFAULT 0;"); } catch { }

    // From 20260320210000: CI Runners table for distributed runner support
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Runners"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""Name"" TEXT NOT NULL,
            ""Token"" TEXT NOT NULL,
            ""Labels"" TEXT NOT NULL DEFAULT '[]',
            ""IsOnline"" INTEGER NOT NULL DEFAULT 0,
            ""LastHeartbeat"" TEXT NULL,
            ""CreatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Runners_Token"" ON ""Runners"" (""Token"");");

    // From 20260320220000: UserSecrets and OrganizationSecrets tables
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""UserSecrets"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""Username"" TEXT NOT NULL,
            ""Name"" TEXT NOT NULL,
            ""EncryptedValue"" TEXT NOT NULL,
            ""CreatedAt"" TEXT NOT NULL,
            ""UpdatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserSecrets_Username_Name"" ON ""UserSecrets"" (""Username"", ""Name"");");
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""OrganizationSecrets"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""OrganizationName"" TEXT NOT NULL,
            ""Name"" TEXT NOT NULL,
            ""EncryptedValue"" TEXT NOT NULL,
            ""CreatedAt"" TEXT NOT NULL,
            ""UpdatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_OrganizationSecrets_OrganizationName_Name"" ON ""OrganizationSecrets"" (""OrganizationName"", ""Name"");");

    // From 20260320230000: Secret scanning, Cherry-pick/Revert, Traffic, Environments, Dependency Updates, Issue Transfers
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""SecretScanResults"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""RepoName"" TEXT NOT NULL,
            ""CommitSha"" TEXT NOT NULL,
            ""FilePath"" TEXT NOT NULL,
            ""LineNumber"" INTEGER NOT NULL,
            ""SecretType"" TEXT NOT NULL,
            ""MatchSnippet"" TEXT NOT NULL,
            ""State"" INTEGER NOT NULL DEFAULT 0,
            ""ResolvedBy"" TEXT NULL,
            ""DetectedAt"" TEXT NOT NULL,
            ""ResolvedAt"" TEXT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_SecretScanResults_RepoName_State"" ON ""SecretScanResults"" (""RepoName"", ""State"");");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_SecretScanResults_RepoName_CommitSha"" ON ""SecretScanResults"" (""RepoName"", ""CommitSha"");");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""SecretScanPatterns"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""Name"" TEXT NOT NULL,
            ""Pattern"" TEXT NOT NULL,
            ""IsEnabled"" INTEGER NOT NULL DEFAULT 1,
            ""IsBuiltIn"" INTEGER NOT NULL DEFAULT 0
        );");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""DependencyUpdateConfigs"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""RepoName"" TEXT NOT NULL,
            ""Ecosystem"" TEXT NOT NULL,
            ""Schedule"" TEXT NOT NULL DEFAULT 'weekly',
            ""IsEnabled"" INTEGER NOT NULL DEFAULT 1,
            ""Directory"" TEXT NULL,
            ""OpenPRLimit"" INTEGER NOT NULL DEFAULT 5,
            ""LastRunAt"" TEXT NULL,
            ""CreatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_DependencyUpdateConfigs_RepoName_Ecosystem"" ON ""DependencyUpdateConfigs"" (""RepoName"", ""Ecosystem"");");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""DependencyUpdateLogs"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""ConfigId"" INTEGER NOT NULL,
            ""RepoName"" TEXT NOT NULL,
            ""PackageName"" TEXT NOT NULL,
            ""CurrentVersion"" TEXT NOT NULL,
            ""NewVersion"" TEXT NOT NULL,
            ""PullRequestNumber"" INTEGER NULL,
            ""CreatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_DependencyUpdateLogs_RepoName"" ON ""DependencyUpdateLogs"" (""RepoName"");");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""DeploymentEnvironments"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""RepoName"" TEXT NOT NULL,
            ""Name"" TEXT NOT NULL,
            ""Url"" TEXT NULL,
            ""WaitTimerMinutes"" INTEGER NOT NULL DEFAULT 0,
            ""RequireApproval"" INTEGER NOT NULL DEFAULT 0,
            ""RequiredReviewers"" TEXT NOT NULL DEFAULT '[]',
            ""AllowedBranches"" TEXT NOT NULL DEFAULT '[]',
            ""CreatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_DeploymentEnvironments_RepoName_Name"" ON ""DeploymentEnvironments"" (""RepoName"", ""Name"");");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Deployments"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""EnvironmentId"" INTEGER NOT NULL,
            ""RepoName"" TEXT NOT NULL,
            ""EnvironmentName"" TEXT NOT NULL,
            ""WorkflowRunId"" INTEGER NULL,
            ""CommitSha"" TEXT NOT NULL,
            ""Ref"" TEXT NOT NULL,
            ""Creator"" TEXT NOT NULL,
            ""Status"" INTEGER NOT NULL DEFAULT 0,
            ""Description"" TEXT NULL,
            ""CreatedAt"" TEXT NOT NULL,
            ""StartedAt"" TEXT NULL,
            ""CompletedAt"" TEXT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_Deployments_RepoName_EnvironmentName"" ON ""Deployments"" (""RepoName"", ""EnvironmentName"");");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_Deployments_Status"" ON ""Deployments"" (""Status"");");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""DeploymentApprovals"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""DeploymentId"" INTEGER NOT NULL,
            ""Reviewer"" TEXT NOT NULL,
            ""Approved"" INTEGER NOT NULL,
            ""Comment"" TEXT NULL,
            ""CreatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_DeploymentApprovals_DeploymentId"" ON ""DeploymentApprovals"" (""DeploymentId"");");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""RepositoryTrafficEvents"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""RepoName"" TEXT NOT NULL,
            ""EventType"" TEXT NOT NULL,
            ""Referrer"" TEXT NULL,
            ""Path"" TEXT NULL,
            ""IpHash"" TEXT NULL,
            ""Timestamp"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_RepositoryTrafficEvents_RepoName_Timestamp"" ON ""RepositoryTrafficEvents"" (""RepoName"", ""Timestamp"");");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""RepositoryTrafficSummaries"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""RepoName"" TEXT NOT NULL,
            ""Date"" TEXT NOT NULL,
            ""Clones"" INTEGER NOT NULL DEFAULT 0,
            ""UniqueCloners"" INTEGER NOT NULL DEFAULT 0,
            ""PageViews"" INTEGER NOT NULL DEFAULT 0,
            ""UniqueVisitors"" INTEGER NOT NULL DEFAULT 0
        );");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_RepositoryTrafficSummaries_RepoName_Date"" ON ""RepositoryTrafficSummaries"" (""RepoName"", ""Date"");");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""IssueTransfers"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""FromRepoName"" TEXT NOT NULL,
            ""FromIssueNumber"" INTEGER NOT NULL,
            ""ToRepoName"" TEXT NOT NULL,
            ""ToIssueNumber"" INTEGER NOT NULL,
            ""TransferredBy"" TEXT NOT NULL,
            ""TransferredAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_IssueTransfers_FromRepoName_FromIssueNumber"" ON ""IssueTransfers"" (""FromRepoName"", ""FromIssueNumber"");");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_IssueTransfers_ToRepoName_ToIssueNumber"" ON ""IssueTransfers"" (""ToRepoName"", ""ToIssueNumber"");");

    // SavedReplies table
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""SavedReplies"" (
            ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            ""Username"" TEXT NOT NULL,
            ""Title"" TEXT NOT NULL,
            ""Body"" TEXT NOT NULL,
            ""CreatedAt"" TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_SavedReplies_Username"" ON ""SavedReplies"" (""Username"");");

    // WorkflowJobs.Environment column
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""WorkflowJobs"" ADD COLUMN ""Environment"" TEXT NULL;"); } catch { }

    // One-time fixup: workflow runs stored with stripped ".git" suffix need to match the DB repo name
    try
    {
        var repos = db.Repositories.Select(r => r.Name).ToList();
        foreach (var repo in repos)
        {
            if (!repo.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) continue;
            var stripped = repo[..^4];
            var orphanedRuns = db.WorkflowRuns.Where(r => r.RepoName == stripped).ToList();
            if (orphanedRuns.Any())
            {
                foreach (var run in orphanedRuns)
                    run.RepoName = repo;
                db.SaveChanges();
                Console.WriteLine($"==> Fixed {orphanedRuns.Count} workflow run(s): '{stripped}' -> '{repo}'");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Workflow run fixup failed: {ex.Message}");
    }

    // Seed built-in secret scan patterns
    try
    {
        var secretScanService = app.Services.GetRequiredService<ISecretScanService>();
        secretScanService.EnsureBuiltInPatternsAsync().GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Failed to seed secret scan patterns: {ex.Message}");
    }

    if (!db.Users.Any())
    {
        db.Users.Add(new User
        {
            Username = "admin",
            Email = "admin@localhost",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
            IsAdmin = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        Console.WriteLine("==> Default admin account created (username: admin, password: admin)");
        Console.WriteLine("==> IMPORTANT: Change the default password immediately!");
    }

    // Emergency password reset via environment variable
    // Usage: docker run -e RESET_ADMIN_PASSWORD=newpassword ...
    var resetPassword = Environment.GetEnvironmentVariable("RESET_ADMIN_PASSWORD");
    if (!string.IsNullOrEmpty(resetPassword))
    {
        var adminUser = db.Users.FirstOrDefault(u => u.IsAdmin);
        if (adminUser != null)
        {
            adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(resetPassword, workFactor: 12);
            db.SaveChanges();
            Console.WriteLine($"==> Password reset for admin account '{adminUser.Username}'");
        }
        else
        {
            // Admin was deleted — recreate it
            db.Users.Add(new User
            {
                Username = "admin",
                Email = "admin@localhost",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(resetPassword, workFactor: 12),
                IsAdmin = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
            Console.WriteLine("==> Admin account recreated with provided password");
        }
        Console.WriteLine("==> IMPORTANT: Remove the RESET_ADMIN_PASSWORD env var and restart!");
    }

    // Cleanup: fix orphaned fork records and recalculate fork counts
    try
    {
        var allRepoNames = db.Repositories.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allForks = db.RepositoryForks.ToList();
        var orphanedForks = allForks.Where(f => !allRepoNames.Contains(f.ForkedRepo)).ToList();

        if (orphanedForks.Count > 0)
        {
            db.RepositoryForks.RemoveRange(orphanedForks);
            Console.WriteLine($"==> Cleaned up {orphanedForks.Count} orphaned fork record(s)");
        }

        // Recalculate fork counts from actual fork records (not orphaned ones)
        var remainingForks = allForks.Except(orphanedForks).ToList();
        var forkCountsBySource = remainingForks
            .GroupBy(f => f.OriginalRepo, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        foreach (var repo in db.Repositories)
        {
            var actualCount = forkCountsBySource.GetValueOrDefault(repo.Name, 0);
            if (repo.Forks != actualCount)
            {
                Console.WriteLine($"==> Fixed fork count for {repo.Name}: {repo.Forks} -> {actualCount}");
                repo.Forks = actualCount;
            }
        }

        db.SaveChanges();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Fork cleanup failed: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Fixed: UseExceptionHandler in .NET 8 does not support 'createScopeForStatusCodePages'
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyPersonalGit API v1"));

// Only redirect HTTP to HTTPS if explicitly enabled in settings
try
{
    var redirectTlsSettings = TlsBootstrap.ReadTlsSettings(builder.Configuration);
    if (redirectTlsSettings.Enabled && redirectTlsSettings.HttpsRedirect)
    {
        app.Use(async (context, next) =>
        {
            if (!context.Request.IsHttps)
            {
                var host = context.Request.Host.Host;
                var port = redirectTlsSettings.HttpsExternalPort;
                var path = context.Request.Path + context.Request.QueryString;
                var redirectUrl = $"https://{host}:{port}{path}";
                context.Response.Redirect(redirectUrl, permanent: false);
                return;
            }
            await next();
        });
    }
}
catch { }

// Localization — supported cultures for i18n
var supportedCultures = new[] { "en", "es", "fr", "de", "ja", "zh", "pt", "ko", "ru", "it", "tr" };
app.UseRequestLocalization(options =>
{
    options.SetDefaultCulture("en");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
    options.ApplyCurrentCultureToResponseHeaders = true;
});

// WebSocket support for integrated terminal
app.UseWebSockets();

// Use standard .NET 8 static file middleware
app.UseStaticFiles();
app.UseAntiforgery();

// Static site hosting for repository Pages
app.UsePages();

// Rate limiting (before auth so rejected requests don't waste auth work)
app.UseRateLimiter();
app.UseRateLimitHeaders();

// SSPI / Windows Integrated Authentication
app.UseSspiAuth();

// Container Registry authentication (OCI /v2/*)
app.UseRegistryAuth();

// REST API authentication
app.UseApiAuth();

// Git Smart HTTP (clone/fetch/push)
// NOTE: This uses `git http-backend` under the hood.
app.UseBasicAuthForGit();
app.UseGitHttpBackend();

// Health check endpoint
app.MapGet("/health", async (IDbContextFactory<AppDbContext> dbFactory) =>
{
    try
    {
        using var db = dbFactory.CreateDbContext();
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow, database = "connected" });
    }
    catch
    {
        return Results.Json(new { status = "unhealthy", timestamp = DateTime.UtcNow, database = "disconnected" }, statusCode: 503);
    }
});

// Dynamic sitemap for SEO
app.MapGet("/sitemap.xml", async (IDbContextFactory<AppDbContext> dbFactory, HttpContext ctx) =>
{
    using var db = dbFactory.CreateDbContext();
    var repos = await db.Repositories.Where(r => !r.IsPrivate).ToListAsync();
    var host = $"{ctx.Request.Scheme}://{ctx.Request.Host}";

    var sb = new System.Text.StringBuilder();
    sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
    sb.AppendLine($"  <url><loc>{host}/</loc><changefreq>daily</changefreq></url>");
    sb.AppendLine($"  <url><loc>{host}/explore</loc><changefreq>daily</changefreq></url>");
    foreach (var repo in repos)
    {
        sb.AppendLine($"  <url><loc>{host}/repo/{repo.Name}</loc><lastmod>{repo.CreatedAt:yyyy-MM-dd}</lastmod></url>");
    }
    sb.AppendLine("</urlset>");
    return Results.Content(sb.ToString(), "application/xml");
});

app.MapControllers();

// Integrated terminal WebSocket endpoint
app.MapTerminalWebSocket();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
