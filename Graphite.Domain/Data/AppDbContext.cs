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
    }
}