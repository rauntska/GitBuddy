using System.Text.Json;
using GitBuddy.Domain.Models;

namespace GitBuddy.Api.Services.Automation;

public record ConditionDef(string Type, string? Value);

public record ActionDef(string Type, Dictionary<string, string>? Params);

/// <summary>Facts about the event that fired a trigger. Everything beyond the PR is optional.</summary>
public class TriggerContext
{
    public required AutomationTriggerType Trigger { get; init; }
    public required int PullRequestId { get; init; }

    /// <summary>The label that was added/removed (LabelAdded / LabelRemoved).</summary>
    public string? Label { get; init; }

    /// <summary>The check run that completed (CheckRunCompleted).</summary>
    public string? CheckName { get; init; }
    public string? CheckConclusion { get; init; }

    /// <summary>"approved", "changes_requested", "commented" (ReviewSubmitted).</summary>
    public string? ReviewState { get; init; }
    public string? Reviewer { get; init; }

    /// <summary>New mergeable state, upper case (MergeableStateChanged).</summary>
    public string? MergeableState { get; init; }
}

public static class AutomationJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static List<ConditionDef> ParseConditions(string json) =>
        JsonSerializer.Deserialize<List<ConditionDef>>(string.IsNullOrWhiteSpace(json) ? "[]" : json, Options) ?? [];

    public static List<ActionDef> ParseActions(string json) =>
        JsonSerializer.Deserialize<List<ActionDef>>(string.IsNullOrWhiteSpace(json) ? "[]" : json, Options) ?? [];

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}
