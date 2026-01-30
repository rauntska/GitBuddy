using Graphite.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Graphite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IWebhookService webhookService, ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    [HttpPost("github")]
    public async Task<IActionResult> HandleGitHubWebhook()
    {
        var eventType = Request.Headers["X-GitHub-Event"].ToString();
        var deliveryId = Request.Headers["X-GitHub-Delivery"].ToString();

        if (string.IsNullOrEmpty(eventType))
        {
            _logger.LogWarning("Webhook received without X-GitHub-Event header");
            return BadRequest("Missing X-GitHub-Event header");
        }

        _logger.LogInformation("GitHub webhook received: EventType={EventType}, DeliveryId={DeliveryId}", eventType, deliveryId);

        try
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();
            var jsonPayload = JsonSerializer.Deserialize<JsonElement>(payload);

            await _webhookService.ProcessWebhookAsync(eventType, jsonPayload);

            return Ok();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing webhook payload");
            return BadRequest("Invalid JSON payload");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}