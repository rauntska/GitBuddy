using Octokit;
using Octokit.GraphQL;

namespace Graphite.Api.Services;

public class GitHubService(ILogger<GitHubService> logger) : IGitHubService
{
    private readonly GitHubClient _restClient = new(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"));

    public async Task<List<GitHubPRData>> GetOpenPullRequestsAsync(string organization, string token)
    {
        _restClient.Credentials = new Octokit.Credentials(token);

        var pullRequests = new List<GitHubPRData>();

        var repositories = await _restClient.Repository.GetAllForOrg(organization);

        foreach (var repo in repositories)
        {
            try
            {
                var prs = await _restClient.PullRequest.GetAllForRepository(organization, repo.Name, new Octokit.PullRequestRequest
                {
                    State = Octokit.ItemStateFilter.Open
                });

                logger.LogInformation("Found {PrsCount} open PRs in repository {RepoName}", prs.Count, repo.Name);
                foreach (var pr in prs)
                {
                    var specificPrFromApi = await _restClient.PullRequest.Get(organization, repo.Name, pr.Number);
                    var reviews = await _restClient.PullRequest.Review.GetAll(organization, repo.Name, pr.Number);
                    var reviewData = reviews.Select(r => new GitHubReviewData(
                        r.User.Login,
                        r.User.AvatarUrl,
                        r.State.ToString(),
                        r.SubmittedAt.UtcDateTime
                    )).ToList();

                    var status = DeterminePRStatus(specificPrFromApi, reviewData);
                    
                    var comments = await GetCommentsAsync(organization, repo.Name, pr.Number, token);
                    var commentData = comments.FirstOrDefault();

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
                        specificPrFromApi.UpdatedAt.UtcDateTime,
                        reviewData,
                        commentData
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
        _restClient.Credentials = new Octokit.Credentials(token);

        try
        {
            var reviews = await _restClient.PullRequest.Review.GetAll(organization, repository, pullRequestNumber);

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



    private static string DeterminePRStatus(Octokit.PullRequest pr, List<GitHubReviewData> reviews)
    {
        if (pr.Draft) return "Draft";

        var hasApproved = reviews.Any(r => r.State == "APPROVED");
        var hasChangesRequested = reviews.Any(r => r.State == "CHANGES_REQUESTED");
        var hasComments = reviews.Any(r => r.State == "COMMENTED");

        if (hasApproved && !hasChangesRequested) return "Approved";
        if (hasChangesRequested) return "ChangesRequested";
        if (hasComments) return "Reviewed";
        return "AwaitingReview";
    }


    public async Task<List<GitHubCommentData>> GetCommentsAsync(string organization, string repository, int pullRequestNumber, string token)
    {
        _restClient.Credentials = new Octokit.Credentials(token);

        try
        {
            // Use REST API for issue comments (simpler and no expression tree issues)
            var issueComments = await _restClient.Issue.Comment.GetAllForIssue(organization, repository, pullRequestNumber);

            // Use GraphQL for review threads with resolved/pending status
            var connection = new Octokit.GraphQL.Connection(new Octokit.GraphQL.ProductHeaderValue("Graphite-PR-Dashboard"), token);
            var query = new Octokit.GraphQL.Query()
                .Repository(repository, organization)
                .PullRequest(pullRequestNumber)
                .ReviewThreads(first: 100)
                .Nodes
                .Select(t => t.IsResolved)
                .Compile();

            var reviewThreadsResolved = await connection.Run(query);
            var reviewThreadsList = reviewThreadsResolved.ToList();

            var totalIssueComments = issueComments.Count;
            var resolvedCount = reviewThreadsList.Count(isResolved => isResolved);
            var pendingCount = reviewThreadsList.Count(isResolved => !isResolved);
            var totalReviewComments = reviewThreadsList.Count;

            DateTime? lastUpdated = null;
            var allDates = issueComments
                .Where(c => c.UpdatedAt.HasValue)
                .Select(c => c.UpdatedAt!.Value.UtcDateTime)
                .ToList();
            if (allDates.Any())
            {
                lastUpdated = allDates.Max();
            }

            return new List<GitHubCommentData>
            {
                new(totalIssueComments + totalReviewComments, resolvedCount, pendingCount, lastUpdated)
            };
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Error fetching GitHub comments for PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            return new List<GitHubCommentData>();
        }
    }
}
