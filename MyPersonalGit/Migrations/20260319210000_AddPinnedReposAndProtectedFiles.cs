using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalGit.Migrations
{
    /// <inheritdoc />
    public partial class AddPinnedReposAndProtectedFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                constraints: table => { table.PrimaryKey("PK_PinnedRepositories", x => x.Id); });

            migrationBuilder.CreateIndex(
                name: "IX_PinnedRepositories_Username_RepoName",
                table: "PinnedRepositories",
                columns: new[] { "Username", "RepoName" },
                unique: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtectedFilePatterns",
                table: "BranchProtectionRules",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PinnedRepositories");
            migrationBuilder.DropColumn(name: "ProtectedFilePatterns", table: "BranchProtectionRules");
        }
    }
}
