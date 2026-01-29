using Octokit.GraphQL;
using Octokit;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Graphite.Domain.Models;

namespace Graphite.Api.Services;

public class GitHubService(ILogger<GitHubService> logger) : IGitHubService
{
    private string? _cachedInstallationToken;
    private DateTime _installationTokenExpiration = DateTime.MinValue;

    public async Task<List<GitHubPRData>> GetOpenPullRequestsAsync(string organization, GitHubConfig config)
    {
        var accessToken = await GetAccessTokenAsync(config);

        var connection = new Octokit.GraphQL.Connection(new Octokit.GraphQL.ProductHeaderValue("Graphite-PR-Dashboard"), accessToken);

        var reposQuery = new Octokit.GraphQL.Query()
            .Organization(organization)
            .Repositories(first: 100)
            .Nodes
            .Select(repo => repo.Name)
            .Compile();

        var repoNames = await connection.Run(reposQuery);

        var pullRequests = new List<GitHubPRData>();

        foreach (var repoName in repoNames)
        {
            var prQuery = new Octokit.GraphQL.Query()
                .Repository(repoName, organization)
                .PullRequests(first: 100, states: new[] { Octokit.GraphQL.Model.PullRequestState.Open })
                .Nodes
                .Select(pr => new
                {
                    pr.Number,
                    pr.Title,
                    AuthorLogin = pr.Author.Login,
                    AuthorAvatar = "",
                    pr.IsDraft,
                    pr.Url,
                    pr.Additions,
                    pr.Deletions,
                    pr.ChangedFiles,
                    pr.CreatedAt,
                    pr.UpdatedAt,
                    pr.Body,
                    HeadRefName = pr.HeadRefName,
                    BaseRefName = pr.BaseRefName,
                    pr.Mergeable
                })
                .Compile();

            var prs = await connection.Run(prQuery);
            
            logger.LogInformation("Found {PrsCount} open PRs in repository {RepoName}", prs.Count(), repoName);

            foreach (var pr in prs)
            {
                var reviews = await GetReviewsAsync(organization, repoName, pr.Number, config);
                var reviewData = reviews;

                var status = DeterminePrStatus(pr.IsDraft, reviewData);

                var reviewThreads = await GetReviewThreadsAsync(organization, repoName, pr.Number, config);

                pullRequests.Add(new GitHubPRData(
                    pr.Number,
                    pr.Title,
                    repoName,
                    pr.AuthorLogin,
                    pr.AuthorAvatar,
                    status,
                    pr.IsDraft,
                    pr.Url,
                    pr.Additions,
                    pr.Deletions,
                    pr.ChangedFiles,
                    pr.CreatedAt.UtcDateTime,
                    pr.UpdatedAt.UtcDateTime,
                    reviewData,
                    reviewThreads,
                    pr.Body ?? string.Empty,
                    pr.HeadRefName,
                    pr.BaseRefName,
                    pr.Mergeable.ToString()
                ));
            }
        }

        return pullRequests.OrderByDescending(pr => pr.UpdatedAt).ToList();
    }

    public async Task<List<GitHubReviewData>> GetReviewsAsync(string organization, string repository, int pullRequestNumber, GitHubConfig config)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync(config);
            var connection = new Octokit.GraphQL.Connection(new Octokit.GraphQL.ProductHeaderValue("Graphite-PR-Dashboard"), accessToken);
            var query = new Octokit.GraphQL.Query()
                .Repository(repository, organization)
                .PullRequest(pullRequestNumber)
                .Reviews(100, null, null, null, null, null)
                .Nodes
                .Select(r => new
                {
                    AuthorLogin = r.Author.Login,
                    AuthorAvatar = r.Author.AvatarUrl(40),
                    r.State,
                    r.SubmittedAt
                })
                .Compile();

            var result = await connection.Run(query);

            return result.Select(r => new GitHubReviewData(
                r.AuthorLogin,
                r.AuthorAvatar,
                r.State.ToString(),
                r.SubmittedAt?.UtcDateTime
            )).ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string DeterminePrStatus(bool isDraft, List<GitHubReviewData> reviews)
    {
        if (isDraft) return "Draft";

        var hasApproved = reviews.Any(r => r.State == "Approved");
        var hasChangesRequested = reviews.Any(r => r.State == "ChangesRequested");
        var hasComments = reviews.Any(r => r.State == "Commented");

        if (hasApproved && !hasChangesRequested) return "Approved";
        if (hasChangesRequested) return "ChangesRequested";
        if (hasComments) return "Reviewed";
        return "AwaitingReview";
    }


    public async Task<List<GitHubReviewThreadData>> GetReviewThreadsAsync(string organization, string repository, int pullRequestNumber, GitHubConfig config)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync(config);
            var connection = new Octokit.GraphQL.Connection(new Octokit.GraphQL.ProductHeaderValue("Graphite-PR-Dashboard"), accessToken);

