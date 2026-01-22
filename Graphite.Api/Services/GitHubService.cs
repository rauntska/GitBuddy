using Octokit;

namespace Graphite.Api.Services;

public class GitHubService(ILogger<GitHubService> logger) : IGitHubService
{
    private readonly GitHubClient _client = new(new ProductHeaderValue("Graphite-PR-Dashboard"));

    public async Task<List<GitHubPRData>> GetOpenPullRequestsAsync(string organization, string token)
    {
        _client.Credentials = new Credentials(token);

        var pullRequests = new List<GitHubPRData>();

        var repositories = await _client.Repository.GetAllForOrg(organization);

        foreach (var repo in repositories)
        {
            try
            {
                var prs = await _client.PullRequest.GetAllForRepository(organization, repo.Name, new PullRequestRequest
                {
                    State = ItemStateFilter.Open
                });
              
                logger.LogInformation("Found {PrsCount} open PRs in repository {RepoName}", prs.Count, repo.Name);
                foreach (var pr in prs)
                {
                    var specificPrFromApi = await _client.PullRequest.Get(organization, repo.Name, pr.Number); // Example of fetching a specific PR by number
                    var status = DeterminePRStatus(specificPrFromApi);

                    pullRequests.Add(new GitHubPRData(
                        specificPrFromApi.Number,
                        specificPrFromApi.Title,
                        repo.Name,
                        specificPrFromApi.User.Login,
                        specificPrFromApi.User.AvatarUrl,
                        status,
                        specificPrFromApi.Draft,
                        specificPrFromApi.HtmlUrl,
                        specificPrFromApi.Additions,
                        specificPrFromApi.Deletions,
                        specificPrFromApi.ChangedFiles,
                        specificPrFromApi.CreatedAt.UtcDateTime,
                        specificPrFromApi.UpdatedAt.UtcDateTime
                    ));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching PRs for {repo.Name}: {ex.Message}");
            }
        }

        return pullRequests.OrderByDescending(pr => pr.UpdatedAt).ToList();
    }

    public async Task<List<GitHubReviewData>> GetReviewsAsync(string organization, string repository, int pullRequestNumber, string token)
    {
        _client.Credentials = new Credentials(token);

        try
        {
            var reviews = await _client.PullRequest.Review.GetAll(organization, repository, pullRequestNumber);

            return reviews.Select(r => new GitHubReviewData(
                r.User.Login,
                r.User.AvatarUrl,
                r.State.ToString(),
                r.SubmittedAt.UtcDateTime
            )).ToList();
        }
        catch
        {
            return new List<GitHubReviewData>();
        }
    }

    public async Task<List<GitHubCommentData>> GetCommentsAsync(string organization, string repository, int pullRequestNumber, string token)
    {
        _client.Credentials = new Credentials(token);

        try
        {
            var issueComments = await _client.Issue.Comment.GetAllForIssue(organization, repository, pullRequestNumber);
            var reviewComments = await _client.PullRequest.ReviewComment.GetAll(organization, repository, pullRequestNumber);
            
            DateTime? lastUpdated = null;
            if (issueComments.Any())
            {
                var latestComment = issueComments.OrderByDescending(c => c.UpdatedAt).FirstOrDefault();
                lastUpdated = latestComment?.UpdatedAt?.UtcDateTime;
            }

            if (reviewComments.Any())
            {
                var latestReviewComment = reviewComments.OrderByDescending(c => c.UpdatedAt).FirstOrDefault();
                if (lastUpdated == null || latestReviewComment.UpdatedAt.UtcDateTime > lastUpdated)
                {
                    lastUpdated = latestReviewComment.UpdatedAt.UtcDateTime;
                }
            }

            var resolvedCount = reviewComments.Count(c => c.OriginalCommitId != c.CommitId);
            var pendingCount = reviewComments.Count - resolvedCount;

            return new List<GitHubCommentData>
            {
                new(issueComments.Count + reviewComments.Count, resolvedCount, pendingCount, lastUpdated)
            };
        }
        catch
        {
            return new List<GitHubCommentData>();
        }
    }

    private static string DeterminePRStatus(Octokit.PullRequest pr)
    {
        return pr.Draft ? "Draft" : "Open";
    }
}