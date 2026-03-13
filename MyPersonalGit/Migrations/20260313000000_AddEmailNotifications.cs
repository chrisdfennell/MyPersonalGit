using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalGit.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SystemSettings: add email notification fields
            migrationBuilder.AddColumn<bool>(
                name: "EmailNotificationsEnabled",
                table: "SystemSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SmtpFromAddress",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SmtpFromName",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "MyPersonalGit");

            // UserProfiles: add email notification preference
            migrationBuilder.AddColumn<bool>(
                name: "EmailNotificationsEnabled",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "EmailNotificationsEnabled", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "SmtpFromAddress", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "SmtpFromName", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "EmailNotificationsEnabled", table: "UserProfiles");
        }
    }
}
