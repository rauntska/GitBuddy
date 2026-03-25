using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;

namespace Graphite.Api.Features.PullRequests.Merge;

public class MergePRHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<MergePRCommand, MergePRResult>
{
    public async Task<MergePRResult> Handle(MergePRCommand request, CancellationToken cancellationToken)
    {
        var (user, accessToken) = await validationService.GetRequiredUserWithTokenAsync(request.User);
        
        var pr = await context.PullRequests.FindAsync([request.PullRequestId], cancellationToken);
        
        if (pr == null)
            return new MergePRResult(false, "Pull request not found", false, null, 404, null);

        if (pr.Draft)
            return new MergePRResult(false, "Cannot merge a draft pull request. Please publish it first.", false, null, 400, null);

        if (pr.IsMerged)
            return new MergePRResult(false, "Pull request has already been merged.", true, pr.MergedAt, 400, null);

        if (pr.MergeableState == "CONFLICTING")
            return new MergePRResult(false, "Pull request has merge conflicts that must be resolved before merging.", false, null, 400, null);

        var config = await validationService.GetRequiredConfigAsync();
        var mergeMethod = request.MergeMethod ?? "merge";

        try
        {
            await gitHubService.MergePullRequestAsync(
                config.Organization,
                pr.Repository,
                pr.GitHubId,
                request.CommitTitle,
                request.CommitMessage,
                mergeMethod,
                accessToken!
            );

            pr.IsMerged = true;
            pr.MergedAt = DateTime.UtcNow;
            pr.Status = "Merged";
            await context.SaveChangesAsync(cancellationToken);

            return new MergePRResult(true, "Pull request merged successfully", true, pr.MergedAt, null, null);
        }
        catch (Octokit.ApiException ex)
        {
            var (errorMessage, statusCode) = ex.StatusCode switch
            {
                System.Net.HttpStatusCode.Conflict => ("Pull request has merge conflicts that must be resolved.", 409),
                System.Net.HttpStatusCode.MethodNotAllowed => ("Merge method not allowed. Check repository settings.", 405),
                System.Net.HttpStatusCode.Forbidden => ("You don't have permission to merge this pull request.", 403),
                System.Net.HttpStatusCode.UnprocessableEntity => ("Pull request cannot be merged. It may be closed or have failing checks.", 422),
                _ => ($"Failed to merge pull request: {ex.Message}", (int)ex.StatusCode)
            };
            return new MergePRResult(false, errorMessage, false, null, statusCode, ex.Message);
        }
        catch (Exception ex)
        {
            return new MergePRResult(false, "Failed to merge pull request", false, null, 500, ex.Message);
        }
    }
}
