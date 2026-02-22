using System.Collections.Concurrent;

namespace MyPersonalGit.Services;

/// <summary>
/// Thread-safe counters for git operations (fetch, push, clone).
/// Incremented by GitHttpBackendMiddleware, read by MetricsController.
/// </summary>
public static class GitOperationCounters
{
    private static readonly ConcurrentDictionary<string, long> _counters = new();

    public static void Increment(string operationType)
    {
        _counters.AddOrUpdate(operationType, 1, (_, current) => current + 1);
    }

    public static long Get(string operationType)
    {
        return _counters.GetValueOrDefault(operationType, 0);
    }
}
