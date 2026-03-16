using MediatR;
using System.Security.Claims;

namespace Graphite.Api.Features.PullRequests.AddReviewers;

public record AddReviewersCommand(int PullRequestId, List<string> Reviewers, ClaimsPrincipal User) 
    : IRequest<AddReviewersResult>;

public record AddReviewersResult(bool Success, string Message, string? Error = null);
