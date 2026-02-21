using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graphite.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchProtectionAndMergeReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentApprovingReviews",
                table: "PullRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasUnresolvedThreads",
                table: "PullRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMergeReady",
                table: "PullRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MergeBlockReason",
                table: "PullRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredApprovingReviews",
                table: "PullRequests",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BranchProtectionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Repository = table.Column<string>(type: "TEXT", nullable: false),
                    BranchPattern = table.Column<string>(type: "TEXT", nullable: false),
                    RequiresApprovingReviews = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiredApprovingReviewCount = table.Column<int>(type: "INTEGER", nullable: true),
                    RequiresStatusChecks = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchProtectionRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BranchProtectionRules_LastSyncedAt",
                table: "BranchProtectionRules",
                column: "LastSyncedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BranchProtectionRules_Repository_BranchPattern",
                table: "BranchProtectionRules",
                columns: new[] { "Repository", "BranchPattern" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchProtectionRules");

            migrationBuilder.DropColumn(
                name: "CurrentApprovingReviews",
                table: "PullRequests");

            migrationBuilder.DropColumn(
                name: "HasUnresolvedThreads",
                table: "PullRequests");

            migrationBuilder.DropColumn(
                name: "IsMergeReady",
                table: "PullRequests");

            migrationBuilder.DropColumn(
                name: "MergeBlockReason",
                table: "PullRequests");

            migrationBuilder.DropColumn(
                name: "RequiredApprovingReviews",
                table: "PullRequests");
        }
    }
}
