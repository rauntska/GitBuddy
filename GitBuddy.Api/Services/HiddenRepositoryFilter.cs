using System.Text.Json;

namespace GitBuddy.Api.Services;

/// <summary>
/// Helpers for the per-user "hidden repositories" preference. The preference stores full names
/// (owner/repo) as a JSON array, while <c>PullRequest.Repository</c> holds the bare repo name,
/// so everything is normalised to the lower-cased bare name.
/// </summary>
public static class HiddenRepositoryFilter
{
    public static string[] Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            var names = JsonSerializer.Deserialize<string[]>(json);
            return names == null
                ? []
                : names.Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(Normalize)
                    .Distinct()
                    .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string Normalize(string repository)
    {
        var slash = repository.LastIndexOf('/');
        return (slash >= 0 ? repository[(slash + 1)..] : repository).Trim().ToLowerInvariant();
    }
}
