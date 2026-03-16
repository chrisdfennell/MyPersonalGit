using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalGit.Migrations
{
    /// <inheritdoc />
    public partial class AddJobNeedsAndStepCondition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Needs",
                table: "WorkflowJobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "WorkflowSteps",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Needs",
                table: "WorkflowJobs");

            migrationBuilder.DropColumn(
                name: "Condition",
                table: "WorkflowSteps");
        }
    }
}
