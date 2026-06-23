namespace GitBuddy.Domain.Classifier;

public static class PrStatus
{
    public const string Closed = "Closed";
    public const string Merged = "Merged";
    public const string Draft = "Draft";
    public const string ReadyToMerge = "ReadyToMerge";
    public const string Approved = "Approved";
    public const string ChangesRequested = "ChangesRequested";
    public const string Reviewed = "Reviewed";
    public const string AwaitingReview = "AwaitingReview";
}