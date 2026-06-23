using GitBuddy.Api.DTOs;

namespace GitBuddy.Api.Services;

public interface IBranchWithoutPRService
{
    Task<List<BranchWithoutPRDto>> GetBranchesWithoutPRsAsync(
        string? organization,
        string accessToken,
        HashSet<(string Repo, string SourceBranch)> openPRBranches,
        int recentDays);
}

public class BranchWithoutPRService(
    IGitHubService gitHubService,
    ILogger<BranchWithoutPRService> logger) : IBranchWithoutPRService
{
    private readonly SemaphoreSlim _branchFetchLimiter = new(5);
    private readonly SemaphoreSlim _commitLookupLimiter = new(10);

    private static readonly HashSet<string> IgnoredBranchNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "production",
        "staging",
        "develop",
        "dev",
        "release",
        "uat",
        "pre-production",
        "preprod",
        "qa",
        "test",
    };

    private static bool IsIgnoredBranch(string branchName)
    {
        if (IgnoredBranchNames.Contains(branchName))
            return true;

        var prefix = branchName.Split('/')[0];
        return prefix is "release" or "hotfix";
    }

    public async Task<List<BranchWithoutPRDto>> GetBranchesWithoutPRsAsync(
        string? organization,
        string accessToken,
        HashSet<(string Repo, string SourceBranch)> openPRBranches,
        int recentDays)
    {
        var repos = !string.IsNullOrEmpty(organization)
            ? await gitHubService.GetOrganizationRepositoriesAsync(organization, accessToken)
            : await gitHubService.GetAccessibleRepositoriesAsync(accessToken);

        var tasks = repos.Select(repo => ProcessRepoAsync(repo, accessToken, openPRBranches, recentDays));
        var results = await Task.WhenAll(tasks);

        return results.SelectMany(r => r).ToList();
    }

    private async Task<List<BranchWithoutPRDto>> ProcessRepoAsync(
        GitHubRepositoryData repo,
        string accessToken,
        HashSet<(string Repo, string SourceBranch)> openPRBranches,
        int recentDays)
    {
        // Phase 1: Fetch branches for this repo
        List<GitHubBranchData> branches;
        await _branchFetchLimiter.WaitAsync();
        try
        {
            branches = await gitHubService.GetBranchesAsync(repo.Owner, repo.Name, accessToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not fetch branches for {Owner}/{Repo}, skipping", repo.Owner, repo.Name);
            return [];
        }
        finally
        {
            _branchFetchLimiter.Release();
        }

        // Phase 2: Filter candidates
        var defaultBranch = repo.DefaultBranch ?? "main";
        var cutoff = recentDays > 0 ? DateTime.UtcNow.AddDays(-recentDays) : DateTime.MinValue;

        var candidates = branches
            .Where(b => !b.Protected)
            .Where(b => b.Name != defaultBranch)
            .Where(b => !IsIgnoredBranch(b.Name))
            .Where(b => !openPRBranches.Contains((repo.Name, b.Name)))
            .ToList();

        if (candidates.Count == 0)
            return [];

        if (recentDays <= 0)
        {
            return candidates.Select(b => new BranchWithoutPRDto(
                repo.Owner, repo.Name, repo.FullName, b.Name, defaultBranch, null)).ToList();
        }

        // Phase 3: Fetch commit dates in parallel and filter by recency
        var dateTasks = candidates.Select(b => FetchWithDateAsync(
            repo.Owner, repo.Name, repo.FullName, b, defaultBranch, accessToken, cutoff));
        var results = await Task.WhenAll(dateTasks);

        return results.Where(r => r != null).Cast<BranchWithoutPRDto>().ToList();
    }

    private async Task<BranchWithoutPRDto?> FetchWithDateAsync(
        string owner, string repo, string repoFullName,
        GitHubBranchData branch, string defaultBranch,
        string accessToken, DateTime cutoff)
    {
        await _commitLookupLimiter.WaitAsync();
        try
        {
            var commitDate = await gitHubService.GetCommitDateAsync(owner, repo, branch.Sha, accessToken);

            if (commitDate == null || commitDate < cutoff)
                return null;

            return new BranchWithoutPRDto(owner, repo, repoFullName, branch.Name, defaultBranch, commitDate);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not fetch commit date for {Owner}/{Repo}/{Branch}, including without date",
                owner, repo, branch.Name);
            return new BranchWithoutPRDto(owner, repo, repoFullName, branch.Name, defaultBranch, null);
        }
        finally
        {
            _commitLookupLimiter.Release();
        }
    }
}
