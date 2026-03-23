using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

/// <summary>
/// WebSocket handler that runs one-shot build/test commands in a repo directory
/// and streams structured JSON output (stdout, stderr, exit code) to the browser.
/// </summary>
public static class TaskRunnerWebSocketHandler
{
    public static void MapTaskRunnerWebSocket(this WebApplication app)
    {
        app.Map("/ws/taskrunner/{repoName}", async (HttpContext context, string repoName) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("WebSocket connection expected.");
                return;
            }

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

            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            await HandleTaskSession(ws, repoPath, context.RequestAborted);
        });
    }

    private static async Task HandleTaskSession(WebSocket ws, string workingDirectory, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        // Wait for the command message: { "command": "dotnet build" }
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
        if (result.MessageType == WebSocketMessageType.Close) return;

        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        string? command;
        try
        {
            using var doc = JsonDocument.Parse(json);
            command = doc.RootElement.GetProperty("command").GetString();
        }
        catch
        {
            await SendJsonMessage(ws, "stderr", "Invalid command message.", 0, cancellationToken);
            await SendJsonMessage(ws, "exit", "", 1, cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            await SendJsonMessage(ws, "exit", "", 1, cancellationToken);
            return;
        }

        var isWindows = OperatingSystem.IsWindows();
        var psi = new ProcessStartInfo
        {
            FileName = isWindows ? "cmd.exe" : "/bin/bash",
            Arguments = isWindows ? $"/c {command}" : $"-c \"{command.Replace("\"", "\\\"")}\"",
            WorkingDirectory = Directory.Exists(workingDirectory) ? workingDirectory : Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        Process? process = null;
        try
        {
            process = Process.Start(psi);
            if (process == null)
            {
                await SendJsonMessage(ws, "stderr", "Failed to start process.", 0, cancellationToken);
                await SendJsonMessage(ws, "exit", "", 1, cancellationToken);
                return;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Listen for cancel message from client
            var cancelTask = Task.Run(async () =>
            {
                try
                {
                    var buf = new byte[256];
                    while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
                    {
                        var r = await ws.ReceiveAsync(new ArraySegment<byte>(buf), cts.Token);
                        if (r.MessageType == WebSocketMessageType.Close) break;
                        var msg = Encoding.UTF8.GetString(buf, 0, r.Count);
                        if (msg.Contains("cancel"))
                        {
                            if (!process.HasExited)
                                try { process.Kill(entireProcessTree: true); } catch { }
                            break;
                        }
                    }
                }
                catch { }
            }, cts.Token);

            // Stream stdout
            var stdoutTask = Task.Run(async () =>
            {
                try
                {
                    string? line;
                    while ((line = await process.StandardOutput.ReadLineAsync(cts.Token)) != null)
                    {
                        if (ws.State == WebSocketState.Open)
                            await SendJsonMessage(ws, "stdout", line, 0, cts.Token);
                    }
                }
                catch (OperationCanceledException) { }
                catch { }
            }, cts.Token);

            // Stream stderr
            var stderrTask = Task.Run(async () =>
            {
                try
                {
                    string? line;
                    while ((line = await process.StandardError.ReadLineAsync(cts.Token)) != null)
                    {
                        if (ws.State == WebSocketState.Open)
                            await SendJsonMessage(ws, "stderr", line, 0, cts.Token);
                    }
                }
                catch (OperationCanceledException) { }
                catch { }
            }, cts.Token);

            await process.WaitForExitAsync(cts.Token);
            await Task.WhenAll(stdoutTask, stderrTask);

            if (ws.State == WebSocketState.Open)
                await SendJsonMessage(ws, "exit", "", process.ExitCode, CancellationToken.None);

            await cts.CancelAsync();
        }
        catch (Exception ex)
        {
            if (ws.State == WebSocketState.Open)
            {
                await SendJsonMessage(ws, "stderr", $"Error: {ex.Message}", 0, CancellationToken.None);
                await SendJsonMessage(ws, "exit", "", 1, CancellationToken.None);
            }
        }
        finally
        {
            if (process != null && !process.HasExited)
                try { process.Kill(entireProcessTree: true); } catch { }
            process?.Dispose();

            if (ws.State == WebSocketState.Open)
                try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Task completed.", CancellationToken.None); } catch { }
        }
    }

    private static async Task SendJsonMessage(WebSocket ws, string type, string data, int code, CancellationToken ct)
    {
        var msg = JsonSerializer.Serialize(new { type, data, code });
        var bytes = Encoding.UTF8.GetBytes(msg);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }
}
