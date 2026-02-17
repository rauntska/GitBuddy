using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graphite.Domain.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedCommentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EditCount",
                table: "Comments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EditedAt",
                table: "Comments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReplyToCommentId",
                table: "Comments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommentDrafts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    PullRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReviewThreadId = table.Column<int>(type: "INTEGER", nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", nullable: true),
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentDrafts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentReactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Reaction = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentReactions_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: true),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsOrganizationTemplate = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentTemplates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ReplyToCommentId",
                table: "Comments",
                column: "ReplyToCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentDrafts_PullRequestId",
                table: "CommentDrafts",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentDrafts_UserId",
                table: "CommentDrafts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentDrafts_UserId_PullRequestId_ReviewThreadId_FilePath_LineNumber",
                table: "CommentDrafts",
                columns: new[] { "UserId", "PullRequestId", "ReviewThreadId", "FilePath", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommentReactions_CommentId",
                table: "CommentReactions",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentReactions_CommentId_Username_Reaction",
                table: "CommentReactions",
                columns: new[] { "CommentId", "Username", "Reaction" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommentTemplates_IsOrganizationTemplate",
                table: "CommentTemplates",
                column: "IsOrganizationTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_CommentTemplates_UserId",
                table: "CommentTemplates",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Comments_ReplyToCommentId",
                table: "Comments",
                column: "ReplyToCommentId",
                principalTable: "Comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Comments_ReplyToCommentId",
                table: "Comments");

            migrationBuilder.DropTable(
                name: "CommentDrafts");

            migrationBuilder.DropTable(
                name: "CommentReactions");

            migrationBuilder.DropTable(
                name: "CommentTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Comments_ReplyToCommentId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EditCount",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "EditedAt",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "ReplyToCommentId",
                table: "Comments");
        }
    }
}
