namespace Chummer.Contracts.Content;

public static class RulePackPublicationStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Archived = "archived";
}

public static class RulePackReviewStates
{
    public const string NotRequired = "not-required";
    public const string PendingReview = "pending-review";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}

public static class RulePackShareSubjectKinds
{
    public const string User = "user";
    public const string Campaign = "campaign";
    public const string PublicCatalog = "public-catalog";
}

public static class RulePackShareAccessLevels
{
    public const string View = "view";
    public const string Install = "install";
    public const string Fork = "fork";
    public const string Manage = "manage";
}

public sealed record RulePackForkLineage(
    string RootPackId,
    string ParentPackId,
    string ParentVersion,
    bool IsFork);

public sealed record RulePackShareGrant(
    string SubjectKind,
    string SubjectId,
    string AccessLevel);

public sealed record RulePackReviewDecision(
    string State,
    string? ReviewerId = null,
    string? Notes = null,
    DateTimeOffset? ReviewedAtUtc = null);

public sealed record RulePackPublicationMetadata(
    string OwnerId,
    string Visibility,
    string PublicationStatus,
    RulePackReviewDecision Review,
    IReadOnlyList<RulePackShareGrant> Shares,
    RulePackForkLineage? ForkLineage = null,
    DateTimeOffset? PublishedAtUtc = null);

public sealed record RulePackRegistryEntry(
    RulePackManifest Manifest,
    RulePackPublicationMetadata Publication,
    ArtifactInstallState Install);

public sealed record RulePackManifestRecord(
    RulePackManifest Manifest);

public sealed record RulePackPublicationRecord(
    string PackId,
    string Version,
    string RulesetId,
    RulePackPublicationMetadata Publication);

public sealed record RulePackInstallRecord(
    string PackId,
    string Version,
    string RulesetId,
    ArtifactInstallState Install);

public sealed record RulePackInstallHistoryRecord(
    string PackId,
    string Version,
    string RulesetId,
    ArtifactInstallHistoryEntry Entry);

public sealed record RulePackPublicationReceipt(
    string PackId,
    string Version,
    string PublicationStatus,
    string Visibility,
    string ReviewState,
    IReadOnlyList<RulePackShareGrant> Shares,
    RulePackForkLineage? ForkLineage = null);
