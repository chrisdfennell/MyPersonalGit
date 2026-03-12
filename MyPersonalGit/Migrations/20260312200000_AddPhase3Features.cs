using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalGit.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase3Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ReviewComments table (inline diff comments on PRs)
            migrationBuilder.CreateTable(
                name: "ReviewComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PullRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    Author = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Side = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReplyToId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewComments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewComments_PullRequestId",
                table: "ReviewComments",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewComments_PullRequestId_FilePath",
                table: "ReviewComments",
                columns: new[] { "PullRequestId", "FilePath" });

            // CommitStatuses table
            migrationBuilder.CreateTable(
                name: "CommitStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    Sha = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    Context = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    TargetUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Creator = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitStatuses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommitStatuses_RepoName_Sha",
                table: "CommitStatuses",
                columns: new[] { "RepoName", "Sha" });

            migrationBuilder.CreateIndex(
                name: "IX_CommitStatuses_RepoName_Sha_Context",
                table: "CommitStatuses",
                columns: new[] { "RepoName", "Sha", "Context" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ReviewComments");
            migrationBuilder.DropTable(name: "CommitStatuses");
        }
    }
}
