using GitBuddy.Api.DTOs;
using MediatR;
using System.Security.Claims;

namespace GitBuddy.Api.Features.PullRequests.GetReviewers;

public record GetReviewersQuery(int PullRequestId) : IRequest<List<ReviewerStatusDto>>;
