using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalGit.Migrations
{
    /// <inheritdoc />
    public partial class AddSshServerAndLdap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Built-in SSH Server settings
            migrationBuilder.AddColumn<bool>(
                name: "EnableBuiltInSshServer",
                table: "SystemSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SshServerPort",
                table: "SystemSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2222);

            // LDAP / Active Directory settings
            migrationBuilder.AddColumn<bool>(
                name: "LdapEnabled",
                table: "SystemSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LdapServer",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "LdapPort",
                table: "SystemSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 389);

            migrationBuilder.AddColumn<bool>(
                name: "LdapUseSsl",
                table: "SystemSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LdapStartTls",
                table: "SystemSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LdapBindDn",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LdapBindPassword",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LdapSearchBase",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LdapUserFilter",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "(sAMAccountName={0})");

            migrationBuilder.AddColumn<string>(
                name: "LdapUsernameAttribute",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "sAMAccountName");

            migrationBuilder.AddColumn<string>(
                name: "LdapEmailAttribute",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "mail");

            migrationBuilder.AddColumn<string>(
                name: "LdapDisplayNameAttribute",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "displayName");

            migrationBuilder.AddColumn<string>(
                name: "LdapAdminGroupDn",
                table: "SystemSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "LdapSkipCertificateValidation",
                table: "SystemSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "EnableBuiltInSshServer", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "SshServerPort", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "LdapEnabled", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "LdapServer", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "LdapPort", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "LdapUseSsl", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "LdapStartTls", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "LdapBindDn", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "LdapBindPassword", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "LdapSearchBase", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "LdapUserFilter", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "LdapUsernameAttribute", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "LdapEmailAttribute", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "LdapDisplayNameAttribute", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "LdapAdminGroupDn", table: "SystemSettings");
        }
    }
}
