using System.Security.Claims;
using Graphite.Api.DTOs;
using MediatR;

namespace Graphite.Api.Features.PullRequests.GetById;

public record GetPullRequestByIdQuery(int Id, ClaimsPrincipal User) : IRequest<PRDetailDto?>;
