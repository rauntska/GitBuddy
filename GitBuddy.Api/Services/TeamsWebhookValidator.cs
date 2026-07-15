using System.Diagnostics.CodeAnalysis;

namespace GitBuddy.Api.Services;

/// <summary>
/// Validates a Microsoft Teams incoming-webhook / Workflow URL.
/// Used by both settings persistence and the runtime send path so the same
/// rule is enforced everywhere.
/// </summary>
public static class TeamsWebhookValidator
{
    /// <summary>
    /// Returns true when the URL is a non-empty, absolute HTTPS URL.
    /// Empty/whitespace is treated as "not configured" (valid=false) so callers
    /// can distinguish unset from set.
    /// </summary>
    public static bool IsValidWebhookUrl([NotNullWhen(true)] string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttps;
    }

    /// <summary>
    /// Returns the first validation error for the URL, or null when valid.
    /// Useful for surfacing a precise message at the API boundary.
    /// </summary>
    public static string? Validate(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "Teams webhook URL is required.";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return "Teams webhook URL must be a valid absolute URL.";

        if (uri.Scheme != Uri.UriSchemeHttps)
            return "Teams webhook URL must use HTTPS.";

        return null;
    }
}
