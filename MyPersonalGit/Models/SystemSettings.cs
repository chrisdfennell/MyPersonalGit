namespace MyPersonalGit.Models;

public class SystemSettings
{
    public int Id { get; set; }
    public string ProjectRoot { get; set; } = string.Empty;
    public bool RequireAuth { get; set; } = true;
    public bool AllowUserRegistration { get; set; } = false;
    public bool RequireEmailVerification { get; set; } = false;
    public int MaxRepositoriesPerUser { get; set; } = 100;
    public long MaxRepositorySize { get; set; } = 1073741824;
    public bool EnableActions { get; set; } = true;
    public bool EnableWiki { get; set; } = true;
    public bool EnableIssues { get; set; } = true;
    public bool EnableProjects { get; set; } = true;
    public bool EmailNotificationsEnabled { get; set; }
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool SmtpEnableSsl { get; set; } = true;
    public string SmtpFromAddress { get; set; } = string.Empty;
    public string SmtpFromName { get; set; } = "MyPersonalGit";

    // Push notifications
    public bool EnablePushNotifications { get; set; }
    public string NtfyUrl { get; set; } = string.Empty;
    public string NtfyTopic { get; set; } = string.Empty;
    public string NtfyAccessToken { get; set; } = string.Empty;
    public string GotifyUrl { get; set; } = string.Empty;
    public string GotifyAppToken { get; set; } = string.Empty;

    // Built-in SSH Server
    public bool EnableBuiltInSshServer { get; set; }
    public int SshServerPort { get; set; } = 2222;

    // LDAP / Active Directory Authentication
    public bool LdapEnabled { get; set; }
    public string LdapServer { get; set; } = string.Empty;
    public int LdapPort { get; set; } = 389;
    public bool LdapUseSsl { get; set; }
    public bool LdapStartTls { get; set; }
    public string LdapBindDn { get; set; } = string.Empty;
    public string LdapBindPassword { get; set; } = string.Empty;
    public string LdapSearchBase { get; set; } = string.Empty;
    public string LdapUserFilter { get; set; } = "(sAMAccountName={0})";
    public string LdapUsernameAttribute { get; set; } = "sAMAccountName";
    public string LdapEmailAttribute { get; set; } = "mail";
    public string LdapDisplayNameAttribute { get; set; } = "displayName";
    public string LdapAdminGroupDn { get; set; } = string.Empty;
    public bool LdapSkipCertificateValidation { get; set; } = true;

    // Merge Commit Signing
    public bool SignMergeCommits { get; set; }
    public string ServerGpgKeyId { get; set; } = string.Empty;

    // SSPI / Windows Integrated Authentication
    public bool SspiEnabled { get; set; }

    // AGit Flow (push to refs/for/*)
    public bool AGitFlowEnabled { get; set; } = true;

    // AI Code Completion
    public bool AiCompletionEnabled { get; set; }
    public string AiCompletionEndpoint { get; set; } = string.Empty; // e.g., "https://api.openai.com/v1"
    public string AiCompletionApiKey { get; set; } = string.Empty;
    public string AiCompletionModel { get; set; } = "gpt-4o-mini";

    // TLS / HTTPS
    public bool EnableHttps { get; set; }
    public int HttpsPort { get; set; } = 8443;
    public int HttpsExternalPort { get; set; } = 8443;
    public bool HttpsRedirect { get; set; }
    public string TlsCertSource { get; set; } = "none"; // "none", "file", "selfSigned"
    public string TlsCertPath { get; set; } = string.Empty;
    public string TlsKeyPath { get; set; } = string.Empty;
    public string TlsPfxPath { get; set; } = string.Empty;
    public string TlsPfxPassword { get; set; } = string.Empty;

    // Footer Pages (customizable content)
    public string SiteName { get; set; } = "PersonalGit";
    public string FooterTerms { get; set; } = "## Terms of Service\n\nBy using this MyPersonalGit instance, you agree to use it responsibly.\n\n- Do not upload malicious code or content that violates applicable laws.\n- Administrators reserve the right to remove content or disable accounts.\n- This is a self-hosted service — your data stays on this server.\n\nFor questions, contact your instance administrator.";
    public string FooterPrivacy { get; set; } = "## Privacy Policy\n\nThis MyPersonalGit instance is self-hosted. Your data is stored locally on this server and is not shared with any third parties.\n\n**What we store:**\n- Account information (username, email, password hash)\n- Repository data and git history\n- Issues, pull requests, and comments\n- Session data for authentication\n\n**What we don't do:**\n- We don't sell or share your data\n- We don't use analytics or tracking services\n- We don't send data to external servers (except features you explicitly enable like SMTP, push notifications, or Docker Hub)";
    public string FooterDocs { get; set; } = "## Documentation\n\nVisit the [GitHub repository](https://github.com/ChrisDFennell/MyPersonalGit) for full documentation, setup guides, and the README.\n\n### Quick Links\n\n- **Clone a repo:** `git clone http://your-server/git/RepoName.git`\n- **SSH access:** Enable in Admin > Settings > Built-in SSH Server\n- **API tokens:** Generate in Settings > Access Tokens\n- **CI/CD:** Add `.github/workflows/*.yml` to your repo\n- **Docker Registry:** `docker push your-server/image:tag`\n- **Package Registry:** NuGet, npm, and generic packages supported";
    public string FooterContact { get; set; } = "## Contact\n\nThis is a self-hosted MyPersonalGit instance.\n\nFor issues with this instance, contact the server administrator.\n\nFor bugs or feature requests with MyPersonalGit itself, visit:\n- **GitHub:** [github.com/ChrisDFennell/MyPersonalGit](https://github.com/ChrisDFennell/MyPersonalGit)\n- **Issues:** [Report a bug](https://github.com/ChrisDFennell/MyPersonalGit/issues)";
}

public class SystemStatistics
{
    public int TotalUsers { get; set; }
    public int TotalRepositories { get; set; }
    public int TotalIssues { get; set; }
    public int TotalPullRequests { get; set; }
    public long TotalStorageUsed { get; set; }
    public int ActiveUsers24h { get; set; }
    public int ActiveUsers7d { get; set; }
    public int ActiveUsers30d { get; set; }
    public DateTime LastBackup { get; set; }
}

public class AuditLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

public class UserManagement
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime LastLogin { get; set; }
    public int RepositoryCount { get; set; }
}
