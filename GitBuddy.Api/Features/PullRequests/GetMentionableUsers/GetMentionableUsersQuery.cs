using MediatR;

namespace GitBuddy.Api.Features.PullRequests.GetMentionableUsers;

public record GetMentionableUsersQuery(int PullRequestId) : IRequest<List<MentionableUserDto>>;

public record MentionableUserDto(
    string Username,
    string? AvatarUrl,
    string? Name);
