using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graphite.Domain.Migrations
{
    /// <inheritdoc />
    public partial class RenameToRepositoryRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchProtectionRules");

            migrationBuilder.CreateTable(
                name: "RepositoryRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Repository = table.Column<string>(type: "TEXT", nullable: false),
                    BranchPattern = table.Column<string>(type: "TEXT", nullable: true),
                    RulesetName = table.Column<string>(type: "TEXT", nullable: true),
                    RequiresApprovingReviews = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiredApprovingReviewCount = table.Column<int>(type: "INTEGER", nullable: true),
                    RequiresStatusChecks = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepositoryRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryRules_LastSyncedAt",
                table: "RepositoryRules",
                column: "LastSyncedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryRules_Repository_BranchPattern",
                table: "RepositoryRules",
                columns: new[] { "Repository", "BranchPattern" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepositoryRules");

            migrationBuilder.CreateTable(
                name: "BranchProtectionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BranchPattern = table.Column<string>(type: "TEXT", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Repository = table.Column<string>(type: "TEXT", nullable: false),
                    RequiredApprovingReviewCount = table.Column<int>(type: "INTEGER", nullable: true),
                    RequiresApprovingReviews = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresStatusChecks = table.Column<bool>(type: "INTEGER", nullable: false)
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
    }
}
