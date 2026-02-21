using Graphite.Domain.Data;
using Graphite.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Services;

public class RepositoryRuleService : IRepositoryRuleService
{
    private readonly AppDbContext _context;
    private readonly IGitHubService _gitHubService;
    private readonly ILogger<RepositoryRuleService> _logger;

    public RepositoryRuleService(
        AppDbContext context,
        IGitHubService gitHubService,
        ILogger<RepositoryRuleService> logger)
    {
        _context = context;
        _gitHubService = gitHubService;
        _logger = logger;
    }

    public async Task SyncRepositoryRulesAsync()
    {
        _logger.LogInformation("Starting repository rules sync...");

        var config = await _context.GitHubConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            _logger.LogWarning("No GitHub configuration found, skipping repository rules sync");
            return;
        }

        var repositories = await _context.PullRequests
            .Where(pr => !pr.IsMerged)
            .Select(pr => pr.Repository)
            .Distinct()
            .ToListAsync();

        if (!repositories.Any())
        {
            _logger.LogInformation("No open PRs found, skipping repository rules sync");
            return;
        }

        foreach (var repository in repositories)
        {
            try
            {
                var rulesets = await _gitHubService.GetRepositoryRulesetsAsync(
                    config.Organization,
                    repository,
                    config
                );

                _logger.LogInformation("Retrieved {Count} ruleset entries for {Repository}", rulesets.Count, repository);

                foreach (var (pattern, ruleData) in rulesets)
                {
                    if (ruleData == null)
                    {
                        _logger.LogWarning("Skipping null ruleData for pattern {Pattern} in {Repository}", pattern, repository);
                        continue;
                    }

                    _logger.LogDebug("Processing rule for {Repository}/{Pattern}: RequiresApprovingReviews={Requires}, Count={Count}", 
                        repository, pattern, ruleData.RequiresApprovingReviews, ruleData.RequiredApprovingReviewCount);

                    var existingRule = await _context.RepositoryRules
                        .FirstOrDefaultAsync(r => r.Repository == repository && r.BranchPattern == pattern);

                    if (existingRule != null)
                    {
                        existingRule.RequiresApprovingReviews = ruleData.RequiresApprovingReviews;
                        existingRule.RequiredApprovingReviewCount = ruleData.RequiredApprovingReviewCount;
                        existingRule.RequiresStatusChecks = ruleData.RequiresStatusChecks;
                        existingRule.LastSyncedAt = DateTime.UtcNow;
                        
                        _logger.LogDebug("Updated existing rule for {Repository}/{Pattern}: RequiresApprovingReviews={Requires}, Count={Count}", 
                            repository, pattern, existingRule.RequiresApprovingReviews, existingRule.RequiredApprovingReviewCount);
                    }
                    else
                    {
                        var newRule = new RepositoryRule
                        {
                            Repository = repository,
                            BranchPattern = pattern,
                            RequiresApprovingReviews = ruleData.RequiresApprovingReviews,
                            RequiredApprovingReviewCount = ruleData.RequiredApprovingReviewCount,
                            RequiresStatusChecks = ruleData.RequiresStatusChecks,
                            LastSyncedAt = DateTime.UtcNow
                        };
                        _context.RepositoryRules.Add(newRule);
                        
                        _logger.LogDebug("Added new rule for {Repository}/{Pattern}: RequiresApprovingReviews={Requires}, Count={Count}", 
                            repository, pattern, newRule.RequiresApprovingReviews, newRule.RequiredApprovingReviewCount);
                    }
                }

                _logger.LogInformation("Synced repository rules for {Repository}", repository);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing repository rules for {Repository}", repository);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Repository rules sync completed");
    }

    public async Task<int?> GetRequiredApprovalsAsync(string repository, string branch)
    {
        var rules = await _context.RepositoryRules
            .Where(r => r.Repository == repository)
            .ToListAsync();

        foreach (var rule in rules)
        {
            if (MatchesBranchPattern(branch, rule.BranchPattern))
            {
                if (rule.RequiresApprovingReviews)
                {
                    return rule.RequiredApprovingReviewCount ?? 1;
                }
            }
        }

        return null;
    }

    public async Task CalculateMergeReadinessAsync(PullRequest pr)
    {
        var requiredApprovals = await GetRequiredApprovalsAsync(pr.Repository, pr.TargetBranch);
        pr.RequiredApprovingReviews = requiredApprovals;

        pr.CurrentApprovingReviews = pr.Reviews
            .Where(r => r.State == "Approved")
            .GroupBy(r => r.Reviewer)
            .Count();

        pr.HasUnresolvedThreads = pr.ReviewThreads.Any(rt => !rt.IsResolved && !rt.IsOutdated);

        if (pr.Draft)
        {
            pr.IsMergeReady = false;
            pr.MergeBlockReason = "PR is a draft";
        }
        else if (pr.MergeableState == "CONFLICTING")
        {
            pr.IsMergeReady = false;
            pr.MergeBlockReason = "Merge conflicts";
        }
        else if (pr.HasUnresolvedThreads)
        {
            pr.IsMergeReady = false;
            var unresolvedCount = pr.ReviewThreads.Count(rt => !rt.IsResolved && !rt.IsOutdated);
            pr.MergeBlockReason = $"{unresolvedCount} unresolved comment{(unresolvedCount > 1 ? "s" : "")}";
        }
        else if (requiredApprovals.HasValue && pr.CurrentApprovingReviews < requiredApprovals.Value)
        {
            pr.IsMergeReady = false;
            var needed = requiredApprovals.Value - pr.CurrentApprovingReviews;
            pr.MergeBlockReason = $"Need {needed} more approval{(needed > 1 ? "s" : "")} ({pr.CurrentApprovingReviews}/{requiredApprovals})";
        }
        else
        {
            pr.IsMergeReady = true;
            pr.MergeBlockReason = null;
        }
    }

    private static bool MatchesBranchPattern(string branch, string? pattern)
    {
        if (string.IsNullOrEmpty(branch) || string.IsNullOrEmpty(pattern))
            return false;

        if (pattern == "*")
            return true;

        if (pattern == branch)
            return true;

        if (pattern.EndsWith("/*"))
        {
            var prefix = pattern[..^2];
            return branch.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.StartsWith("*/"))
        {
            var suffix = pattern[2..];
            return branch.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.Contains('*'))
        {
            var parts = pattern.Split('*');
            if (parts.Length == 2)
            {
                return branch.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase) &&
                       branch.EndsWith(parts[1], StringComparison.OrdinalIgnoreCase);
            }
        }

        return string.Equals(branch, pattern, StringComparison.OrdinalIgnoreCase);
    }
}
