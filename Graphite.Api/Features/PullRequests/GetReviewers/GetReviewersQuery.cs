using Graphite.Api.DTOs;
using MediatR;
using System.Security.Claims;

namespace Graphite.Api.Features.PullRequests.GetReviewers;

public record GetReviewersQuery(int PullRequestId) : IRequest<List<ReviewerStatusDto>>;
