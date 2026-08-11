using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GitBuddy.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddMarkdownDiffMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MarkdownDiffMode",
                table: "UserPreferences",
                type: "text",
                nullable: false,
                defaultValue: "rendered");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarkdownDiffMode",
                table: "UserPreferences");
        }
    }
}
