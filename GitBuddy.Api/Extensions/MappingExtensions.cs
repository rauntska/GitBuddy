using GitBuddy.Api.DTOs;
using GitBuddy.Domain.Models;
using GitBuddy.Api.Services;

namespace GitBuddy.Api.Extensions;

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
            pr.ChecksStatus,
            pr.IsMergeReady,
            pr.RequiredApprovingReviews,
            pr.CurrentApprovingReviews,
            pr.HasUnresolvedThreads,
            pr.MergeBlockReason,
            PriorityService.GetEffectivePriorityStatic(pr),
            pr.Priority is not null
        );

    }

    public static PRListUpdateDto ToPRListUpdateDto(this PullRequest pr)
    {
        return new PRListUpdateDto(
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
            PriorityService.GetEffectivePriorityStatic(pr),
            pr.Priority is not null
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

    public static PRDetailDto ToDetailDto(this PullRequest pr, List<FileDiff> files, List<Comment> comments, List<UserFileViewedState>? viewedStates = null, PendingReviewDto? pendingReview = null, int? requiredApprovingReviews = null, int currentApprovingReviews = 0, bool hasUnresolvedThreads = false, bool isMergeReady = false, string? mergeBlockReason = null)
    {
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
            pr.IsMerged,
            pr.MergedAt,
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
            )).ToList(),
            pendingReview,
            requiredApprovingReviews,
            currentApprovingReviews,
            hasUnresolvedThreads,
            isMergeReady,
            mergeBlockReason,
            PriorityService.GetEffectivePriorityStatic(pr),
            pr.Priority is not null
        );
    }

    public static CommentDto ToDto(this GitHubCommentData comment)
    {
        return new CommentDto(
            0,
            comment.GitHubId,
            null,
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
}