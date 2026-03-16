using Graphite.Api.DTOs;
using MediatR;

namespace Graphite.Api.Features.PullRequests.GetPotentialReviewers;

public record GetPotentialReviewersQuery(int PullRequestId) : IRequest<List<PotentialReviewerDto>>;
