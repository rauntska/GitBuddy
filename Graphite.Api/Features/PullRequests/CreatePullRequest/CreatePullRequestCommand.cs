using System.Security.Claims;
using Graphite.Api.DTOs;
using MediatR;

namespace Graphite.Api.Features.PullRequests.CreatePullRequest;

public record CreatePullRequestCommand(
    string Owner,
    string Repository,
    string Title,
    string? Body,
    string Head,
    string Base,
    bool Draft,
    ClaimsPrincipal User) : IRequest<CreatePullRequestResult>;
