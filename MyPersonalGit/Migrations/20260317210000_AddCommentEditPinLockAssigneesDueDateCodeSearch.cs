using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalGit.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentEditPinLockAssigneesDueDateCodeSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Issue: multiple assignees, due date, pinning, locking
            migrationBuilder.AddColumn<string>(
                name: "Assignees",
                table: "Issues",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Issues",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "Issues",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "Issues",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LockReason",
                table: "Issues",
                type: "TEXT",
                nullable: true);

            // PullRequest: pinning, locking
            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "PullRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "PullRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LockReason",
                table: "PullRequests",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Assignees", table: "Issues");
            migrationBuilder.DropColumn(name: "DueDate", table: "Issues");
            migrationBuilder.DropColumn(name: "IsPinned", table: "Issues");
            migrationBuilder.DropColumn(name: "IsLocked", table: "Issues");
            migrationBuilder.DropColumn(name: "LockReason", table: "Issues");

            migrationBuilder.DropColumn(name: "IsPinned", table: "PullRequests");
            migrationBuilder.DropColumn(name: "IsLocked", table: "PullRequests");
            migrationBuilder.DropColumn(name: "LockReason", table: "PullRequests");
        }
    }
}
