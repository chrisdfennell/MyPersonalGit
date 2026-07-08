using System.Text.RegularExpressions;

namespace MyPersonalGit.Data;

/// <summary>
/// Counts GitHub-style markdown task-list items (`- [ ]` / `- [x]`) in a body of text,
/// used to show checklist progress on issue and PR lists.
/// </summary>
public static partial class TaskListProgress
{
    [GeneratedRegex(@"^\s*[-*+]\s+\[( |x|X)\]\s", RegexOptions.Multiline)]
    private static partial Regex TaskItemRegex();

    public static (int Done, int Total) Count(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return (0, 0);

        var done = 0;
        var total = 0;
        foreach (Match m in TaskItemRegex().Matches(markdown))
        {
            total++;
            if (m.Groups[1].Value is "x" or "X") done++;
        }
        return (done, total);
    }
}
