using System.Text.Json;
using System.Text.RegularExpressions;
using GitBuddy.Domain.Data;
using GitBuddy.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Services.Automation;

/// <summary>Pure helpers for reasoning about check runs, wildcards and comparisons.</summary>
public static class ChecksEvaluator
{
    private static readonly HashSet<string> PassingConclusions = new(StringComparer.OrdinalIgnoreCase)
        { "success", "neutral", "skipped" };

    private static readonly HashSet<string> FailingConclusions = new(StringComparer.OrdinalIgnoreCase)
        { "failure", "timed_out", "cancelled", "action_required", "startup_failure", "stale" };

    /// <summary>The most recent run for each distinct check name.</summary>
    public static List<CheckRun> LatestPerName(IEnumerable<CheckRun> runs) =>
        runs.GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(r => r.StartedAt).ThenByDescending(r => r.Id).First())
            .ToList();

    public static bool IsCompleted(CheckRun run) =>
        string.Equals(run.Status, "completed", StringComparison.OrdinalIgnoreCase);

    public static bool IsPassing(CheckRun run) =>
        IsCompleted(run) && run.Conclusion != null && PassingConclusions.Contains(run.Conclusion);

    public static bool IsFailing(CheckRun run) =>
        IsCompleted(run) && run.Conclusion != null && FailingConclusions.Contains(run.Conclusion);

    /// <summary>
    /// Aggregate state of a PR's checks: SUCCESS, FAILURE, PENDING or NONE (no relevant checks).
    /// When <paramref name="required"/> is non-empty only those check names count, and a missing required check is PENDING.
    /// </summary>
    public static string Aggregate(IEnumerable<CheckRun> runs, IReadOnlyCollection<string>? required = null)
    {
        var latest = LatestPerName(runs);

        if (required is { Count: > 0 })
        {
            var anyFailing = false;
            var allPassing = true;
            foreach (var name in required)
            {
                var run = latest.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
                if (run == null || !IsCompleted(run)) { allPassing = false; continue; }
                if (IsFailing(run)) anyFailing = true;
                if (!IsPassing(run)) allPassing = false;
            }
            return anyFailing ? "FAILURE" : allPassing ? "SUCCESS" : "PENDING";
        }

        if (latest.Count == 0) return "NONE";
        if (latest.Any(IsFailing)) return "FAILURE";
        return latest.All(IsPassing) ? "SUCCESS" : "PENDING";
    }

    public static bool AllPassed(IEnumerable<CheckRun> runs, IReadOnlyCollection<string>? required = null) =>
        Aggregate(runs, required) == "SUCCESS";

    /// <summary>How many of the most recent runs of one check failed in a row (newest first).</summary>
    public static int ConsecutiveFailures(IEnumerable<CheckRun> runs, string checkName)
    {
        var count = 0;
        foreach (var run in runs
                     .Where(r => string.Equals(r.Name, checkName, StringComparison.OrdinalIgnoreCase) && IsCompleted(r))
                     .OrderByDescending(r => r.StartedAt).ThenByDescending(r => r.Id))
        {
            if (!IsFailing(run)) break;
            count++;
        }
        return count;
    }

    /// <summary>Branch protection / ruleset required check names that apply to the PR target branch (empty = none known).</summary>
    public static async Task<List<string>> ResolveRequiredAsync(AppDbContext db, PullRequest pr)
    {
        var rules = await db.RepositoryRules
            .Where(r => r.Repository == pr.Repository && r.RequiresStatusChecks)
            .ToListAsync();

        return rules
            .Where(r => !string.IsNullOrEmpty(pr.TargetBranch) && !string.IsNullOrEmpty(r.BranchPattern)
                        && WildcardMatch(r.BranchPattern, pr.TargetBranch))
            .SelectMany(r => ParseNames(r.RequiredStatusChecks))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<string> ParseNames(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    // ---- wildcards / globs / comparisons ----

    public static bool WildcardMatch(string pattern, string input)
    {
        var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return Regex.IsMatch(input, regex, RegexOptions.IgnoreCase);
    }

    /// <summary>Path glob: ** crosses directories, * and ? do not.</summary>
    public static bool GlobMatch(string glob, string path)
    {
        var p = Regex.Escape(glob.Replace('\\', '/'))
            .Replace("\\*\\*/", "(?:.*/)?")
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", "[^/]");
        return Regex.IsMatch(path.Replace('\\', '/'), "^" + p + "$", RegexOptions.IgnoreCase);
    }

    /// <summary>Evaluates expressions such as ">=2", "<=50", "=1", ">3", "<10" or a bare number (equality).</summary>
    public static bool Compare(long actual, string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return false;
        var e = expression.Trim();
        foreach (var op in new[] { ">=", "<=", "==", ">", "<", "=" })
        {
            if (!e.StartsWith(op)) continue;
            if (!long.TryParse(e[op.Length..].Trim(), out var n)) return false;
            return op switch
            {
                ">=" => actual >= n,
                "<=" => actual <= n,
                ">" => actual > n,
                "<" => actual < n,
                _ => actual == n
            };
        }
        return long.TryParse(e, out var exact) && actual == exact;
    }
}
