using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GitBuddy.Domain.Migrations
{
    /// <inheritdoc />
    public partial class UniqueBranchWithoutPRByKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BranchesWithoutPR_Repo_BranchName",
                table: "BranchesWithoutPR");

            migrationBuilder.CreateIndex(
                name: "IX_BranchesWithoutPR_RepoFullName_BranchName",
                table: "BranchesWithoutPR",
                columns: new[] { "RepoFullName", "BranchName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BranchesWithoutPR_RepoFullName_BranchName",
                table: "BranchesWithoutPR");

            migrationBuilder.CreateIndex(
                name: "IX_BranchesWithoutPR_Repo_BranchName",
                table: "BranchesWithoutPR",
                columns: new[] { "Repo", "BranchName" });
        }
    }
}
