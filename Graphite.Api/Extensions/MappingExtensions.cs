using Graphite.Api.DTOs;
using Graphite.Domain.Models;

namespace Graphite.Api.Extensions;

public static class MappingExtensions
{
    public static PullRequestDto ToDto(this PullRequest pr)
    {
        return new PullRequestDto(
            pr.Id,
            pr.GitHubId,
            pr.Title,
            pr.Repository,
            pr.Author,
            pr.AuthorAvatar,
            pr.Status,
            pr.Draft,
            pr.Url,
            pr.Additions,
            pr.Deletions,
            pr.ChangedFiles,
            pr.CreatedAt,
            pr.UpdatedAt,
            pr.LastSyncedAt,
            pr.Reviews.Select(r => new ReviewDto(
                r.Id,
                r.Reviewer,
                r.ReviewerAvatar,
                r.State,
                r.SubmittedAt
            )).ToList(),
            pr.Comment != null ? new CommentDto(
                pr.Comment.Id,
                pr.Comment.Count,
                pr.Comment.ResolvedCount,
                pr.Comment.PendingCount,
                pr.Comment.LastUpdated
            ) : null
        );
    }

    public static List<PullRequestDto> ToDto(this List<PullRequest> pullRequests)
    {
        return pullRequests.Select(pr => pr.ToDto()).ToList();
    }
}