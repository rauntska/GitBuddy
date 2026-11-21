using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using GitBuddy.Api.Extensions;
using GitBuddy.Domain.Data;
using GitBuddy.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Services.Automation;

public interface IAutomationRuleEngine
{
    /// <summary>Evaluates all enabled rules for the trigger. Never throws; failures are written to the run log.</summary>
    Task EvaluateAsync(TriggerContext context);
}

/// <summary>
/// Remembers label changes caused by automation so the resulting webhook does not re-trigger the rule that made it.
/// </summary>
public static class AutomationEchoes
{
    private static readonly ConcurrentDictionary<string, DateTime> Entries = new();
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(2);

    public static void Register(int ruleId, int pullRequestId, AutomationTriggerType trigger, string label)
    {
        var now = DateTime.UtcNow;
        foreach (var stale in Entries.Where(e => e.Value < now).Select(e => e.Key).ToList())
            Entries.TryRemove(stale, out _);
        Entries[Key(ruleId, pullRequestId, trigger, label)] = now.Add(Ttl);
    }

    public static bool IsEcho(int ruleId, int pullRequestId, AutomationTriggerType trigger, string? label) =>
        label != null
        && Entries.TryGetValue(Key(ruleId, pullRequestId, trigger, label), out var expires)
        && expires > DateTime.UtcNow;

    private static string Key(int ruleId, int pullRequestId, AutomationTriggerType trigger, string label) =>
        $"{ruleId}|{pullRequestId}|{trigger}|{label.ToLowerInvariant()}";
}

