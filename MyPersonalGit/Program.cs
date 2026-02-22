using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;
using MyPersonalGit.Services;
using MyPersonalGit.Components;
using MyPersonalGit.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB for file uploads
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
