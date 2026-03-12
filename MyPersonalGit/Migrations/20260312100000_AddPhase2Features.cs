using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalGit.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // WorkflowArtifacts table
            migrationBuilder.CreateTable(
                name: "WorkflowArtifacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkflowRunId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowArtifacts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowArtifacts_WorkflowRunId",
                table: "WorkflowArtifacts",
                column: "WorkflowRunId");

            // GlobalSecrets table
            migrationBuilder.CreateTable(
                name: "GlobalSecrets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    EncryptedValue = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalSecrets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GlobalSecrets_Name",
                table: "GlobalSecrets",
                column: "Name",
                unique: true);

            // WorkflowSchedules table
            migrationBuilder.CreateTable(
                name: "WorkflowSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowFileName = table.Column<string>(type: "TEXT", nullable: false),
                    CronExpression = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextRunAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSchedules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSchedules_RepoName_WorkflowFileName",
                table: "WorkflowSchedules",
                columns: new[] { "RepoName", "WorkflowFileName" },
                unique: true);

            // PullRequest new columns
            migrationBuilder.AddColumn<bool>(
                name: "AutoMergeEnabled",
                table: "PullRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AutoMergeStrategy",
                table: "PullRequests",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WorkflowArtifacts");
            migrationBuilder.DropTable(name: "GlobalSecrets");
            migrationBuilder.DropTable(name: "WorkflowSchedules");
            migrationBuilder.DropColumn(name: "AutoMergeEnabled", table: "PullRequests");
            migrationBuilder.DropColumn(name: "AutoMergeStrategy", table: "PullRequests");
        }
    }
}
