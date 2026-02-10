using Octokit.GraphQL;
using Octokit.GraphQL.Core;
using Graphite.Domain.Models;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Graphite.Api.Services;

public interface IGitHubGraphQLService
{
    Task<List<GitHubReviewData>> GetReviewsAsync(string organization, string repository, long pullRequestNumber, string accessToken);
    Task<List<GitHubReviewThreadData>> GetReviewThreadsAsync(string organization, string repository, long pullRequestNumber, string accessToken);
    Task<List<GitHubCommentData>> GetCommentsAsync(string organization, string repository, long pullRequestNumber, string accessToken);
    Task<GitHubCommentData> AddPullRequestReviewThreadReplyAsync(string organization, string repository, long pullRequestNumber, string reviewThreadId, string body, string accessToken);
    Task<bool> ResolveReviewThreadAsync(string organization, string repository, string threadId, bool resolved, string accessToken);
    Task<bool> UnresolveReviewThreadAsync(string organization, string repository, string threadId, string accessToken);
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

    public async Task<GitHubCommentData> AddPullRequestReviewThreadReplyAsync(string organization, string repository, long pullRequestNumber, string reviewThreadId, string body, string accessToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Graphite-PR-Dashboard");

            var bodyEscaped = body.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");

            var query = new
            {
                query = $@"
                    mutation {{
                        addPullRequestReviewThreadReply(input: {{ body: ""{bodyEscaped}"", pullRequestReviewThreadId: ""{reviewThreadId}"" }}) {{
                            comment {{
                                author {{
                                    login
                                    avatarUrl(size: 40)
                                }}
                                body
                                createdAt
                                updatedAt
                            }}
                        }}
                    }}
                "
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.github.com/graphql", content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            var data = JsonDocument.Parse(JsonSerializer.Serialize(jsonResponse["data"])).RootElement;

            var commentData = data.GetProperty("addPullRequestReviewThreadReply").GetProperty("comment");
            var authorData = commentData.GetProperty("author");

            return new GitHubCommentData(
                0,
                reviewThreadId,
                authorData.GetProperty("login").GetString() ?? string.Empty,
                authorData.GetProperty("avatarUrl").GetString() ?? string.Empty,
                commentData.GetProperty("body").GetString() ?? string.Empty,
                null,
                null,
                false,
                DateTime.Parse(commentData.GetProperty("createdAt").GetString() ?? string.Empty),
                DateTime.Parse(commentData.GetProperty("updatedAt").GetString() ?? string.Empty)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding reply to thread {ThreadId} for PR {Organization}/{Repository}#{PullRequestNumber}", reviewThreadId, organization, repository, pullRequestNumber);
            throw;
        }
    }

    public async Task<bool> ResolveReviewThreadAsync(string organization, string repository, string threadId, bool resolved, string accessToken)
    {
        try
        {
            if (resolved)
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Graphite-PR-Dashboard");

                var query = new
                {
                    query = $@"
                        mutation {{
                            resolveReviewThread(input: {{ threadId: ""{threadId}"" }}) {{
                                thread {{
                                    id
                                    isResolved
                                }}
                            }}
                        }}
                    "
                };

                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("https://api.github.com/graphql", content);
                response.EnsureSuccessStatusCode();
            }
            else
            {
                await UnresolveReviewThreadAsync(organization, repository, threadId, accessToken);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving/unresolving thread {ThreadId} for PR {Organization}/{Repository}", threadId, organization, repository);
            throw;
        }
    }

    public async Task<bool> UnresolveReviewThreadAsync(string organization, string repository, string threadId, string accessToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Graphite-PR-Dashboard");

            var query = new
            {
                query = $@"
                    mutation {{
                        unresolveReviewThread(input: {{ threadId: ""{threadId}"" }}) {{
                            thread {{
                                id
                                isResolved
                            }}
                        }}
                    }}
                "
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.github.com/graphql", content);
            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unresolving thread {ThreadId} for PR {Organization}/{Repository}", threadId, organization, repository);
            throw;
        }
    }
}
