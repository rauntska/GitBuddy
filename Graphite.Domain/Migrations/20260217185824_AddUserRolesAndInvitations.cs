using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graphite.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRolesAndInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvitationId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AllowedUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    GitHubUsername = table.Column<string>(type: "TEXT", nullable: true),
                    AssignedRole = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllowedUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllowedUsers_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    GitHubUsername = table.Column<string>(type: "TEXT", nullable: true),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    AssignedRole = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AcceptedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invitations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_InvitationId",
                table: "Users",
                column: "InvitationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_AllowedUsers_CreatedByUserId",
                table: "AllowedUsers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AllowedUsers_Email",
                table: "AllowedUsers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_AllowedUsers_GitHubUsername",
                table: "AllowedUsers",
                column: "GitHubUsername");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_AcceptedAt",
                table: "Invitations",
                column: "AcceptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_CreatedByUserId",
                table: "Invitations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Email",
                table: "Invitations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_GitHubUsername",
                table: "Invitations",
                column: "GitHubUsername");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Token",
                table: "Invitations",
                column: "Token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Invitations_InvitationId",
                table: "Users",
                column: "InvitationId",
                principalTable: "Invitations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Invitations_InvitationId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "AllowedUsers");

            migrationBuilder.DropTable(
                name: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Users_InvitationId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "InvitationId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");
        }
    }
}
