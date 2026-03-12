using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalGit.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase5Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Discussions table
            migrationBuilder.CreateTable(
                name: "Discussions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Author = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPinned = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAnswered = table.Column<bool>(type: "INTEGER", nullable: false),
                    AnswerCommentId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discussions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Discussions_RepoName_Number",
                table: "Discussions",
                columns: new[] { "RepoName", "Number" },
                unique: true);

            // DiscussionComments table
            migrationBuilder.CreateTable(
                name: "DiscussionComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiscussionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Author = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    ParentCommentId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsAnswer = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpvoteCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscussionComments_Discussions_DiscussionId",
                        column: x => x.DiscussionId,
                        principalTable: "Discussions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionComments_DiscussionId",
                table: "DiscussionComments",
                column: "DiscussionId");

            // Reactions table
            migrationBuilder.CreateTable(
                name: "Reactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Emoji = table.Column<string>(type: "TEXT", nullable: false),
                    IssueCommentId = table.Column<int>(type: "INTEGER", nullable: true),
                    ReviewCommentId = table.Column<int>(type: "INTEGER", nullable: true),
                    CommitCommentId = table.Column<int>(type: "INTEGER", nullable: true),
                    DiscussionCommentId = table.Column<int>(type: "INTEGER", nullable: true),
                    IssueId = table.Column<int>(type: "INTEGER", nullable: true),
                    PullRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    DiscussionId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_Username_Emoji_IssueId_IssueCommentId_PullRequestId_ReviewCommentId_CommitCommentId_DiscussionId_DiscussionCommentId",
                table: "Reactions",
                columns: new[] { "Username", "Emoji", "IssueId", "IssueCommentId", "PullRequestId", "ReviewCommentId", "CommitCommentId", "DiscussionId", "DiscussionCommentId" });

            // IssueTemplates table
            migrationBuilder.CreateTable(
                name: "IssueTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepoName = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Labels = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueTemplates_RepoName_Name",
                table: "IssueTemplates",
                columns: new[] { "RepoName", "Name" },
                unique: true);

            // Add IsDraft column to Issues
            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "Issues",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Add SuggestionBody column to ReviewComments
            migrationBuilder.AddColumn<string>(
                name: "SuggestionBody",
                table: "ReviewComments",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DiscussionComments");
            migrationBuilder.DropTable(name: "Discussions");
            migrationBuilder.DropTable(name: "Reactions");
            migrationBuilder.DropTable(name: "IssueTemplates");
            migrationBuilder.DropColumn(name: "IsDraft", table: "Issues");
            migrationBuilder.DropColumn(name: "SuggestionBody", table: "ReviewComments");
        }
    }
}
