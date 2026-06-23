using GitBuddy.Domain.Models;

namespace GitBuddy.Api.Services;

public interface IRepositoryRuleService
{
    Task SyncRepositoryRulesAsync();
    Task<int?> GetRequiredApprovalsAsync(string repository, string branch);
    Task CalculateMergeReadinessAsync(PullRequest pr);
}
