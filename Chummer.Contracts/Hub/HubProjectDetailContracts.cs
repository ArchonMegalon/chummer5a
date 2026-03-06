namespace Chummer.Contracts.Hub;

public static class HubProjectDependencyKinds
{
    public const string DependsOn = "depends-on";
    public const string ConflictsWith = "conflicts-with";
    public const string IncludesRulePack = "includes-rulepack";
    public const string RequiresRulePack = "requires-rulepack";
    public const string RequiresRuntimeFingerprint = "requires-runtime-fingerprint";
}

public static class HubProjectActionKinds
{
    public const string Install = "install";
    public const string Apply = "apply";
    public const string PreviewRuntime = "preview-runtime";
    public const string InspectRuntime = "inspect-runtime";
    public const string OpenRegistry = "open-registry";
}

public sealed record HubProjectDetailFact(
    string FactId,
    string Label,
    string Value);

public sealed record HubProjectDependency(
    string Kind,
    string ItemKind,
    string ItemId,
    string Version,
    string? Notes = null);

public sealed record HubProjectAction(
    string ActionId,
    string Label,
    string Kind,
    string? LinkTarget = null,
    bool Enabled = true,
    string? DisabledReason = null);

public sealed record HubProjectDetailProjection(
    HubCatalogItem Summary,
    string? OwnerId,
    string? CatalogKind,
    string? PublicationStatus,
    string? ReviewState,
    string? RuntimeFingerprint,
    IReadOnlyList<HubProjectDetailFact> Facts,
    IReadOnlyList<HubProjectDependency> Dependencies,
    IReadOnlyList<HubProjectAction> Actions);
