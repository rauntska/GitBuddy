using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graphite.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentReviewThreadRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsResolved",
                table: "Comments");

            migrationBuilder.AddColumn<int>(
                name: "ReviewThreadId",
                table: "Comments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ReviewThreadId",
                table: "Comments",
                column: "ReviewThreadId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_ReviewThreads_ReviewThreadId",
                table: "Comments",
                column: "ReviewThreadId",
                principalTable: "ReviewThreads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_ReviewThreads_ReviewThreadId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_ReviewThreadId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "ReviewThreadId",
                table: "Comments");

            migrationBuilder.AddColumn<bool>(
                name: "IsResolved",
                table: "Comments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
