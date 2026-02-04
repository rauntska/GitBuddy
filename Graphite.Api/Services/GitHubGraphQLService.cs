using Octokit.GraphQL;
using Octokit.GraphQL.Core;
using Graphite.Domain.Models;

namespace Graphite.Api.Services;

public interface IGitHubGraphQLService
{
    Task<List<GitHubReviewData>> GetReviewsAsync(string organization, string repository, long pullRequestNumber, string accessToken);
    Task<List<GitHubReviewThreadData>> GetReviewThreadsAsync(string organization, string repository, long pullRequestNumber, string accessToken);
    Task<List<GitHubCommentData>> GetCommentsAsync(string organization, string repository, long pullRequestNumber, string accessToken);
}

public class GitHubGraphQLService : IGitHubGraphQLService
{
    private readonly ILogger<GitHubGraphQLService> _logger;

    public GitHubGraphQLService(ILogger<GitHubGraphQLService> logger)
    {
        _logger = logger;
    }

    public async Task<List<GitHubReviewData>> GetReviewsAsync(string organization, string repository, long pullRequestNumber, string accessToken)
    {
        try
        {
            var connection = CreateConnection(accessToken);

            var query = new Query()
                .Repository(repository, organization)
                .PullRequest((Arg<int>)pullRequestNumber)
                .Reviews(100, null, null, null, null, null)
                .Nodes
                .Select(r => new
                {
                    r.Id,
                    AuthorLogin = r.Author.Login,
                    AuthorAvatar = r.Author.AvatarUrl(40),
                    r.State,
                    r.SubmittedAt
                })
                .Compile();

            var result = await connection.Run(query);

            return result.Select(r => new GitHubReviewData(
                r.Id.Value,
                r.AuthorLogin,
                r.AuthorAvatar,
                r.State.ToString(),
                r.SubmittedAt?.UtcDateTime
            )).ToList();
        }
        catch
        {
            return new List<GitHubReviewData>();
        }
    }

    public async Task<List<GitHubReviewThreadData>> GetReviewThreadsAsync(string organization, string repository, long pullRequestNumber, string accessToken)
    {
        try
        {
            var connection = CreateConnection(accessToken);

            var query = new Query()
                .Repository(repository, organization)
                .PullRequest((Arg<int>)pullRequestNumber)
                .ReviewThreads(100, null, null, null)
                .Nodes
                .Select(rt => new
                {
                    Id = rt.Id.Value,
                    Path = rt.Path,
                    Line = rt.Line,
                    DiffSide = rt.DiffSide,
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

            var reviewThreads = await connection.Run(query);

            return reviewThreads.Select(rt =>
            {
                var firstComment = rt.Comments.FirstOrDefault();
                var state = rt.IsResolved ? "RESOLVED" : "UNRESOLVED";

                return new GitHubReviewThreadData(
                    rt.Id,
                    rt.Path ?? string.Empty,
                    rt.Line,
                    rt.DiffSide,
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
            _logger.LogError(ex, "Error fetching GitHub review threads for PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            return new List<GitHubReviewThreadData>();
        }
    }

    public async Task<List<GitHubCommentData>> GetCommentsAsync(string organization, string repository, long pullRequestNumber, string accessToken)
    {
        try
        {
            var connection = CreateConnection(accessToken);

            var query = new Query()
                .Repository(repository, organization)
                .PullRequest((Arg<int>)pullRequestNumber)
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

            var reviewThreads = await connection.Run(query);

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
            _logger.LogError(ex, "Error fetching GitHub comments for PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            return new List<GitHubCommentData>();
        }
    }

    private static Connection CreateConnection(string accessToken)
    {
        return new Connection(new ProductHeaderValue("Graphite-PR-Dashboard"), accessToken);
    }
}
