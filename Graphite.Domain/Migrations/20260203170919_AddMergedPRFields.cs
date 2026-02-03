using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graphite.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddMergedPRFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMerged",
                table: "PullRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MergedAt",
                table: "PullRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeleteOldPRs",
                table: "GitHubConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMerged",
                table: "PullRequests");

            migrationBuilder.DropColumn(
                name: "MergedAt",
                table: "PullRequests");

            migrationBuilder.DropColumn(
                name: "DeleteOldPRs",
                table: "GitHubConfigs");
        }
    }
}
