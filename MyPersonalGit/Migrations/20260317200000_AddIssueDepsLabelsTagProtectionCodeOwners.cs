using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalGit.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueDepsLabelsTagProtectionCodeOwners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequireCodeOwnersApproval",
                table: "BranchProtectionRules",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

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
                name: "IX_RepositoryLabels_RepoName_Name",
                table: "RepositoryLabels",
                columns: new[] { "RepoName", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TagProtectionRules_RepoName",
                table: "TagProtectionRules",
                column: "RepoName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "IssueDependencies");
            migrationBuilder.DropTable(name: "RepositoryLabels");
            migrationBuilder.DropTable(name: "TagProtectionRules");

            migrationBuilder.DropColumn(
                name: "RequireCodeOwnersApproval",
                table: "BranchProtectionRules");
        }
    }
}
