using GitBuddy.Domain.Models;

namespace GitBuddy.Api.Services.Automation;

public static class ConditionTypes
{
    public const string Label = "Label";                          // value: label name
    public const string CheckName = "CheckName";                  // value: check name (CheckRunCompleted)
    public const string CheckConclusion = "CheckConclusion";      // value: failure | success | ...
    public const string AllRequiredChecksPassed = "AllRequiredChecksPassed"; // value: optional comma list override
    public const string ConsecutiveFailures = "ConsecutiveFailures"; // value: N (>= N failures in a row for the check)
    public const string ApprovalCount = "ApprovalCount";          // value: >=N | <=N | =N | >N | <N
    public const string HasRequestedReviewers = "HasRequestedReviewers"; // value: true | false
    public const string AuthorPattern = "AuthorPattern";          // value: wildcard pattern
    public const string BaseBranch = "BaseBranch";                // value: wildcard pattern
    public const string ChangedFileGlob = "ChangedFileGlob";      // value: glob; any changed file matches
    public const string ChangedFilesAllMatch = "ChangedFilesAllMatch"; // value: glob; every changed file matches
    public const string PrSizeLines = "PrSizeLines";              // value: comparison against additions+deletions
    public const string ReviewState = "ReviewState";              // value: approved | changes_requested | commented
    public const string MergeableState = "MergeableState";        // value: CONFLICTING | NOT_CONFLICTING

    public static readonly string[] All =
    [
        Label, CheckName, CheckConclusion, AllRequiredChecksPassed, ConsecutiveFailures, ApprovalCount,
        HasRequestedReviewers, AuthorPattern, BaseBranch, ChangedFileGlob, ChangedFilesAllMatch, PrSizeLines,
        ReviewState, MergeableState
    ];
}

public static class ActionTypes
{
    public const string AddComment = "AddComment";               // params: template
    public const string AddLabel = "AddLabel";                   // params: label
    public const string RemoveLabel = "RemoveLabel";             // params: label
    public const string SetLabelExclusive = "SetLabelExclusive"; // params: group (prefix e.g. "size/"), label
    public const string RequestReview = "RequestReview";         // params: users, teams (comma separated)
    public const string PostTeamsMessage = "PostTeamsMessage";   // params: template, title?, recipient (author|reviewers|channel)
    public const string SetPriority = "SetPriority";             // params: priority (0-3)

    public static readonly string[] All =
        [AddComment, AddLabel, RemoveLabel, SetLabelExclusive, RequestReview, PostTeamsMessage, SetPriority];
}

public record AutomationTemplate(
    string Key,
    string Name,
    string Description,
    AutomationTriggerType TriggerType,
    List<ConditionDef> Conditions,
    List<ActionDef> Actions,
    int MinIntervalSeconds = 0);

public static class AutomationCatalog
{
    /// <summary>Which condition types make sense for which trigger (UI hint + server validation).</summary>
    public static readonly Dictionary<AutomationTriggerType, string[]> ConditionsByTrigger = new()
    {
        [AutomationTriggerType.PROpened] = Common(),
        [AutomationTriggerType.PRUpdated] = Common(),
        [AutomationTriggerType.LabelAdded] = Common(),
        [AutomationTriggerType.LabelRemoved] = Common(),
        [AutomationTriggerType.ReviewSubmitted] = Common(ConditionTypes.ReviewState),
        [AutomationTriggerType.CheckRunCompleted] = Common(ConditionTypes.CheckName, ConditionTypes.CheckConclusion, ConditionTypes.ConsecutiveFailures),
        [AutomationTriggerType.ChecksStatusChanged] = Common(),
        [AutomationTriggerType.MergeableStateChanged] = Common(ConditionTypes.MergeableState),
    };

    private static string[] Common(params string[] extra) =>
    [
        ConditionTypes.Label, ConditionTypes.AllRequiredChecksPassed, ConditionTypes.ApprovalCount,
        ConditionTypes.HasRequestedReviewers, ConditionTypes.AuthorPattern, ConditionTypes.BaseBranch,
        ConditionTypes.ChangedFileGlob, ConditionTypes.ChangedFilesAllMatch, ConditionTypes.PrSizeLines,
        ..extra
    ];

