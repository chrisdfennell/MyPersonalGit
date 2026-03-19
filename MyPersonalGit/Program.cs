using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;
using MyPersonalGit.Services;
using MyPersonalGit.Services.SshServer;
using MyPersonalGit.Components;
using MyPersonalGit.Data;

var builder = WebApplication.CreateBuilder(args);

// Remove request body size limit so large git pushes can succeed
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null; // unlimited
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = null; // unlimited
    });

builder.Services.AddControllers();

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
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SystemSettings"" ADD COLUMN ""SspiEnabled"" INTEGER NOT NULL DEFAULT 0;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SystemSettings"" ADD COLUMN ""AGitFlowEnabled"" INTEGER NOT NULL DEFAULT 1;"); } catch { }

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
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Fixed: UseExceptionHandler in .NET 8 does not support 'createScopeForStatusCodePages'
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Localization — supported cultures for i18n
var supportedCultures = new[] { "en", "es", "fr", "de", "ja", "zh", "pt", "ko" };
app.UseRequestLocalization(options =>
{
    options.SetDefaultCulture("en");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
    options.ApplyCurrentCultureToResponseHeaders = true;
});

// Use standard .NET 8 static file middleware
app.UseStaticFiles();
app.UseAntiforgery();

// Static site hosting for repository Pages
app.UsePages();

// Rate limiting (before auth so rejected requests don't waste auth work)
app.UseRateLimiter();

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

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
