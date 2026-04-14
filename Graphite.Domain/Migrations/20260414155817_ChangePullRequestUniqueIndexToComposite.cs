using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graphite.Domain.Migrations
{
    /// <inheritdoc />
    public partial class ChangePullRequestUniqueIndexToComposite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PullRequests_GitHubId",
                table: "PullRequests");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequests_GitHubId_Repository",
                table: "PullRequests",
                columns: new[] { "GitHubId", "Repository" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PullRequests_GitHubId_Repository",
                table: "PullRequests");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequests_GitHubId",
                table: "PullRequests",
                column: "GitHubId",
                unique: true);
        }
    }
}
