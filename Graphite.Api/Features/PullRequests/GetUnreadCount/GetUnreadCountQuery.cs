using Graphite.Api.DTOs;
using MediatR;

namespace Graphite.Api.Features.PullRequests.GetUnreadCount;

public record GetUnreadCountQuery(string Username) : IRequest<int>;
