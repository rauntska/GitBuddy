using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GitBuddy.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomationRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequiredStatusChecks",
                table: "RepositoryRules",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Labels",
                table: "PullRequests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AutomationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RepositoryFullName = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    DryRun = table.Column<bool>(type: "boolean", nullable: false),
                    TriggerType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Conditions = table.Column<string>(type: "text", nullable: false),
                    Actions = table.Column<string>(type: "text", nullable: false),
                    MinIntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastTriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TriggerCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationRules_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CheckFailureStreaks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PullRequestId = table.Column<int>(type: "integer", nullable: false),
                    CheckName = table.Column<string>(type: "text", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckFailureStreaks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutomationRunLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AutomationRuleId = table.Column<int>(type: "integer", nullable: false),
                    PullRequestId = table.Column<int>(type: "integer", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConditionsMatched = table.Column<bool>(type: "boolean", nullable: false),
                    DryRun = table.Column<bool>(type: "boolean", nullable: false),
                    ActionsTaken = table.Column<string>(type: "text", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationRunLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationRunLogs_AutomationRules_AutomationRuleId",
                        column: x => x.AutomationRuleId,
                        principalTable: "AutomationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRules_CreatedBy",
                table: "AutomationRules",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRules_Enabled_TriggerType",
                table: "AutomationRules",
                columns: new[] { "Enabled", "TriggerType" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRules_RepositoryFullName",
                table: "AutomationRules",
                column: "RepositoryFullName");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRunLogs_AutomationRuleId_PullRequestId_Idempotenc~",
                table: "AutomationRunLogs",
                columns: new[] { "AutomationRuleId", "PullRequestId", "IdempotencyKey" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRunLogs_AutomationRuleId_TriggeredAt",
                table: "AutomationRunLogs",
                columns: new[] { "AutomationRuleId", "TriggeredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckFailureStreaks_PullRequestId_CheckName",
                table: "CheckFailureStreaks",
                columns: new[] { "PullRequestId", "CheckName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutomationRunLogs");

            migrationBuilder.DropTable(
                name: "CheckFailureStreaks");

            migrationBuilder.DropTable(
                name: "AutomationRules");

            migrationBuilder.DropColumn(
                name: "RequiredStatusChecks",
                table: "RepositoryRules");

            migrationBuilder.DropColumn(
                name: "Labels",
                table: "PullRequests");
        }
    }
}