public class AutomationRuleEngine(
    AppDbContext db,
    IGitHubService gitHubService,
    ITeamsNotificationService teamsService,
    INotificationService notificationService,
    ILogger<AutomationRuleEngine> logger) : IAutomationRuleEngine
{
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(15);

    public async Task EvaluateAsync(TriggerContext context)
    {
        try
        {
            var pr = await db.PullRequests
                .Include(p => p.Reviews)
                .Include(p => p.CheckRuns)
                .FirstOrDefaultAsync(p => p.Id == context.PullRequestId);
            if (pr == null) return;

            var config = await db.GitHubConfigs.FirstOrDefaultAsync();
            if (config == null) return;

            var rules = await db.AutomationRules
                .Include(r => r.CreatedByUser)
                .Where(r => r.Enabled && r.TriggerType == context.Trigger)
                .ToListAsync();

            foreach (var rule in rules.Where(r => AppliesToRepository(r, pr)))
            {
                try
                {
                    await EvaluateRuleAsync(rule, pr, config, context);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Automation rule {RuleId} failed for PR {PrId}", rule.Id, pr.Id);
                    await WriteLogAsync(rule, pr, context, matched: false, actions: [], error: ex.Message, key: null);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Automation evaluation failed for trigger {Trigger} on PR {PrId}", context.Trigger, context.PullRequestId);
        }
    }

    private static bool AppliesToRepository(AutomationRule rule, PullRequest pr)
    {
        if (string.IsNullOrWhiteSpace(rule.Repositories)) return true;
        return rule.Repositories
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(r => string.Equals(r.Split('/').Last(), pr.Repository, StringComparison.OrdinalIgnoreCase));
    }

    private async Task EvaluateRuleAsync(AutomationRule rule, PullRequest pr, GitHubConfig config, TriggerContext context)
    {
        // Loop prevention: ignore label webhooks caused by this very rule's own actions.
        var label = context.Trigger is AutomationTriggerType.LabelAdded or AutomationTriggerType.LabelRemoved ? context.Label : null;
        if (AutomationEchoes.IsEcho(rule.Id, pr.Id, context.Trigger, label))
        {
            logger.LogDebug("Rule {RuleId} skipped: echo of its own label action on PR {PrId}", rule.Id, pr.Id);
            return;
        }

        var key = BuildKey(context);
        var now = DateTime.UtcNow;

        // Duplicate webhook deliveries and flapping conditions.
        var window = rule.MinIntervalSeconds > 0 ? TimeSpan.FromSeconds(rule.MinIntervalSeconds) : DuplicateWindow;
        var since = now - window;
        var recentQuery = db.AutomationRunLogs
            .Where(l => l.AutomationRuleId == rule.Id && l.PullRequestId == pr.Id && l.ConditionsMatched && l.TriggeredAt > since);
        if (rule.MinIntervalSeconds <= 0)
            recentQuery = recentQuery.Where(l => l.IdempotencyKey == key);
        if (await recentQuery.AnyAsync())
        {
            logger.LogDebug("Rule {RuleId} skipped for PR {PrId}: within rate limit / duplicate window", rule.Id, pr.Id);
            return;
        }

        var conditions = AutomationJson.ParseConditions(rule.Conditions);
        var failedCondition = await FirstFailedConditionAsync(conditions, pr, config, context);
        if (failedCondition != null)
        {
            // Non-matches are only recorded for dry-run rules, so admins can sanity-check them without flooding the log.
            if (rule.DryRun)
                await WriteLogAsync(rule, pr, context, matched: false, actions: [$"Not matched: {failedCondition}"], error: null, key);
            return;
        }

        var results = new List<string>();
        string? error = null;
        var actions = AutomationJson.ParseActions(rule.Actions);
        var token = rule.CreatedByUser?.AccessToken;

        foreach (var action in actions)
        {
            try
            {
                results.Add(await ExecuteActionAsync(rule, action, pr, config, context, token));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Automation rule {RuleId} action {Action} failed on PR {PrId}", rule.Id, action.Type, pr.Id);
                results.Add($"FAILED {action.Type}: {ex.Message}");
                error = string.IsNullOrEmpty(error) ? ex.Message : $"{error}; {ex.Message}";
            }
        }

        rule.LastTriggeredAt = now;
        rule.TriggerCount++;
        await WriteLogAsync(rule, pr, context, matched: true, actions: results, error, key);
    }

    private static string BuildKey(TriggerContext c) => c.Trigger switch
    {
        AutomationTriggerType.LabelAdded or AutomationTriggerType.LabelRemoved => $"{c.Trigger}|{c.Label}",
        AutomationTriggerType.CheckRunCompleted => $"{c.Trigger}|{c.CheckName}|{c.CheckConclusion}",
        AutomationTriggerType.ReviewSubmitted => $"{c.Trigger}|{c.Reviewer}|{c.ReviewState}",
        AutomationTriggerType.MergeableStateChanged => $"{c.Trigger}|{c.MergeableState}",
        _ => c.Trigger.ToString()
    };

    private async Task WriteLogAsync(AutomationRule rule, PullRequest pr, TriggerContext context,
        bool matched, List<string> actions, string? error, string? key)
    {
        db.AutomationRunLogs.Add(new AutomationRunLog
        {
            AutomationRuleId = rule.Id,
            PullRequestId = pr.Id,
            TriggeredAt = DateTime.UtcNow,
            ConditionsMatched = matched,
            DryRun = rule.DryRun,
            ActionsTaken = AutomationJson.Serialize(actions),
            Error = error,
            IdempotencyKey = key ?? BuildKey(context)
        });
        await db.SaveChangesAsync();
    }

    // ---------------------------------------------------------------- conditions

    private async Task<string?> FirstFailedConditionAsync(
        List<ConditionDef> conditions, PullRequest pr, GitHubConfig config, TriggerContext context)
    {
        foreach (var c in conditions)
        {
            if (!await EvaluateConditionAsync(c, pr, config, context))
                return string.IsNullOrWhiteSpace(c.Value) ? c.Type : $"{c.Type} {c.Value}";
        }
        return null;
    }

    private async Task<bool> EvaluateConditionAsync(ConditionDef c, PullRequest pr, GitHubConfig config, TriggerContext context)
    {
        var value = c.Value?.Trim() ?? string.Empty;
        switch (c.Type)
        {
            case ConditionTypes.Label:
                if (context.Trigger is AutomationTriggerType.LabelAdded or AutomationTriggerType.LabelRemoved)
                    return string.Equals(context.Label, value, StringComparison.OrdinalIgnoreCase);
                return SplitLabels(pr.Labels).Contains(value, StringComparer.OrdinalIgnoreCase);

            case ConditionTypes.CheckName:
                return string.Equals(context.CheckName, value, StringComparison.OrdinalIgnoreCase);

            case ConditionTypes.CheckConclusion:
                return string.Equals(context.CheckConclusion, value, StringComparison.OrdinalIgnoreCase);

            case ConditionTypes.AllRequiredChecksPassed:
            {
                var required = await ResolveRequiredChecksAsync(pr, value);
                return ChecksEvaluator.AllPassed(pr.CheckRuns, required);
            }

            case ConditionTypes.ConsecutiveFailures:
            {
                if (!int.TryParse(value, out var n) || context.CheckName == null) return false;
                var streak = await db.CheckFailureStreaks
                    .Where(s => s.PullRequestId == pr.Id && s.CheckName == context.CheckName)
                    .Select(s => s.Count)
                    .FirstOrDefaultAsync();
                return streak >= n;
            }

            case ConditionTypes.ApprovalCount:
                return ChecksEvaluator.Compare(CountApprovals(pr), value);

            case ConditionTypes.HasRequestedReviewers:
            {
                var requested = await gitHubService.GetRequestedReviewersAsync(config.Organization, pr.Repository, pr.GitHubId, config);
                var has = requested.Users.Count + requested.Teams.Count > 0;
                return bool.TryParse(value, out var expected) && has == expected;
            }

            case ConditionTypes.AuthorPattern:
                return ChecksEvaluator.WildcardMatch(value, pr.Author);

            case ConditionTypes.BaseBranch:
                return ChecksEvaluator.WildcardMatch(value, pr.TargetBranch);

            case ConditionTypes.ChangedFileGlob:
            {
                var paths = await ChangedPathsAsync(pr);
                return paths.Any(p => ChecksEvaluator.GlobMatch(value, p));
            }

            case ConditionTypes.ChangedFilesAllMatch:
            {
                var paths = await ChangedPathsAsync(pr);
                return paths.Count > 0 && paths.All(p => ChecksEvaluator.GlobMatch(value, p));
            }

            case ConditionTypes.PrSizeLines:
                return ChecksEvaluator.Compare(pr.Additions + pr.Deletions, value);

            case ConditionTypes.ReviewState:
                return string.Equals(context.ReviewState, value, StringComparison.OrdinalIgnoreCase);

            case ConditionTypes.MergeableState:
            {
                var state = (context.MergeableState ?? pr.MergeableState ?? string.Empty).ToUpperInvariant();
                return value.Equals("NOT_CONFLICTING", StringComparison.OrdinalIgnoreCase)
                    ? state != "CONFLICTING"
                    : state == value.ToUpperInvariant();
            }

            default:
                return false;
        }
    }

    private static int CountApprovals(PullRequest pr) =>
        pr.Reviews
            .Where(r => !string.Equals(r.State, "Commented", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(r.State, "Pending", StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => r.Reviewer, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(r => r.SubmittedAt).First())
            .Count(r => string.Equals(r.State, "Approved", StringComparison.OrdinalIgnoreCase));

    private async Task<List<string>> ChangedPathsAsync(PullRequest pr) =>
        await db.FileDiffs.Where(f => f.PullRequestId == pr.Id).Select(f => f.Path).ToListAsync();

    /// <summary>Rule override list, else branch protection / ruleset names, else empty (= all checks).</summary>
    private async Task<List<string>> ResolveRequiredChecksAsync(PullRequest pr, string overrideList)
    {
        if (!string.IsNullOrWhiteSpace(overrideList))
            return overrideList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        return await ChecksEvaluator.ResolveRequiredAsync(db, pr);
    }

    private static List<string> SplitLabels(string? labels) =>
        (labels ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    // ---------------------------------------------------------------- actions

    private async Task<string> ExecuteActionAsync(
        AutomationRule rule, ActionDef action, PullRequest pr, GitHubConfig config, TriggerContext context, string? token)
    {
        string P(string key) => action.Params != null && action.Params.TryGetValue(key, out var v) ? v.Trim() : string.Empty;
        var dry = rule.DryRun;
        var org = config.Organization;

        // Actions that talk to GitHub as the rule creator need their token.
        bool NeedsToken() => action.Type is ActionTypes.AddComment or ActionTypes.AddLabel or ActionTypes.RemoveLabel
            or ActionTypes.SetLabelExclusive or ActionTypes.RequestReview;
        if (!dry && NeedsToken() && string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("The rule creator has no GitHub token; ask them to sign in again or recreate the rule.");

        switch (action.Type)
        {
            case ActionTypes.AddComment:
            {
                var body = Render(P("template"), pr, context);
                if (dry) return $"Would comment: {body}";
                await gitHubService.AddPullRequestCommentAsync(org, pr.Repository, pr.GitHubId, body, null, null, config, token!);
                return "Commented";
            }

            case ActionTypes.AddLabel:
            {
                var l = P("label");
                if (dry) return $"Would add label {l}";
                await gitHubService.AddLabelsAsync(org, pr.Repository, pr.GitHubId, [l], token!);
                AutomationEchoes.Register(rule.Id, pr.Id, AutomationTriggerType.LabelAdded, l);
                await SetStoredLabelsAsync(pr, labels => { if (!labels.Contains(l, StringComparer.OrdinalIgnoreCase)) labels.Add(l); });
                return $"Added label {l}";
            }

            case ActionTypes.RemoveLabel:
            {
                var l = P("label");
                if (dry) return $"Would remove label {l}";
                await gitHubService.RemoveLabelAsync(org, pr.Repository, pr.GitHubId, l, token!);
                AutomationEchoes.Register(rule.Id, pr.Id, AutomationTriggerType.LabelRemoved, l);
                await SetStoredLabelsAsync(pr, labels => labels.RemoveAll(x => x.Equals(l, StringComparison.OrdinalIgnoreCase)));
                return $"Removed label {l}";
            }

            case ActionTypes.SetLabelExclusive:
            {
                var group = P("group");
                var target = P("label");
                if (dry) return $"Would set label {target} (replacing {group}*)";

                var current = await gitHubService.GetPullRequestLabelsAsync(org, pr.Repository, pr.GitHubId, token!);
                var removed = new List<string>();
                foreach (var old in current.Where(x => x.StartsWith(group, StringComparison.OrdinalIgnoreCase)
                                                       && !x.Equals(target, StringComparison.OrdinalIgnoreCase)))
                {
                    await gitHubService.RemoveLabelAsync(org, pr.Repository, pr.GitHubId, old, token!);
                    AutomationEchoes.Register(rule.Id, pr.Id, AutomationTriggerType.LabelRemoved, old);
                    removed.Add(old);
                }

                if (!current.Contains(target, StringComparer.OrdinalIgnoreCase))
                {
                    await gitHubService.AddLabelsAsync(org, pr.Repository, pr.GitHubId, [target], token!);
                    AutomationEchoes.Register(rule.Id, pr.Id, AutomationTriggerType.LabelAdded, target);
                }

                await SetStoredLabelsAsync(pr, labels =>
                {
                    labels.RemoveAll(x => removed.Contains(x, StringComparer.OrdinalIgnoreCase));
                    if (!labels.Contains(target, StringComparer.OrdinalIgnoreCase)) labels.Add(target);
                });
                return removed.Count > 0 ? $"Set label {target} (removed {string.Join(", ", removed)})" : $"Set label {target}";
            }

            case ActionTypes.RequestReview:
            {
                var users = Split(P("users")).Where(u => !u.Equals(pr.Author, StringComparison.OrdinalIgnoreCase)).ToList();
                var teams = Split(P("teams"));
                if (users.Count == 0 && teams.Count == 0) return "Skipped: no reviewers to request";
                var who = string.Join(", ", users.Concat(teams.Select(t => $"team:{t}")));
                if (dry) return $"Would request review from {who}";
                await gitHubService.RequestReviewersAsync(org, pr.Repository, pr.GitHubId, users, teams, token!);
                return $"Requested review from {who}";
            }

            case ActionTypes.PostTeamsMessage:
            {
                var recipient = P("recipient").ToLowerInvariant();
                var title = string.IsNullOrWhiteSpace(P("title")) ? "GitBuddy automation" : P("title");
                var text = Render(P("template"), pr, context);
                var to = recipient switch
                {
                    "author" => $"To: {pr.Author}",
                    "reviewers" => $"To: {ReviewersText(pr)}",
                    _ => null
                };
                if (to != null) text = $"{text}\n\n{to}";
                if (dry) return $"Would post to Teams ({(recipient == "" ? "channel" : recipient)}): {text}";
                var sent = await teamsService.SendMessageAsync(pr, title, text);
                return sent ? "Posted Teams message" : "Teams message not sent (Teams disabled or webhook failed)";
            }

            case ActionTypes.SetPriority:
            {
                var priority = int.Parse(P("priority"));
                if (dry) return $"Would set priority {priority}";
                var tracked = await db.PullRequests.FirstAsync(p => p.Id == pr.Id);
                tracked.Priority = priority;
                await db.SaveChangesAsync();
                await BroadcastAsync(pr.Id);
                return $"Set priority {priority}";
            }

            default:
                throw new InvalidOperationException($"Unknown action type {action.Type}");
        }
    }

    private async Task SetStoredLabelsAsync(PullRequest pr, Action<List<string>> mutate)
    {
        var tracked = await db.PullRequests.FirstAsync(p => p.Id == pr.Id);
        var labels = SplitLabels(tracked.Labels);
        mutate(labels);
        tracked.Labels = string.Join(",", labels);
        pr.Labels = tracked.Labels;
        await db.SaveChangesAsync();
    }

    private async Task BroadcastAsync(int prId)
    {
        var full = await db.PullRequests.Include(p => p.Reviews).Include(p => p.ReviewThreads).FirstOrDefaultAsync(p => p.Id == prId);
        if (full != null)
            await notificationService.BroadcastPRUpdatedAsync(full.ToPRListUpdateDto());
    }

    private static List<string> Split(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string ReviewersText(PullRequest pr)
    {
        var names = pr.Reviews.Select(r => r.Reviewer).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return names.Count == 0 ? "(no reviewers yet)" : string.Join(", ", names);
    }

    /// <summary>Tiny token renderer used only by automation actions: {{author}}, {{prTitle}}, {{prUrl}}, {{reviewers}}, {{checkName}}, {{label}}, {{repository}}.</summary>
    public static string Render(string template, PullRequest pr, TriggerContext context)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["author"] = pr.Author,
            ["prTitle"] = pr.Title,
            ["prUrl"] = pr.Url,
            ["reviewers"] = ReviewersText(pr),
            ["checkName"] = context.CheckName ?? string.Empty,
            ["label"] = context.Label ?? string.Empty,
            ["repository"] = pr.Repository
        };
        return Regex.Replace(template, @"\{\{\s*(\w+)\s*\}\}",
            m => values.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);
    }
}

/// <summary>Runs rule evaluation off the webhook request path, in its own DI scope.</summary>
public interface IAutomationDispatcher
{
    void Dispatch(TriggerContext context);
}

public class AutomationDispatcher(IServiceScopeFactory scopeFactory, ILogger<AutomationDispatcher> logger) : IAutomationDispatcher
{
    public void Dispatch(TriggerContext context)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var engine = scope.ServiceProvider.GetRequiredService<IAutomationRuleEngine>();
                await engine.EvaluateAsync(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Automation dispatch failed for {Trigger} on PR {PrId}", context.Trigger, context.PullRequestId);
            }
        });
    }
}
