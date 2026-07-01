using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GitBuddy.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnHeadersPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowColumnHeaders",
                table: "UserPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowColumnHeaders",
                table: "UserPreferences");
        }
    }
}
