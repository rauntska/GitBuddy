using Graphite.Api.Services;
using Octokit;

namespace Graphite.Api.Services;

public class GitHubService : IGitHubService
{
    private readonly GitHubClient _client;

    public GitHubService()
    {
        _client = new GitHubClient(new ProductHeaderValue("Graphite-PR-Dashboard"));
    }

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

                foreach (var pr in prs)
                {
                    var status = DeterminePRStatus(pr);

                    pullRequests.Add(new GitHubPRData(
                        pr.Number,
                        pr.Title,
                        repo.Name,
                        pr.User.Login,
                        pr.User.AvatarUrl,
                        status,
                        pr.Draft,
                        pr.HtmlUrl,
                        pr.Additions,
                        pr.Deletions,
                        pr.ChangedFiles,
                        pr.CreatedAt.UtcDateTime,
                        pr.UpdatedAt.UtcDateTime
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
            var comments = await _client.Issue.Comment.GetAllForIssue(organization, repository, pullRequestNumber);
            DateTime? lastUpdated = null;
            if (comments.Any())
            {
                var latestComment = comments.OrderByDescending(c => c.UpdatedAt).FirstOrDefault();
                lastUpdated = latestComment?.UpdatedAt?.UtcDateTime;
            }

            return new List<GitHubCommentData>
            {
                new(comments.Count, lastUpdated)
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