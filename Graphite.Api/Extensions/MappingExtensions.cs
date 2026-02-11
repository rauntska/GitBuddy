using Graphite.Api.DTOs;
using Graphite.Domain.Models;

namespace Graphite.Api.Extensions;

public static class MappingExtensions
{
    private static List<ReviewDto> GetLatestReviewsPerReviewer(IEnumerable<Review> reviews)
    {
        return reviews
            .GroupBy(r => r.Reviewer)
            .Select(g => g.OrderByDescending(r => r.SubmittedAt).First())
            .Select(r => new ReviewDto(
                r.Id,
                r.GitHubId,
                r.Reviewer,
                r.ReviewerAvatar,
                r.State,
                r.SubmittedAt
            ))
            .ToList();
    }

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
            GetLatestReviewsPerReviewer(pr.Reviews),
            pr.ReviewThreads.Select(rt => new ReviewThreadDto(
                rt.Id,
                rt.GitHubId,
                rt.Path,
                rt.Line,
                rt.DiffSide.ToString(),
                rt.State,
                rt.IsResolved,
                rt.IsOutdated,
                rt.CreatedAt,
                rt.UpdatedAt,
                rt.FirstCommentAuthor,
                rt.FirstCommentBody,
                rt.CommentCount
            )).ToList(),
            pr.ChecksStatus
        );

    }

    public static List<PullRequestDto> ToDto(this List<PullRequest> pullRequests)
    {
        return pullRequests.Select(pr => pr.ToDto()).ToList();
    }

    public static CommentDto ToDto(this Comment comment)
    {
        return new CommentDto(
            comment.Id,
            comment.GitHubId,
            comment.ReviewThreadId,
            comment.Author,
            comment.AuthorAvatar,
            comment.Body,
            comment.CreatedAt,
            comment.UpdatedAt,
            comment.Path,
            comment.Line,
            comment.IsOutdated
        );
    }

    public static FileDiffDto ToDto(this FileDiff fileDiff, string? viewedState = null, DateTime? viewedAt = null)
    {
        return new FileDiffDto(
            fileDiff.Id,
            fileDiff.Path,
            fileDiff.OldPath,
            fileDiff.Status,
            fileDiff.Additions,
            fileDiff.Deletions,
            fileDiff.Changes,
            fileDiff.Patch,
            fileDiff.Language,
            viewedState,
            viewedAt
        );
    }

    public static PRDetailDto ToDetailDto(this PullRequest pr, List<FileDiff> files, List<Comment> comments, List<UserFileViewedState>? viewedStates = null)
    {
        // Build file diffs with viewed states
        var fileDtos = files.Select(f =>
        {
            var viewedState = viewedStates?.FirstOrDefault(vs => vs.FileDiffId == f.Id);
            return f.ToDto(viewedState?.ViewedState, viewedState?.UpdatedAt);
        }).ToList();

        return new PRDetailDto(
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
            pr.Description,
            pr.SourceBranch,
            pr.TargetBranch,
            pr.MergeableState,
            pr.ChecksStatus,
            GetLatestReviewsPerReviewer(pr.Reviews),
            pr.ReviewThreads.Select(rt => new ReviewThreadDto(
                rt.Id,
                rt.GitHubId,
                rt.Path,
                rt.Line,
                rt.DiffSide.ToString(),
                rt.State,
                rt.IsResolved,
                rt.IsOutdated,
                rt.CreatedAt,
                rt.UpdatedAt,
                rt.FirstCommentAuthor,
                rt.FirstCommentBody,
                rt.CommentCount
            )).ToList(),
            fileDtos,
            comments.Select(c => c.ToDto()).ToList(),
            pr.CheckRuns.Select(cr => new CheckRunDto(
                cr.Id,
                cr.GitHubId,
                cr.Name,
                cr.Status,
                cr.Conclusion,
                cr.Url,
                cr.StartedAt,
                cr.CompletedAt
            )).ToList()
        );
    }
}