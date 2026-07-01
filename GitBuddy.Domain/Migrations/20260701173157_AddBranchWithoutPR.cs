using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GitBuddy.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchWithoutPR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchRefreshIntervalMinutes",
                table: "GitHubConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.CreateTable(
                name: "BranchesWithoutPR",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Owner = table.Column<string>(type: "text", nullable: false),
                    Repo = table.Column<string>(type: "text", nullable: false),
                    RepoFullName = table.Column<string>(type: "text", nullable: false),
                    BranchName = table.Column<string>(type: "text", nullable: false),
                    DefaultBranch = table.Column<string>(type: "text", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRefreshedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchesWithoutPR", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BranchesWithoutPR_LastRefreshedAt",
                table: "BranchesWithoutPR",
                column: "LastRefreshedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BranchesWithoutPR_Repo_BranchName",
                table: "BranchesWithoutPR",
                columns: new[] { "Repo", "BranchName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchesWithoutPR");

            migrationBuilder.DropColumn(
                name: "BranchRefreshIntervalMinutes",
                table: "GitHubConfigs");
        }
    }
}
