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
            pr.ReviewThreads.Select(rt => new ReviewThreadDto(
                rt.Id,
                rt.GitHubId,
                rt.Path,
                rt.Line,
                rt.State,
                rt.IsResolved,
                rt.IsOutdated,
                rt.CreatedAt,
                rt.UpdatedAt,
                rt.FirstCommentAuthor,
                rt.FirstCommentBody,
                rt.CommentCount
            )).ToList()
        );

    }

    public static List<PullRequestDto> ToDto(this List<PullRequest> pullRequests)
    {
        return pullRequests.Select(pr => pr.ToDto()).ToList();
    }
}