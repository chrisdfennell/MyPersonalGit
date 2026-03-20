namespace MyPersonalGit.Models;

public class IssueTransfer
{
    public int Id { get; set; }
    public required string FromRepoName { get; set; }
    public int FromIssueNumber { get; set; }
    public required string ToRepoName { get; set; }
    public int ToIssueNumber { get; set; }
    public required string TransferredBy { get; set; }
    public DateTime TransferredAt { get; set; }
}
