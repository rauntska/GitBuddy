using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graphite.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingReviewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PullRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    GitHubReviewId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingReviews_PullRequests_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PendingReviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PendingReviewId = table.Column<int>(type: "INTEGER", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Line = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingComments_PendingReviews_PendingReviewId",
                        column: x => x.PendingReviewId,
                        principalTable: "PendingReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingComments_PendingReviewId",
                table: "PendingComments",
                column: "PendingReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingComments_PendingReviewId_Path_Line",
                table: "PendingComments",
                columns: new[] { "PendingReviewId", "Path", "Line" });

            migrationBuilder.CreateIndex(
                name: "IX_PendingReviews_GitHubReviewId",
                table: "PendingReviews",
                column: "GitHubReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingReviews_PullRequestId_UserId",
                table: "PendingReviews",
                columns: new[] { "PullRequestId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingReviews_UserId",
                table: "PendingReviews",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingComments");

            migrationBuilder.DropTable(
                name: "PendingReviews");
        }
    }
}
