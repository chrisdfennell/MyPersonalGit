using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalGit.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuth2ProviderWebAuthnSspiAGit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // OAuth2 Provider - Apps
            migrationBuilder.CreateTable(
                name: "OAuth2Apps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    ClientSecret = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    RedirectUri = table.Column<string>(type: "TEXT", nullable: false),
                    Owner = table.Column<string>(type: "TEXT", nullable: false),
                    IsConfidential = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_OAuth2Apps", x => x.Id); });

            migrationBuilder.CreateIndex(name: "IX_OAuth2Apps_ClientId", table: "OAuth2Apps", column: "ClientId", unique: true);

            // OAuth2 Provider - Auth Codes
            migrationBuilder.CreateTable(
                name: "OAuth2AuthCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    RedirectUri = table.Column<string>(type: "TEXT", nullable: false),
                    Scope = table.Column<string>(type: "TEXT", nullable: true),
                    CodeChallenge = table.Column<string>(type: "TEXT", nullable: true),
                    CodeChallengeMethod = table.Column<string>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Used = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_OAuth2AuthCodes", x => x.Id); });

            migrationBuilder.CreateIndex(name: "IX_OAuth2AuthCodes_Code", table: "OAuth2AuthCodes", column: "Code", unique: true);

            // OAuth2 Provider - Tokens
            migrationBuilder.CreateTable(
                name: "OAuth2Tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: true),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Scope = table.Column<string>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_OAuth2Tokens", x => x.Id); });

            migrationBuilder.CreateIndex(name: "IX_OAuth2Tokens_AccessToken", table: "OAuth2Tokens", column: "AccessToken", unique: true);

            // WebAuthn / Passkeys
            migrationBuilder.CreateTable(
                name: "WebAuthnCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CredentialId = table.Column<string>(type: "TEXT", nullable: false),
                    PublicKey = table.Column<string>(type: "TEXT", nullable: false),
                    SignCount = table.Column<long>(type: "INTEGER", nullable: false),
                    AaGuid = table.Column<string>(type: "TEXT", nullable: true),
                    IsPlatform = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_WebAuthnCredentials", x => x.Id); });

            migrationBuilder.CreateIndex(name: "IX_WebAuthnCredentials_Username_CredentialId", table: "WebAuthnCredentials",
                columns: new[] { "Username", "CredentialId" }, unique: true);

            // SSPI and AGit Flow settings
            migrationBuilder.AddColumn<bool>(name: "SspiEnabled", table: "SystemSettings", type: "INTEGER", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "AGitFlowEnabled", table: "SystemSettings", type: "INTEGER", nullable: false, defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OAuth2Apps");
            migrationBuilder.DropTable(name: "OAuth2AuthCodes");
            migrationBuilder.DropTable(name: "OAuth2Tokens");
            migrationBuilder.DropTable(name: "WebAuthnCredentials");
            migrationBuilder.DropColumn(name: "SspiEnabled", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "AGitFlowEnabled", table: "SystemSettings");
        }
    }
}
