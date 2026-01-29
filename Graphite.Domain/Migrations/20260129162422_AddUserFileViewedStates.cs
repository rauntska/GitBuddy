using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graphite.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFileViewedStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserFileViewedStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileDiffId = table.Column<int>(type: "INTEGER", nullable: false),
                    ViewedState = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFileViewedStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFileViewedStates_FileDiffs_FileDiffId",
                        column: x => x.FileDiffId,
                        principalTable: "FileDiffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFileViewedStates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserFileViewedStates_FileDiffId",
                table: "UserFileViewedStates",
                column: "FileDiffId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFileViewedStates_UserId_FileDiffId",
                table: "UserFileViewedStates",
                columns: new[] { "UserId", "FileDiffId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserFileViewedStates");
        }
    }
}
