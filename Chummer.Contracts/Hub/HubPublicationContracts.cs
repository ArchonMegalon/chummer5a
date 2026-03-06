namespace Chummer.Contracts.Hub;

public static class HubPublicationOperations
{
    public const string CreateDraft = "create-draft";
    public const string SubmitProject = "submit-project";
    public const string ListModerationQueue = "list-moderation-queue";
}

public static class HubPublicationStates
{
    public const string Draft = "draft";
    public const string Submitted = "submitted";
    public const string PendingReview = "pending-review";
}

public static class HubModerationStates
{
    public const string PendingReview = "pending-review";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}

public sealed record HubPublishDraftRequest(
    string ProjectKind,
    string ProjectId,
    string RulesetId,
    string Title);

public sealed record HubPublishDraftReceipt(
    string DraftId,
    string ProjectKind,
    string ProjectId,
    string RulesetId,
    string Title,
    string OwnerId,
    string State);

public sealed record HubSubmitProjectRequest(
    string? Notes = null);

public sealed record HubProjectSubmissionReceipt(
    string ProjectKind,
    string ProjectId,
    string RulesetId,
    string OwnerId,
    string State,
    string ReviewState,
    string? Notes = null);

public sealed record HubModerationQueueItem(
    string CaseId,
    string ProjectKind,
    string ProjectId,
    string RulesetId,
    string OwnerId,
    string State,
    DateTimeOffset CreatedAtUtc,
    string? Summary = null);

public sealed record HubModerationQueue(
    IReadOnlyList<HubModerationQueueItem> Items);

public sealed record HubPublicationNotImplementedReceipt(
    string Error,
    string Operation,
    string Message,
    string? ProjectKind = null,
    string? ProjectId = null,
    string? OwnerId = null);

public sealed record HubPublicationResult<T>(
    T? Payload = default,
    HubPublicationNotImplementedReceipt? NotImplemented = null)
{
    public bool IsImplemented => NotImplemented is null;

    public static HubPublicationResult<T> Implemented(T payload)
        => new(payload, null);

    public static HubPublicationResult<T> FromNotImplemented(HubPublicationNotImplementedReceipt receipt)
        => new(default, receipt);
}
