using Chummer.Contracts.Owners;

namespace Chummer.Contracts.Content;

public static class RuntimeLockCatalogKinds
{
    public const string Saved = "saved";
    public const string Published = "published";
    public const string Derived = "derived";
}

public static class RuntimeLockCompatibilityStates
{
    public const string Compatible = "compatible";
    public const string RebindRequired = "rebind-required";
    public const string MissingPack = "missing-pack";
    public const string RulesetMismatch = "ruleset-mismatch";
    public const string EngineApiMismatch = "engine-api-mismatch";
}

public sealed record RuntimeLockRegistryEntry(
    string LockId,
    OwnerScope Owner,
    string Title,
    string Visibility,
    string CatalogKind,
    ResolvedRuntimeLock RuntimeLock,
    DateTimeOffset UpdatedAtUtc,
    string? Description = null);

public sealed record RuntimeLockCompatibilityDiagnostic(
    string State,
    string Message,
    string? RequiredRulesetId = null,
    string? RequiredRuntimeFingerprint = null);

public sealed record RuntimeLockInstallCandidate(
    string TargetKind,
    string TargetId,
    RuntimeLockRegistryEntry Entry,
    IReadOnlyList<RuntimeLockCompatibilityDiagnostic> Diagnostics,
    bool CanInstall = true);

public sealed record RuntimeLockRegistryPage(
    IReadOnlyList<RuntimeLockRegistryEntry> Entries,
    int TotalCount,
    string? ContinuationToken = null);