            var reviewThreadsQuery = new Octokit.GraphQL.Query()
                .Repository(repository, organization)
                .PullRequest(pullRequestNumber)
                .ReviewThreads(100, null, null, null)
                .Nodes
                .Select(rt => new
                {
                    Id = rt.Id.Value,
                    Path = rt.Path,
                    Line = rt.Line,
                    IsResolved = rt.IsResolved,
                    IsOutdated = rt.IsOutdated,
                    Comments = rt.Comments(100, null, null, null, null).Nodes.Select(c => new
                    {
                        DatabaseId = c.DatabaseId,
                        Author = c.Author.Login,
                        AuthorAvatar = c.Author.AvatarUrl(40),
                        Body = c.Body,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt
                    }).ToList()
                })
                .Compile();

            var reviewThreads = await connection.Run(reviewThreadsQuery);

            return reviewThreads.Select(rt =>
            {
                var firstComment = rt.Comments.FirstOrDefault();
                var state = rt.IsResolved ? "RESOLVED" : "UNRESOLVED";

                return new GitHubReviewThreadData(
                    rt.Id,
                    rt.Path ?? string.Empty,
                    rt.Line,
                    state,
                    rt.IsResolved,
                    rt.IsOutdated,
                    firstComment?.CreatedAt.UtcDateTime ?? DateTime.UtcNow,
                    firstComment?.UpdatedAt.UtcDateTime,
                    firstComment?.Author ?? string.Empty,
                    firstComment?.Body ?? string.Empty,
                    rt.Comments.Count
                );
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching GitHub review threads for PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            return new List<GitHubReviewThreadData>();
        }
    }

    public async Task<List<GitHubCommentData>> GetCommentsAsync(string organization, string repository, int pullRequestNumber, GitHubConfig config)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync(config);
            var connection = new Octokit.GraphQL.Connection(new Octokit.GraphQL.ProductHeaderValue("Graphite-PR-Dashboard"), accessToken);

            var reviewThreadsQuery = new Octokit.GraphQL.Query()
                .Repository(repository, organization)
                .PullRequest(pullRequestNumber)
                .ReviewThreads(100, null, null, null)
                .Nodes
                .Select(rt => new
                {
                    Id = rt.Id.Value,
                    Path = rt.Path,
                    Line = rt.Line,
                    IsResolved = rt.IsResolved,
                    IsOutdated = rt.IsOutdated,
                    Comments = rt.Comments(100, null, null, null, null).Nodes.Select(c => new
                    {
                        DatabaseId = c.DatabaseId,
                        FullDatabaseId = c.FullDatabaseId,
                        Author = c.Author.Login,
                        AuthorAvatar = c.Author.AvatarUrl(40),
                        Body = c.Body,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt
                    }).ToList()
                })
                .Compile();

            var reviewThreads = await connection.Run(reviewThreadsQuery);

            var comments = new List<GitHubCommentData>();

            foreach (var thread in reviewThreads)
            {
                foreach (var comment in thread.Comments)
                {
                    comments.Add(new GitHubCommentData(
                        comment.DatabaseId ?? 0,
                        thread.Id,
                        comment.Author,
                        comment.AuthorAvatar,
                        comment.Body,
                        thread.Path,
                        thread.Line,
                        thread.IsOutdated,
                        comment.CreatedAt.UtcDateTime,
                        comment.UpdatedAt.UtcDateTime
                    ));
                }
            }

            return comments;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching GitHub comments for PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            return new List<GitHubCommentData>();
        }
    }

    public async Task<List<GitHubFileDiffData>> GetFileDiffsAsync(string organization, string repository, int pullRequestNumber, GitHubConfig config)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync(config);
            var client = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
            {
                Credentials = new Credentials(accessToken)
            };

            var files = await client.PullRequest.Files(organization, repository, pullRequestNumber);

            return files.Select(file =>
            {
                var status = file.Status.ToLowerInvariant();
                if (status == "removed") status = "deleted";
                if (status == "renamed") status = "renamed";
                if (status == "added") status = "added";
                if (status == "modified") status = "modified";

                return new GitHubFileDiffData(
                    file.FileName,
                    file.PreviousFileName,
                    status,
                    file.Additions,
                    file.Deletions,
                    file.Changes,
                    file.Patch
                );
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching file diffs for PR {Organization}/{Repository}#{PullRequestNumber}", organization, repository, pullRequestNumber);
            return new List<GitHubFileDiffData>();
        }
    }

    private async Task<string> GetAccessTokenAsync(GitHubConfig config)
    {
        if (config.UseGitHubApp)
        {
            return await GetGitHubAppAccessTokenAsync(config);
        }
        else
        {
            return config.PersonalAccessToken;
        }
    }

    private async Task<string> GetGitHubAppAccessTokenAsync(GitHubConfig config)
    {
        var installationId = long.Parse(config.InstallationId);
        
        if (_cachedInstallationToken == null || DateTime.UtcNow >= _installationTokenExpiration)
        {
            try
            {
                var jwt = GenerateJwt(config.AppId, config.PrivateKey);
                logger.LogInformation("Generated JWT for App ID: {AppId}", config.AppId);
                
                var client = new GitHubClient(new Octokit.ProductHeaderValue("Graphite-PR-Dashboard"))
                {
                    Credentials = new Credentials(jwt, AuthenticationType.Bearer)
                };

                var installationToken = await client.GitHubApps.CreateInstallationToken(installationId);
                _cachedInstallationToken = installationToken.Token;
                _installationTokenExpiration = installationToken.ExpiresAt.UtcDateTime.AddMinutes(-5);
                logger.LogInformation("Successfully obtained installation token");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating GitHub App JWT or obtaining installation token. AppId: {AppId}, InstallationId: {InstallationId}", config.AppId, config.InstallationId);
                throw;
            }
        }

        return _cachedInstallationToken;
    }

