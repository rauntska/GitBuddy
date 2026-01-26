using Octokit.GraphQL;

namespace Graphite.Api.Services;

public class GitHubService(ILogger<GitHubService> logger) : IGitHubService
{

    public async Task<List<GitHubPRData>> GetOpenPullRequestsAsync(string organization, string token)
    {
        var connection = new Octokit.GraphQL.Connection(new Octokit.GraphQL.ProductHeaderValue("Graphite-PR-Dashboard"), token);

        // First, get all repository names
        var reposQuery = new Octokit.GraphQL.Query()
            .Organization(organization)
            .Repositories(first: 100)
            .Nodes
            .Select(repo => repo.Name)
            .Compile();

        var repoNames = await connection.Run(reposQuery);

        var pullRequests = new List<GitHubPRData>();

        foreach (var repoName in repoNames)
        {
            // For each repo, get PRs
            var prQuery = new Octokit.GraphQL.Query()
                .Repository(repoName, organization)
                .PullRequests(first: 100, states: new[] { Octokit.GraphQL.Model.PullRequestState.Open })
                .Nodes
                .Select(pr => new
                {
                    pr.Number,
                    pr.Title,
                    AuthorLogin = pr.Author.Login,
                    AuthorAvatar = "",
                    pr.IsDraft,
                    pr.Url,
                    pr.Additions,
                    pr.Deletions,
                    pr.ChangedFiles,
                    pr.CreatedAt,
                    pr.UpdatedAt
                })
                .Compile();

            var prs = await connection.Run(prQuery);
            
            logger.LogInformation("Found {PrsCount} open PRs in repository {RepoName}", prs.Count(), repoName);

            foreach (var pr in prs)
            {
                var reviews = await GetReviewsAsync(organization, repoName, pr.Number, token);
                var reviewData = reviews;

                var status = DeterminePrStatus(pr.IsDraft, reviewData);

                var comments = await GetCommentsAsync(organization, repoName, pr.Number, token);

                pullRequests.Add(new GitHubPRData(
                    pr.Number,
                    pr.Title,
                    repoName,
                    pr.AuthorLogin,
                    pr.AuthorAvatar,
                    status,
                    pr.IsDraft,
                    pr.Url,
                    pr.Additions,
                    pr.Deletions,
                    pr.ChangedFiles,
                    pr.CreatedAt.UtcDateTime,
                    pr.UpdatedAt.UtcDateTime,
                    reviewData,
                    comments
                ));
            }
        }

        return pullRequests.OrderByDescending(pr => pr.UpdatedAt).ToList();
    }

    public async Task<List<GitHubReviewData>> GetReviewsAsync(string organization, string repository, int pullRequestNumber, string token)
    {
        try
        {
            var connection = new Octokit.GraphQL.Connection(new Octokit.GraphQL.ProductHeaderValue("Graphite-PR-Dashboard"), token);
            var query = new Octokit.GraphQL.Query()
                .Repository(repository, organization)
                .PullRequest(pullRequestNumber)
                .Reviews(100, null, null, null, null, null)
                .Nodes
                .Select(r => new
                {
                    AuthorLogin = r.Author.Login,
                    AuthorAvatar = r.Author.AvatarUrl(40),
                    r.State,
                    r.SubmittedAt
                })
                .Compile();

            var result = await connection.Run(query);

            return result.Select(r => new GitHubReviewData(
                r.AuthorLogin,
                r.AuthorAvatar,
                r.State.ToString(),
                r.SubmittedAt?.UtcDateTime
            )).ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string DeterminePrStatus(bool isDraft, List<GitHubReviewData> reviews)
    {
        if (isDraft) return "Draft";

        var hasApproved = reviews.Any(r => r.State == "Approved");
        var hasChangesRequested = reviews.Any(r => r.State == "ChangesRequested");
        var hasComments = reviews.Any(r => r.State == "Commented");

        if (hasApproved && !hasChangesRequested) return "Approved";
        if (hasChangesRequested) return "ChangesRequested";
        if (hasComments) return "Reviewed";
        return "AwaitingReview";
    }


    public async Task<List<GitHubCommentData>> GetCommentsAsync(string organization, string repository, int pullRequestNumber, string token)
    {
        try
        {
            var connection = new Octokit.GraphQL.Connection(new Octokit.GraphQL.ProductHeaderValue("Graphite-PR-Dashboard"), token);

            // Get issue comments
            var issueCommentsQuery = new Octokit.GraphQL.Query()
                .Repository(repository, organization)
                .PullRequest(pullRequestNumber)
                .Comments(100, null, null, null, null).Nodes
                .Select(c => new
                {
                    GitHubId = (long)c.DatabaseId,
                    Author = c.Author.Login,
                    Body = c.Body,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .Compile();

            var issueComments = await connection.Run(issueCommentsQuery);

            var issueCommentData = issueComments.Select(c => new GitHubCommentData(c.GitHubId, c.Author, c.Body, c.CreatedAt.UtcDateTime, c.UpdatedAt.UtcDateTime, true)).ToList();

            return issueCommentData;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching GitHub comments for PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            return new List<GitHubCommentData>();
        }
    }
}
