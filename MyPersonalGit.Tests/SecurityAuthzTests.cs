using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MyPersonalGit.Controllers;
using MyPersonalGit.Data;
using MyPersonalGit.Models;
using MyPersonalGit.Services;
using NSubstitute;

namespace MyPersonalGit.Tests;

/// <summary>
/// Regression tests for the round-4 security hardening: Web IDE WebSocket repo
/// authorization, LFS oid validation, and the LFS-upload-as-write classification.
/// </summary>
public class WebSocketRepoAuthzTests
{
    private static User MakeUser(string name, bool admin = false) =>
        new() { Username = name, Email = $"{name}@x.test", PasswordHash = "x", IsAdmin = admin };

    private static (HttpContext ctx, IRepositoryService repo, ICollaboratorService collab) Build()
    {
        var repo = Substitute.For<IRepositoryService>();
        var collab = Substitute.For<ICollaboratorService>();
        var services = new ServiceCollection();
        services.AddSingleton(repo);
        services.AddSingleton(collab);
        var ctx = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        return (ctx, repo, collab);
    }

    [Fact]
    public async Task UnknownRepo_Returns404_NotAuthorized()
    {
        var (ctx, repo, _) = Build();
        repo.GetRepositoryAsync("ghost").Returns((Repository?)null);

        var ok = await WebSocketRepoAuthz.AuthorizeAsync(ctx, MakeUser("alice"), "ghost", requireWrite: true);

        Assert.False(ok);
        Assert.Equal(StatusCodes.Status404NotFound, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Admin_Bypasses_AllChecks()
    {
        var (ctx, repo, _) = Build();
        repo.GetRepositoryAsync("r").Returns(new Repository { Name = "r", Owner = "bob", IsPrivate = true });

        var ok = await WebSocketRepoAuthz.AuthorizeAsync(ctx, MakeUser("root", admin: true), "r", requireWrite: true);

        Assert.True(ok);
    }

    [Fact]
    public async Task Owner_GetsWriteAccess()
    {
        var (ctx, repo, _) = Build();
        repo.GetRepositoryAsync("r").Returns(new Repository { Name = "r", Owner = "alice", IsPrivate = true });

        var ok = await WebSocketRepoAuthz.AuthorizeAsync(ctx, MakeUser("alice"), "r", requireWrite: true);

        Assert.True(ok);
    }

    [Fact]
    public async Task WriteCollaborator_GetsWriteAccess()
    {
        var (ctx, repo, collab) = Build();
        repo.GetRepositoryAsync("r").Returns(new Repository { Name = "r", Owner = "bob", IsPrivate = false });
        collab.HasPermissionAsync("r", "alice", CollaboratorPermission.Write).Returns(true);

        var ok = await WebSocketRepoAuthz.AuthorizeAsync(ctx, MakeUser("alice"), "r", requireWrite: true);

        Assert.True(ok);
    }

    [Fact]
    public async Task NonCollaborator_DeniedWrite_OnPublicRepo_Returns403()
    {
        var (ctx, repo, collab) = Build();
        repo.GetRepositoryAsync("r").Returns(new Repository { Name = "r", Owner = "bob", IsPrivate = false });
        collab.HasPermissionAsync("r", "alice", CollaboratorPermission.Write).Returns(false);

        var ok = await WebSocketRepoAuthz.AuthorizeAsync(ctx, MakeUser("alice"), "r", requireWrite: true);

        Assert.False(ok);
        Assert.Equal(StatusCodes.Status403Forbidden, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task NonCollaborator_DeniedWrite_OnPrivateRepo_Returns404_NoDisclosure()
    {
        var (ctx, repo, collab) = Build();
        repo.GetRepositoryAsync("r").Returns(new Repository { Name = "r", Owner = "bob", IsPrivate = true });
        collab.HasPermissionAsync("r", "alice", CollaboratorPermission.Write).Returns(false);

        var ok = await WebSocketRepoAuthz.AuthorizeAsync(ctx, MakeUser("alice"), "r", requireWrite: true);

        Assert.False(ok);
        Assert.Equal(StatusCodes.Status404NotFound, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task ReadGate_PublicRepo_AllowsAnyUser()
    {
        var (ctx, repo, collab) = Build();
        repo.GetRepositoryAsync("r").Returns(new Repository { Name = "r", Owner = "bob", IsPrivate = false });
        collab.HasPermissionAsync("r", "alice", CollaboratorPermission.Read).Returns(false);

        var ok = await WebSocketRepoAuthz.AuthorizeAsync(ctx, MakeUser("alice"), "r", requireWrite: false);

        Assert.True(ok);
    }

    [Fact]
    public async Task ReadGate_PrivateRepo_NonCollaborator_Returns404()
    {
        var (ctx, repo, collab) = Build();
        repo.GetRepositoryAsync("r").Returns(new Repository { Name = "r", Owner = "bob", IsPrivate = true });
        collab.HasPermissionAsync("r", "alice", CollaboratorPermission.Read).Returns(false);

        var ok = await WebSocketRepoAuthz.AuthorizeAsync(ctx, MakeUser("alice"), "r", requireWrite: false);

        Assert.False(ok);
        Assert.Equal(StatusCodes.Status404NotFound, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task ReadGate_PrivateRepo_ReadCollaborator_Allowed()
    {
        var (ctx, repo, collab) = Build();
        repo.GetRepositoryAsync("r").Returns(new Repository { Name = "r", Owner = "bob", IsPrivate = true });
        collab.HasPermissionAsync("r", "alice", CollaboratorPermission.Read).Returns(true);

        var ok = await WebSocketRepoAuthz.AuthorizeAsync(ctx, MakeUser("alice"), "r", requireWrite: false);

        Assert.True(ok);
    }
}

public class LfsOidValidationTests
{
    [Theory]
    [InlineData("3f1e2d4c5b6a79880123456789abcdef0123456789abcdef0123456789abcdef")] // 64 hex
    public void ValidOid_IsAccepted(string oid) => Assert.True(LfsController.IsValidLfsOid(oid));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]                                                              // too short (would crash [..2])
    [InlineData("../../etc/passwd")]                                                 // traversal
    [InlineData("3f1e2d4c5b6a79880123456789abcdef0123456789abcdef0123456789abcdeZ")] // non-hex char
    [InlineData("3F1E2D4C5B6A79880123456789ABCDEF0123456789ABCDEF0123456789ABCDEF")] // uppercase (LFS oids are lowercase)
    public void InvalidOid_IsRejected(string? oid) => Assert.False(LfsController.IsValidLfsOid(oid));
}

public class LfsReadWriteClassificationTests
{
    private static HttpRequest Req(string method, string path)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = method;
        ctx.Request.Path = path;
        return ctx.Request;
    }

    [Fact]
    public void GitReceivePack_IsWrite() =>
        Assert.False(BasicAuthMiddleware.IsReadOperation(Req("POST", "/git/r.git/git-receive-pack")));

    [Fact]
    public void LfsObjectUpload_Put_IsWrite() =>
        Assert.False(BasicAuthMiddleware.IsReadOperation(Req("PUT", "/git/r.git/info/lfs/objects/abc123")));

    [Fact]
    public void LfsObjectDownload_Get_IsRead() =>
        Assert.True(BasicAuthMiddleware.IsReadOperation(Req("GET", "/git/r.git/info/lfs/objects/abc123")));

    [Fact]
    public void UploadPackInfoRefs_IsRead() =>
        Assert.True(BasicAuthMiddleware.IsReadOperation(Req("GET", "/git/r.git/info/refs")));
}
