using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody);
    Task SendIssueNotificationAsync(Issue issue, string action, string actorUsername);
    Task SendPullRequestNotificationAsync(PullRequest pr, string action, string actorUsername);
    Task SendMentionNotificationAsync(string mentionedUsername, string context, string url);
    Task<(bool Success, string Message)> SendTestEmailAsync(string to);
}

public class EmailService : IEmailService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAdminService _adminService;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IDbContextFactory<AppDbContext> dbFactory, IAdminService adminService, ILogger<EmailService> logger)
    {
        _dbFactory = dbFactory;
        _adminService = adminService;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var settings = await _adminService.GetSystemSettingsAsync();
        if (!settings.EmailNotificationsEnabled || string.IsNullOrEmpty(settings.SmtpServer))
        {
            _logger.LogDebug("Email notifications disabled or SMTP not configured, skipping email to {To}", to);
            return;
        }

        await SendViaSmtpAsync(settings, to, subject, htmlBody);
    }

    public async Task SendIssueNotificationAsync(Issue issue, string action, string actorUsername)
    {
        var settings = await _adminService.GetSystemSettingsAsync();
        if (!settings.EmailNotificationsEnabled || string.IsNullOrEmpty(settings.SmtpServer)) return;

        // Find users who should be notified (assignee, author)
        var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(issue.Author)) recipients.Add(issue.Author);
        if (!string.IsNullOrEmpty(issue.Assignee)) recipients.Add(issue.Assignee);
        recipients.Remove(actorUsername); // Don't notify the actor

        foreach (var username in recipients)
        {
            var email = await GetUserEmailIfEnabledAsync(username);
            if (email == null) continue;

            var subject = $"[{issue.RepoName}] Issue #{issue.Number}: {issue.Title} ({action})";
            var body = BuildIssueEmailBody(issue, action, actorUsername);
            await SafeSendAsync(settings, email, subject, body);
        }
    }

    public async Task SendPullRequestNotificationAsync(PullRequest pr, string action, string actorUsername)
    {
        var settings = await _adminService.GetSystemSettingsAsync();
        if (!settings.EmailNotificationsEnabled || string.IsNullOrEmpty(settings.SmtpServer)) return;

        var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(pr.Author)) recipients.Add(pr.Author);
        foreach (var reviewer in pr.Reviewers) recipients.Add(reviewer);
        recipients.Remove(actorUsername);

        foreach (var username in recipients)
        {
            var email = await GetUserEmailIfEnabledAsync(username);
            if (email == null) continue;

            var subject = $"[{pr.RepoName}] Pull Request #{pr.Number}: {pr.Title} ({action})";
            var body = BuildPullRequestEmailBody(pr, action, actorUsername);
            await SafeSendAsync(settings, email, subject, body);
        }
    }

    public async Task SendMentionNotificationAsync(string mentionedUsername, string context, string url)
    {
        var settings = await _adminService.GetSystemSettingsAsync();
        if (!settings.EmailNotificationsEnabled || string.IsNullOrEmpty(settings.SmtpServer)) return;

        var email = await GetUserEmailIfEnabledAsync(mentionedUsername);
        if (email == null) return;

        var subject = $"You were mentioned in {context}";
        var body = $@"
<html>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; color: #24292e;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""border-bottom: 1px solid #e1e4e8; padding-bottom: 10px;"">You were mentioned</h2>
        <p>You were mentioned in <strong>{WebUtility.HtmlEncode(context)}</strong>.</p>
        {(string.IsNullOrEmpty(url) ? "" : $@"<p><a href=""{WebUtility.HtmlEncode(url)}"" style=""color: #0366d6;"">View on MyPersonalGit</a></p>")}
        <hr style=""border: none; border-top: 1px solid #e1e4e8; margin-top: 20px;"" />
        <p style=""color: #6a737d; font-size: 12px;"">You received this email because you were mentioned. You can update your notification preferences in your account settings.</p>
    </div>
</body>
</html>";

        await SafeSendAsync(settings, email, subject, body);
    }

    public async Task<(bool Success, string Message)> SendTestEmailAsync(string to)
    {
        var settings = await _adminService.GetSystemSettingsAsync();
        if (string.IsNullOrEmpty(settings.SmtpServer))
            return (false, "SMTP server is not configured.");

        try
        {
            var subject = "MyPersonalGit - Test Email";
            var body = @"
<html>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; color: #24292e;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""border-bottom: 1px solid #e1e4e8; padding-bottom: 10px;"">Test Email</h2>
        <p>This is a test email from <strong>MyPersonalGit</strong>.</p>
        <p>If you received this message, your SMTP settings are configured correctly.</p>
        <hr style=""border: none; border-top: 1px solid #e1e4e8; margin-top: 20px;"" />
        <p style=""color: #6a737d; font-size: 12px;"">Sent from MyPersonalGit admin panel.</p>
    </div>
</body>
</html>";

            await SendViaSmtpAsync(settings, to, subject, body);
            return (true, $"Test email sent successfully to {to}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test email to {To}", to);
            return (false, $"Failed to send test email: {ex.Message}");
        }
    }

    private async Task<string?> GetUserEmailIfEnabledAsync(string username)
    {
        using var db = _dbFactory.CreateDbContext();

        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Username == username);
        if (profile is { EmailNotificationsEnabled: false }) return null;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null || string.IsNullOrEmpty(user.Email)) return null;

        return user.Email;
    }

    private async Task SafeSendAsync(SystemSettings settings, string to, string subject, string body)
    {
        try
        {
            await SendViaSmtpAsync(settings, to, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email to {To}: {Subject}", to, subject);
        }
    }

    private async Task SendViaSmtpAsync(SystemSettings settings, string to, string subject, string htmlBody)
    {
        var fromAddress = string.IsNullOrEmpty(settings.SmtpFromAddress)
            ? settings.SmtpUsername
            : settings.SmtpFromAddress;

        if (string.IsNullOrEmpty(fromAddress))
        {
            _logger.LogWarning("Cannot send email: no from address configured");
            return;
        }

        using var message = new MailMessage();
        message.From = new MailAddress(fromAddress, string.IsNullOrEmpty(settings.SmtpFromName) ? "MyPersonalGit" : settings.SmtpFromName);
        message.To.Add(new MailAddress(to));
        message.Subject = subject;
        message.Body = htmlBody;
        message.IsBodyHtml = true;

        using var client = new SmtpClient(settings.SmtpServer, settings.SmtpPort);
        client.EnableSsl = settings.SmtpEnableSsl;

        if (!string.IsNullOrEmpty(settings.SmtpUsername))
        {
            client.Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword);
        }

        client.Timeout = 15000; // 15 second timeout

        await client.SendMailAsync(message);
        _logger.LogDebug("Email sent to {To}: {Subject}", to, subject);
    }

    private static string BuildIssueEmailBody(Issue issue, string action, string actor)
    {
        return $@"
<html>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; color: #24292e;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""border-bottom: 1px solid #e1e4e8; padding-bottom: 10px;"">
            [{WebUtility.HtmlEncode(issue.RepoName)}] Issue #{issue.Number}
        </h2>
        <p><strong>{WebUtility.HtmlEncode(actor)}</strong> {WebUtility.HtmlEncode(action)} issue <strong>{WebUtility.HtmlEncode(issue.Title)}</strong></p>
        {(string.IsNullOrEmpty(issue.Body) ? "" : $@"<div style=""background: #f6f8fa; border: 1px solid #e1e4e8; border-radius: 6px; padding: 16px; margin: 16px 0;"">{WebUtility.HtmlEncode(issue.Body)}</div>")}
        <hr style=""border: none; border-top: 1px solid #e1e4e8; margin-top: 20px;"" />
        <p style=""color: #6a737d; font-size: 12px;"">You received this because you are subscribed to this issue. Manage your notification preferences in your account settings.</p>
    </div>
</body>
</html>";
    }

    private static string BuildPullRequestEmailBody(PullRequest pr, string action, string actor)
    {
        return $@"
<html>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; color: #24292e;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""border-bottom: 1px solid #e1e4e8; padding-bottom: 10px;"">
            [{WebUtility.HtmlEncode(pr.RepoName)}] Pull Request #{pr.Number}
        </h2>
        <p><strong>{WebUtility.HtmlEncode(actor)}</strong> {WebUtility.HtmlEncode(action)} pull request <strong>{WebUtility.HtmlEncode(pr.Title)}</strong></p>
        <p style=""color: #6a737d;""><code>{WebUtility.HtmlEncode(pr.SourceBranch)}</code> &rarr; <code>{WebUtility.HtmlEncode(pr.TargetBranch)}</code></p>
        {(string.IsNullOrEmpty(pr.Body) ? "" : $@"<div style=""background: #f6f8fa; border: 1px solid #e1e4e8; border-radius: 6px; padding: 16px; margin: 16px 0;"">{WebUtility.HtmlEncode(pr.Body)}</div>")}
        <hr style=""border: none; border-top: 1px solid #e1e4e8; margin-top: 20px;"" />
        <p style=""color: #6a737d; font-size: 12px;"">You received this because you are subscribed to this pull request. Manage your notification preferences in your account settings.</p>
    </div>
</body>
</html>";
    }
}
