using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;
using MyPersonalGit.Services;
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

// Core infrastructure
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=mypersonalgit.db"));

// Domain services (registered as interfaces for testability)
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
builder.Services.AddScoped<CurrentUserService>();

var app = builder.Build();

// Auto-migrate database and seed default admin on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

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

// Use standard .NET 8 static file middleware
app.UseStaticFiles();
app.UseAntiforgery();

// Rate limiting (before auth so rejected requests don't waste auth work)
app.UseRateLimiter();

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
