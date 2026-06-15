using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Tests;

/// <summary>
/// Hosts the real application — full middleware pipeline, routing, controllers and
/// minimal-API endpoints — via <see cref="WebApplicationFactory{TEntryPoint}"/>.
///
/// Two things make this possible without the SSH server / TLS / SQLite migrations
/// the production host normally runs at startup:
///   1. The <c>MPG_TEST_HOST</c> env var tells Program to skip the relational
///      migration + schema-patch block.
///   2. <see cref="ConfigureWebHost"/> swaps the DbContext factory for an in-memory
///      store and removes all hosted (background) services.
/// </summary>
public class MpgWebApplicationFactory : WebApplicationFactory<Program>
{
    public MpgWebApplicationFactory()
    {
        // Must be set before Program's top-level statements read it.
        Environment.SetEnvironmentVariable("MPG_TEST_HOST", "1");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Avoid the production exception handler / HSTS so failures surface directly.
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            // No port-binding / DB-touching background work during tests.
            services.RemoveAll<IHostedService>();

            // Replace the production SQLite/Postgres factory with an isolated in-memory DB.
            services.RemoveAll(typeof(IDbContextFactory<AppDbContext>));
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            // The app container already holds the SQLite EF services; give InMemory its
            // own internal provider so the two don't collide ("only a single database
            // provider can be registered in a service provider").
            var efServices = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();
            services.AddDbContextFactory<AppDbContext>(o =>
                o.UseInMemoryDatabase("mpg-integration-tests")
                 .UseInternalServiceProvider(efServices));
        });
    }

    /// <summary>
    /// Seeds an admin account so the first-run setup gate (which otherwise redirects
    /// every page/API request to /setup) is satisfied for the test process.
    /// </summary>
    public void EnsureSeeded()
    {
        var factory = Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = factory.CreateDbContext();
        if (!db.Users.Any(u => u.IsAdmin))
        {
            db.Users.Add(new User
            {
                Username = "admin",
                Email = "admin@test.local",
                PasswordHash = "x",
                IsAdmin = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }
    }
}

public class HttpPipelineIntegrationTests : IClassFixture<MpgWebApplicationFactory>
{
    private readonly MpgWebApplicationFactory _factory;

    public HttpPipelineIntegrationTests(MpgWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
    }

    [Fact]
    public async Task Health_Endpoint_ReportsHealthy()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("healthy", await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Swagger_Document_IsServed()
    {
        // Regression: swagger.json used to 500 on a Swashbuckle "conflicting method/path"
        // for the PyPI /simple dual-route. ResolveConflictingActions fixes generation.
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("MyPersonalGit API", await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Sitemap_IsServedAsXml()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/sitemap.xml");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("urlset", await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ApiRequest_WithoutAuthorizationHeader_Returns401()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/repos");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        Assert.Contains("Authorization", await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ApiResponse_CarriesRateLimitHeaders()
    {
        // Exercises RateLimitHeadersMiddleware across the real pipeline — these headers
        // are written via OnStarting even though ApiAuth writes the 401 body first.
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/repos");
        Assert.True(resp.Headers.Contains("X-RateLimit-Limit"));
        Assert.True(resp.Headers.Contains("X-RateLimit-Reset"));
    }

    [Fact]
    public async Task ApiRequest_WithInvalidToken_Returns401()
    {
        var client = _factory.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/repos");
        req.Headers.Add("Authorization", "Bearer mypg_not_a_real_token");
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        Assert.Contains("Invalid token", await resp.Content.ReadAsStringAsync());
    }
}
