using Octokit.GraphQL;
using Octokit;

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
                    pr.UpdatedAt,
                    pr.Body,
                    HeadRefName = pr.HeadRefName,
                    BaseRefName = pr.BaseRefName,
                    pr.Mergeable
                })
                .Compile();

            var prs = await connection.Run(prQuery);
            
            logger.LogInformation("Found {PrsCount} open PRs in repository {RepoName}", prs.Count(), repoName);

            foreach (var pr in prs)
            {
                var reviews = await GetReviewsAsync(organization, repoName, pr.Number, token);
                var reviewData = reviews;

                var status = DeterminePrStatus(pr.IsDraft, reviewData);

                var reviewThreads = await GetReviewThreadsAsync(organization, repoName, pr.Number, token);

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
                    reviewThreads,
                    pr.Body ?? string.Empty,
                    pr.HeadRefName,
                    pr.BaseRefName,
                    pr.Mergeable.ToString()
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


    public async Task<List<GitHubReviewThreadData>> GetReviewThreadsAsync(string organization, string repository, int pullRequestNumber, string token)
    {
        try
        {
            var connection = new Octokit.GraphQL.Connection(new Octokit.GraphQL.ProductHeaderValue("Graphite-PR-Dashboard"), token);

            var reviewThreadsQuery = new Octokit.GraphQL.Query()
                .Repository(repository, organization)
                .PullRequest(pullRequestNumber)
                .ReviewThreads(100, null, null, null)
                .Nodes
                .Select(rt => new
                {
                    Id = rt.Id.Value,
                    Path = rt.Path,
                    Line = rt.Line,
                    IsResolved = rt.IsResolved,
                    IsOutdated = rt.IsOutdated,
                    Comments = rt.Comments(100, null, null, null, null).Nodes.Select(c => new
                    {
                        DatabaseId = c.DatabaseId,
                        Author = c.Author.Login,
                        AuthorAvatar = c.Author.AvatarUrl(40),
                        Body = c.Body,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt
                    }).ToList()
                })
                .Compile();

            var reviewThreads = await connection.Run(reviewThreadsQuery);

            return reviewThreads.Select(rt =>
            {
                var firstComment = rt.Comments.FirstOrDefault();
                var state = rt.IsResolved ? "RESOLVED" : "UNRESOLVED";

                return new GitHubReviewThreadData(
                    rt.Id,
                    rt.Path ?? string.Empty,
                    rt.Line,
                    state,
                    rt.IsResolved,
                    rt.IsOutdated,
                    firstComment?.CreatedAt.UtcDateTime ?? DateTime.UtcNow,
                    firstComment?.UpdatedAt.UtcDateTime,
                    firstComment?.Author ?? string.Empty,
                    firstComment?.Body ?? string.Empty,
                    rt.Comments.Count
                );
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching GitHub review threads for PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            return new List<GitHubReviewThreadData>();
        }
    }

    public async Task<List<GitHubCommentData>> GetCommentsAsync(string organization, string repository, int pullRequestNumber, string token)
    {
        try
        {
            var connection = new Octokit.GraphQL.Connection(new Octokit.GraphQL.ProductHeaderValue("Graphite-PR-Dashboard"), token);

            var reviewThreadsQuery = new Octokit.GraphQL.Query()
                .Repository(repository, organization)
                .PullRequest(pullRequestNumber)
                .ReviewThreads(100, null, null, null)
                .Nodes
                .Select(rt => new
                {
                    Id = rt.Id.Value,
                    Path = rt.Path,
                    Line = rt.Line,
                    IsResolved = rt.IsResolved,
                    IsOutdated = rt.IsOutdated,
                    Comments = rt.Comments(100, null, null, null, null).Nodes.Select(c => new
                    {
                        DatabaseId = c.DatabaseId,
                        FullDatabaseId = c.FullDatabaseId,
                        Author = c.Author.Login,
                        AuthorAvatar = c.Author.AvatarUrl(40),
                        Body = c.Body,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt
                    }).ToList()
                })
                .Compile();

            var reviewThreads = await connection.Run(reviewThreadsQuery);

            var comments = new List<GitHubCommentData>();

            foreach (var thread in reviewThreads)
            {
                foreach (var comment in thread.Comments)
                {
                    comments.Add(new GitHubCommentData(
                        comment.DatabaseId ?? 0,
                        thread.Id,
                        comment.Author,
                        comment.AuthorAvatar,
                        comment.Body,
                        thread.Path,
                        thread.Line,
                        thread.IsOutdated,
                        comment.CreatedAt.UtcDateTime,
                        comment.UpdatedAt.UtcDateTime
                    ));
                }
            }

            return comments;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching GitHub comments for PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            return new List<GitHubCommentData>();
        }
    }

    public async Task<List<GitHubFileDiffData>> GetFileDiffsAsync(string organization, string repository, int pullRequestNumber, string token)
    {
        try
        {
            var client = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
            {
                Credentials = new Credentials(token)
            };

            var files = await client.PullRequest.Files(organization, repository, pullRequestNumber);

            return files.Select(file =>
            {
                var status = file.Status.ToLowerInvariant();
                if (status == "removed") status = "deleted";
                if (status == "renamed") status = "renamed";
                if (status == "added") status = "added";
                if (status == "modified") status = "modified";

                return new GitHubFileDiffData(
                    file.FileName,
                    file.PreviousFileName,
                    status,
                    file.Additions,
                    file.Deletions,
                    file.Changes,
                    file.Patch
                );
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching file diffs for PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            return new List<GitHubFileDiffData>();
        }
    }
}
