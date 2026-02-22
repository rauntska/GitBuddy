using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graphite.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddListViewModeToUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ListViewMode",
                table: "UserPreferences",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ListViewMode",
                table: "UserPreferences");
        }
    }
}
