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
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PullRequestId);
        });

        modelBuilder.Entity<ReviewThread>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PullRequestId);
            entity.HasIndex(e => e.GitHubId).IsUnique();
            entity.HasIndex(e => e.State);
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
            
            entity.HasOne(e => e.Preferences)
                .WithOne(p => p.User)
                .HasForeignKey<UserPreferences>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
        });
    }
}