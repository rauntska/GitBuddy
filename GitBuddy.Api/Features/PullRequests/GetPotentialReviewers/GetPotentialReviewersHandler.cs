using GitBuddy.Api.DTOs;
using GitBuddy.Api.Services;
using GitBuddy.Domain.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Features.PullRequests.GetPotentialReviewers;

public class GetPotentialReviewersHandler(
    AppDbContext context,
    IGitHubService gitHubService,
    IPullRequestValidationService validationService) 
    : IRequestHandler<GetPotentialReviewersQuery, List<PotentialReviewerDto>>
{
    public async Task<List<PotentialReviewerDto>> Handle(GetPotentialReviewersQuery request, CancellationToken cancellationToken)
    {
        var pr = await context.PullRequests
            .FirstOrDefaultAsync(p => p.Id == request.PullRequestId, cancellationToken);

        if (pr == null)
            return new List<PotentialReviewerDto>();

        var config = await validationService.GetRequiredConfigAsync();
        
        var collaboratorsAndTeams = await gitHubService.GetRepositoryCollaboratorsAndTeamsAsync(
            config.Organization,
            pr.Repository,
            config
        );

        var result = new List<PotentialReviewerDto>();

        foreach (var user in collaboratorsAndTeams.Users)
        {
            result.Add(new PotentialReviewerDto(
                user.Username,
                user.Avatar,
                "User"
            ));
        }

        foreach (var team in collaboratorsAndTeams.Teams)
        {
            result.Add(new PotentialReviewerDto(
                team.Slug,
                null,
                "Team"
            ));
        }

        return result.OrderBy(r => r.Type).ThenBy(r => r.Name).ToList();
    }
}
