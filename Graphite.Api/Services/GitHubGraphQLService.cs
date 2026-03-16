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
    Task<GitHubCommentData> AddPullRequestCommentAsync(string organization, string repository, long pullRequestNumber, string body, string? path, int? line, string accessToken);
    Task<GitHubCommentData> AddPullRequestReviewCommentAsync(string organization, string repository, long pullRequestNumber, string body, string path, int line, string accessToken);
    Task<bool> ResolveReviewThreadAsync(string organization, string repository, string threadId, bool resolved, string accessToken);
    Task<bool> UnresolveReviewThreadAsync(string organization, string repository, string threadId, string accessToken);
    Task<(string? OverallStatus, List<GitHubCheckRunData> CheckRuns)> GetCheckStatusAsync(string organization, string repository, long pullRequestNumber, string accessToken);
    Task<bool> ConvertPullRequestToReadyForReviewAsync(string organization, string repository, long pullRequestNumber, string accessToken);
    Task<string?> GetFileContentAsync(string organization, string repository, string branch, string path, string accessToken);
    Task<GitHubPendingReviewData?> GetPendingReviewAsync(string organization, string repository, long pullRequestNumber, string userLogin, string accessToken);
    Task<GitHubPendingReviewCommentData> AddPendingReviewCommentAsync(string organization, string repository, long pullRequestNumber, string body, string path, int line, string accessToken);
    Task<bool> DeletePendingReviewCommentAsync(string organization, string repository, string commentId, string accessToken);
    Task<bool> SubmitPendingReviewAsync(string organization, string repository, string reviewId, string state, string? body, string accessToken);
    Task<bool> DeletePendingReviewAsync(string organization, string repository, string reviewId, string accessToken);
    Task<bool> UpdateReviewCommentAsync(string organization, string repository, long commentId, string body, string accessToken);
    Task<bool> DeleteReviewCommentAsync(string organization, string repository, long commentId, string accessToken);
    Task<List<GitHubCommentData>> GetIssueCommentsAsync(string organization, string repository, long pullRequestNumber, string accessToken);
    Task<bool> UpdateIssueCommentAsync(string organization, string repository, long commentId, string body, string accessToken);
    Task<bool> DeleteIssueCommentAsync(string organization, string repository, long commentId, string accessToken);
}

