using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graphite.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentResolvedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PendingCount",
                table: "Comments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ResolvedCount",
                table: "Comments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingCount",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "ResolvedCount",
                table: "Comments");
        }
    }
}