private string GenerateJwt(string appId, string privateKeyPem)
{
    try
    {
        if (string.IsNullOrWhiteSpace(privateKeyPem))
        {
            throw new ArgumentException("Private key is empty or whitespace", nameof(privateKeyPem));
        }
        
        // Normalize line endings - handle both actual newlines and escaped \n characters
        // First, if the key is stored with literal \n characters, convert them to actual newlines
        if (privateKeyPem.Contains("\\n"))
        {
            privateKeyPem = privateKeyPem.Replace("\\n", "\n");
        }
        
        // Ensure consistent line endings (LF only)
        privateKeyPem = privateKeyPem.Replace("\r\n", "\n").Replace("\r", "\n");
        
        // Trim any extra whitespace from start and end
        privateKeyPem = privateKeyPem.Trim();
        
        // Ensure the key has the proper BEGIN and END markers
        if (!privateKeyPem.StartsWith("-----BEGIN"))
        {
            throw new ArgumentException("Private key does not start with BEGIN marker", nameof(privateKeyPem));
        }
        
        if (!privateKeyPem.Contains("-----END"))
        {
            throw new ArgumentException("Private key does not contain END marker", nameof(privateKeyPem));
        }
        
        logger.LogInformation("Generating JWT for App ID: {AppId}", appId);
        
        // Import the RSA private key
        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(privateKeyPem);
            logger.LogInformation("Successfully imported RSA private key. Key size: {KeySize} bits", rsa.KeySize);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to import RSA private key");
            throw;
        }
        
        // Create the JWT payload
        var now = DateTimeOffset.UtcNow;
        var issuedAt = now.AddSeconds(-60); // Backdate by 60 seconds to account for clock drift
        var expiresAt = now.AddMinutes(10); // GitHub max is 10 minutes
        
        var iat = issuedAt.ToUnixTimeSeconds();
        var exp = expiresAt.ToUnixTimeSeconds();
        
        // Create JWT header - must be minimal
        var headerJson = "{\"alg\":\"RS256\",\"typ\":\"JWT\"}";
        
        // Create payload JSON manually to ensure correct format (no quotes around numbers)
        var payloadJson = $"{{\"iat\":{iat},\"exp\":{exp},\"iss\":\"{appId}\"}}";
        
        logger.LogInformation("Generating JWT with iat={Iat}, exp={Exp}, iss={Iss}", iat, exp, appId);
        
        var headerBase64 = Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(headerJson));
        var payloadBase64 = Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(payloadJson));
        
        var unsignedToken = $"{headerBase64}.{payloadBase64}";
        
        // Sign the token
        var dataToSign = System.Text.Encoding.UTF8.GetBytes(unsignedToken);
        var signature = rsa.SignData(dataToSign, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        var signatureBase64 = Base64UrlEncode(signature);
        
        var jwt = $"{unsignedToken}.{signatureBase64}";
        
        logger.LogInformation("JWT generated successfully. Length: {Length}", jwt.Length);
        
        // Decode and verify the JWT structure (for debugging)
        try
        {
            var parts = jwt.Split('.');
            var headerDecoded = System.Text.Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
            var payloadDecoded = System.Text.Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            logger.LogInformation("JWT Header (decoded): {Header}", headerDecoded);
            logger.LogInformation("JWT Payload (decoded): {Payload}", payloadDecoded);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not decode JWT for verification");
        }
        
        return jwt;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error generating JWT for App ID: {AppId}", appId);
        throw;
    }
}

private static string Base64UrlEncode(byte[] input)
{
    var output = Convert.ToBase64String(input);
    output = output.TrimEnd('='); // Remove padding
    output = output.Replace('+', '-'); // URL-safe
    output = output.Replace('/', '_'); // URL-safe
    return output;
}

private static byte[] Base64UrlDecode(string input)
{
    var output = input;
    output = output.Replace('-', '+'); // Reverse URL-safe
    output = output.Replace('_', '/'); // Reverse URL-safe
    
    // Add padding
    switch (output.Length % 4)
    {
        case 2: output += "=="; break;
        case 3: output += "="; break;
    }
    
    return Convert.FromBase64String(output);
}
}
