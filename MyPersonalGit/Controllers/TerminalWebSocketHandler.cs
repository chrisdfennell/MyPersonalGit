using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

/// <summary>
/// WebSocket middleware that spawns a shell process (cmd.exe on Windows, bash on Linux)
/// scoped to a repository directory and pipes stdin/stdout/stderr between the WebSocket and process.
/// </summary>
public static class TerminalWebSocketHandler
{
    public static void MapTerminalWebSocket(this WebApplication app)
    {
        app.Map("/ws/terminal/{repoName}", async (HttpContext context, string repoName) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("WebSocket connection expected.");
                return;
            }

            // Verify user is authenticated via cookie session
            var authService = context.RequestServices.GetRequiredService<IAuthService>();
            var sessionCookie = context.Request.Cookies["session"];
            if (string.IsNullOrEmpty(sessionCookie))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }

            var user = await authService.GetUserBySessionAsync(sessionCookie);
            if (user == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
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

            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            await HandleTerminalSession(ws, repoPath, context.RequestAborted);
        });
    }

    private static async Task HandleTerminalSession(WebSocket ws, string workingDirectory, CancellationToken cancellationToken)
    {
        var isWindows = OperatingSystem.IsWindows();
        var shellPath = isWindows ? "cmd.exe" : "/bin/bash";
        var shellArgs = isWindows ? "" : "--login";

        var psi = new ProcessStartInfo
        {
            FileName = shellPath,
            Arguments = shellArgs,
            WorkingDirectory = Directory.Exists(workingDirectory) ? workingDirectory : Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        // Set TERM for better compatibility
        psi.Environment["TERM"] = "xterm-256color";

        Process? process = null;
        try
        {
            process = Process.Start(psi);
            if (process == null)
            {
                await ws.CloseAsync(WebSocketCloseStatus.InternalServerError, "Failed to start shell process.", cancellationToken);
                return;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Task to read from process stdout and send to WebSocket
            var stdoutTask = Task.Run(async () =>
            {
                var buffer = new byte[4096];
                try
                {
                    var stream = process.StandardOutput.BaseStream;
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
                    {
                        if (ws.State == WebSocketState.Open)
                        {
                            await ws.SendAsync(
                                new ArraySegment<byte>(buffer, 0, bytesRead),
                                WebSocketMessageType.Text,
                                true,
                                cts.Token);
                        }
                        else break;
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception) { }
            }, cts.Token);

            // Task to read from process stderr and send to WebSocket
            var stderrTask = Task.Run(async () =>
            {
                var buffer = new byte[4096];
                try
                {
                    var stream = process.StandardError.BaseStream;
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
                    {
                        if (ws.State == WebSocketState.Open)
                        {
                            await ws.SendAsync(
                                new ArraySegment<byte>(buffer, 0, bytesRead),
                                WebSocketMessageType.Text,
                                true,
                                cts.Token);
                        }
                        else break;
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception) { }
            }, cts.Token);

            // Task to read from WebSocket and write to process stdin
            var stdinTask = Task.Run(async () =>
            {
                var buffer = new byte[4096];
                try
                {
                    while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
                    {
                        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }
                        if (result.MessageType == WebSocketMessageType.Text || result.MessageType == WebSocketMessageType.Binary)
                        {
                            var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            if (!process.HasExited)
                            {
                                await process.StandardInput.WriteAsync(text);
                                await process.StandardInput.FlushAsync();
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (WebSocketException) { }
                catch (Exception) { }
            }, cts.Token);

            // Wait for any task to finish (process exit, WebSocket close, or cancellation)
            var processExitTask = process.WaitForExitAsync(cts.Token);
            await Task.WhenAny(stdinTask, processExitTask);

            // Cancel remaining tasks
            cts.Cancel();

            // Clean up
            if (!process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); }
                catch { }
            }

            if (ws.State == WebSocketState.Open)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Terminal session ended.", CancellationToken.None);
                }
                catch { }
            }
        }
        catch (Exception)
        {
            if (ws.State == WebSocketState.Open)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.InternalServerError, "Terminal error.", CancellationToken.None);
                }
                catch { }
            }
        }
        finally
        {
            if (process != null && !process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); }
                catch { }
            }
            process?.Dispose();
        }
    }
}
