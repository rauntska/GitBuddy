using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Graphite.Api.DTOs;
using Graphite.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Graphite.Domain.Models;

namespace Graphite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;
    private readonly IInvitationService _invitationService;
    private readonly IAllowlistService _allowlistService;
    private readonly HttpClient _httpClient;

    public AuthController(
        IConfiguration configuration,
        IUserService userService,
        IJwtService jwtService,
        IInvitationService invitationService,
        IAllowlistService allowlistService,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _userService = userService;
        _jwtService = jwtService;
        _invitationService = invitationService;
        _allowlistService = allowlistService;
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet("github")]
    public IActionResult GitHubLogin([FromQuery] string? invite)
    {
        var clientId = _configuration["GitHub:ClientId"];
        var redirectUri = _configuration["GitHub:RedirectUri"];
        var scope = "user:email read:org repo";
        var state = invite ?? "";

        var githubAuthUrl = $"https://github.com/login/oauth/authorize?client_id={clientId}&redirect_uri={redirectUri}&scope={scope}&state={state}";
        return Redirect(githubAuthUrl);
    }

    [HttpGet("github/callback")]
    public async Task<IActionResult> GitHubCallback([FromQuery] string code, [FromQuery] string? state)
    {
        try
        {
            var clientId = _configuration["GitHub:ClientId"];
            var clientSecret = _configuration["GitHub:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                return BadRequest(new { message = "GitHub OAuth configuration is missing. Please configure ClientId and ClientSecret in appsettings.json." });
            }

            var tokenRequest = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "code", code }
            };

            var tokenRequestContent = new FormUrlEncodedContent(tokenRequest);

            var tokenResponse = await _httpClient.PostAsync(
                "https://github.com/login/oauth/access_token",
                tokenRequestContent);

            tokenResponse.EnsureSuccessStatusCode();

            var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();
            
            var accessToken = tokenResponseContent
                .Split('&')
                .FirstOrDefault(pair => pair.StartsWith("access_token="))?
                .Split('=')[1];

            if (string.IsNullOrEmpty(accessToken))
            {
                return BadRequest(new { message = "Failed to obtain access token from GitHub" });
            }

            var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
            userRequest.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken);
            userRequest.Headers.UserAgent.Add(new ProductInfoHeaderValue("Graphite", "1.0"));

            var userResponse = await _httpClient.SendAsync(userRequest);
            userResponse.EnsureSuccessStatusCode();

            var userResponseContent = await userResponse.Content.ReadAsStringAsync();
            var githubUser = JsonSerializer.Deserialize<GitHubUserDto>(
                userResponseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (githubUser == null)
            {
                return BadRequest(new { message = "Failed to obtain user information from GitHub" });
            }

            var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:5173";

            var existingUser = await _userService.GetAllUsersAsync();
            var user = existingUser.FirstOrDefault(u => u.Provider == "GitHub" && u.ProviderUserId == githubUser.Id.ToString());

            if (user != null)
            {
                user.Username = githubUser.Login;
                user.Email = githubUser.Email ?? $"{githubUser.Login}@github.local";
                user.AvatarUrl = githubUser.AvatarUrl;
                user.AccessToken = accessToken;
                user.LastLoginAt = DateTime.UtcNow;
                var token = _jwtService.GenerateToken(user);
                var redirectUrl = $"{frontendUrl}/auth/callback?token={token}&username={user.Username}&avatar={Uri.EscapeDataString(user.AvatarUrl ?? "")}&role={user.Role}";
                return Redirect(redirectUrl);
            }

            var hasUsers = await _userService.HasAnyUsersAsync();

            if (hasUsers)
            {
                var allowedEntry = await _allowlistService.FindMatchAsync(githubUser.Email ?? "", githubUser.Login);

                if (allowedEntry != null)
                {
                    user = await CreateGitHubUserAsync(githubUser, accessToken, allowedEntry.AssignedRole, null);
                    var token = _jwtService.GenerateToken(user);
                    var redirectUrl = $"{frontendUrl}/auth/callback?token={token}&username={user.Username}&avatar={Uri.EscapeDataString(user.AvatarUrl ?? "")}&role={user.Role}";
                    return Redirect(redirectUrl);
                }

                if (!string.IsNullOrEmpty(state))
                {
                    var invitation = await _invitationService.ValidateInvitationAsync(state);
                    if (invitation != null)
                    {
                        var emailMatches = invitation.Email.Equals(githubUser.Email, StringComparison.OrdinalIgnoreCase);
                        var usernameMatches = invitation.GitHubUsername?.Equals(githubUser.Login, StringComparison.OrdinalIgnoreCase) == true;

                        if (emailMatches || usernameMatches)
                        {
                            user = await CreateGitHubUserAsync(githubUser, accessToken, invitation.AssignedRole, invitation.Id);
                            await _invitationService.AcceptInvitationAsync(invitation.Id, user.Id);
                            var token = _jwtService.GenerateToken(user);
                            var redirectUrl = $"{frontendUrl}/auth/callback?token={token}&username={user.Username}&avatar={Uri.EscapeDataString(user.AvatarUrl ?? "")}&role={user.Role}";
                            return Redirect(redirectUrl);
                        }
                    }
                }

                var errorUrl = $"{frontendUrl}/access-denied?reason=not_invited";
                return Redirect(errorUrl);
            }

            user = await CreateGitHubUserAsync(githubUser, accessToken, UserRole.Admin, null);
            var adminToken = _jwtService.GenerateToken(user);
            var adminRedirectUrl = $"{frontendUrl}/auth/callback?token={adminToken}&username={user.Username}&avatar={Uri.EscapeDataString(user.AvatarUrl ?? "")}&role={user.Role}";
            return Redirect(adminRedirectUrl);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Authentication failed: {ex.Message}" });
        }
    }

    private async Task<User> CreateGitHubUserAsync(GitHubUserDto githubUser, string accessToken, UserRole role, int? invitationId)
    {
        var user = new User
        {
            Username = githubUser.Login,
            Email = githubUser.Email ?? $"{githubUser.Login}@github.local",
            Provider = "GitHub",
            ProviderUserId = githubUser.Id.ToString(),
            AvatarUrl = githubUser.AvatarUrl,
            AccessToken = accessToken,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            Role = role,
            InvitationId = invitationId
        };

        return await _userService.GetOrCreateGitHubUserAsync(githubUser, accessToken);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst("UserId");
        if (userIdClaim == null)
        {
            return Unauthorized();
        }

        if (!int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized();
        }

        var user = await _userService.GetCurrentUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserDto(user.Id, user.Username, user.Email ?? "", user.AvatarUrl, user.Role));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out successfully" });
    }
}
