using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GitBuddy.Domain.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_PostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GitHubConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Organization = table.Column<string>(type: "text", nullable: false),
                    PersonalAccessToken = table.Column<string>(type: "text", nullable: false),
                    LastRefresh = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefreshIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    AppId = table.Column<string>(type: "text", nullable: false),
                    PrivateKey = table.Column<string>(type: "text", nullable: false),
                    InstallationId = table.Column<string>(type: "text", nullable: false),
                    UseGitHubApp = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteOldPRs = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitHubConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PullRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GitHubId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Repository = table.Column<string>(type: "text", nullable: false),
                    Author = table.Column<string>(type: "text", nullable: false),
                    AuthorAvatar = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Draft = table.Column<bool>(type: "boolean", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Additions = table.Column<long>(type: "bigint", nullable: false),
                    Deletions = table.Column<long>(type: "bigint", nullable: false),
                    ChangedFiles = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    SourceBranch = table.Column<string>(type: "text", nullable: false),
                    TargetBranch = table.Column<string>(type: "text", nullable: false),
                    MergeableState = table.Column<string>(type: "text", nullable: true),
                    ChecksStatus = table.Column<string>(type: "text", nullable: true),
                    IsMerged = table.Column<bool>(type: "boolean", nullable: false),
                    MergedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequiredApprovingReviews = table.Column<int>(type: "integer", nullable: true),
                    CurrentApprovingReviews = table.Column<int>(type: "integer", nullable: false),
                    HasUnresolvedThreads = table.Column<bool>(type: "boolean", nullable: false),
                    IsMergeReady = table.Column<bool>(type: "boolean", nullable: false),
                    MergeBlockReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepositoryRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Repository = table.Column<string>(type: "text", nullable: false),
                    BranchPattern = table.Column<string>(type: "text", nullable: true),
                    RulesetName = table.Column<string>(type: "text", nullable: true),
                    RequiresApprovingReviews = table.Column<bool>(type: "boolean", nullable: false),
                    RequiredApprovingReviewCount = table.Column<int>(type: "integer", nullable: true),
                    RequiresStatusChecks = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepositoryRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CheckRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GitHubId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Conclusion = table.Column<string>(type: "text", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PullRequestId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckRuns_PullRequests_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileDiffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PullRequestId = table.Column<int>(type: "integer", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    OldPath = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Additions = table.Column<int>(type: "integer", nullable: false),
                    Deletions = table.Column<int>(type: "integer", nullable: false),
                    Changes = table.Column<int>(type: "integer", nullable: false),
                    Patch = table.Column<string>(type: "text", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileDiffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileDiffs_PullRequests_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PullRequestId = table.Column<int>(type: "integer", nullable: false),
                    GitHubId = table.Column<string>(type: "text", nullable: false),
                    Reviewer = table.Column<string>(type: "text", nullable: false),
                    ReviewerAvatar = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_PullRequests_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewThreads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PullRequestId = table.Column<int>(type: "integer", nullable: false),
                    GitHubId = table.Column<string>(type: "text", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    Line = table.Column<int>(type: "integer", nullable: true),
                    DiffSide = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    IsOutdated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FirstCommentAuthor = table.Column<string>(type: "text", nullable: false),
                    FirstCommentBody = table.Column<string>(type: "text", nullable: false),
                    CommentCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewThreads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewThreads_PullRequests_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PullRequestId = table.Column<int>(type: "integer", nullable: false),
                    ReviewThreadId = table.Column<int>(type: "integer", nullable: true),
                    GitHubId = table.Column<long>(type: "bigint", nullable: false),
                    Author = table.Column<string>(type: "text", nullable: false),
                    AuthorAvatar = table.Column<string>(type: "text", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Path = table.Column<string>(type: "text", nullable: true),
                    Line = table.Column<int>(type: "integer", nullable: true),
                    IsOutdated = table.Column<bool>(type: "boolean", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EditCount = table.Column<int>(type: "integer", nullable: false),
                    ReplyToCommentId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_Comments_ReplyToCommentId",
                        column: x => x.ReplyToCommentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comments_PullRequests_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_ReviewThreads_ReviewThreadId",
                        column: x => x.ReviewThreadId,
                        principalTable: "ReviewThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentReactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommentId = table.Column<int>(type: "integer", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Reaction = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "AllowedUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: true),
                    GitHubUsername = table.Column<string>(type: "text", nullable: true),
                    AssignedRole = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllowedUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommentDrafts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PullRequestId = table.Column<int>(type: "integer", nullable: false),
                    ReviewThreadId = table.Column<int>(type: "integer", nullable: true),
                    FilePath = table.Column<string>(type: "text", nullable: true),
                    LineNumber = table.Column<int>(type: "integer", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentDrafts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommentTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsOrganizationTemplate = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    GitHubUsername = table.Column<string>(type: "text", nullable: true),
                    Token = table.Column<string>(type: "text", nullable: false),
                    AssignedRole = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: true),
                    ProviderUserId = table.Column<string>(type: "text", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AccessToken = table.Column<string>(type: "text", nullable: true),
                    PersonalAccessToken = table.Column<string>(type: "text", nullable: true),
                    InvitationId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PendingReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PullRequestId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    GitHubReviewId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "UserFileViewedStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    FileDiffId = table.Column<int>(type: "integer", nullable: false),
                    ViewedState = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    DiffViewMode = table.Column<string>(type: "text", nullable: false),
                    FileTreeWidth = table.Column<int>(type: "integer", nullable: false),
                    CommentsPanelWidth = table.Column<int>(type: "integer", nullable: false),
                    FileTreeVisible = table.Column<bool>(type: "boolean", nullable: false),
                    ListViewMode = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PendingReviewId = table.Column<int>(type: "integer", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    Line = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "IX_CheckRuns_GitHubId",
                table: "CheckRuns",
                column: "GitHubId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheckRuns_PullRequestId",
                table: "CheckRuns",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentDrafts_PullRequestId",
                table: "CommentDrafts",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentDrafts_UserId",
                table: "CommentDrafts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentDrafts_UserId_PullRequestId_ReviewThreadId_FilePath_~",
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
                name: "IX_Comments_GitHubId",
                table: "Comments",
                column: "GitHubId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_PullRequestId",
                table: "Comments",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ReplyToCommentId",
                table: "Comments",
                column: "ReplyToCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ReviewThreadId",
                table: "Comments",
                column: "ReviewThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentTemplates_IsOrganizationTemplate",
                table: "CommentTemplates",
                column: "IsOrganizationTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_CommentTemplates_UserId",
                table: "CommentTemplates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FileDiffs_PullRequestId",
                table: "FileDiffs",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_FileDiffs_PullRequestId_Path",
                table: "FileDiffs",
                columns: new[] { "PullRequestId", "Path" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_PullRequests_GitHubId",
                table: "PullRequests",
                column: "GitHubId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PullRequests_Status",
                table: "PullRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequests_UpdatedAt",
                table: "PullRequests",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryRules_LastSyncedAt",
                table: "RepositoryRules",
                column: "LastSyncedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryRules_Repository_BranchPattern",
                table: "RepositoryRules",
                columns: new[] { "Repository", "BranchPattern" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_GitHubId",
                table: "Reviews",
                column: "GitHubId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_PullRequestId",
                table: "Reviews",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewThreads_GitHubId",
                table: "ReviewThreads",
                column: "GitHubId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewThreads_PullRequestId",
                table: "ReviewThreads",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewThreads_State",
                table: "ReviewThreads",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_UserFileViewedStates_FileDiffId",
                table: "UserFileViewedStates",
                column: "FileDiffId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFileViewedStates_UserId_FileDiffId",
                table: "UserFileViewedStates",
                columns: new[] { "UserId", "FileDiffId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

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
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AllowedUsers_Users_CreatedByUserId",
                table: "AllowedUsers",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommentDrafts_Users_UserId",
                table: "CommentDrafts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommentTemplates_Users_UserId",
                table: "CommentTemplates",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Users_CreatedByUserId",
                table: "Invitations",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Users_CreatedByUserId",
                table: "Invitations");

            migrationBuilder.DropTable(
                name: "AllowedUsers");

            migrationBuilder.DropTable(
                name: "CheckRuns");

            migrationBuilder.DropTable(
                name: "CommentDrafts");

            migrationBuilder.DropTable(
                name: "CommentReactions");

            migrationBuilder.DropTable(
                name: "CommentTemplates");

            migrationBuilder.DropTable(
                name: "GitHubConfigs");

            migrationBuilder.DropTable(
                name: "PendingComments");

            migrationBuilder.DropTable(
                name: "RepositoryRules");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "UserFileViewedStates");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "PendingReviews");

            migrationBuilder.DropTable(
                name: "FileDiffs");

            migrationBuilder.DropTable(
                name: "ReviewThreads");

            migrationBuilder.DropTable(
                name: "PullRequests");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Invitations");
        }
    }
}
