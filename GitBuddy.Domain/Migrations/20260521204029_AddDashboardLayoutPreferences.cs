using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GitBuddy.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardLayoutPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DashboardGroupOrder",
                table: "UserPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HiddenDashboardGroups",
                table: "UserPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PinnedPrIds",
                table: "UserPreferences",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DashboardGroupOrder",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "HiddenDashboardGroups",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "PinnedPrIds",
                table: "UserPreferences");
        }
    }
}
