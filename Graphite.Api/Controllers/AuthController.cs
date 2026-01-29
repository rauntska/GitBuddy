using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Graphite.Api.DTOs;
using Graphite.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace Graphite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;
    private readonly HttpClient _httpClient;

    public AuthController(
        IConfiguration configuration,
        IUserService userService,
        IJwtService jwtService,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _userService = userService;
        _jwtService = jwtService;
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet("github")]
    public IActionResult GitHubLogin()
    {
        var clientId = _configuration["GitHub:ClientId"];
        var redirectUri = _configuration["GitHub:RedirectUri"];
        var scope = "user:email read:org repo";

        var githubAuthUrl = $"https://github.com/login/oauth/authorize?client_id={clientId}&redirect_uri={redirectUri}&scope={scope}";
        return Redirect(githubAuthUrl);
    }

    [HttpGet("github/callback")]
    public async Task<IActionResult> GitHubCallback([FromQuery] string code)
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

            var user = await _userService.GetOrCreateGitHubUserAsync(githubUser, accessToken);

            var token = _jwtService.GenerateToken(user);

            var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:5173";
            var redirectUrl = $"{frontendUrl}/auth/callback?token={token}&username={user.Username}&avatar={user.AvatarUrl ?? ""}";

            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Authentication failed: {ex.Message}" });
        }
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

        var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        var avatarUrl = User.FindFirst("AvatarUrl")?.Value;

        return Ok(new CurrentUserDto(userId, username, email, avatarUrl));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out successfully" });
    }
}
