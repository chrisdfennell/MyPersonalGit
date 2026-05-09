using MyPersonalGit.Services;
using MyPersonalGit.Components;
using MyPersonalGit.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();

builder.Services.AddSingleton<IssueService>();
builder.Services.AddSingleton<PullRequestService>();
builder.Services.AddSingleton<RepositoryService>();
builder.Services.AddSingleton<WikiService>();
builder.Services.AddSingleton<ProjectService>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<WorkflowService>();
builder.Services.AddSingleton<SecurityService>();
builder.Services.AddSingleton<AdminService>();
builder.Services.AddSingleton<UserProfileService>();
builder.Services.AddSingleton<BranchProtectionService>();
builder.Services.AddScoped<CurrentUserService>();

var app = builder.Build();

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