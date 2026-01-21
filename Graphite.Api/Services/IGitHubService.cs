using Graphite.Domain.Models;

namespace Graphite.Api.Services;

public interface IGitHubService
{
    Task<List<GitHubPRData>> GetOpenPullRequestsAsync(string organization, string token);
    Task<List<GitHubReviewData>> GetReviewsAsync(string organization, string repository, int pullRequestNumber, string token);
    Task<List<GitHubCommentData>> GetCommentsAsync(string organization, string repository, int pullRequestNumber, string token);
}

public record GitHubPRData(
    int Id,
    string Title,
    string Repository,
    string Author,
    string? AuthorAvatar,
    string Status,
    bool Draft,
    string Url,
    int Additions,
    int Deletions,
    int ChangedFiles,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record GitHubReviewData(
    string Reviewer,
    string? ReviewerAvatar,
    string State,
    DateTime? SubmittedAt
);

public record GitHubCommentData(
    int Count,
    DateTime? LastUpdated
);