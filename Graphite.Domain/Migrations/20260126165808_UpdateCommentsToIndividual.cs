using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graphite.Domain.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCommentsToIndividual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Comments_PullRequestId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Count",
                table: "Comments");

            migrationBuilder.RenameColumn(
                name: "ResolvedCount",
                table: "Comments",
                newName: "IsResolved");

            migrationBuilder.RenameColumn(
                name: "PendingCount",
                table: "Comments",
                newName: "GitHubId");

            migrationBuilder.RenameColumn(
                name: "LastUpdated",
                table: "Comments",
                newName: "UpdatedAt");

            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "Comments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Body",
                table: "Comments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Comments_PullRequestId",
                table: "Comments",
                column: "PullRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Comments_PullRequestId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Author",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Body",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Comments");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Comments",
                newName: "LastUpdated");

            migrationBuilder.RenameColumn(
                name: "IsResolved",
                table: "Comments",
                newName: "ResolvedCount");

            migrationBuilder.RenameColumn(
                name: "GitHubId",
                table: "Comments",
                newName: "PendingCount");

            migrationBuilder.AddColumn<int>(
                name: "Count",
                table: "Comments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_PullRequestId",
                table: "Comments",
                column: "PullRequestId",
                unique: true);
        }
    }
}
