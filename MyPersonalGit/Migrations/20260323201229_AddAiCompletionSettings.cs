using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalGit.Migrations
{
    /// <inheritdoc />
    public partial class AddAiCompletionSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Environment",
                table: "WorkflowJobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AGitFlowEnabled",
                table: "SystemSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AiCompletionApiKey",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "AiCompletionEnabled",
                table: "SystemSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AiCompletionEndpoint",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AiCompletionModel",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EnableHttps",
                table: "SystemSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "HttpsExternalPort",
                table: "SystemSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HttpsPort",
                table: "SystemSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HttpsRedirect",
                table: "SystemSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SspiEnabled",
                table: "SystemSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TlsCertPath",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TlsCertSource",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TlsKeyPath",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TlsPfxPassword",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TlsPfxPath",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExternalIssueTrackerPattern",
                table: "Repositories",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalIssueTrackerUrl",
                table: "Repositories",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseExternalIssueTracker",
                table: "Repositories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "PullRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "PullRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LockReason",
                table: "PullRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Assignees",
                table: "Issues",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Issues",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "Issues",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "Issues",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LockReason",
                table: "Issues",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtectedFilePatterns",
                table: "BranchProtectionRules",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequireCodeOwnersApproval",
                table: "BranchProtectionRules",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireSignedCommits",
                table: "BranchProtectionRules",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AutolinkPatterns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    Prefix = table.Column<string>(type: "TEXT", nullable: false),
                    UrlTemplate = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutolinkPatterns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DependencyUpdateConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    Ecosystem = table.Column<string>(type: "TEXT", nullable: false),
                    Schedule = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Directory = table.Column<string>(type: "TEXT", nullable: true),
                    OpenPRLimit = table.Column<int>(type: "INTEGER", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependencyUpdateConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DependencyUpdateLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    PackageName = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentVersion = table.Column<string>(type: "TEXT", nullable: false),
                    NewVersion = table.Column<string>(type: "TEXT", nullable: false),
                    PullRequestNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependencyUpdateLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeploymentApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeploymentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Reviewer = table.Column<string>(type: "TEXT", nullable: false),
                    Approved = table.Column<bool>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentApprovals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeploymentEnvironments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    WaitTimerMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    RequireApproval = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiredReviewers = table.Column<string>(type: "TEXT", nullable: false),
                    AllowedBranches = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentEnvironments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Deployments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnvironmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    EnvironmentName = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowRunId = table.Column<int>(type: "INTEGER", nullable: true),
                    CommitSha = table.Column<string>(type: "TEXT", nullable: false),
                    Ref = table.Column<string>(type: "TEXT", nullable: false),
                    Creator = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deployments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IssueDependencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    BlockingIssueNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    BlockedIssueNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueDependencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IssueTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FromRepoName = table.Column<string>(type: "TEXT", nullable: false),
                    FromIssueNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ToRepoName = table.Column<string>(type: "TEXT", nullable: false),
                    ToIssueNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TransferredBy = table.Column<string>(type: "TEXT", nullable: false),
                    TransferredAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueTransfers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OAuth2Apps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    ClientSecret = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    RedirectUri = table.Column<string>(type: "TEXT", nullable: false),
                    Owner = table.Column<string>(type: "TEXT", nullable: false),
                    IsConfidential = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuth2Apps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OAuth2AuthCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    RedirectUri = table.Column<string>(type: "TEXT", nullable: false),
                    Scope = table.Column<string>(type: "TEXT", nullable: true),
                    CodeChallenge = table.Column<string>(type: "TEXT", nullable: true),
                    CodeChallengeMethod = table.Column<string>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Used = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuth2AuthCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OAuth2Tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: true),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Scope = table.Column<string>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuth2Tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSecrets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrganizationName = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    EncryptedValue = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSecrets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PinnedRepositories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PinnedRepositories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepositoryLabels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepositoryLabels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepositoryTrafficEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", nullable: false),
                    Referrer = table.Column<string>(type: "TEXT", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    IpHash = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepositoryTrafficEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepositoryTrafficSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Clones = table.Column<int>(type: "INTEGER", nullable: false),
                    UniqueCloners = table.Column<int>(type: "INTEGER", nullable: false),
                    PageViews = table.Column<int>(type: "INTEGER", nullable: false),
                    UniqueVisitors = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepositoryTrafficSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Runners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    Labels = table.Column<string>(type: "TEXT", nullable: false),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastHeartbeat = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedReplies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedReplies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecretScanPatterns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Pattern = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretScanPatterns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecretScanResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    CommitSha = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    SecretType = table.Column<string>(type: "TEXT", nullable: false),
                    MatchSnippet = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    ResolvedBy = table.Column<string>(type: "TEXT", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretScanResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TagProtectionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    TagPattern = table.Column<string>(type: "TEXT", nullable: false),
                    PreventDeletion = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreventForcePush = table.Column<bool>(type: "INTEGER", nullable: false),
                    RestrictCreation = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowedUsers = table.Column<string>(type: "TEXT", nullable: false),
                    RequireSignedTags = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagProtectionRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSecrets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    EncryptedValue = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSecrets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebAuthnCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CredentialId = table.Column<string>(type: "TEXT", nullable: false),
                    PublicKey = table.Column<string>(type: "TEXT", nullable: false),
                    SignCount = table.Column<long>(type: "INTEGER", nullable: false),
                    AaGuid = table.Column<string>(type: "TEXT", nullable: true),
                    IsPlatform = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebAuthnCredentials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutolinkPatterns_RepoName_Prefix",
                table: "AutolinkPatterns",
                columns: new[] { "RepoName", "Prefix" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DependencyUpdateConfigs_RepoName_Ecosystem",
                table: "DependencyUpdateConfigs",
                columns: new[] { "RepoName", "Ecosystem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DependencyUpdateLogs_ConfigId",
                table: "DependencyUpdateLogs",
                column: "ConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_DependencyUpdateLogs_RepoName",
                table: "DependencyUpdateLogs",
                column: "RepoName");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentApprovals_DeploymentId",
                table: "DeploymentApprovals",
                column: "DeploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentEnvironments_RepoName_Name",
                table: "DeploymentEnvironments",
                columns: new[] { "RepoName", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deployments_RepoName_EnvironmentName",
                table: "Deployments",
                columns: new[] { "RepoName", "EnvironmentName" });

            migrationBuilder.CreateIndex(
                name: "IX_Deployments_Status",
                table: "Deployments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IssueDependencies_RepoName_BlockedIssueNumber",
                table: "IssueDependencies",
                columns: new[] { "RepoName", "BlockedIssueNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_IssueDependencies_RepoName_BlockingIssueNumber",
                table: "IssueDependencies",
                columns: new[] { "RepoName", "BlockingIssueNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_IssueDependencies_RepoName_BlockingIssueNumber_BlockedIssueNumber",
                table: "IssueDependencies",
                columns: new[] { "RepoName", "BlockingIssueNumber", "BlockedIssueNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueTransfers_FromRepoName_FromIssueNumber",
                table: "IssueTransfers",
                columns: new[] { "FromRepoName", "FromIssueNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_IssueTransfers_ToRepoName_ToIssueNumber",
                table: "IssueTransfers",
                columns: new[] { "ToRepoName", "ToIssueNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_OAuth2Apps_ClientId",
                table: "OAuth2Apps",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuth2AuthCodes_Code",
                table: "OAuth2AuthCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuth2Tokens_AccessToken",
                table: "OAuth2Tokens",
                column: "AccessToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSecrets_OrganizationName_Name",
                table: "OrganizationSecrets",
                columns: new[] { "OrganizationName", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PinnedRepositories_Username_RepoName",
                table: "PinnedRepositories",
                columns: new[] { "Username", "RepoName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryLabels_RepoName_Name",
                table: "RepositoryLabels",
                columns: new[] { "RepoName", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryTrafficEvents_RepoName_Timestamp",
                table: "RepositoryTrafficEvents",
                columns: new[] { "RepoName", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryTrafficSummaries_RepoName_Date",
                table: "RepositoryTrafficSummaries",
                columns: new[] { "RepoName", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Runners_Token",
                table: "Runners",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedReplies_Username",
                table: "SavedReplies",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_SecretScanResults_RepoName_CommitSha",
                table: "SecretScanResults",
                columns: new[] { "RepoName", "CommitSha" });

            migrationBuilder.CreateIndex(
                name: "IX_SecretScanResults_RepoName_State",
                table: "SecretScanResults",
                columns: new[] { "RepoName", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_TagProtectionRules_RepoName",
                table: "TagProtectionRules",
                column: "RepoName");

            migrationBuilder.CreateIndex(
                name: "IX_UserSecrets_Username_Name",
                table: "UserSecrets",
                columns: new[] { "Username", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebAuthnCredentials_Username_CredentialId",
                table: "WebAuthnCredentials",
                columns: new[] { "Username", "CredentialId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutolinkPatterns");

            migrationBuilder.DropTable(
                name: "DependencyUpdateConfigs");

            migrationBuilder.DropTable(
                name: "DependencyUpdateLogs");

            migrationBuilder.DropTable(
                name: "DeploymentApprovals");

            migrationBuilder.DropTable(
                name: "DeploymentEnvironments");

            migrationBuilder.DropTable(
                name: "Deployments");

            migrationBuilder.DropTable(
                name: "IssueDependencies");

            migrationBuilder.DropTable(
                name: "IssueTransfers");

            migrationBuilder.DropTable(
                name: "OAuth2Apps");

            migrationBuilder.DropTable(
                name: "OAuth2AuthCodes");

            migrationBuilder.DropTable(
                name: "OAuth2Tokens");

            migrationBuilder.DropTable(
                name: "OrganizationSecrets");

            migrationBuilder.DropTable(
                name: "PinnedRepositories");

            migrationBuilder.DropTable(
                name: "RepositoryLabels");

            migrationBuilder.DropTable(
                name: "RepositoryTrafficEvents");

            migrationBuilder.DropTable(
                name: "RepositoryTrafficSummaries");

            migrationBuilder.DropTable(
                name: "Runners");

            migrationBuilder.DropTable(
                name: "SavedReplies");

            migrationBuilder.DropTable(
                name: "SecretScanPatterns");

            migrationBuilder.DropTable(
                name: "SecretScanResults");

            migrationBuilder.DropTable(
                name: "TagProtectionRules");

            migrationBuilder.DropTable(
                name: "UserSecrets");

            migrationBuilder.DropTable(
                name: "WebAuthnCredentials");

            migrationBuilder.DropColumn(
                name: "Environment",
                table: "WorkflowJobs");

            migrationBuilder.DropColumn(
                name: "AGitFlowEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "AiCompletionApiKey",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "AiCompletionEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "AiCompletionEndpoint",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "AiCompletionModel",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EnableHttps",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "HttpsExternalPort",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "HttpsPort",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "HttpsRedirect",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SspiEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "TlsCertPath",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "TlsCertSource",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "TlsKeyPath",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "TlsPfxPassword",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "TlsPfxPath",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "ExternalIssueTrackerPattern",
                table: "Repositories");

            migrationBuilder.DropColumn(
                name: "ExternalIssueTrackerUrl",
                table: "Repositories");

            migrationBuilder.DropColumn(
                name: "UseExternalIssueTracker",
                table: "Repositories");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "PullRequests");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "PullRequests");

            migrationBuilder.DropColumn(
                name: "LockReason",
                table: "PullRequests");

            migrationBuilder.DropColumn(
                name: "Assignees",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "LockReason",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "ProtectedFilePatterns",
                table: "BranchProtectionRules");

            migrationBuilder.DropColumn(
                name: "RequireCodeOwnersApproval",
                table: "BranchProtectionRules");

            migrationBuilder.DropColumn(
                name: "RequireSignedCommits",
                table: "BranchProtectionRules");
        }
    }
}
