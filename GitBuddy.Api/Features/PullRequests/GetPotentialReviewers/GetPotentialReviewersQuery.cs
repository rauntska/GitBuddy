using GitBuddy.Api.DTOs;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.GetPotentialReviewers;

public record GetPotentialReviewersQuery(int PullRequestId) : IRequest<List<PotentialReviewerDto>>;
