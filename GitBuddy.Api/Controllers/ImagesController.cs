using GitBuddy.Api.Features.Images.UploadImage;
using GitBuddy.Domain.Data;
using GitBuddy.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace GitBuddy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController(
    IHttpClientFactory httpClientFactory,
    AppDbContext context,
    ILogger<ImagesController> logger,
    IConfiguration configuration,
    IMemoryCache cache,
    ISender mediator)
    : BaseController(context)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);
    private const long MaxCacheableBytes = 10 * 1024 * 1024;

    private static bool IsAllowedGitHubHost(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host;
        if (uri.Scheme != "https")
            return false;

        if (host == "github.com")
            return uri.AbsolutePath.StartsWith("/user-attachments/", StringComparison.OrdinalIgnoreCase);

        return host == "raw.githubusercontent.com"
            || host == "objects.githubusercontent.com"
            || host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet("proxy")]
    public async Task<IActionResult> ProxyImage([FromQuery] string url)
    {
        logger.LogInformation("Image proxy request received for URL: {Url}", url);

        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogWarning("Empty URL provided");
            return BadRequest("URL is required");
        }

        // Validate that URL is from a GitHub-owned image host
        if (!IsAllowedGitHubHost(url))
        {
            logger.LogWarning("Invalid URL provided: {Url}", url);
            return BadRequest("Only GitHub image URLs are allowed");
        }

        var cacheKey = $"img-proxy:{url}";
        if (cache.Get<CachedImage>(cacheKey) is { } cached)
        {
            Response.Headers.CacheControl = "public, max-age=3600";
            return File(cached.Bytes, cached.ContentType);
        }

        // Get current user's GitHub access token
        var userIdClaim = User.FindFirst("UserId")?.Value;
        string? accessToken = null;

        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
        {
            logger.LogInformation("User ID found: {UserId}, checking for GitHub access token", userId);
            var user = await context.Users.FindAsync(userId);

            if (user?.AccessToken != null && user.AccessToken.Length > 0)
            {
                logger.LogInformation("Using user's GitHub access token");
                accessToken = user.AccessToken;
            }
            else
            {
                logger.LogWarning("User {UserId} does not have a GitHub access token", userId);
            }
        }
        else
        {
            logger.LogWarning("UserId claim not found in token");
        }

        // Fallback to app's GitHub token if user doesn't have one
        if (string.IsNullOrEmpty(accessToken))
        {
            logger.LogInformation("No user token found, falling back to app's GitHub token");
            var appToken = configuration["GitHubConfig:PersonalAccessToken"];

            if (string.IsNullOrEmpty(appToken))
            {
                logger.LogWarning("No app GitHub token configured either");
                return Unauthorized("No GitHub token available. Configure an app token or re-login with your GitHub account.");
            }

            accessToken = appToken;
            logger.LogInformation("Using app's GitHub token for image proxy");
        }

        // Create HTTP client and fetch image
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GitBuddy-PR-Dashboard");
        client.Timeout = TimeSpan.FromSeconds(30);

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await client.GetAsync(url);
            stopwatch.Stop();

            logger.LogInformation("GitHub image fetch completed in {Ms}ms with status {StatusCode}",
                stopwatch.ElapsedMilliseconds, response.StatusCode);

            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
            var imageBytes = await response.Content.ReadAsByteArrayAsync();

            logger.LogInformation("Successfully fetched image: {Bytes} bytes, Content-Type: {ContentType}",
                imageBytes.Length, contentType);

            if (imageBytes.Length <= MaxCacheableBytes)
            {
                cache.Set(cacheKey, new CachedImage(imageBytes, contentType), CacheTtl);
            }

            Response.Headers.CacheControl = "public, max-age=3600";

            return File(imageBytes, contentType);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            logger.LogError(ex, "GitHub returned 401 Unauthorized when fetching image. Token may be expired.");
            return Unauthorized("GitHub access token is invalid or expired. Please login again with your GitHub account.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to fetch image from GitHub");
            return StatusCode(500, $"Failed to fetch image: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error fetching image");
            return StatusCode(500, "An unexpected error occurred");
        }
    }

    [HttpGet("uploads/{filename}")]
    [AllowAnonymous]
    public IActionResult GetUploadedFile(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename) || filename.Contains('/') || filename.Contains('\\'))
            return BadRequest("Invalid filename.");

        var ext = Path.GetExtension(filename);
        var contentType = ext.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };

        var storagePath = configuration["ImageUpload:StoragePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "uploads", "images");
        var filePath = Path.Combine(storagePath, filename);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return PhysicalFile(filePath, contentType);
    }

    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(
        [FromForm] IFormFile file,
        [FromForm] int prId)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/png", "image/jpeg", "image/gif", "image/webp", "image/svg+xml"
        };

        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = $"Unsupported file type: {file.ContentType}. Allowed: png, jpg, gif, webp, svg" });

        if (file.Length > 25 * 1024 * 1024)
            return BadRequest(new { message = "File size exceeds the 25 MB limit." });

        try
        {
            using var stream = file.OpenReadStream();
            var imageData = new byte[stream.Length];
            await stream.ReadAsync(imageData.AsMemory(0, imageData.Length));

            var result = await mediator.Send(new UploadImageCommand(
                prId, imageData, file.FileName, file.ContentType, User));

            return Ok(new { url = result.Url });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload image");
            return StatusCode(500, new { message = "Failed to upload image.", error = ex.Message });
        }
    }

    private sealed record CachedImage(byte[] Bytes, string ContentType);
}
