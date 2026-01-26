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

    public static CommentDto ToDto(this Comment comment)
    {
        return new CommentDto(
            comment.Id,
            comment.GitHubId,
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

    public static FileDiffDto ToDto(this FileDiff fileDiff)
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
            fileDiff.Language
        );
    }

    public static PRDetailDto ToDetailDto(this PullRequest pr, List<FileDiff> files, List<Comment> comments)
    {
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
            )).ToList(),
            files.Select(f => f.ToDto()).ToList(),
            comments.Select(c => c.ToDto()).ToList()
        );
    }
}