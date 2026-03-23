using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

/// <summary>
/// WebSocket middleware that relays JSON-RPC (LSP) messages between the browser
/// and a language server process via stdin/stdout with Content-Length framing.
/// </summary>
public static class LspWebSocketHandler
{
    public static void MapLspWebSocket(this WebApplication app)
    {
        app.Map("/ws/lsp/{repoName}/{language}", async (HttpContext context, string repoName, string language) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("WebSocket connection expected.");
                return;
            }

            // Verify user is authenticated via cookie or query param
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

            if (!LspProcessManager.IsSupported(language))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync($"Unsupported language: {language}");
                return;
            }

            // Resolve the repo working directory
            var adminService = context.RequestServices.GetRequiredService<IAdminService>();
            var config = context.RequestServices.GetRequiredService<IConfiguration>();
            var systemSettings = await adminService.GetSystemSettingsAsync();
            var projectRoot = !string.IsNullOrEmpty(systemSettings.ProjectRoot)
                ? systemSettings.ProjectRoot
                : config["Git:ProjectRoot"] ?? config["Git:ReposPath"] ?? "/repos";
            var repoPath = Path.Combine(projectRoot, repoName);

            // Validate path exists — check bare repo variants
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

            var lspManager = context.RequestServices.GetRequiredService<LspProcessManager>();
            var session = lspManager.GetOrStartSession(repoName, language, user.Username, repoPath);

            if (session?.Process == null || session.Process.HasExited)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Failed to start language server.");
                return;
            }

            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            await HandleLspSession(ws, session, context.RequestAborted);
        });
    }

    /// <summary>
    /// Bidirectional relay between WebSocket (browser) and language server process (stdin/stdout).
    /// Browser sends/receives plain JSON-RPC messages over the WebSocket.
    /// The language server expects/produces Content-Length framed messages on stdin/stdout.
    /// </summary>
    private static async Task HandleLspSession(WebSocket ws, LspSession session, CancellationToken cancellationToken)
    {
        var process = session.Process!;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Task: read Content-Length framed messages from language server stdout, send as WebSocket text messages
        var stdoutTask = Task.Run(async () =>
        {
            try
            {
                var stream = process.StandardOutput.BaseStream;
                var headerBuf = new StringBuilder();

                while (!cts.Token.IsCancellationRequested && !process.HasExited && ws.State == WebSocketState.Open)
                {
                    // Read headers until we get a blank line
                    headerBuf.Clear();
                    int contentLength = -1;

                    while (true)
                    {
                        var line = await ReadLineAsync(stream, cts.Token);
                        if (line == null) return; // EOF
                        if (line.Length == 0) break; // End of headers

                        if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        {
                            var val = line["Content-Length:".Length..].Trim();
                            if (int.TryParse(val, out var len))
                                contentLength = len;
                        }
                    }

                    if (contentLength <= 0) continue;

                    // Read exactly contentLength bytes
                    var bodyBytes = new byte[contentLength];
                    int totalRead = 0;
                    while (totalRead < contentLength)
                    {
                        var bytesRead = await stream.ReadAsync(bodyBytes.AsMemory(totalRead, contentLength - totalRead), cts.Token);
                        if (bytesRead == 0) return; // EOF
                        totalRead += bytesRead;
                    }

                    // Send JSON to browser as a text WebSocket message
                    if (ws.State == WebSocketState.Open)
                    {
                        await ws.SendAsync(
                            new ArraySegment<byte>(bodyBytes, 0, contentLength),
                            WebSocketMessageType.Text,
                            true,
                            cts.Token);
                    }

                    session.Touch();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[LSP stdout] Error: {ex.Message}");
            }
        }, cts.Token);

        // Task: read stderr and log (don't send to browser, just consume to prevent blocking)
        var stderrTask = Task.Run(async () =>
        {
            try
            {
                var reader = process.StandardError;
                while (!cts.Token.IsCancellationRequested && !process.HasExited)
                {
                    var line = await reader.ReadLineAsync(cts.Token);
                    if (line == null) break;
                    // Optionally log LSP stderr for debugging
                    Console.WriteLine($"[LSP stderr] {line}");
                }
            }
            catch (OperationCanceledException) { }
            catch { }
        }, cts.Token);

        // Task: read JSON messages from WebSocket, write Content-Length framed messages to language server stdin
        var stdinTask = Task.Run(async () =>
        {
            var buffer = new byte[65536];
            try
            {
                while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
                {
                    // Read a complete WebSocket message (may span multiple frames)
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                            return;
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    var jsonBytes = ms.ToArray();
                    if (jsonBytes.Length == 0) continue;

                    // Write Content-Length framed message to stdin
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
                    finally
                    {
                        session.WriteLock.Release();
                    }

                    session.Touch();
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[LSP stdin] Error: {ex.Message}");
            }
        }, cts.Token);

        // Wait for either the process to exit or the WebSocket to close
        var processExitTask = process.WaitForExitAsync(cts.Token);
        await Task.WhenAny(stdinTask, processExitTask);

        // Cancel remaining tasks
        await cts.CancelAsync();

        // Close WebSocket gracefully if still open
        if (ws.State == WebSocketState.Open)
        {
            try
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "LSP session ended.", CancellationToken.None);
            }
            catch { }
        }
    }

    /// <summary>
    /// Read a single line (terminated by \r\n or \n) from a stream.
    /// Returns null on EOF, empty string for blank lines.
    /// </summary>
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
                // Strip trailing \r if present
                if (sb.Length > 0 && sb[sb.Length - 1] == '\r')
                    sb.Length--;
                return sb.ToString();
            }
            sb.Append(c);
        }
    }
}
