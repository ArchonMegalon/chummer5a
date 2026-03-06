using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;

namespace Chummer.Application.Content;

public sealed class OverlayRulePackRegistryService : IRulePackRegistryService
{
    private const string SystemOwnerId = "system";
    private readonly IContentOverlayCatalogService _overlays;
    private readonly IRulePackPublicationStore _publicationStore;
    private readonly IRulesetSelectionPolicy _rulesetSelectionPolicy;

    public OverlayRulePackRegistryService(
        IContentOverlayCatalogService overlays,
        IRulesetSelectionPolicy rulesetSelectionPolicy,
        IRulePackPublicationStore publicationStore)
    {
        _overlays = overlays;
        _rulesetSelectionPolicy = rulesetSelectionPolicy;
        _publicationStore = publicationStore;
    }

    public IReadOnlyList<RulePackRegistryEntry> List(OwnerScope owner, string? rulesetId = null)
    {
        string effectiveRulesetId = RulesetDefaults.NormalizeOptional(rulesetId)
            ?? _rulesetSelectionPolicy.GetDefaultRulesetId();
        Dictionary<string, RulePackPublicationMetadata> publicationLookup = _publicationStore.List(owner, effectiveRulesetId)
            .ToDictionary(
                record => CreatePublicationKey(record.PackId, record.Version, record.RulesetId),
                record => record.Publication,
                StringComparer.Ordinal);

        return _overlays.GetCatalog().Overlays
            .Select(overlay => ToRegistryEntry(overlay, effectiveRulesetId, publicationLookup))
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
        IReadOnlyDictionary<string, RulePackPublicationMetadata> publicationLookup)
    {
        RulePackManifest manifest = overlay.ToRulePackManifest(rulesetId);
        RulePackPublicationMetadata publication = publicationLookup.TryGetValue(
            CreatePublicationKey(manifest.PackId, manifest.Version, rulesetId),
            out RulePackPublicationMetadata? persisted)
            ? persisted
            : new RulePackPublicationMetadata(
                OwnerId: SystemOwnerId,
                Visibility: manifest.Visibility,
                PublicationStatus: RulePackPublicationStatuses.Published,
                Review: new RulePackReviewDecision(RulePackReviewStates.NotRequired),
                Shares: []);

        return new RulePackRegistryEntry(manifest, publication);
    }

    private static string CreatePublicationKey(string packId, string version, string rulesetId)
    {
        return $"{packId}|{version}|{rulesetId}";
    }
}
