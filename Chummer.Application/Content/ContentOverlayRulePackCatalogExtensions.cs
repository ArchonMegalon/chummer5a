using Chummer.Contracts.Content;
using Chummer.Contracts.Rulesets;

namespace Chummer.Application.Content;

public static class ContentOverlayRulePackCatalogExtensions
{
    public static RulePackCatalog ToRulePackCatalog(
        this ContentOverlayCatalog catalog,
        string rulesetId,
        string engineApiVersion = "rulepack-v1")
    {
        ArgumentNullException.ThrowIfNull(catalog);

        string normalizedRulesetId = RulesetDefaults.NormalizeRequired(rulesetId);
        IReadOnlyList<RulePackManifest> installedRulePacks = catalog.Overlays
            .Select(overlay => overlay.ToRulePackManifest(normalizedRulesetId, engineApiVersion))
            .ToArray();

        return new RulePackCatalog(installedRulePacks);
    }

    public static RulePackManifest ToRulePackManifest(
        this ContentOverlayPack overlay,
        string rulesetId,
        string engineApiVersion = "rulepack-v1")
    {
        ArgumentNullException.ThrowIfNull(overlay);

        string normalizedRulesetId = RulesetDefaults.NormalizeRequired(rulesetId);
        List<RulePackAssetDescriptor> assets = [];

        if (!string.IsNullOrWhiteSpace(overlay.DataPath))
        {
            assets.Add(new RulePackAssetDescriptor(
                Kind: RulePackAssetKinds.Xml,
                Mode: overlay.Mode,
                RelativePath: "data/",
                Checksum: string.Empty));
        }

        if (!string.IsNullOrWhiteSpace(overlay.LanguagePath))
        {
            assets.Add(new RulePackAssetDescriptor(
                Kind: RulePackAssetKinds.Localization,
                Mode: RulePackAssetModes.ReplaceFile,
                RelativePath: "lang/",
                Checksum: string.Empty));
        }

        return new RulePackManifest(
            PackId: overlay.Id,
            Version: "overlay-v1",
            Title: overlay.Name,
            Author: string.Empty,
            Description: overlay.Description,
            Targets: [normalizedRulesetId],
            EngineApiVersion: engineApiVersion,
            DependsOn: [],
            ConflictsWith: [],
            Visibility: ArtifactVisibilityModes.LocalOnly,
            TrustTier: ArtifactTrustTiers.LocalOnly,
            Assets: assets);
    }
}
