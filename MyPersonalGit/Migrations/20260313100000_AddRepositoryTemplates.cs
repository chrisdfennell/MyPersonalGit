using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalGit.Migrations
{
    /// <inheritdoc />
    public partial class AddRepositoryTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTemplate",
                table: "Repositories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TemplateRepositoryId",
                table: "Repositories",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsTemplate", table: "Repositories");
            migrationBuilder.DropColumn(name: "TemplateRepositoryId", table: "Repositories");
        }
    }
}
