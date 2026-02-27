using Graphite.Domain.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Features.PullRequests.GetMentionableUsers;

public class GetMentionableUsersHandler(AppDbContext context) 
    : IRequestHandler<GetMentionableUsersQuery, List<MentionableUserDto>>
{
    public async Task<List<MentionableUserDto>> Handle(GetMentionableUsersQuery request, CancellationToken cancellationToken)
    {
        var pr = await context.PullRequests
            .Include(p => p.Reviews)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == request.PullRequestId, cancellationToken);

        if (pr == null)
            throw new KeyNotFoundException("Pull request not found");

        var users = new HashSet<string> { pr.Author };

        foreach (var review in pr.Reviews)
        {
            users.Add(review.Reviewer);
        }

        foreach (var comment in pr.Comments)
        {
            users.Add(comment.Author);
        }

        var userAvatars = await context.Users
            .Where(u => users.Contains(u.Username))
            .Select(u => new { u.Username, u.AvatarUrl, u.Name })
            .ToDictionaryAsync(u => u.Username, cancellationToken);

        return users.Select(username => new MentionableUserDto(
            username,
            userAvatars.GetValueOrDefault(username)?.AvatarUrl,
            userAvatars.GetValueOrDefault(username)?.Name
        )).ToList();
    }
}
