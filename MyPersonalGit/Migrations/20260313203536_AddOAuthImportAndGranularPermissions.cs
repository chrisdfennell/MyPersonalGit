using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalGit.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthImportAndGranularPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: EmailNotificationsEnabled, SmtpFromAddress, SmtpFromName already added
            // by AddEmailNotifications migration. DeployKeys already added by AddDeployKeys
            // migration. Only unique additions remain here.

            migrationBuilder.AddColumn<int>(
                name: "DiscussionCommentId",
                table: "DiscussionComments",
                type: "INTEGER",
                nullable: true);

            // Remap CollaboratorPermission enum values: old Write(1)->new Write(2), old Admin(2)->new Admin(4)
            migrationBuilder.Sql("UPDATE RepositoryCollaborators SET Permission = 4 WHERE Permission = 2"); // Admin 2->4
            migrationBuilder.Sql("UPDATE RepositoryCollaborators SET Permission = 2 WHERE Permission = 1"); // Write 1->2

            migrationBuilder.CreateTable(
                name: "ExternalLogins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderUserId = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderUsername = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: true),
                    LinkedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalLogins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MigrationTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetRepoName = table.Column<string>(type: "TEXT", nullable: false),
                    Owner = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgressPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    StatusMessage = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    ImportIssues = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImportPullRequests = table.Column<bool>(type: "INTEGER", nullable: false),
                    MakePrivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    AuthToken = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MigrationTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OAuthProviderConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProviderName = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    ClientSecret = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthProviderConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionComments_DiscussionCommentId",
                table: "DiscussionComments",
                column: "DiscussionCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_Provider_ProviderUserId",
                table: "ExternalLogins",
                columns: new[] { "Provider", "ProviderUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_UserId",
                table: "ExternalLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MigrationTasks_Owner",
                table: "MigrationTasks",
                column: "Owner");

            migrationBuilder.CreateIndex(
                name: "IX_MigrationTasks_Status",
                table: "MigrationTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthProviderConfigs_ProviderName",
                table: "OAuthProviderConfigs",
                column: "ProviderName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionComments_DiscussionComments_DiscussionCommentId",
                table: "DiscussionComments",
                column: "DiscussionCommentId",
                principalTable: "DiscussionComments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscussionComments_DiscussionComments_DiscussionCommentId",
                table: "DiscussionComments");

            migrationBuilder.DropTable(name: "ExternalLogins");
            migrationBuilder.DropTable(name: "MigrationTasks");
            migrationBuilder.DropTable(name: "OAuthProviderConfigs");

            migrationBuilder.DropIndex(
                name: "IX_DiscussionComments_DiscussionCommentId",
                table: "DiscussionComments");

            migrationBuilder.DropColumn(name: "DiscussionCommentId", table: "DiscussionComments");
        }
    }
}
