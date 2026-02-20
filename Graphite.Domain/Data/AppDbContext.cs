using Graphite.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Domain.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PullRequest> PullRequests { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<ReviewThread> ReviewThreads { get; set; }
    public DbSet<GitHubConfig> GitHubConfigs { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<FileDiff> FileDiffs { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserPreferences> UserPreferences { get; set; }
    public DbSet<UserFileViewedState> UserFileViewedStates { get; set; }
    public DbSet<Invitation> Invitations { get; set; }
    public DbSet<AllowedUser> AllowedUsers { get; set; }
    public DbSet<CheckRun> CheckRuns { get; set; }
    public DbSet<CommentReaction> CommentReactions { get; set; }
    public DbSet<CommentDraft> CommentDrafts { get; set; }
    public DbSet<CommentTemplate> CommentTemplates { get; set; }
    public DbSet<PendingReview> PendingReviews { get; set; }
    public DbSet<PendingComment> PendingComments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PullRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.GitHubId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UpdatedAt);

            entity.HasMany(e => e.Reviews)
                .WithOne(r => r.PullRequest)
                .HasForeignKey(r => r.PullRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ReviewThreads)
                .WithOne(rt => rt.PullRequest)
                .HasForeignKey(rt => rt.PullRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Comments)
                .WithOne()
                .HasForeignKey(c => c.PullRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.CheckRuns)
                .WithOne(cr => cr.PullRequest)
                .HasForeignKey(cr => cr.PullRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PullRequestId);
            entity.HasIndex(e => e.GitHubId).IsUnique();
        });

        modelBuilder.Entity<ReviewThread>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PullRequestId);
            entity.HasIndex(e => e.GitHubId).IsUnique();
            entity.HasIndex(e => e.State);

            entity.HasMany(e => e.Comments)
                .WithOne(c => c.ReviewThread)
                .HasForeignKey(c => c.ReviewThreadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GitHubConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PersonalAccessToken)
                .IsRequired();
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PullRequestId);
            entity.HasIndex(e => e.GitHubId).IsUnique();
            entity.HasIndex(e => e.ReviewThreadId);
            entity.HasIndex(e => e.ReplyToCommentId);

            entity.HasOne(e => e.ReplyToComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(e => e.ReplyToCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Reactions)
                .WithOne(r => r.Comment)
                .HasForeignKey(r => r.CommentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FileDiff>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PullRequestId);
            entity.HasIndex(e => new { e.PullRequestId, e.Path }).IsUnique();
            
            entity.HasOne(e => e.PullRequest)
                .WithMany()
                .HasForeignKey(e => e.PullRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Role);
            
            entity.HasOne(e => e.Preferences)
                .WithOne(p => p.User)
                .HasForeignKey<UserPreferences>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Invitation)
                .WithOne()
                .HasForeignKey<User>(e => e.InvitationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        modelBuilder.Entity<UserFileViewedState>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.FileDiffId }).IsUnique();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.FileDiff)
                .WithMany(f => f.UserViewStates)
                .HasForeignKey(e => e.FileDiffId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CheckRun>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.GitHubId).IsUnique();
            entity.HasIndex(e => e.PullRequestId);

            entity.HasOne(e => e.PullRequest)
                .WithMany(pr => pr.CheckRuns)
                .HasForeignKey(e => e.PullRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CommentReaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CommentId);
            entity.HasIndex(e => new { e.CommentId, e.Username, e.Reaction }).IsUnique();

            entity.HasOne(e => e.Comment)
                .WithMany(c => c.Reactions)
                .HasForeignKey(e => e.CommentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CommentDraft>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.PullRequestId);
            entity.HasIndex(e => new { e.UserId, e.PullRequestId, e.ReviewThreadId, e.FilePath, e.LineNumber }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CommentTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsOrganizationTemplate);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.GitHubUsername);
            entity.HasIndex(e => e.AcceptedAt);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AllowedUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.GitHubUsername);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PendingReview>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PullRequestId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.GitHubReviewId);

            entity.HasOne(e => e.PullRequest)
                .WithMany()
                .HasForeignKey(e => e.PullRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.PendingComments)
                .WithOne(pc => pc.PendingReview)
                .HasForeignKey(pc => pc.PendingReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PendingComment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PendingReviewId);
            entity.HasIndex(e => new { e.PendingReviewId, e.Path, e.Line });
        });
    }
}