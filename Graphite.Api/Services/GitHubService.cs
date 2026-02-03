using Graphite.Domain.Models;
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
        var status = statusService.DeterminePrStatus(pr.IsDraft, reviews);
        var reviewThreads = await GetReviewThreadsAsync(organization, repoName, pr.Number, config);

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
            pr.Mergeable.ToString()
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
}
