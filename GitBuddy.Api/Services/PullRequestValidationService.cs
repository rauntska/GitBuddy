using GitBuddy.Domain.Data;
using GitBuddy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GitBuddy.Api.Services;

public interface IPullRequestValidationService
{
    Task<(User User, string? AccessToken)> GetRequiredUserWithTokenAsync(ClaimsPrincipal principal);
    Task<User?> GetUserAsync(ClaimsPrincipal principal);
    Task<GitHubConfig> GetRequiredConfigAsync();
    Task<GitHubConfig?> GetConfigAsync();
    Task<PullRequest> GetRequiredPRAsync(int prId);
    Task<PullRequest?> GetPRAsync(int prId);
}

public class PullRequestValidationService(AppDbContext context) : IPullRequestValidationService
{
    public async Task<(User User, string? AccessToken)> GetRequiredUserWithTokenAsync(ClaimsPrincipal principal)
    {
        var user = await GetUserAsync(principal);
        if (user == null)
            throw new UnauthorizedAccessException("User not authenticated");
        
        return (user, user.AccessToken);
    }

    public async Task<User?> GetUserAsync(ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        return await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<GitHubConfig> GetRequiredConfigAsync()
    {
        var config = await GetConfigAsync();
        if (config == null)
            throw new InvalidOperationException("GitHub configuration not found");
        
        return config;
    }

    public async Task<GitHubConfig?> GetConfigAsync()
    {
        return await context.GitHubConfigs.FirstOrDefaultAsync();
    }

    public async Task<PullRequest> GetRequiredPRAsync(int prId)
    {
        var pr = await GetPRAsync(prId);
        if (pr == null)
            throw new KeyNotFoundException("Pull request not found");
        
        return pr;
    }

    public async Task<PullRequest?> GetPRAsync(int prId)
    {
        return await context.PullRequests
            .Include(p => p.Reviews)
            .Include(p => p.ReviewThreads)
            .Include(p => p.CheckRuns)
            .FirstOrDefaultAsync(p => p.Id == prId);
    }
}
