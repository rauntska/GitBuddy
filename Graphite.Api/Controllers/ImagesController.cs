using Graphite.Domain.Data;
using Graphite.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace Graphite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _context;
    private readonly ILogger<ImagesController> _logger;
    private readonly IConfiguration _configuration;

    public ImagesController(
        IHttpClientFactory httpClientFactory,
        AppDbContext context,
        ILogger<ImagesController> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet("proxy")]
    public async Task<IActionResult> ProxyImage([FromQuery] string url)
    {
        _logger.LogInformation("Image proxy request received for URL: {Url}", url);

        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogWarning("Empty URL provided");
            return BadRequest("URL is required");
        }

        // Validate that URL is from GitHub
        if (!url.StartsWith("https://github.com/") && !url.StartsWith("https://github.com/user-attachments"))
        {
            _logger.LogWarning("Invalid URL provided: {Url}", url);
            return BadRequest("Only GitHub URLs are allowed");
        }

        // Get current user's GitHub access token
        var userIdClaim = User.FindFirst("UserId")?.Value;
        string? accessToken = null;

        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogInformation("User ID found: {UserId}, checking for personal access token", userId);
            var user = await _context.Users.FindAsync(userId);
            
            if (user?.AccessToken != null && user.AccessToken.Length > 0)
            {
                _logger.LogInformation("User has personal access token, using it");
                accessToken = user.AccessToken;
            }
            else
            {
                _logger.LogWarning("User {UserId} does not have a GitHub access token", userId);
            }
        }
        else
        {
            _logger.LogWarning("UserId claim not found in token");
        }

        // Fallback to app's GitHub token if user doesn't have personal token
        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogInformation("No user token found, falling back to app's GitHub token");
            var personalAccessToken = _configuration["GitHubConfig:PersonalAccessToken"];
            
            if (!string.IsNullOrEmpty(personalAccessToken))
            {
                _logger.LogWarning("No app GitHub token configured");
                return Unauthorized("GitHub authentication not configured. Please login with your GitHub account.");
            }
            
            accessToken = personalAccessToken;
            _logger.LogInformation("Using app's GitHub token for image proxy");
        }

        // Create HTTP client and fetch image
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Graphite-PR-Dashboard");
        client.Timeout = TimeSpan.FromSeconds(30);

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await client.GetAsync(url);
            stopwatch.Stop();

            _logger.LogInformation("GitHub image fetch completed in {Ms}ms with status {StatusCode}",
                stopwatch.ElapsedMilliseconds, response.StatusCode);

            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
            var imageBytes = await response.Content.ReadAsByteArrayAsync();

            _logger.LogInformation("Successfully fetched image: {Bytes} bytes, Content-Type: {ContentType}",
                imageBytes.Length, contentType);

            // Cache for 1 hour
            Response.Headers.CacheControl = "public, max-age=3600";

            return File(imageBytes, contentType);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogError(ex, "GitHub returned 401 Unauthorized when fetching image. Token may be expired.");
            return Unauthorized("GitHub access token is invalid or expired. Please login again with your GitHub account.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch image from GitHub");
            return StatusCode(500, $"Failed to fetch image: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching image");
            return StatusCode(500, "An unexpected error occurred");
        }
    }
}
