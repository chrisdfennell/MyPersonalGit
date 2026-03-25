using System.Net.WebSockets;
using System.Text;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

/// <summary>
/// WebSocket middleware that relays DAP (Debug Adapter Protocol) messages between
/// the browser and a debug adapter process via stdin/stdout with Content-Length framing.
/// Identical wire format to LSP — reuses the same relay pattern.
/// </summary>
public static class DapWebSocketHandler
{
    public static void MapDapWebSocket(this WebApplication app)
    {
        app.Map("/ws/dap/{repoName}", async (HttpContext context, string repoName) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("WebSocket connection expected.");
                return;
            }

            var language = context.Request.Query["language"].FirstOrDefault() ?? "python";
            var branch = context.Request.Query["branch"].FirstOrDefault() ?? "main";

            var authService = context.RequestServices.GetRequiredService<IAuthService>();
            var sessionId = context.Request.Cookies["session"];
            if (string.IsNullOrEmpty(sessionId))
                sessionId = context.Request.Query["session"].FirstOrDefault();
            if (string.IsNullOrEmpty(sessionId))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }

            var user = await authService.GetUserBySessionAsync(sessionId);
            if (user == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }

            if (!DapSessionManager.IsSupported(language))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync($"Unsupported debug language: {language}");
                return;
            }

            var adminService = context.RequestServices.GetRequiredService<IAdminService>();
            var config = context.RequestServices.GetRequiredService<IConfiguration>();
            var systemSettings = await adminService.GetSystemSettingsAsync();
            var projectRoot = !string.IsNullOrEmpty(systemSettings.ProjectRoot)
                ? systemSettings.ProjectRoot
                : config["Git:ProjectRoot"] ?? config["Git:ReposPath"] ?? "/repos";
            var repoPath = Path.Combine(projectRoot, repoName);

            if (!Directory.Exists(repoPath))
            {
                var dotGit = repoPath + ".git";
                if (Directory.Exists(dotGit))
                    repoPath = dotGit;
                else
                {
                    var nested = Path.Combine(repoPath, repoName + ".git");
                    if (Directory.Exists(nested))
                        repoPath = nested;
                }
            }

            // Accept WebSocket FIRST — the session startup (dotnet build etc.) can take
            // several minutes on slower hardware and the client will time out if we block.
            using var ws = await context.WebSockets.AcceptWebSocketAsync();

            var dapManager = context.RequestServices.GetRequiredService<DapSessionManager>();
            var sessionTask = Task.Run(() => dapManager.GetOrStartSession(repoName, language, user.Username, repoPath, branch));

            // Send periodic status messages while building so the client doesn't time out
            var elapsed = 0;
            while (!sessionTask.IsCompleted)
            {
                if (ws.State != WebSocketState.Open) return;
                var statusMsg = System.Text.Json.JsonSerializer.Serialize(new { type = "status", message = $"Building project... ({elapsed}s)" });
                await ws.SendAsync(Encoding.UTF8.GetBytes(statusMsg), WebSocketMessageType.Text, true, context.RequestAborted);
                await Task.WhenAny(sessionTask, Task.Delay(5000, context.RequestAborted));
                elapsed += 5;
            }

            var session = sessionTask.Result;

            if (session?.Process == null || session.Process.HasExited)
            {
                var errMsg = System.Text.Json.JsonSerializer.Serialize(new { type = "error", message = "Failed to start debug adapter." });
                await ws.SendAsync(Encoding.UTF8.GetBytes(errMsg), WebSocketMessageType.Text, true, context.RequestAborted);
                await ws.CloseAsync(WebSocketCloseStatus.InternalServerError, "Adapter failed", CancellationToken.None);
                return;
            }

            // Send worktree path as init message
            var initMsg = System.Text.Json.JsonSerializer.Serialize(new { type = "init", rootUri = "file://" + session.WorkTree!.Replace('\\', '/') });
            var initBytes = Encoding.UTF8.GetBytes(initMsg);
            await ws.SendAsync(new ArraySegment<byte>(initBytes), WebSocketMessageType.Text, true, context.RequestAborted);

            await HandleDapSession(ws, session, context.RequestAborted);
        });
    }

    /// <summary>
    /// Bidirectional relay — identical to LspWebSocketHandler.HandleLspSession.
    /// Content-Length framed stdin/stdout ↔ plain JSON WebSocket messages.
    /// </summary>
    private static async Task HandleDapSession(WebSocket ws, DapSession session, CancellationToken cancellationToken)
    {
        var process = session.Process!;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var stdoutTask = Task.Run(async () =>
        {
            try
            {
                var stream = process.StandardOutput.BaseStream;
                while (!cts.Token.IsCancellationRequested && !process.HasExited && ws.State == WebSocketState.Open)
                {
                    var headerBuf = new StringBuilder();
                    int contentLength = -1;

                    while (true)
                    {
                        var line = await ReadLineAsync(stream, cts.Token);
                        if (line == null) return;
                        if (line.Length == 0) break;
                        if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        {
                            var val = line["Content-Length:".Length..].Trim();
                            if (int.TryParse(val, out var len)) contentLength = len;
                        }
                    }

                    if (contentLength <= 0) continue;

                    var bodyBytes = new byte[contentLength];
                    int totalRead = 0;
                    while (totalRead < contentLength)
                    {
                        var bytesRead = await stream.ReadAsync(bodyBytes.AsMemory(totalRead, contentLength - totalRead), cts.Token);
                        if (bytesRead == 0) return;
                        totalRead += bytesRead;
                    }

                    if (ws.State == WebSocketState.Open)
                        await ws.SendAsync(new ArraySegment<byte>(bodyBytes, 0, contentLength), WebSocketMessageType.Text, true, cts.Token);

                    session.Touch();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Console.WriteLine($"[DAP stdout] Error: {ex.Message}"); }
        }, cts.Token);

        var stderrTask = Task.Run(async () =>
        {
            try
            {
                var reader = process.StandardError;
                while (!cts.Token.IsCancellationRequested && !process.HasExited)
                {
                    var line = await reader.ReadLineAsync(cts.Token);
                    if (line == null) break;
                    Console.WriteLine($"[DAP stderr] {line}");
                }
            }
            catch (OperationCanceledException) { }
            catch { }
        }, cts.Token);

        var stdinTask = Task.Run(async () =>
        {
            var buffer = new byte[65536];
            try
            {
                while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close) return;
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    var jsonBytes = ms.ToArray();
                    if (jsonBytes.Length == 0) continue;

                    var header = $"Content-Length: {jsonBytes.Length}\r\n\r\n";
                    var headerBytes = Encoding.UTF8.GetBytes(header);

                    await session.WriteLock.WaitAsync(cts.Token);
                    try
                    {
                        var stdin = process.StandardInput.BaseStream;
                        await stdin.WriteAsync(headerBytes, cts.Token);
                        await stdin.WriteAsync(jsonBytes, cts.Token);
                        await stdin.FlushAsync(cts.Token);
                    }
                    finally { session.WriteLock.Release(); }

                    session.Touch();
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
            catch (Exception ex) { Console.WriteLine($"[DAP stdin] Error: {ex.Message}"); }
        }, cts.Token);

        var processExitTask = process.WaitForExitAsync(cts.Token);
        await Task.WhenAny(stdinTask, processExitTask);
        await cts.CancelAsync();

        if (ws.State == WebSocketState.Open)
            try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Debug session ended.", CancellationToken.None); } catch { }
    }

    private static async Task<string?> ReadLineAsync(Stream stream, CancellationToken ct)
    {
        var sb = new StringBuilder();
        var buf = new byte[1];
        while (true)
        {
            var bytesRead = await stream.ReadAsync(buf.AsMemory(0, 1), ct);
            if (bytesRead == 0) return sb.Length > 0 ? sb.ToString() : null;
            var c = (char)buf[0];
            if (c == '\n')
            {
                if (sb.Length > 0 && sb[sb.Length - 1] == '\r') sb.Length--;
                return sb.ToString();
            }
            sb.Append(c);
        }
    }
}
