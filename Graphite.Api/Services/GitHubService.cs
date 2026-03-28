using Graphite.Domain.Models;
using System.Globalization;
using Mono.TextTemplating;
using Octokit;
using Octokit.GraphQL;
using Octokit.GraphQL.Core;
using GraphQLConnection = Octokit.GraphQL.Connection;
using GraphQLProductHeaderValue = Octokit.GraphQL.ProductHeaderValue;
using GraphQLQuery = Octokit.GraphQL.Query;

namespace Graphite.Api.Services;

public class GitHubService(
    ILogger<GitHubService> logger,
    IGitHubTokenService tokenService,
    IGitHubGraphQLService graphQlService,
    IPullRequestStatusService statusService)
    : IGitHubService
{
    public async Task<List<GitHubPRData>> GetOpenPullRequestsAsync(string organization, GitHubConfig config)
    {
        var accessToken = await tokenService.GetAccessTokenAsync(config);
        var repoNames = await GetRepositoryNamesAsync(organization, accessToken);
        var pullRequests = new List<GitHubPRData>();

        foreach (var repoName in repoNames)
        {
            var prs = await GetPullRequestsForRepositoryAsync(organization, repoName, accessToken, config);
            pullRequests.AddRange(prs);
        }

        return pullRequests.OrderByDescending(pr => pr.UpdatedAt).ToList();
    }

    public async Task<GitHubPRData?> GetPullRequestAsync(string organization, string repository, long pullRequestNumber, GitHubConfig config)
    {
        var accessToken = await tokenService.GetAccessTokenAsync(config);
        var connection = new GraphQLConnection(new GraphQLProductHeaderValue("Graphite-PR-Dashboard"), accessToken);

        var prQuery = new GraphQLQuery()
            .Repository(repository, organization)
            .PullRequest((Arg<int>)pullRequestNumber) 
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

        try
        {
            var pr = await connection.Run(prQuery);
            return await CreatePullRequestDataAsync(organization, repository, pr, config);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            return null;
        }
    }

    private static async Task<IEnumerable<string>> GetRepositoryNamesAsync(string organization, string accessToken)
    {
        var connection = new GraphQLConnection(new GraphQLProductHeaderValue("Graphite-PR-Dashboard"), accessToken);

        var reposQuery = new GraphQLQuery()
            .Organization(organization)
            .Repositories(first: 100)
            .Nodes
            .Select(repo => repo.Name)
            .Compile();

        return await connection.Run(reposQuery);
    }

    private async Task<List<GitHubPRData>> GetPullRequestsForRepositoryAsync(string organization, string repoName, string accessToken, GitHubConfig config)
    {
        var connection = new GraphQLConnection(new GraphQLProductHeaderValue("Graphite-PR-Dashboard"), accessToken);

        var prQuery = new GraphQLQuery()
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

        var pullRequests = new List<GitHubPRData>();

        foreach (var pr in prs)
        {
            var prData = await CreatePullRequestDataAsync(organization, repoName, pr, config);
            pullRequests.Add(prData);
        }

        return pullRequests;
    }

    private async Task<GitHubPRData> CreatePullRequestDataAsync(string organization, string repoName, dynamic pr, GitHubConfig config)
    {
        var reviews = await GetReviewsAsync(organization, repoName, pr.Number, config);
        var status = statusService.DeterminePrStatus("",false,pr.IsDraft, false, reviews);
        var reviewThreads = await GetReviewThreadsAsync(organization, repoName, pr.Number, config);

        var checkResult = await GetCheckStatusAsync(organization, repoName, pr.Number, config);
        var checkStatus = checkResult.Item1;
        var checkRuns = checkResult.Item2;

        return new GitHubPRData(
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
            reviews,
            reviewThreads,
            pr.Body ?? string.Empty,
            pr.HeadRefName,
            pr.BaseRefName,
            pr.Mergeable.ToString(),
            checkStatus,
            checkRuns
        );
    }

    public async Task<List<GitHubReviewData>> GetReviewsAsync(string organization, string repository,
        long pullRequestNumber, GitHubConfig config)
    {
        var accessToken = await tokenService.GetAccessTokenAsync(config);
        return await graphQlService.GetReviewsAsync(organization, repository, pullRequestNumber, accessToken);
    }

    public async Task<List<GitHubReviewThreadData>> GetReviewThreadsAsync(string organization, string repository,
        long pullRequestNumber, GitHubConfig config)
    {
        var accessToken = await tokenService.GetAccessTokenAsync(config);
        return await graphQlService.GetReviewThreadsAsync(organization, repository, pullRequestNumber, accessToken);
    }

    public async Task<List<GitHubCommentData>> GetCommentsAsync(string organization, string repository,
        long pullRequestNumber, GitHubConfig config)
    {
        var accessToken = await tokenService.GetAccessTokenAsync(config);
        return await graphQlService.GetCommentsAsync(organization, repository, pullRequestNumber, accessToken);
    }

    public async Task<List<GitHubFileDiffData>> GetFileDiffsAsync(string organization, string repository,
        long pullRequestNumber, GitHubConfig config, string? userAccessToken = null)
    {
        try
        {
            logger.LogInformation("Fetching file diffs for {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);

            var accessToken = await GetTokenForFileDiffsAsync(config, userAccessToken);

            logger.LogInformation("Using {TokenType} token", string.IsNullOrEmpty(userAccessToken) ? "app/installation" : "user");

            var (prFiles, viewedStates) = await FetchFileDiffsAndViewedStatesAsync(organization, repository, pullRequestNumber, accessToken);

            return MapFileDiffsToData(prFiles, viewedStates);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching file diffs for PR {Organization}/{Repository}#{PullRequestNumber}",
                organization, repository, pullRequestNumber);
            return new List<GitHubFileDiffData>();
        }
    }

    private async Task<string> GetTokenForFileDiffsAsync(GitHubConfig config, string? userAccessToken)
    {
        return string.IsNullOrEmpty(userAccessToken)
            ? await tokenService.GetAccessTokenAsync(config)
            : userAccessToken;
    }

    private async Task<(IReadOnlyList<Octokit.PullRequestFile> prFiles, IEnumerable<dynamic> viewedStates)> FetchFileDiffsAndViewedStatesAsync(
        string organization, string repository, long pullRequestNumber, string accessToken)
    {
        var restClient = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
        {
            Credentials = new Credentials(accessToken)
        };

        var connection = new GraphQLConnection(
            new GraphQLProductHeaderValue("Graphite-PR-Dashboard"),
            accessToken);

        var restTask = restClient.PullRequest.Files(organization, repository, (int)pullRequestNumber);

        var filesQuery = new GraphQLQuery()
            .Repository(repository, organization)
            .PullRequest((Arg<int>)pullRequestNumber)
            .Files(100, null, null, null)
            .Nodes
            .Select(f => new
            {
                f.Path,
                ViewerViewedState = f.ViewerViewedState.ToString()
            })
            .Compile();

        var graphqlTask = connection.Run(filesQuery);

        await Task.WhenAll(restTask, graphqlTask);

        return (await restTask, await graphqlTask);
    }

    private static List<GitHubFileDiffData> MapFileDiffsToData(
        IReadOnlyList<Octokit.PullRequestFile> prFiles,
        IEnumerable<dynamic> viewedStates)
    {
        var viewedStateDict = viewedStates.ToDictionary(v => (string)v.Path, v => (string)v.ViewerViewedState);

        return prFiles.Select(file =>
        {
            var status = DetermineFileStatus(file.Status.ToString());
            var viewedState = viewedStateDict.TryGetValue(file.FileName, out var state) ? state : "UNVIEWED";

            return new GitHubFileDiffData(
                file.FileName,
                file.PreviousFileName,
                status,
                file.Additions,
                file.Deletions,
                file.Changes,
                file.Patch,
                viewedState
            );
        }).ToList();
    }

    public async Task MarkFileAsViewedAsync(string organization, string repository, int pullRequestNumber, string path, GitHubConfig config, string? userAccessToken = null)
    {
        var accessToken = await GetTokenForFileDiffsAsync(config, userAccessToken);
        var connection = new GraphQLConnection(new GraphQLProductHeaderValue("Graphite-PR-Dashboard"), accessToken);

        var pullRequestId = await GetPullRequestIdAsync(organization, repository, pullRequestNumber, accessToken);

        var mutation = new Mutation()
            .MarkFileAsViewed(new Octokit.GraphQL.Model.MarkFileAsViewedInput
            {
                PullRequestId = new ID(pullRequestId),
                Path = path
            })
            .Select(m => m.PullRequest.Id)
            .Compile();

        var result = await connection.Run(mutation);
    }

    public async Task UnmarkFileAsViewedAsync(string organization, string repository, int pullRequestNumber, string path, GitHubConfig config, string? userAccessToken = null)
    {
        var accessToken = await GetTokenForFileDiffsAsync(config, userAccessToken);
        var connection = new GraphQLConnection(new GraphQLProductHeaderValue("Graphite-PR-Dashboard"), accessToken);

        var pullRequestId = await GetPullRequestIdAsync(organization, repository, pullRequestNumber, accessToken);

        var mutation = new Mutation()
            .UnmarkFileAsViewed(new Octokit.GraphQL.Model.UnmarkFileAsViewedInput
            {
                PullRequestId = new ID(pullRequestId),
                Path = path
            })
            .Select(m => m.PullRequest.Id)
            .Compile();

        await connection.Run(mutation);
    }

    public async Task SubmitPullRequestReviewAsync(string organization, string repository, long pullRequestNumber, string state, string? body, GitHubConfig config, string userAccessToken)
    {
        var restClient = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
        {
            Credentials = new Octokit.Credentials(userAccessToken)
        };

        var reviewEvent = MapReviewStateToRestEvent(state);

        var reviewBody = body?.Trim() ?? string.Empty;

        await restClient.PullRequest.Review.Create(organization, repository, (int)pullRequestNumber, new PullRequestReviewCreate
        {
            Body = reviewBody,
            Event = reviewEvent
        });

        logger.LogInformation("Successfully submitted review {State} for PR {Organization}/{Repository}#{PullRequestNumber}", state, organization, repository, pullRequestNumber);
    }

    private async Task<string> GetPullRequestIdAsync(string organization, string repository, int pullRequestNumber, string accessToken)
    {
        var connection = new GraphQLConnection(new GraphQLProductHeaderValue("Graphite-PR-Dashboard"), accessToken);

        var query = new GraphQLQuery()
            .Repository(repository, organization)
            .PullRequest(pullRequestNumber)
            .Select(pr => pr.Id)
            .Compile();

        var id = await connection.Run(query);
        return id.Value;
    }

    private static string DetermineFileStatus(string status)
    {
        return status.ToLower() switch
        {
            "added" => "added",
            "removed" => "deleted",
            "renamed" => "renamed",
            _ => "modified"
        };
    }

    private static PullRequestReviewEvent MapReviewStateToRestEvent(string state)
    {
        return state.ToUpper() switch
        {
            "APPROVED" => PullRequestReviewEvent.Approve,
            "CHANGES_REQUESTED" => PullRequestReviewEvent.RequestChanges,
            "COMMENT" => PullRequestReviewEvent.Comment,
            _ => PullRequestReviewEvent.Comment
        };
    }

    public async Task<GitHubPRStatusData?> GetPullRequestStatusAsync(string organization, string repository, long pullRequestNumber, GitHubConfig config)
    {
        var accessToken = await tokenService.GetAccessTokenAsync(config);
        var connection = new GraphQLConnection(new GraphQLProductHeaderValue("Graphite-PR-Dashboard"), accessToken);

        var prQuery = new GraphQLQuery()
            .Repository(repository, organization)
            .PullRequest((Arg<int>)pullRequestNumber)
            .Select(pr => new
            {
                pr.Number,
                pr.State,
                pr.MergedAt,
                pr.ClosedAt
            })
            .Compile();

        try
        {
            var pr = await connection.Run(prQuery);

            return new GitHubPRStatusData(
                IsMerged: pr.MergedAt.HasValue,
                IsClosed: pr.State == Octokit.GraphQL.Model.PullRequestState.Closed || pr.MergedAt.HasValue,
                IsOpen: pr.State == Octokit.GraphQL.Model.PullRequestState.Open,
                State: pr.State,
                MergedAt: pr.MergedAt?.UtcDateTime,
                ClosedAt: pr.ClosedAt?.UtcDateTime
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching PR status for {Organization}/{Repository}#{PullRequestNumber}",
                organization, repository, pullRequestNumber);
            return null;
        }
    }

    public async Task<GitHubCommentData> AddPullRequestReviewThreadReplyAsync(string organization, string repository, long pullRequestNumber, string reviewThreadId, string body, GitHubConfig config, string userAccessToken)
    {
        return await graphQlService.AddPullRequestReviewThreadReplyAsync(organization, repository, pullRequestNumber, reviewThreadId, body, userAccessToken);
    }

    public async Task<GitHubCommentData> AddPullRequestCommentAsync(string organization, string repository, long pullRequestNumber, string body, string? path, int? line, GitHubConfig config, string userAccessToken)
    {
        return await graphQlService.AddPullRequestCommentAsync(organization, repository, pullRequestNumber, body, path, line, userAccessToken);
    }

    public async Task ResolveReviewThreadAsync(string organization, string repository, string threadId, bool resolved, GitHubConfig config, string userAccessToken)
    {
        await graphQlService.ResolveReviewThreadAsync(organization, repository, threadId, resolved, userAccessToken);
    }

    public async Task UnresolveReviewThreadAsync(string organization, string repository, string threadId, GitHubConfig config, string userAccessToken)
    {
        await graphQlService.UnresolveReviewThreadAsync(organization, repository, threadId, userAccessToken);
    }

    public async Task<(string? OverallStatus, List<GitHubCheckRunData> CheckRuns)> GetCheckStatusAsync(string organization, string repository, long pullRequestNumber, GitHubConfig config)
    {
        var accessToken = await tokenService.GetAccessTokenAsync(config);
        return await graphQlService.GetCheckStatusAsync(organization, repository, pullRequestNumber, accessToken);
    }

    public async Task PublishDraftPullRequestAsync(string organization, string repository, long pullRequestNumber, GitHubConfig config, string userAccessToken)
    {
        await graphQlService.ConvertPullRequestToReadyForReviewAsync(organization, repository, pullRequestNumber, userAccessToken);
    }

    public async Task<GitHubRepoMergeOptions> GetRepositoryMergeOptionsAsync(string organization, string repository, string userAccessToken)
    {
        var connection = new GraphQLConnection(new GraphQLProductHeaderValue("Graphite-PR-Dashboard"), userAccessToken);

        var query = new GraphQLQuery()
            .Repository(repository, organization)
            .Select(repo => new
            {
                repo.MergeCommitAllowed,
                repo.SquashMergeAllowed,
                repo.RebaseMergeAllowed
            })
            .Compile();

        var result = await connection.Run(query);

        var defaultMethod = DetermineDefaultMergeMethod(
            result.MergeCommitAllowed,
            result.SquashMergeAllowed,
            result.RebaseMergeAllowed);

        return new GitHubRepoMergeOptions(
            result.MergeCommitAllowed,
            result.SquashMergeAllowed,
            result.RebaseMergeAllowed,
            defaultMethod
        );
    }

    private static string DetermineDefaultMergeMethod(bool mergeCommitAllowed, bool squashMergeAllowed, bool rebaseMergeAllowed)
    {
        if (squashMergeAllowed)
        {
            return "squash";
        }
        if (mergeCommitAllowed)
        {
            return "merge";
        }
        if (rebaseMergeAllowed)
        {
            return "rebase";
        }
        return "merge";
    }

    public async Task MergePullRequestAsync(string organization, string repository, long pullRequestNumber, string? commitTitle, string? commitMessage, string mergeMethod, string userAccessToken)
    {
        var restClient = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
        {
            Credentials = new Octokit.Credentials(userAccessToken)
        };

        var mergeRequest = new MergePullRequest
        {
            CommitTitle = commitTitle,
            CommitMessage = commitMessage,
            MergeMethod = MapMergeMethod(mergeMethod)
        };

        await restClient.PullRequest.Merge(organization, repository, (int)pullRequestNumber, mergeRequest);

        logger.LogInformation("Successfully merged PR {Organization}/{Repository}#{PullRequestNumber} using {MergeMethod}",
            organization, repository, pullRequestNumber, mergeMethod);
    }

    private static Octokit.PullRequestMergeMethod? MapMergeMethod(string method)
    {
        return method.ToLower() switch
        {
            "squash" => Octokit.PullRequestMergeMethod.Squash,
            "rebase" => Octokit.PullRequestMergeMethod.Rebase,
            _ => Octokit.PullRequestMergeMethod.Merge
        };
    }

    public async Task<GitHubPendingReviewData?> GetPendingReviewAsync(string organization, string repository, long pullRequestNumber, string userLogin, string userAccessToken)
    {
        return await graphQlService.GetPendingReviewAsync(organization, repository, pullRequestNumber, userLogin, userAccessToken);
    }

    public async Task<GitHubPendingReviewCommentData> AddPendingReviewCommentAsync(string organization, string repository, long pullRequestNumber, string body, string path, int line, string side, GitHubConfig config, string userAccessToken)
    {
        var restClient = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
        {
            Credentials = new Octokit.Credentials(userAccessToken)
        };

        var pr = await restClient.PullRequest.Get(organization, repository, (int)pullRequestNumber);
        var commitId = pr.Head.Sha;

        var sideValue = side.ToUpperInvariant() == "LEFT" ? "LEFT" : "RIGHT";
        
        var commentRequest = new
        {
            body,
            commit_id = commitId,
            path,
            line,
            side = sideValue
        };

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {userAccessToken}");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Graphite-PR-Dashboard");
        httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

        var url = $"https://api.github.com/repos/{organization}/{repository}/pulls/{pullRequestNumber}/comments";
        var jsonContent = System.Text.Json.JsonSerializer.Serialize(commentRequest);
        var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync(url, content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("GitHub REST API error creating pending review comment: {StatusCode} - {Response}", response.StatusCode, responseString);
            throw new Exception($"GitHub API error: {response.StatusCode}");
        }

        var jsonResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseString);
        
        var user = jsonResponse.GetProperty("user");
        
        string? reviewId = null;
        if (jsonResponse.TryGetProperty("pull_request_review_id", out var reviewIdElement))
        {
            reviewId = reviewIdElement.GetInt64().ToString();
        }

        return new GitHubPendingReviewCommentData(
            jsonResponse.GetProperty("node_id").GetString() ?? string.Empty,
            jsonResponse.GetProperty("path").GetString(),
            jsonResponse.GetProperty("line").GetInt32(),
            jsonResponse.GetProperty("body").GetString() ?? string.Empty,
            user.GetProperty("login").GetString() ?? string.Empty,
            user.GetProperty("avatar_url").GetString() ?? string.Empty,
            DateTime.Parse(jsonResponse.GetProperty("created_at").GetString() ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
            DateTime.Parse(jsonResponse.GetProperty("updated_at").GetString() ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
            reviewId
        );
    }

    public async Task<bool> DeletePendingReviewCommentAsync(string organization, string repository, string commentId, string userAccessToken)
    {
        return await graphQlService.DeletePendingReviewCommentAsync(organization, repository, commentId, userAccessToken);
    }

    public async Task<bool> SubmitPendingReviewAsync(string organization, string repository, string reviewId, string state, string? body, string userAccessToken)
    {
        return await graphQlService.SubmitPendingReviewAsync(organization, repository, reviewId, state, body, userAccessToken);
    }

    public async Task<bool> DeletePendingReviewAsync(string organization, string repository, string reviewId, string userAccessToken)
    {
        return await graphQlService.DeletePendingReviewAsync(organization, repository, reviewId, userAccessToken);
    }

    public async Task<bool> UpdateReviewCommentAsync(string organization, string repository, long commentId, string body, string userAccessToken)
    {
        return await graphQlService.UpdateReviewCommentAsync(organization, repository, commentId, body, userAccessToken);
    }

    public async Task<bool> DeleteReviewCommentAsync(string organization, string repository, long commentId, string userAccessToken)
    {
        return await graphQlService.DeleteReviewCommentAsync(organization, repository, commentId, userAccessToken);
    }

    public async Task UpdatePullRequestAsync(string organization, string repository, long pullRequestNumber, string? title, string? body, string userAccessToken)
    {
        var restClient = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
        {
            Credentials = new Octokit.Credentials(userAccessToken)
        };

        var update = new PullRequestUpdate();
        if (title != null)
            update.Title = title;
        if (body != null)
            update.Body = body;

        await restClient.PullRequest.Update(organization, repository, (int)pullRequestNumber, update);

        logger.LogInformation("Successfully updated PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
    }

    public async Task<bool> UpdateIssueCommentAsync(string organization, string repository, long commentId, string body, string userAccessToken)
    {
        return await graphQlService.UpdateIssueCommentAsync(organization, repository, commentId, body, userAccessToken);
    }

    public async Task<bool> DeleteIssueCommentAsync(string organization, string repository, long commentId, string userAccessToken)
    {
        return await graphQlService.DeleteIssueCommentAsync(organization, repository, commentId, userAccessToken);
    }

    public async Task<Dictionary<string, GitHubBranchProtectionData?>> GetRepositoryRulesetsAsync(string organization, string repository, GitHubConfig config)
    {
        var result = new Dictionary<string, GitHubBranchProtectionData?>();
        
        try
        {
            var accessToken = await tokenService.GetAccessTokenAsync(config);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Graphite-PR-Dashboard");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

            var rulesetsResponse = await httpClient.GetAsync($"https://api.github.com/repos/{organization}/{repository}/rulesets");
            
            if (!rulesetsResponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to fetch rulesets for {Organization}/{Repository}: {StatusCode}", 
                    organization, repository, rulesetsResponse.StatusCode);
                return result;
            }

            var rulesetsContent = await rulesetsResponse.Content.ReadAsStringAsync();
            var rulesets = System.Text.Json.JsonSerializer.Deserialize<List<GitHubRuleset>>(rulesetsContent, 
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (rulesets == null || rulesets.Count == 0)
            {
                logger.LogInformation("No rulesets found for {Organization}/{Repository}", organization, repository);
                return result;
            }

            foreach (var ruleset in rulesets)
            {
                if (ruleset.Enforcement != "active" && ruleset.Enforcement != "enabled")
                    continue;

                try
                {
                    var rulesetDetailResponse = await httpClient.GetAsync($"https://api.github.com/repos/{organization}/{repository}/rulesets/{ruleset.Id}");
                    
                    if (!rulesetDetailResponse.IsSuccessStatusCode)
                        continue;

                    var rulesetDetailContent = await rulesetDetailResponse.Content.ReadAsStringAsync();
                    var rulesetDetail = System.Text.Json.JsonSerializer.Deserialize<GitHubRulesetDetail>(rulesetDetailContent);

                    if (rulesetDetail == null)
                    {
                        logger.LogWarning("Failed to deserialize ruleset detail for {RulesetId}", ruleset.Id);
                        continue;
                    }

                    logger.LogDebug("Deserialized ruleset '{Name}' with {RuleCount} rules", 
                        rulesetDetail.Name, rulesetDetail.Rules?.Count ?? 0);

                    var pullRequestRule = rulesetDetail.Rules?.FirstOrDefault(r => r.Type == "pull_request");
                    var requiredApprovals = 0;
                    var requiresApprovingReviews = false;

                    if (pullRequestRule?.Parameters != null)
                    {
                        requiredApprovals = pullRequestRule.Parameters.RequiredApprovingReviewCount;
                        requiresApprovingReviews = requiredApprovals > 0;
                        
                        logger.LogDebug("Found pull_request rule: required_approving_review_count = {Count}, requiresApprovingReviews = {Requires}", 
                            requiredApprovals, requiresApprovingReviews);
                    }
                    else
                    {
                        logger.LogWarning("No pull_request rule found in ruleset '{RulesetName}'", rulesetDetail.Name);
                    }

                    var branchPatterns = new List<string>();
                    if (rulesetDetail.Conditions?.RefName?.Include != null && 
                        rulesetDetail.Conditions.RefName.Include.Count > 0)
                    {
                        branchPatterns.AddRange(rulesetDetail.Conditions.RefName.Include);
                    }

                    if (branchPatterns.Count == 0)
                    {
                        branchPatterns.Add("*");
                    }

                    var requiresStatusChecks = rulesetDetail.Rules?.Any(r => r.Type == "required_status_checks") ?? false;

                    foreach (var pattern in branchPatterns)
                    {
                        var normalizedPattern = NormalizeBranchPattern(pattern);
                        result[normalizedPattern] = new GitHubBranchProtectionData(
                            requiresApprovingReviews,
                            requiresApprovingReviews ? requiredApprovals : null,
                            requiresStatusChecks,
                            new List<string>()
                        );
                    }

                    logger.LogInformation("Processed ruleset '{RulesetName}' for {Organization}/{Repository}: requires {RequiredApprovals} approvals, patterns: {Patterns}",
                        rulesetDetail.Name, organization, repository, requiredApprovals, string.Join(", ", branchPatterns));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error fetching ruleset details for {RulesetId} in {Organization}/{Repository}", ruleset.Id, organization, repository);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching repository rulesets for {Organization}/{Repository}", organization, repository);
        }

        return result;
    }

    private static string NormalizeBranchPattern(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return "*";

        if (pattern.StartsWith("refs/heads/"))
        {
            pattern = pattern.Substring(11);
        }

        if (pattern.StartsWith("~"))
        {
            pattern = pattern.Substring(1);
        }

        return pattern;
    }

    private class GitHubRuleset
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public int Id { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("enforcement")]
        public string? Enforcement { get; set; }
    }

    private class GitHubRulesetDetail
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public long Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("target")]
        public string? Target { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("source_type")]
        public string? SourceType { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("source")]
        public string? Source { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("enforcement")]
        public string Enforcement { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("conditions")]
        public GitHubRulesetConditions? Conditions { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("rules")]
        public List<GitHubRule>? Rules { get; set; }
    }

    private class GitHubRulesetConditions
    {
        [System.Text.Json.Serialization.JsonPropertyName("ref_name")]
        public GitHubRefNameCondition? RefName { get; set; }
    }

    private class GitHubRefNameCondition
    {
        [System.Text.Json.Serialization.JsonPropertyName("include")]
        public List<string>? Include { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("exclude")]
        public List<string>? Exclude { get; set; }
    }

    private class GitHubRule
    {
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string? Type { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("parameters")]
        public GitHubPullRequestRuleParameters? Parameters { get; set; }
    }

    private class GitHubPullRequestRuleParameters
    {
        [System.Text.Json.Serialization.JsonPropertyName("required_approving_review_count")]
        public int RequiredApprovingReviewCount { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("dismiss_stale_reviews_on_push")]
        public bool DismissStaleReviewsOnPush { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("require_code_owner_review")]
        public bool RequireCodeOwnerReview { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("require_last_push_approval")]
        public bool RequireLastPushApproval { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("required_review_thread_resolution")]
        public bool RequiredReviewThreadResolution { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("allowed_merge_methods")]
        public List<string>? AllowedMergeMethods { get; set; }
    }

    public async Task<GitHubRequestedReviewersData> GetRequestedReviewersAsync(string organization, string repository, long pullRequestNumber, GitHubConfig config)
    {
        var accessToken = await tokenService.GetAccessTokenAsync(config);
        var restClient = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
        {
            Credentials = new Credentials(accessToken)
        };

        try
        {
            var requestedReviewers = await restClient.PullRequest.ReviewRequest.Get(organization, repository, (int)pullRequestNumber);
            
            var users = requestedReviewers.Users.Select(u => new GitHubRequestedReviewerData(
                u.Login,
                u.AvatarUrl,
                "User"
            )).ToList();

            var teams = requestedReviewers.Teams.Select(t => new GitHubRequestedReviewerData(
                t.Name,
                null,
                "Team"
            )).ToList();

            return new GitHubRequestedReviewersData(users, teams);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching requested reviewers for PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            return new GitHubRequestedReviewersData(new List<GitHubRequestedReviewerData>(), new List<GitHubRequestedReviewerData>());
        }
    }

    public async Task RequestReviewersAsync(string organization, string repository, long pullRequestNumber, List<string> reviewers, string userAccessToken)
    {
        var restClient = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
        {
            Credentials = new Credentials(userAccessToken)
        };

        var request = new Octokit.PullRequestReviewRequest(reviewers, new List<string>());
        await restClient.PullRequest.ReviewRequest.Create(organization, repository, (int)pullRequestNumber, request);

        logger.LogInformation("Successfully requested review from {Reviewers} for PR {Organization}/{Repository}#{PullRequestNumber}", 
            string.Join(", ", reviewers), organization, repository, pullRequestNumber);
    }

    public async Task RemoveReviewersAsync(string organization, string repository, long pullRequestNumber, string username, string userAccessToken)
    {
        var restClient = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
        {
            Credentials = new Credentials(userAccessToken)
        };

        var request = new Octokit.PullRequestReviewRequest(new List<string> { username }, new List<string>());
        await restClient.PullRequest.ReviewRequest.Delete(organization, repository, (int)pullRequestNumber, request);

        logger.LogInformation("Successfully removed reviewer {Username} from PR {Organization}/{Repository}#{PullRequestNumber}", 
            username, organization, repository, pullRequestNumber);
    }

    public async Task<GitHubCollaboratorsAndTeamsData> GetRepositoryCollaboratorsAndTeamsAsync(string organization, string repository, GitHubConfig config)
    {
        var accessToken = await tokenService.GetAccessTokenAsync(config);
        var restClient = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
        {
            Credentials = new Credentials(accessToken)
        };

        var users = new List<GitHubCollaboratorData>();
        var teams = new List<GitHubTeamData>();

        try
        {
            var collaborators = await restClient.Repository.Collaborator.GetAll(organization, repository);
            users = collaborators
                .Where(c => c.Permissions?.Pull == true || c.Permissions?.Push == true || c.Permissions?.Admin == true)
                .Select(c => new GitHubCollaboratorData(c.Login, c.AvatarUrl))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching collaborators for {Organization}/{Repository}", organization, repository);
        }

        try
        {
            var repoTeams = await restClient.Repository.GetAllTeams(organization, repository);
            teams = repoTeams
                .Select(t => new GitHubTeamData(t.Name, t.Slug))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching teams for {Organization}/{Repository}", organization, repository);
        }

        return new GitHubCollaboratorsAndTeamsData(users, teams);
    }

    public async Task<List<GitHubRepositoryData>> GetAccessibleRepositoriesAsync(string userAccessToken)
    {
        var restClient = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
        {
            Credentials = new Credentials(userAccessToken)
        };

        try
        {
            var repos = await restClient.Repository.GetAllForCurrent(new RepositoryRequest
            {
                Type = RepositoryType.All,
                Sort = RepositorySort.Pushed,
                Direction = SortDirection.Descending
            });

            return repos.Select(r => new GitHubRepositoryData(
                r.Id,
                r.Owner.Login,
                r.Name,
                r.FullName,
                r.Description,
                r.Private,
                r.DefaultBranch,
                r.HtmlUrl
            )).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching accessible repositories");
            return [];
        }
    }

    public async Task<List<GitHubRepositoryData>> GetOrganizationRepositoriesAsync(string organization, string accessToken)
    {
        var restClient = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
        {
            Credentials = new Credentials(accessToken)
        };

        try
        {
            var repos = await restClient.Repository.GetAllForOrg(organization);

            return repos.Select(r => new GitHubRepositoryData(
                r.Id,
                r.Owner.Login,
                r.Name,
                r.FullName,
                r.Description,
                r.Private,
                r.DefaultBranch,
                r.HtmlUrl
            )).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching organization repositories for {Organization}", organization);
            return new List<GitHubRepositoryData>();
        }
    }

    public async Task<List<GitHubBranchData>> GetBranchesAsync(string owner, string repository, string userAccessToken)
    {
        var restClient = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
        {
            Credentials = new Credentials(userAccessToken)
        };

        try
        {
            var branches = await restClient.Repository.Branch.GetAll(owner, repository);
            return branches.Select(b => new GitHubBranchData(
                b.Name,
                b.Commit.Sha,
                b.Protected
            )).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching branches for {Owner}/{Repository}", owner, repository);
            return new List<GitHubBranchData>();
        }
    }

    public async Task<GitHubBranchComparisonData?> CompareBranchesAsync(string owner, string repository, string baseBranch, string headBranch, string userAccessToken)
    {
        var restClient = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
        {
            Credentials = new Credentials(userAccessToken)
        };

        try
        {
            var comparison = await restClient.Repository.Commit.Compare(owner, repository, baseBranch, headBranch);

            var commits = comparison.Commits.Select(c => new GitHubCommitData(
                c.Sha,
                c.Commit.Message,
                c.Author?.Login ?? c.Commit.Author?.Name ?? "Unknown",
                c.Commit.Author?.Date.UtcDateTime ?? DateTime.UtcNow
            )).ToList();

            var files = comparison.Files?.Select(f => new GitHubFileDiffData(
                f.Filename,
                f.PreviousFileName,
                f.Status,
                f.Additions,
                f.Deletions,
                f.Changes,
                f.Patch,
                "UNVIEWED"
            )).ToList() ?? new List<GitHubFileDiffData>();

            return new GitHubBranchComparisonData(
                comparison.Status.ToString(),
                comparison.AheadBy,
                comparison.BehindBy,
                comparison.TotalCommits,
                commits,
                files
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error comparing branches {BaseBranch}...{HeadBranch} for {Owner}/{Repository}", 
                baseBranch, headBranch, owner, repository);
            return null;
        }
    }

    public async Task<GitHubPRData> CreatePullRequestAsync(string owner, string repository, string title, string? body, string head, string @base, bool draft, string userAccessToken)
    {
        var restClient = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
        {
            Credentials = new Credentials(userAccessToken)
        };

        try
        {
            var newPR = new NewPullRequest(title, head, @base)
            {
                Body = body,
                Draft = draft
            };

            var pr = await restClient.PullRequest.Create(owner, repository, newPR);

            logger.LogInformation("Successfully created PR {Owner}/{Repository}#{PullRequestNumber}", owner, repository, pr.Number);

            return new GitHubPRData(
                pr.Number,
                pr.Title,
                repository,
                pr.User.Login,
                pr.User.AvatarUrl,
                "AwaitingReview",
                draft,
                pr.HtmlUrl,
                pr.Additions,
                pr.Deletions,
                pr.ChangedFiles,
                pr.CreatedAt.UtcDateTime,
                pr.UpdatedAt.UtcDateTime,
                new List<GitHubReviewData>(),
                new List<GitHubReviewThreadData>(),
                pr.Body ?? string.Empty,
                pr.Head.Ref,
                pr.Base.Ref,
                pr.MergeableState?.ToString(),
                null,
                new List<GitHubCheckRunData>()
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating PR for {Owner}/{Repository}", owner, repository);
            throw;
        }
    }
}