    public static List<string> Validate(AutomationTriggerType trigger, List<ConditionDef> conditions, List<ActionDef> actions)
    {
        var errors = new List<string>();
        var allowed = ConditionsByTrigger[trigger];

        foreach (var c in conditions)
        {
            if (!ConditionTypes.All.Contains(c.Type))
                errors.Add($"Unknown condition type '{c.Type}'.");
            else if (!allowed.Contains(c.Type))
                errors.Add($"Condition '{c.Type}' is not available for trigger '{trigger}'.");
            else if (c.Type != ConditionTypes.AllRequiredChecksPassed && string.IsNullOrWhiteSpace(c.Value))
                errors.Add($"Condition '{c.Type}' requires a value.");
        }

        if (actions.Count == 0)
            errors.Add("At least one action is required.");

        foreach (var a in actions)
        {
            if (!ActionTypes.All.Contains(a.Type))
            {
                errors.Add($"Unknown action type '{a.Type}'.");
                continue;
            }

            string P(string key) => a.Params != null && a.Params.TryGetValue(key, out var v) ? v : string.Empty;
            switch (a.Type)
            {
                case ActionTypes.AddComment when string.IsNullOrWhiteSpace(P("template")):
                    errors.Add("AddComment requires 'template'."); break;
                case ActionTypes.AddLabel or ActionTypes.RemoveLabel when string.IsNullOrWhiteSpace(P("label")):
                    errors.Add($"{a.Type} requires 'label'."); break;
                case ActionTypes.SetLabelExclusive when string.IsNullOrWhiteSpace(P("group")) || string.IsNullOrWhiteSpace(P("label")):
                    errors.Add("SetLabelExclusive requires 'group' and 'label'."); break;
                case ActionTypes.RequestReview when string.IsNullOrWhiteSpace(P("users")) && string.IsNullOrWhiteSpace(P("teams")):
                    errors.Add("RequestReview requires 'users' and/or 'teams'."); break;
                case ActionTypes.PostTeamsMessage when string.IsNullOrWhiteSpace(P("template")):
                    errors.Add("PostTeamsMessage requires 'template'."); break;
                case ActionTypes.PostTeamsMessage when !new[] { "", "channel", "author", "reviewers" }.Contains(P("recipient").ToLowerInvariant()):
                    errors.Add("PostTeamsMessage 'recipient' must be channel, author or reviewers."); break;
                case ActionTypes.SetPriority when !int.TryParse(P("priority"), out var p) || p < 0 || p > 3:
                    errors.Add("SetPriority requires 'priority' between 0 and 3."); break;
            }
        }

        return errors;
    }

    private static ActionDef Act(string type, params (string k, string v)[] p) =>
        new(type, p.ToDictionary(x => x.k, x => x.v));

