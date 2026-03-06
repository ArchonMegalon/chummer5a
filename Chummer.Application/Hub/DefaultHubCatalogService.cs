using System.Linq;
using Chummer.Application.Content;
using Chummer.Contracts.Content;
using Chummer.Contracts.Hub;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;

namespace Chummer.Application.Hub;

public sealed class DefaultHubCatalogService : IHubCatalogService
{
    private readonly IRulesetPluginRegistry _rulesetPluginRegistry;
    private readonly IRulePackRegistryService _rulePackRegistryService;
    private readonly IRuleProfileRegistryService _ruleProfileRegistryService;
    private readonly IBuildKitRegistryService _buildKitRegistryService;
    private readonly IRuntimeLockRegistryService _runtimeLockRegistryService;

    public DefaultHubCatalogService(
        IRulesetPluginRegistry rulesetPluginRegistry,
        IRulePackRegistryService rulePackRegistryService,
        IRuleProfileRegistryService ruleProfileRegistryService,
        IBuildKitRegistryService buildKitRegistryService,
        IRuntimeLockRegistryService runtimeLockRegistryService)
    {
        _rulesetPluginRegistry = rulesetPluginRegistry;
        _rulePackRegistryService = rulePackRegistryService;
        _ruleProfileRegistryService = ruleProfileRegistryService;
        _buildKitRegistryService = buildKitRegistryService;
        _runtimeLockRegistryService = runtimeLockRegistryService;
    }

    public HubCatalogResultPage Search(OwnerScope owner, BrowseQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        HubCatalogItem[] allItems = EnumerateAllItems(owner).ToArray();
        HubCatalogItem[] filtered = allItems
            .Where(item => MatchesQueryText(item, query.QueryText))
            .Where(item => MatchesFacets(item, query.FacetSelections))
            .ToArray();
        HubCatalogItem[] sorted = Sort(filtered, query).ToArray();
        HubCatalogItem[] paged = sorted
            .Skip(Math.Max(0, query.Offset))
            .Take(Math.Max(1, query.Limit))
            .ToArray();

        return new HubCatalogResultPage(
            Query: query,
            Items: paged,
            Facets: BuildFacets(filtered, query),
            Sorts: BuildSorts(query),
            TotalCount: filtered.Length);
    }

    private IEnumerable<HubCatalogItem> EnumerateAllItems(OwnerScope owner)
    {
        foreach (IRulesetPlugin plugin in _rulesetPluginRegistry.All)
        {
            string rulesetId = plugin.Id.NormalizedValue;

            foreach (RulePackRegistryEntry entry in _rulePackRegistryService.List(owner, rulesetId))
            {
                yield return new HubCatalogItem(
                    ItemId: entry.Manifest.PackId,
                    Kind: HubCatalogItemKinds.RulePack,
                    Title: entry.Manifest.Title,
                    Description: entry.Manifest.Description,
                    RulesetId: rulesetId,
                    Visibility: entry.Publication.Visibility,
                    TrustTier: entry.Manifest.TrustTier,
                    LinkTarget: $"/hub/rulepacks/{entry.Manifest.PackId}",
                    Version: entry.Manifest.Version);
            }

            foreach (BuildKitRegistryEntry entry in _buildKitRegistryService.List(owner, rulesetId))
            {
                yield return new HubCatalogItem(
                    ItemId: entry.Manifest.BuildKitId,
                    Kind: HubCatalogItemKinds.BuildKit,
                    Title: entry.Manifest.Title,
                    Description: entry.Manifest.Description,
                    RulesetId: rulesetId,
                    Visibility: entry.Visibility,
                    TrustTier: entry.Manifest.TrustTier,
                    LinkTarget: $"/hub/buildkits/{entry.Manifest.BuildKitId}",
                    Version: entry.Manifest.Version);
            }
        }

        foreach (RuleProfileRegistryEntry entry in _ruleProfileRegistryService.List(owner))
        {
            yield return new HubCatalogItem(
                ItemId: entry.Manifest.ProfileId,
                Kind: HubCatalogItemKinds.RuleProfile,
                Title: entry.Manifest.Title,
                Description: entry.Manifest.Description,
                RulesetId: entry.Manifest.RulesetId,
                Visibility: entry.Publication.Visibility,
                TrustTier: entry.Publication.Visibility == ArtifactVisibilityModes.Public ? ArtifactTrustTiers.Curated : ArtifactTrustTiers.LocalOnly,
                LinkTarget: $"/hub/profiles/{entry.Manifest.ProfileId}",
                Version: entry.Manifest.RuntimeLock.RuntimeFingerprint);
        }

        foreach (RuntimeLockRegistryEntry entry in _runtimeLockRegistryService.List(owner).Entries)
        {
            yield return new HubCatalogItem(
                ItemId: entry.LockId,
                Kind: HubCatalogItemKinds.RuntimeLock,
                Title: entry.Title,
                Description: entry.Description ?? string.Empty,
                RulesetId: entry.RuntimeLock.RulesetId,
                Visibility: entry.Visibility,
                TrustTier: entry.Visibility == ArtifactVisibilityModes.Public ? ArtifactTrustTiers.Curated : ArtifactTrustTiers.LocalOnly,
                LinkTarget: $"/hub/runtime-locks/{entry.LockId}",
                Version: entry.RuntimeLock.RuntimeFingerprint,
                Installable: true);
        }
    }

