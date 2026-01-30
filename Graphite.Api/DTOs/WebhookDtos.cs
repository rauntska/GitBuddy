using System.Text.Json.Serialization;

namespace Graphite.Api.DTOs;

public record PullRequestWebhookPayload(
    string Action,
    int Number,
    PullRequestData PullRequest,
    RepositoryData Repository,
    SenderData Sender
);

public record PullRequestData(
    int Id,
    int Number,
    string Title,
    string State,
    bool Draft,
    [property: JsonPropertyName("user")] UserData User,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? Body,
    [property: JsonPropertyName("merged_by")] UserData? MergedBy,
    [property: JsonPropertyName("head")] HeadBranchData Head,
    [property: JsonPropertyName("base")] BaseBranchData Base,
    string Url,
    int Additions,
    int Deletions,
    int ChangedFiles
);

public record HeadBranchData(
    [property: JsonPropertyName("ref")] string Ref
);

public record BaseBranchData(
    [property: JsonPropertyName("ref")] string Ref
);

public record UserData(
    string Login,
    [property: JsonPropertyName("avatar_url")] string? AvatarUrl
);

public record PushWebhookPayload(
    string Ref,
    string Before,
    string After,
    RepositoryData Repository,
    PusherData Pusher,
    List<CommitData> Commits
);

public record CommitData(
    string Id,
    string Message,
    string Timestamp,
    AuthorData Author
);

public record AuthorData(
    string Name,
    string Email
);

public record CommentWebhookPayload(
    string Action,
    [property: JsonPropertyName("comment")] CommentData Comment,
    [property: JsonPropertyName("pull_request")] SimplePullRequestData? PullRequest,
    [property: JsonPropertyName("issue")] IssueData? Issue,
    RepositoryData Repository,
    SenderData Sender
);

public record SimplePullRequestData(
    int Id,
    int Number
);

public record CommentData(
    long Id,
    string Body,
    [property: JsonPropertyName("user")] UserData User,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? Path,
    int? Line,
    bool? IsOutdated,
    string? DiffHunk
);

public record IssueData(
    int Id,
    int Number
);

public record ReviewThreadWebhookPayload(
    string Action,
    long Id,
    ThreadData Thread,
    PullRequestData? PullRequest,
    RepositoryData Repository,
    SenderData Sender
);

public record ThreadData(
    long Id,
    string? Path,
    int? Line,
    bool IsResolved,
    bool IsOutdated
);

public record RepositoryData(
    int Id,
    string Name,
    [property: JsonPropertyName("full_name")] string FullName,
    bool Private,
    OwnerData Owner
);

public record OwnerData(
    string Login,
    [property: JsonPropertyName("avatar_url")] string? AvatarUrl
);

public record SenderData(
    string Login,
    string? Avatar
);

public record PusherData(
    string Name,
    string Email
);