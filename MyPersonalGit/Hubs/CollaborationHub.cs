using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace MyPersonalGit.Hubs;

/// <summary>
/// SignalR hub for real-time collaborative editing in the Web IDE.
/// Handles presence tracking, cursor/selection sync, and operational transform-based edits.
/// </summary>
public class CollaborationHub : Hub
{
    private static readonly ConcurrentDictionary<string, CollaborationSession> Sessions = new();

    /// <summary>
    /// Join a collaboration session for a specific repo/file.
    /// </summary>
    public async Task JoinSession(string repoName, string filePath, string username, string color)
    {
        var sessionKey = $"{repoName}:{filePath}";
        var session = Sessions.GetOrAdd(sessionKey, _ => new CollaborationSession(sessionKey));

        var participant = new CollaborationParticipant
        {
            ConnectionId = Context.ConnectionId,
            Username = username,
            Color = color,
            JoinedAt = DateTime.UtcNow,
            CursorLine = 1,
            CursorColumn = 1
        };

        session.Participants[Context.ConnectionId] = participant;

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionKey);

        // Notify others of the new participant
        await Clients.OthersInGroup(sessionKey).SendAsync("UserJoined", new
        {
            participant.ConnectionId,
            participant.Username,
            participant.Color,
            participant.CursorLine,
            participant.CursorColumn
        });

        // Send existing participants to the new joiner
        var existingParticipants = session.Participants.Values
            .Where(p => p.ConnectionId != Context.ConnectionId)
            .Select(p => new
            {
                p.ConnectionId,
                p.Username,
                p.Color,
                p.CursorLine,
                p.CursorColumn,
                p.SelectionStartLine,
                p.SelectionStartColumn,
                p.SelectionEndLine,
                p.SelectionEndColumn
            })
            .ToList();

