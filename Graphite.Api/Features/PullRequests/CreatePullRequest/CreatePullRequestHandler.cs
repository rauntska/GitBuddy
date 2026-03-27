using Graphite.Api.DTOs;
using Graphite.Api.Extensions;
using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;

namespace Graphite.Api.Features.PullRequests.CreatePullRequest;

public class CreatePullRequestHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService)
    : IRequestHandler<CreatePullRequestCommand, CreatePullRequestResult>
{
    public async Task<CreatePullRequestResult> Handle(CreatePullRequestCommand request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);

        if (string.IsNullOrEmpty(accessToken))
        {
            return new CreatePullRequestResult(false, "GitHub access token not found", null, "No access token available");
        }

        try
        {
            var githubPR = await gitHubService.CreatePullRequestAsync(
                request.Owner,
                request.Repository,
                request.Title,
                request.Body,
                request.Head,
                request.Base,
                request.Draft,
                accessToken
            );

            var pullRequest = new Domain.Models.PullRequest
            {
                GitHubId = githubPR.Id,
                Title = githubPR.Title,
                Repository = githubPR.Repository,
                Author = githubPR.Author,
                AuthorAvatar = githubPR.AuthorAvatar,
                Status = githubPR.Status,
                Draft = githubPR.Draft,
                Url = githubPR.Url,
                Description = githubPR.Description,
                SourceBranch = githubPR.SourceBranch,
                TargetBranch = githubPR.TargetBranch,
                MergeableState = githubPR.MergeableState,
                Additions = githubPR.Additions,
                Deletions = githubPR.Deletions,
                ChangedFiles = githubPR.ChangedFiles,
                CreatedAt = githubPR.CreatedAt,
                UpdatedAt = githubPR.UpdatedAt,
                LastSyncedAt = DateTime.UtcNow
            };

            context.PullRequests.Add(pullRequest);
            await context.SaveChangesAsync(cancellationToken);

            var prDetail = pullRequest.ToDetailDto(
                new List<Domain.Models.FileDiff>(),
                new List<Domain.Models.Comment>(),
                new List<Domain.Models.UserFileViewedState>(),
                null
            );

            return new CreatePullRequestResult(true, "Pull request created successfully", prDetail, null);
        }
        catch (Exception ex)
        {
            return new CreatePullRequestResult(false, "Failed to create pull request", null, ex.Message);
        }
    }
}
