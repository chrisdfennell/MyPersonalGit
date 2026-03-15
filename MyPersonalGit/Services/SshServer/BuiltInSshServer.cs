using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Data;

namespace MyPersonalGit.Services.SshServer;

/// <summary>
/// Background service that runs a built-in SSH server for Git operations.
/// Eliminates the need for an external OpenSSH installation and authorized_keys management.
/// Supports ECDSA host key, ECDH key exchange, and public key authentication (RSA + ECDSA).
/// </summary>
public sealed class BuiltInSshServer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<BuiltInSshServer> _logger;
    private TcpListener? _listener;
    private ECDsa? _hostKey;
    private byte[]? _hostKeyBlob;
    private readonly string _dataDir;

    public BuiltInSshServer(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<BuiltInSshServer> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
        _dataDir = config["Ssh:DataDir"] ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".mypersonalgit", "ssh");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait briefly for the app to finish starting before checking settings
        await Task.Delay(2000, stoppingToken);

        int port;
        var dbFactory = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        using (var db = dbFactory.CreateDbContext())
        {
            var settings = await db.SystemSettings.FirstOrDefaultAsync(stoppingToken);
            if (settings is { EnableBuiltInSshServer: true })
            {
                port = settings.SshServerPort > 0 ? settings.SshServerPort : 2222;
                LoadOrGenerateHostKey();
                await RunListener(port, stoppingToken);
                return;
            }
        }

        _logger.LogInformation("Built-in SSH server is disabled — polling for setting changes");

        // Poll for setting changes every 30 seconds
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            using var db = dbFactory.CreateDbContext();
            var s = await db.SystemSettings.FirstOrDefaultAsync(stoppingToken);
            if (s is { EnableBuiltInSshServer: true })
            {
                port = s.SshServerPort > 0 ? s.SshServerPort : 2222;
                LoadOrGenerateHostKey();
                await RunListener(port, stoppingToken);
                return;
            }
        }
    }

    private async Task RunListener(int port, CancellationToken stoppingToken)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        try
        {
            _listener.Start();
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "Failed to start SSH server on port {Port}. Is the port in use?", port);
            return;
        }

        _logger.LogInformation("Built-in SSH server listening on port {Port}", port);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                _ = HandleClientAsync(client, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error accepting SSH connection");
            }
        }

        _listener.Stop();
        _logger.LogInformation("Built-in SSH server stopped");
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        var remoteEp = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        _logger.LogDebug("SSH connection from {Remote}", remoteEp);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sshAuthService = scope.ServiceProvider.GetRequiredService<ISshAuthService>();
            var repoService = scope.ServiceProvider.GetRequiredService<IRepositoryService>();
            var collaboratorService = scope.ServiceProvider.GetRequiredService<ICollaboratorService>();
            var deployKeyService = scope.ServiceProvider.GetRequiredService<IDeployKeyService>();
            var issueAutoCloseService = scope.ServiceProvider.GetRequiredService<IIssueAutoCloseService>();
            var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();
            var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";

            var session = new SshSession(
                client, _hostKey!, _hostKeyBlob!, projectRoot,
                sshAuthService, repoService, collaboratorService, deployKeyService, issueAutoCloseService,
                workflowService, _logger);

            await session.RunAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SSH session from {Remote} ended with error", remoteEp);
        }
        finally
        {
            client.Dispose();
        }
    }

    private void LoadOrGenerateHostKey()
    {
        if (_hostKey != null) return;

        Directory.CreateDirectory(_dataDir);
        var keyPath = Path.Combine(_dataDir, "ssh_host_ecdsa_key");

        if (File.Exists(keyPath))
        {
            var pem = File.ReadAllBytes(keyPath);
            _hostKey = ECDsa.Create();
            _hostKey.ImportECPrivateKey(pem, out _);
            _logger.LogInformation("Loaded SSH host key from {Path}", keyPath);
        }
        else
        {
            _hostKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var pem = _hostKey.ExportECPrivateKey();
            File.WriteAllBytes(keyPath, pem);
            _logger.LogInformation("Generated new SSH host key at {Path}", keyPath);
        }

        // Build the SSH host key blob: string "ecdsa-sha2-nistp256" + string "nistp256" + string Q
        var parameters = _hostKey.ExportParameters(false);
        var q = new byte[1 + parameters.Q.X!.Length + parameters.Q.Y!.Length];
        q[0] = 0x04; // uncompressed point
        Buffer.BlockCopy(parameters.Q.X, 0, q, 1, parameters.Q.X.Length);
        Buffer.BlockCopy(parameters.Q.Y, 0, q, 1 + parameters.Q.X.Length, parameters.Q.Y.Length);

        using var ms = new MemoryStream();
        SshDataHelper.WriteString(ms, "ecdsa-sha2-nistp256");
        SshDataHelper.WriteString(ms, "nistp256");
        SshDataHelper.WriteBytes(ms, q);
        _hostKeyBlob = ms.ToArray();
    }

    public override void Dispose()
    {
        _listener?.Stop();
        _hostKey?.Dispose();
        base.Dispose();
    }
}
