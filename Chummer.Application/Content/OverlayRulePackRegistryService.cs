using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;

namespace Chummer.Application.Content;

public sealed class OverlayRulePackRegistryService : IRulePackRegistryService
{
    private const string SystemOwnerId = "system";
    private readonly IContentOverlayCatalogService _overlays;
    private readonly IRulesetSelectionPolicy _rulesetSelectionPolicy;

    public OverlayRulePackRegistryService(
        IContentOverlayCatalogService overlays,
        IRulesetSelectionPolicy rulesetSelectionPolicy)
    {
        _overlays = overlays;
        _rulesetSelectionPolicy = rulesetSelectionPolicy;
    }

    public IReadOnlyList<RulePackRegistryEntry> List(OwnerScope owner, string? rulesetId = null)
    {
        string effectiveRulesetId = RulesetDefaults.NormalizeOptional(rulesetId)
            ?? _rulesetSelectionPolicy.GetDefaultRulesetId();

        return _overlays.GetCatalog().Overlays
            .Select(overlay => ToRegistryEntry(overlay, effectiveRulesetId))
            .ToArray();
    }

    public RulePackRegistryEntry? Get(OwnerScope owner, string packId, string? rulesetId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);

        return List(owner, rulesetId)
            .FirstOrDefault(entry => string.Equals(entry.Manifest.PackId, packId, StringComparison.Ordinal));
    }

    private static RulePackRegistryEntry ToRegistryEntry(ContentOverlayPack overlay, string rulesetId)
    {
        RulePackManifest manifest = overlay.ToRulePackManifest(rulesetId);
        RulePackPublicationMetadata publication = new(
            OwnerId: SystemOwnerId,
            Visibility: manifest.Visibility,
            PublicationStatus: RulePackPublicationStatuses.Published,
            Review: new RulePackReviewDecision(RulePackReviewStates.NotRequired),
            Shares: []);

        return new RulePackRegistryEntry(manifest, publication);
    }
}
