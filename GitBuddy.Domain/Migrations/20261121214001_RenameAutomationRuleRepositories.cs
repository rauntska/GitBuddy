using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GitBuddy.Domain.Migrations
{
    /// <inheritdoc />
    public partial class RenameAutomationRuleRepositories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RepositoryFullName",
                table: "AutomationRules",
                newName: "Repositories");

            migrationBuilder.RenameIndex(
                name: "IX_AutomationRules_RepositoryFullName",
                table: "AutomationRules",
                newName: "IX_AutomationRules_Repositories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Repositories",
                table: "AutomationRules",
                newName: "RepositoryFullName");

            migrationBuilder.RenameIndex(
                name: "IX_AutomationRules_Repositories",
                table: "AutomationRules",
                newName: "IX_AutomationRules_RepositoryFullName");
        }
    }
}
