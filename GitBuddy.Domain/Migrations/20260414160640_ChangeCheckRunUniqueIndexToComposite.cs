using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GitBuddy.Domain.Migrations
{
    /// <inheritdoc />
    public partial class ChangeCheckRunUniqueIndexToComposite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CheckRuns_GitHubId",
                table: "CheckRuns");

            migrationBuilder.CreateIndex(
                name: "IX_CheckRuns_GitHubId_PullRequestId",
                table: "CheckRuns",
                columns: new[] { "GitHubId", "PullRequestId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CheckRuns_GitHubId_PullRequestId",
                table: "CheckRuns");

            migrationBuilder.CreateIndex(
                name: "IX_CheckRuns_GitHubId",
                table: "CheckRuns",
                column: "GitHubId",
                unique: true);
        }
    }
}
