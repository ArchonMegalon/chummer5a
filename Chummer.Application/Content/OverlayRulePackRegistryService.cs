using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;

namespace Chummer.Application.Content;

public sealed class OverlayRulePackRegistryService : IRulePackRegistryService
{
    private const string SystemOwnerId = "system";
    private readonly IContentOverlayCatalogService _overlays;
    private readonly IRulePackInstallStateStore _installStateStore;
    private readonly IRulePackPublicationStore _publicationStore;
    private readonly IRulesetSelectionPolicy _rulesetSelectionPolicy;

    public OverlayRulePackRegistryService(
        IContentOverlayCatalogService overlays,
        IRulesetSelectionPolicy rulesetSelectionPolicy,
        IRulePackPublicationStore publicationStore,
        IRulePackInstallStateStore installStateStore)
    {
        _overlays = overlays;
        _rulesetSelectionPolicy = rulesetSelectionPolicy;
        _publicationStore = publicationStore;
        _installStateStore = installStateStore;
    }

    public IReadOnlyList<RulePackRegistryEntry> List(OwnerScope owner, string? rulesetId = null)
    {
        string effectiveRulesetId = RulesetDefaults.NormalizeOptional(rulesetId)
            ?? _rulesetSelectionPolicy.GetDefaultRulesetId();
        Dictionary<string, ArtifactInstallState> installStateLookup = _installStateStore.List(owner, effectiveRulesetId)
            .ToDictionary(
                record => CreatePublicationKey(record.PackId, record.Version, record.RulesetId),
                record => record.Install,
                StringComparer.Ordinal);
        Dictionary<string, RulePackPublicationMetadata> publicationLookup = _publicationStore.List(owner, effectiveRulesetId)
            .ToDictionary(
                record => CreatePublicationKey(record.PackId, record.Version, record.RulesetId),
                record => record.Publication,
                StringComparer.Ordinal);

        return _overlays.GetCatalog().Overlays
            .Select(overlay => ToRegistryEntry(overlay, effectiveRulesetId, publicationLookup, installStateLookup))
            .ToArray();
    }

    public RulePackRegistryEntry? Get(OwnerScope owner, string packId, string? rulesetId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);

        return List(owner, rulesetId)
            .FirstOrDefault(entry => string.Equals(entry.Manifest.PackId, packId, StringComparison.Ordinal));
    }

    private static RulePackRegistryEntry ToRegistryEntry(
        ContentOverlayPack overlay,
        string rulesetId,
        IReadOnlyDictionary<string, RulePackPublicationMetadata> publicationLookup,
        IReadOnlyDictionary<string, ArtifactInstallState> installStateLookup)
    {
        RulePackManifest manifest = overlay.ToRulePackManifest(rulesetId);
        string key = CreatePublicationKey(manifest.PackId, manifest.Version, rulesetId);
        RulePackPublicationMetadata publication = publicationLookup.TryGetValue(
            key,
            out RulePackPublicationMetadata? persisted)
            ? persisted
            : new RulePackPublicationMetadata(
                OwnerId: SystemOwnerId,
                Visibility: manifest.Visibility,
                PublicationStatus: RulePackPublicationStatuses.Published,
                Review: new RulePackReviewDecision(RulePackReviewStates.NotRequired),
                Shares: []);
        ArtifactInstallState install = installStateLookup.TryGetValue(key, out ArtifactInstallState? persistedInstall)
            ? persistedInstall
            : new ArtifactInstallState(
                State: ArtifactInstallStates.Installed,
                InstalledTargetKind: RuleProfileApplyTargetKinds.GlobalDefaults,
                InstalledTargetId: OwnerScope.LocalSingleUser.NormalizedValue);

        return new RulePackRegistryEntry(manifest, publication, install);
    }

    private static string CreatePublicationKey(string packId, string version, string rulesetId)
    {
        return $"{packId}|{version}|{rulesetId}";
    }
}
