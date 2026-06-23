using System.Security.Claims;
using GitBuddy.Api.DTOs;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.GetById;

public record GetPullRequestByIdQuery(int Id, ClaimsPrincipal User) : IRequest<PRDetailDto?>;
