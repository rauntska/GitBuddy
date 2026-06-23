using GitBuddy.Api.DTOs;
using MediatR;

namespace GitBuddy.Api.Features.PullRequests.GetUnreadCount;

public record GetUnreadCountQuery(string Username) : IRequest<int>;
