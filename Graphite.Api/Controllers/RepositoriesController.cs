using Graphite.Api.DTOs;
using Graphite.Api.Services;
using Graphite.Domain.Data;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Graphite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RepositoriesController(
    IGitHubService gitHubService,
    IPullRequestValidationService validationService,
    ICacheService cacheService,
    IBranchWithoutPRService branchWithoutPRService,
    AppDbContext context)
    : BaseController(context)
{
    [HttpGet]
    public async Task<IActionResult> GetRepositories()
    {
        var (_, accessToken) = await validationService.GetRequiredUserWithTokenAsync(User);

        if (string.IsNullOrEmpty(accessToken))
        {
            return BadRequest(new { message = "GitHub access token not found" });
        }

        var repos = await gitHubService.GetAccessibleRepositoriesAsync(accessToken);
        var dtos = repos.Select(r => new RepositoryDto(
            r.Id,
            r.Owner,
            r.Name,
            r.FullName,
            r.Description,
            r.Private,
            r.DefaultBranch,
            r.Url
        ));

        return Ok(dtos);
    }

    [HttpGet("organization")]
    public async Task<IActionResult> GetOrganizationRepositories()
    {
        var config = await cacheService.GetConfigAsync();
        if (config == null || string.IsNullOrEmpty(config.Organization))
        {
            return BadRequest(new { message = "Organization not configured" });
        }

        var (_, accessToken) = await validationService.GetRequiredUserWithTokenAsync(User);

        if (string.IsNullOrEmpty(accessToken))
        {
            return BadRequest(new { message = "GitHub access token not found" });
        }

        var repos = await gitHubService.GetOrganizationRepositoriesAsync(config.Organization, accessToken);
        var dtos = repos.Select(r => new RepositoryDto(
            r.Id,
            r.Owner,
            r.Name,
            r.FullName,
            r.Description,
            r.Private,
            r.DefaultBranch,
            r.Url
        ));

        return Ok(new { organization = config.Organization, repositories = dtos });
    }

    [HttpGet("{owner}/{repo}/branches")]
    public async Task<IActionResult> GetBranches(string owner, string repo)
    {
        var (_, accessToken) = await validationService.GetRequiredUserWithTokenAsync(User);

        if (string.IsNullOrEmpty(accessToken))
        {
            return BadRequest(new { message = "GitHub access token not found" });
        }

        var branches = await gitHubService.GetBranchesAsync(owner, repo, accessToken);
        var dtos = branches.Select(b => new BranchDto(b.Name, b.Sha, b.Protected));

        return Ok(dtos);
    }

    [HttpGet("{owner}/{repo}/compare")]
    public async Task<IActionResult> CompareBranches(string owner, string repo, [FromQuery] string baseBranch, [FromQuery] string headBranch)
    {
        var (_, accessToken) = await validationService.GetRequiredUserWithTokenAsync(User);

        if (string.IsNullOrEmpty(accessToken))
        {
            return BadRequest(new { message = "GitHub access token not found" });
        }

        var comparison = await gitHubService.CompareBranchesAsync(owner, repo, baseBranch, headBranch, accessToken);

        if (comparison == null)
        {
            return NotFound(new { message = "Could not compare branches" });
        }

        var dto = new BranchComparisonDto(
            comparison.Status,
            comparison.AheadBy,
            comparison.BehindBy,
            comparison.TotalCommits,
            comparison.Commits.Select(c => new CommitDto(c.Sha, c.Message, c.Author, c.AuthoredAt)).ToList(),
            comparison.Files.Select(f => new FileDiffDto(
                0,
                f.Path,
                f.OldPath,
                f.Status,
                f.Additions,
                f.Deletions,
                f.Changes,
                f.Patch,
                null,
                f.ViewerViewedState,
                null
            )).ToList()
        );

        return Ok(dto);
    }

    [HttpGet("branches-without-prs")]
    public async Task<IActionResult> GetBranchesWithoutPRs([FromQuery] int recentDays = 7)
    {
        var (_, accessToken) = await validationService.GetRequiredUserWithTokenAsync(User);

        if (string.IsNullOrEmpty(accessToken))
        {
            return BadRequest(new { message = "GitHub access token not found" });
        }

        var config = await cacheService.GetConfigAsync();

        var openPRBranches = await context.PullRequests
            .Where(pr => pr.Status != "Closed" && pr.Status != "Merged")
            .Select(pr => new { pr.Repository, pr.SourceBranch })
            .ToListAsync();

        var openPRSet = openPRBranches
            .Select(x => (x.Repository, x.SourceBranch))
            .ToHashSet();

        var result = await branchWithoutPRService.GetBranchesWithoutPRsAsync(
            config?.Organization, accessToken, openPRSet, recentDays);

        return Ok(result);
    }
}
