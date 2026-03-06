namespace Chummer.Contracts.Content;

public static class ArtifactVisibilityModes
{
    public const string Private = "private";
    public const string Shared = "shared";
    public const string CampaignShared = "campaign-shared";
    public const string Public = "public";
    public const string LocalOnly = "local-only";
}

public static class ArtifactTrustTiers
{
    public const string Official = "official";
    public const string Curated = "curated";
    public const string Private = "private";
    public const string LocalOnly = "local-only";
}

public static class RulePackAssetKinds
{
    public const string Xml = "xml";
    public const string Localization = "localization";
    public const string DeclarativeRules = "declarative-rules";
    public const string Lua = "lua";
    public const string Tests = "tests";
}

public static class RulePackAssetModes
{
    public const string ReplaceFile = "replace-file";
    public const string MergeCatalog = "merge-catalog";
    public const string AppendCatalog = "append-catalog";
    public const string RemoveNode = "remove-node";
    public const string PatchNode = "patch-node";
    public const string SetConstant = "set-constant";
    public const string OverrideThreshold = "override-threshold";
    public const string EnableOption = "enable-option";
    public const string DisableOption = "disable-option";
    public const string ReplaceCreationProfile = "replace-creation-profile";
    public const string ModifyCap = "modify-cap";
    public const string RenameLabel = "rename-label";
    public const string AddProvider = "add-provider";
    public const string WrapProvider = "wrap-provider";
    public const string ReplaceProvider = "replace-provider";
    public const string DisableProvider = "disable-provider";
}

public sealed record ArtifactVersionReference(
    string Id,
    string Version);

public sealed record ContentBundleDescriptor(
    string BundleId,
    string RulesetId,
    string Version,
    string Title,
    string Description,
    IReadOnlyList<string> AssetPaths);

public sealed record RulePackAssetDescriptor(
    string Kind,
    string Mode,
    string RelativePath,
    string Checksum);

public sealed record RulePackManifest(
    string PackId,
    string Version,
    string Title,
    string Author,
    string Description,
    IReadOnlyList<string> Targets,
    string EngineApiVersion,
    IReadOnlyList<ArtifactVersionReference> DependsOn,
    IReadOnlyList<ArtifactVersionReference> ConflictsWith,
    string Visibility,
    string TrustTier,
    IReadOnlyList<RulePackAssetDescriptor> Assets,
    string? Signature = null);

public sealed record RulePackCatalog(
    IReadOnlyList<RulePackManifest> InstalledRulePacks);

public sealed record BuildKitManifest(
    string BuildKitId,
    string Version,
    string Title,
    string Description,
    IReadOnlyList<string> Targets,
    IReadOnlyList<ArtifactVersionReference> RequiredRulePacks,
    string Visibility,
    string TrustTier);

public sealed record ResolvedRuntimeLock(
    string RulesetId,
    IReadOnlyList<ContentBundleDescriptor> ContentBundles,
    IReadOnlyList<ArtifactVersionReference> RulePacks,
    IReadOnlyDictionary<string, string> ProviderBindings,
    string EngineApiVersion,
    string RuntimeFingerprint);
