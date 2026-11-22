using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GitBuddy.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddHiddenRepositoriesPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HiddenRepositories",
                table: "UserPreferences",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HiddenRepositories",
                table: "UserPreferences");
        }
    }
}
