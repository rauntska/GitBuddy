using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GitBuddy.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddPriorityAndTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastNudgedAt",
                table: "PullRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "PullRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TeamsEnabled",
                table: "GitHubConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TeamsWebhookUrl",
                table: "GitHubConfigs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequests_Priority",
                table: "PullRequests",
                column: "Priority");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PullRequests_Priority",
                table: "PullRequests");

            migrationBuilder.DropColumn(
                name: "LastNudgedAt",
                table: "PullRequests");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "PullRequests");

            migrationBuilder.DropColumn(
                name: "TeamsEnabled",
                table: "GitHubConfigs");

            migrationBuilder.DropColumn(
                name: "TeamsWebhookUrl",
                table: "GitHubConfigs");
        }
    }
}
