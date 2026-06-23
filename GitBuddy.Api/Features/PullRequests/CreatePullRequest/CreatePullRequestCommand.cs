using System.Security.Claims;
using GitBuddy.Api.DTOs;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.CreatePullRequest;

public record CreatePullRequestCommand(
    string Owner,
    string Repository,
    string Title,
    string? Body,
    string Head,
    string Base,
    bool Draft,
    ClaimsPrincipal User) : IRequest<CreatePullRequestResult>;