    private static bool MatchesQueryText(HubCatalogItem item, string queryText)
    {
        if (string.IsNullOrWhiteSpace(queryText))
        {
            return true;
        }

        return item.Title.Contains(queryText, StringComparison.OrdinalIgnoreCase)
            || item.Description.Contains(queryText, StringComparison.OrdinalIgnoreCase)
            || item.ItemId.Contains(queryText, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesFacets(HubCatalogItem item, IReadOnlyDictionary<string, IReadOnlyList<string>> selections)
    {
        return MatchesFacet(item.Kind, selections, HubCatalogFacetIds.Kind)
            && MatchesFacet(item.RulesetId, selections, HubCatalogFacetIds.Ruleset)
            && MatchesFacet(item.Visibility, selections, HubCatalogFacetIds.Visibility)
            && MatchesFacet(item.TrustTier, selections, HubCatalogFacetIds.Trust);
    }

    private static bool MatchesFacet(string value, IReadOnlyDictionary<string, IReadOnlyList<string>> selections, string facetId)
    {
        if (!selections.TryGetValue(facetId, out IReadOnlyList<string>? selectedValues) || selectedValues.Count == 0)
        {
            return true;
        }

        return selectedValues.Contains(value, StringComparer.Ordinal);
    }

    private static IEnumerable<HubCatalogItem> Sort(IEnumerable<HubCatalogItem> items, BrowseQuery query)
    {
        Func<HubCatalogItem, string> keySelector = query.SortId switch
        {
            HubCatalogSortIds.Kind => item => item.Kind,
            HubCatalogSortIds.Ruleset => item => item.RulesetId,
            _ => item => item.Title
        };

        return string.Equals(query.SortDirection, BrowseSortDirections.Descending, StringComparison.Ordinal)
            ? items.OrderByDescending(keySelector, StringComparer.Ordinal)
            : items.OrderBy(keySelector, StringComparer.Ordinal);
    }

    private static IReadOnlyList<FacetDefinition> BuildFacets(IReadOnlyList<HubCatalogItem> filtered, BrowseQuery query)
    {
        return
        [
            BuildFacet(HubCatalogFacetIds.Kind, "Kind", filtered, query, item => item.Kind),
            BuildFacet(HubCatalogFacetIds.Ruleset, "Ruleset", filtered, query, item => item.RulesetId),
            BuildFacet(HubCatalogFacetIds.Visibility, "Visibility", filtered, query, item => item.Visibility),
            BuildFacet(HubCatalogFacetIds.Trust, "Trust", filtered, query, item => item.TrustTier)
        ];
    }

    private static FacetDefinition BuildFacet(
        string facetId,
        string label,
        IReadOnlyList<HubCatalogItem> items,
        BrowseQuery query,
        Func<HubCatalogItem, string> selector)
    {
        IReadOnlyList<string> selectedValues = query.FacetSelections.TryGetValue(facetId, out IReadOnlyList<string>? values)
            ? values
            : [];
        FacetOptionDefinition[] options = items
            .GroupBy(selector, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new FacetOptionDefinition(
                Value: group.Key,
                Label: group.Key,
                Count: group.Count(),
                Selected: selectedValues.Contains(group.Key, StringComparer.Ordinal)))
            .ToArray();

        return new FacetDefinition(
            FacetId: facetId,
            Label: label,
            Kind: BrowseFacetKinds.MultiSelect,
            Options: options,
            MultiSelect: true);
    }

    private static IReadOnlyList<SortDefinition> BuildSorts(BrowseQuery query)
    {
        return
        [
            new SortDefinition(HubCatalogSortIds.Title, "Title", BrowseSortDirections.Ascending, IsDefault: string.Equals(query.SortId, HubCatalogSortIds.Title, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(query.SortId)),
            new SortDefinition(HubCatalogSortIds.Kind, "Kind", BrowseSortDirections.Ascending, IsDefault: string.Equals(query.SortId, HubCatalogSortIds.Kind, StringComparison.Ordinal)),
            new SortDefinition(HubCatalogSortIds.Ruleset, "Ruleset", BrowseSortDirections.Ascending, IsDefault: string.Equals(query.SortId, HubCatalogSortIds.Ruleset, StringComparison.Ordinal))
        ];
    }
}