    /// <summary>
    /// Templates intentionally leave reviewer/team fields empty; the admin must fill them in before saving
    /// (RequestReview validation rejects empty users and teams).
    /// </summary>
    public static readonly List<AutomationTemplate> Templates =
    [
        new("checks-pass-request-reviewers", "Checks pass → request reviewers",
            "When all required checks pass and nobody has been asked to review yet, request review from the configured users/teams.",
            AutomationTriggerType.ChecksStatusChanged,
            [new(ConditionTypes.AllRequiredChecksPassed, null), new(ConditionTypes.HasRequestedReviewers, "false")],
            [Act(ActionTypes.RequestReview, ("users", ""), ("teams", ""))]),

        new("label-specialist-review", "Label → specialist team review",
            "When a label such as needs-design-review is added, request review from a team.",
            AutomationTriggerType.LabelAdded,
            [new(ConditionTypes.Label, "needs-design-review")],
            [Act(ActionTypes.RequestReview, ("teams", ""))]),

        new("file-path-owner", "File path → owner review",
            "When migrations change, request the database owner and add a migration label.",
            AutomationTriggerType.PRUpdated,
            [new(ConditionTypes.ChangedFileGlob, "**/Migrations/**")],
            [Act(ActionTypes.AddLabel, ("label", "migration")), Act(ActionTypes.RequestReview, ("users", ""))]),

        new("size-small", "Size label: small", "PRs up to 50 changed lines get size/S.",
            AutomationTriggerType.PRUpdated,
            [new(ConditionTypes.PrSizeLines, "<=50")],
            [Act(ActionTypes.SetLabelExclusive, ("group", "size/"), ("label", "size/S"))]),
        new("size-medium", "Size label: medium", "PRs of 51-300 changed lines get size/M.",
            AutomationTriggerType.PRUpdated,
            [new(ConditionTypes.PrSizeLines, ">50"), new(ConditionTypes.PrSizeLines, "<=300")],
            [Act(ActionTypes.SetLabelExclusive, ("group", "size/"), ("label", "size/M"))]),
        new("size-large", "Size label: large", "PRs over 300 changed lines get size/L.",
            AutomationTriggerType.PRUpdated,
            [new(ConditionTypes.PrSizeLines, ">300")],
            [Act(ActionTypes.SetLabelExclusive, ("group", "size/"), ("label", "size/L"))]),

        new("area-frontend", "Area label: frontend", "PRs touching only gitbuddy-vue get the frontend label.",
            AutomationTriggerType.PRUpdated,
            [new(ConditionTypes.ChangedFilesAllMatch, "gitbuddy-vue/**")],
            [Act(ActionTypes.AddLabel, ("label", "frontend"))]),
        new("area-backend", "Area label: backend", "PRs touching only GitBuddy.Api get the backend label.",
            AutomationTriggerType.PRUpdated,
            [new(ConditionTypes.ChangedFilesAllMatch, "GitBuddy.Api/**")],
            [Act(ActionTypes.AddLabel, ("label", "backend"))]),

        new("ci-failed-notify", "CI failed repeatedly → notify",
            "Post to Teams after a check has failed several times in a row.",
            AutomationTriggerType.CheckRunCompleted,
            [new(ConditionTypes.CheckConclusion, "failure"), new(ConditionTypes.ConsecutiveFailures, "2")],
            [Act(ActionTypes.PostTeamsMessage, ("recipient", "author"), ("title", "CI is failing"),
                ("template", "{{author}}: check {{checkName}} has failed repeatedly on {{prTitle}}."))],
            MinIntervalSeconds: 3600),

        new("changes-requested-waiting", "Changes requested → waiting on author",
            "Mark the PR as waiting on the author.",
            AutomationTriggerType.ReviewSubmitted,
            [new(ConditionTypes.ReviewState, "changes_requested")],
            [Act(ActionTypes.SetLabelExclusive, ("group", "waiting-on-"), ("label", "waiting-on-author"))]),

        new("merge-conflict", "Merge conflict → needs rebase",
            "Comment and label when a PR becomes conflicting.",
            AutomationTriggerType.MergeableStateChanged,
            [new(ConditionTypes.MergeableState, "CONFLICTING")],
            [Act(ActionTypes.AddLabel, ("label", "conflict")),
             Act(ActionTypes.AddComment, ("template", "@{{author}} this PR has merge conflicts and needs a rebase."))],
            MinIntervalSeconds: 3600),
        new("merge-conflict-cleared", "Merge conflict cleared", "Remove the conflict label once the conflict is resolved.",
            AutomationTriggerType.MergeableStateChanged,
            [new(ConditionTypes.MergeableState, "NOT_CONFLICTING")],
            [Act(ActionTypes.RemoveLabel, ("label", "conflict"))]),

        new("approved-and-green", "Approved and green → ready to merge",
            "Label the PR and tell the team channel once it is approved and all required checks pass.",
            AutomationTriggerType.ChecksStatusChanged,
            [new(ConditionTypes.AllRequiredChecksPassed, null), new(ConditionTypes.ApprovalCount, ">=1")],
            [Act(ActionTypes.AddLabel, ("label", "ready-to-merge")),
             Act(ActionTypes.PostTeamsMessage, ("recipient", "channel"), ("title", "Ready to merge"),
                 ("template", "{{prTitle}} by {{author}} is approved and green."))]),

        new("re-review-after-push", "Re-review after a push",
            "When new commits land on an approved PR, drop ready-to-merge and ask approvers to look again.",
            AutomationTriggerType.PRUpdated,
            [new(ConditionTypes.ApprovalCount, ">=1")],
            [Act(ActionTypes.RemoveLabel, ("label", "ready-to-merge")),
             Act(ActionTypes.PostTeamsMessage, ("recipient", "reviewers"), ("title", "New commits pushed"),
                 ("template", "{{reviewers}}: {{prTitle}} has new commits since approval."))]),
    ];
}
