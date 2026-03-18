using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalGit.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeTrackingAndSignedMerge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Time Tracking
            migrationBuilder.CreateTable(
                name: "TimeEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    IssueNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StoppedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsRunning = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_RepoName_IssueNumber",
                table: "TimeEntries",
                columns: new[] { "RepoName", "IssueNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_Username_IsRunning",
                table: "TimeEntries",
                columns: new[] { "Username", "IsRunning" });

            // Signed Merge Commits
            migrationBuilder.AddColumn<bool>(
                name: "SignMergeCommits",
                table: "SystemSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ServerGpgKeyId",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TimeEntries");
            migrationBuilder.DropColumn(name: "SignMergeCommits", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "ServerGpgKeyId", table: "SystemSettings");
        }
    }
}
