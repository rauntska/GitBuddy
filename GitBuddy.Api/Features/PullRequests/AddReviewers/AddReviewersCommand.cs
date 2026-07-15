using GitBuddy.Api.DTOs;
using MediatR;
using System.Security.Claims;

namespace GitBuddy.Api.Features.PullRequests.AddReviewers;

public record AddReviewersCommand(int PullRequestId, List<ReviewerEntry> Reviewers, ClaimsPrincipal User)
    : IRequest<AddReviewersResult>;

public record AddReviewersResult(bool Success, string Message, string? Error = null);
