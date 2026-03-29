namespace Chummer.Contracts.Hub;

public static class HubProjectCompatibilityRowKinds
{
    public const string Ruleset = "ruleset";
    public const string EngineApi = "engine-api";
    public const string Visibility = "visibility";
    public const string Trust = "trust";
    public const string InstallState = "install-state";
    public const string Capabilities = "capabilities";
    public const string SessionRuntime = "session-runtime";
    public const string HostedPublic = "hosted-public";
    public const string RuntimeFingerprint = "runtime-fingerprint";
    public const string RuntimeRequirements = "runtime-requirements";
    public const string CampaignReturn = "campaign-return";
    public const string SupportClosure = "support-closure";
}

public static class HubProjectCompatibilityStates
{
    public const string Compatible = "compatible";
    public const string ReviewRequired = "review-required";
    public const string Blocked = "blocked";
    public const string Informational = "informational";
}

public sealed record HubProjectCompatibilityRow(
    string Kind,
    string Label,
    string State,
    string CurrentValue,
    string? RequiredValue = null,
    string? Notes = null);

public sealed record HubProjectCompatibilityMatrix(
    string Kind,
    string ItemId,
    IReadOnlyList<HubProjectCompatibilityRow> Rows,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<HubProjectCapabilityDescriptorProjection>? Capabilities = null);