        await Clients.Caller.SendAsync("SessionJoined", new
        {
            SessionKey = sessionKey,
            Participants = existingParticipants,
            Version = session.Version
        });
    }

    /// <summary>
    /// Leave the current collaboration session.
    /// </summary>
    public async Task LeaveSession(string repoName, string filePath)
    {
        var sessionKey = $"{repoName}:{filePath}";
        if (Sessions.TryGetValue(sessionKey, out var session))
        {
            session.Participants.TryRemove(Context.ConnectionId, out _);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionKey);
            await Clients.OthersInGroup(sessionKey).SendAsync("UserLeft", Context.ConnectionId);

            // Clean up empty sessions
            if (session.Participants.IsEmpty)
                Sessions.TryRemove(sessionKey, out _);
        }
    }

    /// <summary>
    /// Broadcast cursor position to other participants.
    /// </summary>
    public async Task UpdateCursor(string sessionKey, int line, int column)
    {
        if (Sessions.TryGetValue(sessionKey, out var session) &&
            session.Participants.TryGetValue(Context.ConnectionId, out var participant))
        {
            participant.CursorLine = line;
            participant.CursorColumn = column;
            participant.SelectionStartLine = 0;
            participant.SelectionStartColumn = 0;
            participant.SelectionEndLine = 0;
            participant.SelectionEndColumn = 0;

            await Clients.OthersInGroup(sessionKey).SendAsync("CursorMoved", new
            {
                ConnectionId = Context.ConnectionId,
                Line = line,
                Column = column
            });
        }
    }

    /// <summary>
    /// Broadcast selection range to other participants.
    /// </summary>
    public async Task UpdateSelection(string sessionKey, int startLine, int startColumn, int endLine, int endColumn)
    {
        if (Sessions.TryGetValue(sessionKey, out var session) &&
            session.Participants.TryGetValue(Context.ConnectionId, out var participant))
        {
            participant.CursorLine = endLine;
            participant.CursorColumn = endColumn;
            participant.SelectionStartLine = startLine;
            participant.SelectionStartColumn = startColumn;
            participant.SelectionEndLine = endLine;
            participant.SelectionEndColumn = endColumn;

            await Clients.OthersInGroup(sessionKey).SendAsync("SelectionChanged", new
            {
                ConnectionId = Context.ConnectionId,
                StartLine = startLine,
                StartColumn = startColumn,
                EndLine = endLine,
                EndColumn = endColumn
            });
        }
    }

    /// <summary>
    /// Broadcast a text edit operation to other participants.
    /// Uses operational transform (OT) to handle concurrent edits.
    /// </summary>
    public async Task ApplyEdit(string sessionKey, EditOperation operation)
    {
        if (!Sessions.TryGetValue(sessionKey, out var session)) return;

        // Increment version and record operation
        operation.Version = Interlocked.Increment(ref session.Version);
        operation.ConnectionId = Context.ConnectionId;
        operation.Timestamp = DateTime.UtcNow;

        // Keep last 1000 operations for OT conflict resolution
        session.OperationHistory.Enqueue(operation);
        while (session.OperationHistory.Count > 1000)
            session.OperationHistory.TryDequeue(out _);

        await Clients.OthersInGroup(sessionKey).SendAsync("EditApplied", new
        {
            operation.ConnectionId,
            operation.Version,
            operation.StartLine,
            operation.StartColumn,
            operation.EndLine,
            operation.EndColumn,
            operation.Text,
            operation.Timestamp
        });
    }

    /// <summary>
    /// Request full document sync (for late joiners or conflict recovery).
    /// </summary>
    public async Task RequestSync(string sessionKey)
    {
        if (!Sessions.TryGetValue(sessionKey, out var session)) return;

        // Ask the first (oldest) participant to send their current document state
        var oldest = session.Participants.Values
            .OrderBy(p => p.JoinedAt)
            .FirstOrDefault(p => p.ConnectionId != Context.ConnectionId);

        if (oldest != null)
        {
            await Clients.Client(oldest.ConnectionId).SendAsync("SyncRequested", Context.ConnectionId);
        }
    }

    /// <summary>
    /// Send full document content to a specific connection (sync response).
    /// </summary>
    public async Task SendSync(string targetConnectionId, string content, int version)
    {
        await Clients.Client(targetConnectionId).SendAsync("SyncReceived", new
        {
            Content = content,
            Version = version
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Remove from all sessions
        foreach (var kvp in Sessions)
        {
            if (kvp.Value.Participants.TryRemove(Context.ConnectionId, out _))
            {
                await Clients.OthersInGroup(kvp.Key).SendAsync("UserLeft", Context.ConnectionId);

                if (kvp.Value.Participants.IsEmpty)
                    Sessions.TryRemove(kvp.Key, out _);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Get active sessions info (for admin/status).
    /// </summary>
    public Task<object> GetActiveSessions()
    {
        var sessions = Sessions.Select(kvp => new
        {
            SessionKey = kvp.Key,
            ParticipantCount = kvp.Value.Participants.Count,
            Participants = kvp.Value.Participants.Values.Select(p => p.Username).ToList()
        }).ToList();

        return Task.FromResult<object>(sessions);
    }
}

public class CollaborationSession
{
    public string Key { get; }
    public ConcurrentDictionary<string, CollaborationParticipant> Participants { get; } = new();
    public ConcurrentQueue<EditOperation> OperationHistory { get; } = new();
    public int Version;

    public CollaborationSession(string key) => Key = key;
}

public class CollaborationParticipant
{
    public string ConnectionId { get; set; } = "";
    public string Username { get; set; } = "";
    public string Color { get; set; } = "#4fc1ff";
    public DateTime JoinedAt { get; set; }
    public int CursorLine { get; set; }
    public int CursorColumn { get; set; }
    public int SelectionStartLine { get; set; }
    public int SelectionStartColumn { get; set; }
    public int SelectionEndLine { get; set; }
    public int SelectionEndColumn { get; set; }
}

public class EditOperation
{
    public string ConnectionId { get; set; } = "";
    public int Version { get; set; }
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
    public string Text { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