public record FileLineContent(int LineNumber, string Content);

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
        var allComments = new List<GitHubCommentData>();

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

            foreach (var thread in reviewThreads)
            {
                foreach (var comment in thread.Comments)
                {
                    allComments.Add(new GitHubCommentData(
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching review thread comments for PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
        }

        try
        {
            var issueComments = await GetIssueCommentsAsync(organization, repository, pullRequestNumber, accessToken);
            allComments.AddRange(issueComments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching issue comments for PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
        }

        return allComments;
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
                                id
                                author {{
                                    login
                                    avatarUrl(size: 40)
                                }}
                                body
                                createdAt
                                updatedAt
                                pullRequestReview {{
                                    id
                                    state
                                }}
                            }}
                        }}
                    }}
                "
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.github.com/graphql", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("GitHub GraphQL API error: {StatusCode} - {Response}", response.StatusCode, responseString);
                throw new Exception($"GitHub API error: {response.StatusCode}");
            }

            var jsonResponse = JsonDocument.Parse(responseString);

            if (jsonResponse.RootElement.TryGetProperty("errors", out var errors))
            {
                var errorMessage = errors.EnumerateArray().FirstOrDefault().GetProperty("message").GetString();
                _logger.LogError("GraphQL mutation error: {Error}", errorMessage);
                throw new Exception($"GitHub GraphQL error: {errorMessage}");
            }

            var data = jsonResponse.RootElement.GetProperty("data");
            var commentData = data.GetProperty("addPullRequestReviewThreadReply").GetProperty("comment");
            var authorData = commentData.GetProperty("author");

            // Check if the reply is part of a pending review
            bool isPending = false;
            string? pendingReviewId = null;
            if (commentData.TryGetProperty("pullRequestReview", out var reviewRef) && 
                reviewRef.ValueKind != JsonValueKind.Null)
            {
                pendingReviewId = reviewRef.GetProperty("id").GetString();
                var state = reviewRef.GetProperty("state").GetString();
                isPending = state == "PENDING";
                _logger.LogInformation("Reply added to review {ReviewId} with state {State}", pendingReviewId, state);
            }

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
                commentData.GetProperty("updatedAt").GetString() != null 
                    ? DateTime.Parse(commentData.GetProperty("updatedAt").GetString()!)
                    : null,
                isPending,
                pendingReviewId,
                commentData.GetProperty("id").GetString()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding reply to thread {ThreadId} for PR {Organization}/{Repository}#{PullRequestNumber}", reviewThreadId, organization, repository, pullRequestNumber);
            throw;
        }
    }

    public async Task<GitHubCommentData> AddPullRequestCommentAsync(string organization, string repository, long pullRequestNumber, string body, string? path, int? line, string accessToken)
    {
        if (!string.IsNullOrEmpty(path) && line.HasValue)
        {
            return await AddPullRequestReviewCommentAsync(organization, repository, pullRequestNumber, body, path, line.Value, accessToken);
        }

        try
        {
            var restClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
            {
                Credentials = new Octokit.Credentials(accessToken)
            };

            var comment = await restClient.Issue.Comment.Create(organization, repository, (int)pullRequestNumber, body);

            return new GitHubCommentData(
                comment.Id,
                null,
                comment.User.Login,
                comment.User.AvatarUrl,
                comment.Body,
                null,
                null,
                false,
                comment.CreatedAt.UtcDateTime,
                comment.UpdatedAt.Value.UtcDateTime
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            throw;
        }
    }

    public async Task<GitHubCommentData> AddPullRequestReviewCommentAsync(string organization, string repository, long pullRequestNumber, string body, string path, int line, string accessToken)
    {
        try
        {
            var prNodeId = await GetPullRequestIdAsync(organization, repository, (int)pullRequestNumber, accessToken);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Graphite-PR-Dashboard");

            var bodyEscaped = body.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
            var pathEscaped = path.Replace("\\", "\\\\").Replace("\"", "\\\"");

            var query = new
            {
                query = $@"
                    mutation {{
                        addPullRequestReviewThread(input: {{
                            pullRequestId: ""{prNodeId}"",
                            path: ""{pathEscaped}"",
                            line: {line},
                            side: RIGHT,
                            body: ""{bodyEscaped}""
                        }}) {{
                            thread {{
                                id
                                path
                                line
                                comments(first: 1) {{
                                    nodes {{
                                        databaseId
                                        body
                                        createdAt
                                        updatedAt
                                        author {{
                                            login
                                            avatarUrl(size: 40)
                                        }}
                                    }}
                                }}
                            }}
                        }}
                    }}
                "
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.github.com/graphql", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("GitHub GraphQL API error: {StatusCode} - {Response}", response.StatusCode, responseString);
                throw new Exception($"GitHub API error: {response.StatusCode}");
            }

            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

            if (jsonResponse.TryGetProperty("errors", out var errors))
            {
                var errorMessage = errors.EnumerateArray().FirstOrDefault().GetProperty("message").GetString();
                _logger.LogError("GitHub GraphQL mutation error: {Errors}", errorMessage);
                throw new Exception($"GitHub GraphQL error: {errorMessage}");
            }

            var data = jsonResponse.GetProperty("data");
            var threadData = data.GetProperty("addPullRequestReviewThread").GetProperty("thread");
            var threadId = threadData.GetProperty("id").GetString();
            var commentsData = threadData.GetProperty("comments").GetProperty("nodes");
            var commentData = commentsData.EnumerateArray().First();

            var authorData = commentData.GetProperty("author");

            return new GitHubCommentData(
                commentData.GetProperty("databaseId").GetInt64(),
                threadId,
                authorData.GetProperty("login").GetString() ?? string.Empty,
                authorData.GetProperty("avatarUrl").GetString() ?? string.Empty,
                commentData.GetProperty("body").GetString() ?? string.Empty,
                threadData.GetProperty("path").GetString(),
                threadData.GetProperty("line").GetInt32(),
                false,
                DateTime.Parse(commentData.GetProperty("createdAt").GetString() ?? string.Empty),
                DateTime.Parse(commentData.GetProperty("updatedAt").GetString() ?? string.Empty)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding review comment to PR {Organization}/{Repository}#{PullRequestNumber} at {Path}:{Line}", organization, repository, pullRequestNumber, path, line);
            throw;
        }
    }

    private async Task<string> GetPullRequestIdAsync(string organization, string repository, int pullRequestNumber, string accessToken)
    {
        var connection = CreateConnection(accessToken);

        var query = new Query()
            .Repository(repository, organization)
            .PullRequest(pullRequestNumber)
            .Select(pr => pr.Id)
            .Compile();

        var id = await connection.Run(query);
        return id.Value;
    }

    private async Task<(string NodeId, string HeadCommitOid)> GetPullRequestInfoAsync(string organization, string repository, int pullRequestNumber, string accessToken)
    {
        var connection = CreateConnection(accessToken);

        var query = new Query()
            .Repository(repository, organization)
            .PullRequest(pullRequestNumber)
            .Select(pr => new
            {
                Id = pr.Id.Value,
                HeadRefOid = pr.HeadRefOid
            })
            .Compile();

        var result = await connection.Run(query);
        return (result.Id, result.HeadRefOid);
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

    public async Task<(string? OverallStatus, List<GitHubCheckRunData> CheckRuns)> GetCheckStatusAsync(string organization, string repository, long pullRequestNumber, string accessToken)
    {
        try
        {
            // Use REST API instead of GraphQL as it has better permissions for CheckRuns
            var restClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
            {
                Credentials = new Octokit.Credentials(accessToken)
            };

            // First, get the PR to find the head SHA
            var pullRequest = await restClient.PullRequest.Get(organization, repository, (int)pullRequestNumber);
            var headSha = pullRequest.Head.Sha;

            // Get all check runs for the head commit
            var checkRunsResponse = await restClient.Check.Run.GetAllForReference(organization, repository, headSha);
            
            var allCheckRuns = new List<GitHubCheckRunData>();

            foreach (var checkRun in checkRunsResponse.CheckRuns)
            {
                var status = checkRun.Status.Value.ToString().ToLowerInvariant();
                var conclusion = checkRun.Conclusion.HasValue 
                    ? checkRun.Conclusion.Value.Value.ToString().ToLowerInvariant()
                    : null;

                allCheckRuns.Add(new GitHubCheckRunData(
                    checkRun.Id.ToString(),
                    checkRun.Name,
                    status,
                    conclusion,
                    checkRun.HtmlUrl,
                    checkRun.StartedAt.UtcDateTime,
                    checkRun.CompletedAt?.UtcDateTime
                ));
            }

            // Calculate overall status based on check runs
            string? overallStatus = null;

            if (allCheckRuns.Count > 0)
            {
                var hasFailure = allCheckRuns.Any(cr => cr.Conclusion == "failure");
                var hasPending = allCheckRuns.Any(cr => cr.Status == "queued" || cr.Status == "in_progress");
                var allCompleted = allCheckRuns.All(cr => cr.Status == "completed");

                if (hasFailure)
                {
                    overallStatus = "FAILURE";
                }
                else if (hasPending)
                {
                    overallStatus = "PENDING";
                }
                else if (allCompleted)
                {
                    var hasSuccess = allCheckRuns.Any(cr => cr.Conclusion == "success");
                    overallStatus = hasSuccess ? "SUCCESS" : "NEUTRAL";
                }
            }

            return (overallStatus, allCheckRuns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching check status for PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            return (null, new List<GitHubCheckRunData>());
        }
    }

    public async Task<bool> ConvertPullRequestToReadyForReviewAsync(string organization, string repository, long pullRequestNumber, string accessToken)
    {
        try
        {
            // First, get the PR node ID using GraphQL
            var connection = CreateConnection(accessToken);

            var prIdQuery = new Query()
                .Repository(repository, organization)
                .PullRequest((Arg<int>)pullRequestNumber)
                .Select(pr => pr.Id.Value)
                .Compile();

            var prNodeId = await connection.Run(prIdQuery);

            if (string.IsNullOrEmpty(prNodeId))
            {
                _logger.LogError("Could not find PR node ID for {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
                return false;
            }

            _logger.LogInformation("Found PR node ID: {PrNodeId} for {Organization}/{Repository}#{PullRequestNumber}", prNodeId, organization, repository, pullRequestNumber);

            // Now execute the convertPullRequestToReadyForReview mutation
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Graphite-PR-Dashboard");

            var query = new
            {
                query = $@"
                    mutation {{
                        markPullRequestReadyForReview(input: {{ pullRequestId: ""{prNodeId}"" }}) {{
                            pullRequest {{
                                id
                                isDraft
                            }}
                        }}
                    }}
                "
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.github.com/graphql", content);

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("GraphQL response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("GraphQL request failed with status {StatusCode}: {Response}", response.StatusCode, responseContent);
                return false;
            }

            // Parse and validate the response
            var jsonResponse = JsonDocument.Parse(responseContent);
            var root = jsonResponse.RootElement;

            // Check for GraphQL errors
            if (root.TryGetProperty("errors", out var errors))
            {
                var errorMessage = errors[0].GetProperty("message").GetString();
                _logger.LogError("GraphQL mutation error: {Error}", errorMessage);
                throw new Exception($"GitHub GraphQL error: {errorMessage}");
            }

            // Verify the mutation succeeded by checking isDraft
            if (root.TryGetProperty("data", out var data) &&
                data.TryGetProperty("convertPullRequestToReadyForReview", out var mutationResult) &&
                mutationResult.TryGetProperty("pullRequest", out var pr))
            {
                var isDraft = pr.GetProperty("isDraft").GetBoolean();
                _logger.LogInformation("PR isDraft after mutation: {IsDraft}", isDraft);

                if (isDraft)
                {
                    _logger.LogWarning("PR is still marked as draft after mutation");
                    return false;
                }
            }

            _logger.LogInformation("Successfully published draft PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing draft PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            throw;
        }
    }

    public async Task<string?> GetFileContentAsync(string organization, string repository, string branch, string path, string accessToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Graphite-PR-Dashboard");

            var escapedPath = path.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var expression = $"{branch}:{escapedPath}";

            var query = new
            {
                query = $@"
                    query {{
                        repository(owner: ""{organization}"", name: ""{repository}"") {{
                            object(expression: ""{expression}"") {{
                                ... on Blob {{
                                    text
                                }}
                            }}
                        }}
                    }}
                "
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.github.com/graphql", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("GraphQL request failed with status {StatusCode}: {Response}", response.StatusCode, errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseContent);

            if (jsonResponse.RootElement.TryGetProperty("errors", out var errors))
            {
                var errorMessage = errors[0].GetProperty("message").GetString();
                _logger.LogError("GraphQL query error: {Error}", errorMessage);
                return null;
            }

            if (jsonResponse.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("repository", out var repoData) &&
                repoData.TryGetProperty("object", out var objectData) &&
                objectData.ValueKind != JsonValueKind.Null)
            {
                return objectData.GetProperty("text").GetString();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching file content for {Organization}/{Repository}/{Path}@{Branch}", organization, repository, path, branch);
            return null;
        }
    }

    public async Task<GitHubPendingReviewData?> GetPendingReviewAsync(string organization, string repository, long pullRequestNumber, string userLogin, string accessToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Graphite-PR-Dashboard");

            var query = new
            {
                query = $@"
                    query {{
                        repository(owner: ""{organization}"", name: ""{repository}"") {{
                            pullRequest(number: {pullRequestNumber}) {{
                                reviews(first: 10, states: [PENDING]) {{
                                    nodes {{
                                        id
                                        state
                                        author {{
                                            login
                                        }}
                                        comments(first: 100) {{
                                            totalCount
                                            nodes {{
                                                id
                                                path
                                                line
                                                body
                                                createdAt
                                                updatedAt
                                                author {{
                                                    login
                                                    avatarUrl(size: 40)
                                                }}
                                            }}
                                        }}
                                    }}
                                }}
                            }}
                        }}
                    }}
                "
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.github.com/graphql", content);
            var responseString = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("GetPendingReview RAW RESPONSE: {Response}", responseString);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("GitHub GraphQL API error: {StatusCode} - {Response}", response.StatusCode, responseString);
                return null;
            }

            var jsonResponse = JsonDocument.Parse(responseString);

            if (jsonResponse.RootElement.TryGetProperty("errors", out var errors))
            {
                var errorMessage = errors.EnumerateArray().FirstOrDefault().GetProperty("message").GetString();
                _logger.LogError("GraphQL query error: {Error}", errorMessage);
                return null;
            }

            var data = jsonResponse.RootElement.GetProperty("data");
            var repoData = data.GetProperty("repository");
            var prData = repoData.GetProperty("pullRequest");
            var reviewsData = prData.GetProperty("reviews").GetProperty("nodes");

            _logger.LogInformation("GetPendingReview: Found {Count} pending reviews, looking for user {UserLogin}", reviewsData.GetArrayLength(), userLogin);

            if (reviewsData.GetArrayLength() == 0)
            {
                _logger.LogInformation("GetPendingReview: No pending reviews found");
                return null;
            }

            // Filter to find the current user's pending review
            JsonElement myPendingReview = default;
            bool found = false;
            foreach (var review in reviewsData.EnumerateArray())
            {
                if (review.TryGetProperty("author", out var author) &&
                    author.TryGetProperty("login", out var loginProp))
                {
                    var login = loginProp.GetString();
                    _logger.LogInformation("GetPendingReview: Checking review by {Login}, looking for {UserLogin}", login, userLogin);
                    if (string.Equals(login, userLogin, StringComparison.OrdinalIgnoreCase))
                    {
                        myPendingReview = review;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                _logger.LogInformation("GetPendingReview: No pending review found for user {UserLogin}", userLogin);
                return null;
            }

            var reviewId = myPendingReview.GetProperty("id").GetString() ?? string.Empty;

            var comments = new List<GitHubPendingReviewCommentData>();
            
            if (myPendingReview.TryGetProperty("comments", out var commentsConn) && 
                commentsConn.ValueKind != JsonValueKind.Null)
            {
                var totalCount = commentsConn.TryGetProperty("totalCount", out var countProp) ? countProp.GetInt32() : -1;
                _logger.LogInformation("GetPendingReview: Comments connection has total count: {TotalCount}", totalCount);
                
                var commentsNodes = commentsConn.GetProperty("nodes");
                _logger.LogInformation("GetPendingReview: Found {Count} comment nodes in pending review", commentsNodes.GetArrayLength());
                
                foreach (var comment in commentsNodes.EnumerateArray())
                {
                    var authorData = comment.GetProperty("author");
                    
                    string? path = null;
                    int? line = null;
                    
                    if (comment.TryGetProperty("path", out var cPath) && cPath.ValueKind != JsonValueKind.Null)
                    {
                        path = cPath.GetString();
                    }
                    if (comment.TryGetProperty("line", out var cLine) && cLine.ValueKind != JsonValueKind.Null)
                    {
                        line = cLine.GetInt32();
                    }

                    var commentId = comment.GetProperty("id").GetString() ?? string.Empty;
                    var commentBody = comment.GetProperty("body").GetString() ?? string.Empty;
                    
                    _logger.LogInformation("GetPendingReview: Loaded comment {CommentId} body='{Body}' path={Path} line={Line}", 
                        commentId, commentBody.Length > 50 ? commentBody.Substring(0, 50) + "..." : commentBody, path, line);

                    comments.Add(new GitHubPendingReviewCommentData(
                        commentId,
                        path,
                        line,
                        commentBody,
                        authorData.GetProperty("login").GetString() ?? string.Empty,
                        authorData.GetProperty("avatarUrl").GetString(),
                        DateTime.Parse(comment.GetProperty("createdAt").GetString() ?? string.Empty),
                        comment.TryGetProperty("updatedAt", out var updatedProp) && updatedProp.ValueKind != JsonValueKind.Null
                            ? DateTime.Parse(updatedProp.GetString() ?? string.Empty)
                            : null,
                        reviewId,
                        null
                    ));
                }
            }
            else
            {
                _logger.LogWarning("GetPendingReview: No comments property found on pending review");
            }

            _logger.LogInformation("GetPendingReview: Returning {CommentCount} pending comments for review {ReviewId}", 
                comments.Count, reviewId);

            return new GitHubPendingReviewData(reviewId, "PENDING", comments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending review for PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            return null;
        }
    }

    public async Task<GitHubPendingReviewCommentData> AddPendingReviewCommentAsync(string organization, string repository, long pullRequestNumber, string body, string path, int line, string accessToken)
    {
        try
        {
            var prNodeId = await GetPullRequestIdAsync(organization, repository, (int)pullRequestNumber, accessToken);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Graphite-PR-Dashboard");

            var bodyEscaped = body.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
            var pathEscaped = path.Replace("\\", "\\\\").Replace("\"", "\\\"");

            var query = new
            {
                query = $@"
                    mutation {{
                        addPullRequestReviewThread(input: {{
                            pullRequestId: ""{prNodeId}"",
                            path: ""{pathEscaped}"",
                            line: {line},
                            side: RIGHT,
                            body: ""{bodyEscaped}""
                        }}) {{
                            thread {{
                                id
                                path
                                line
                                comments(first: 1) {{
                                    nodes {{
                                        id
                                        databaseId
                                        body
                                        createdAt
                                        updatedAt
                                        pullRequestReview {{
                                            id
                                            state
                                        }}
                                        author {{
                                            login
                                            avatarUrl(size: 40)
                                        }}
                                    }}
                                }}
                            }}
                        }}
                    }}
                "
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.github.com/graphql", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("GitHub GraphQL API error: {StatusCode} - {Response}", response.StatusCode, responseString);
                throw new Exception($"GitHub API error: {response.StatusCode}");
            }

            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

            if (jsonResponse.TryGetProperty("errors", out var errors))
            {
                var errorMessage = errors.EnumerateArray().FirstOrDefault().GetProperty("message").GetString();
                _logger.LogError("GitHub GraphQL mutation error: {Errors}", errorMessage);
                throw new Exception($"GitHub GraphQL error: {errorMessage}");
            }

            var data = jsonResponse.GetProperty("data");
            var mutationData = data.GetProperty("addPullRequestReviewThread");
            var threadData = mutationData.GetProperty("thread");
            var commentsData = threadData.GetProperty("comments").GetProperty("nodes");
            var commentData = commentsData.EnumerateArray().First();
            var authorData = commentData.GetProperty("author");

            string? reviewId = null;
            if (commentData.TryGetProperty("pullRequestReview", out var reviewData) && reviewData.ValueKind != JsonValueKind.Null)
            {
                reviewId = reviewData.GetProperty("id").GetString();
            }

            return new GitHubPendingReviewCommentData(
                commentData.GetProperty("id").GetString() ?? string.Empty,
                threadData.GetProperty("path").GetString(),
                threadData.GetProperty("line").GetInt32(),
                commentData.GetProperty("body").GetString() ?? string.Empty,
                authorData.GetProperty("login").GetString() ?? string.Empty,
                authorData.GetProperty("avatarUrl").GetString() ?? string.Empty,
                DateTime.Parse(commentData.GetProperty("createdAt").GetString() ?? string.Empty),
                DateTime.Parse(commentData.GetProperty("updatedAt").GetString() ?? string.Empty),
                reviewId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding pending review comment to PR {Organization}/{Repository}#{PullRequestNumber} at {Path}:{Line}", organization, repository, pullRequestNumber, path, line);
            throw;
        }
    }

    public async Task<bool> DeletePendingReviewCommentAsync(string organization, string repository, string commentId, string accessToken)
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
                        deletePullRequestReviewComment(input: {{ id: ""{commentId}"" }}) {{
                            pullRequestReview {{
                                id
                                state
                            }}
                        }}
                    }}
                "
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.github.com/graphql", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("GitHub GraphQL API error deleting comment: {StatusCode} - {Response}", response.StatusCode, responseString);
                throw new Exception($"GitHub API error: {response.StatusCode}");
            }

            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

            if (jsonResponse.TryGetProperty("errors", out var errors))
            {
                var errorMessage = errors.EnumerateArray().FirstOrDefault().GetProperty("message").GetString();
                _logger.LogError("GitHub GraphQL mutation error: {Errors}", errorMessage);
                throw new Exception($"GitHub GraphQL error: {errorMessage}");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pending review comment {CommentId} for {Organization}/{Repository}", commentId, organization, repository);
            throw;
        }
    }

    public async Task<bool> SubmitPendingReviewAsync(string organization, string repository, string reviewId, string state, string? body, string accessToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Graphite-PR-Dashboard");

            var bodyEscaped = body?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") ?? "";
            var eventValue = state.ToUpper() switch
            {
                "APPROVED" => "APPROVE",
                "CHANGES_REQUESTED" => "REQUEST_CHANGES",
                "COMMENT" => "COMMENT",
                _ => "COMMENT"
            };

            var query = new
            {
                query = $@"
                    mutation {{
                        submitPullRequestReview(input: {{ 
                            pullRequestReviewId: ""{reviewId}"",
                            event: {eventValue},
                            body: ""{bodyEscaped}""
                        }}) {{
                            pullRequestReview {{
                                id
                                state
                                submittedAt
                            }}
                        }}
                    }}
                "
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.github.com/graphql", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("GitHub GraphQL API error submitting review: {StatusCode} - {Response}", response.StatusCode, responseString);
                throw new Exception($"GitHub API error: {response.StatusCode}");
            }

            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

            if (jsonResponse.TryGetProperty("errors", out var errors))
            {
                var errorMessage = errors.EnumerateArray().FirstOrDefault().GetProperty("message").GetString();
                _logger.LogError("GitHub GraphQL mutation error: {Errors}", errorMessage);
                throw new Exception($"GitHub GraphQL error: {errorMessage}");
            }

            _logger.LogInformation("Successfully submitted pending review {ReviewId} for {Organization}/{Repository}", reviewId, organization, repository);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting pending review {ReviewId} for {Organization}/{Repository}", reviewId, organization, repository);
            throw;
        }
    }

    public async Task<bool> DeletePendingReviewAsync(string organization, string repository, string reviewId, string accessToken)
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
                        deletePullRequestReview(input: {{ pullRequestReviewId: ""{reviewId}"" }}) {{
                            pullRequestReview {{
                                id
                            }}
                        }}
                    }}
                "
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.github.com/graphql", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("GitHub GraphQL API error deleting review: {StatusCode} - {Response}", response.StatusCode, responseString);
                throw new Exception($"GitHub API error: {response.StatusCode}");
            }

            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

            if (jsonResponse.TryGetProperty("errors", out var errors))
            {
                var errorMessage = errors.EnumerateArray().FirstOrDefault().GetProperty("message").GetString();
                _logger.LogError("GitHub GraphQL mutation error: {Errors}", errorMessage);
                throw new Exception($"GitHub GraphQL error: {errorMessage}");
            }

            _logger.LogInformation("Successfully deleted pending review {ReviewId} for {Organization}/{Repository}", reviewId, organization, repository);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pending review {ReviewId} for {Organization}/{Repository}", reviewId, organization, repository);
            throw;
        }
    }

    public async Task<bool> UpdateReviewCommentAsync(string organization, string repository, long commentId, string body, string accessToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Graphite-PR-Dashboard");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

            var payload = new { body };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            
            var response = await httpClient.PatchAsync($"https://api.github.com/repos/{organization}/{repository}/pulls/comments/{commentId}", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("GitHub REST API error updating comment: {StatusCode} - {Response}", response.StatusCode, responseString);
                throw new Exception($"GitHub API error: {response.StatusCode}");
            }

            _logger.LogInformation("Successfully updated review comment {CommentId} for {Organization}/{Repository}", commentId, organization, repository);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating review comment {CommentId} for {Organization}/{Repository}", commentId, organization, repository);
            throw;
        }
    }

    public async Task<bool> DeleteReviewCommentAsync(string organization, string repository, long commentId, string accessToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Graphite-PR-Dashboard");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

            var response = await httpClient.DeleteAsync($"https://api.github.com/repos/{organization}/{repository}/pulls/comments/{commentId}");

            if (!response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                _logger.LogError("GitHub REST API error deleting comment: {StatusCode} - {Response}", response.StatusCode, responseString);
                throw new Exception($"GitHub API error: {response.StatusCode}");
            }

            _logger.LogInformation("Successfully deleted review comment {CommentId} for {Organization}/{Repository}", commentId, organization, repository);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review comment {CommentId} for {Organization}/{Repository}", commentId, organization, repository);
            throw;
        }
    }

    public async Task<List<GitHubCommentData>> GetIssueCommentsAsync(string organization, string repository, long pullRequestNumber, string accessToken)
    {
        try
        {
            var connection = CreateConnection(accessToken);

            var query = new Query()
                .Repository(repository, organization)
                .PullRequest((Arg<int>)pullRequestNumber)
                .Comments(100, null, null, null)
                .Nodes
                .Select(c => new
                {
                    c.DatabaseId,
                    Author = c.Author.Login,
                    AuthorAvatar = c.Author.AvatarUrl(40),
                    c.Body,
                    c.CreatedAt,
                    c.UpdatedAt
                })
                .Compile();

            var comments = await connection.Run(query);

            return comments.Select(c => new GitHubCommentData(
                c.DatabaseId ?? 0,
                null,
                c.Author,
                c.AuthorAvatar,
                c.Body,
                null,
                null,
                false,
                c.CreatedAt.UtcDateTime,
                c.UpdatedAt.UtcDateTime
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching issue comments for PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            return new List<GitHubCommentData>();
        }
    }

    public async Task<bool> UpdateIssueCommentAsync(string organization, string repository, long commentId, string body, string accessToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Graphite-PR-Dashboard");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

            var payload = new { body };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            
            var response = await httpClient.PatchAsync($"https://api.github.com/repos/{organization}/{repository}/issues/comments/{commentId}", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("GitHub REST API error updating issue comment: {StatusCode} - {Response}", response.StatusCode, responseString);
                throw new Exception($"GitHub API error: {response.StatusCode}");
            }

            _logger.LogInformation("Successfully updated issue comment {CommentId} for {Organization}/{Repository}", commentId, organization, repository);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating issue comment {CommentId} for {Organization}/{Repository}", commentId, organization, repository);
            throw;
        }
    }

    public async Task<bool> DeleteIssueCommentAsync(string organization, string repository, long commentId, string accessToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Graphite-PR-Dashboard");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

            var response = await httpClient.DeleteAsync($"https://api.github.com/repos/{organization}/{repository}/issues/comments/{commentId}");

            if (!response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                _logger.LogError("GitHub REST API error deleting issue comment: {StatusCode} - {Response}", response.StatusCode, responseString);
                throw new Exception($"GitHub API error: {response.StatusCode}");
            }

            _logger.LogInformation("Successfully deleted issue comment {CommentId} for {Organization}/{Repository}", commentId, organization, repository);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting issue comment {CommentId} for {Organization}/{Repository}", commentId, organization, repository);
            throw;
        }
    }
}

public record GitHubPendingReviewData(
    string GitHubId,
    string State,
    List<GitHubPendingReviewCommentData> Comments
);

public record GitHubPendingReviewCommentData(
    string GitHubId,
    string? Path,
    int? Line,
    string Body,
    string Author,
    string? AuthorAvatar,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? ReviewId = null,
    string? ThreadId = null
);
