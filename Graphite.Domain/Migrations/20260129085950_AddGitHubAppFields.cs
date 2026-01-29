using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graphite.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddGitHubAppFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppId",
                table: "GitHubConfigs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InstallationId",
                table: "GitHubConfigs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrivateKey",
                table: "GitHubConfigs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "UseGitHubApp",
                table: "GitHubConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppId",
                table: "GitHubConfigs");

            migrationBuilder.DropColumn(
                name: "InstallationId",
                table: "GitHubConfigs");

            migrationBuilder.DropColumn(
                name: "PrivateKey",
                table: "GitHubConfigs");

            migrationBuilder.DropColumn(
                name: "UseGitHubApp",
                table: "GitHubConfigs");
        }
    }
}
