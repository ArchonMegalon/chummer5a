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

    public HubProjectDetailProjection? GetProjectDetail(OwnerScope owner, string kind, string itemId, string? rulesetId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        return kind.Trim() switch
        {
            HubCatalogItemKinds.RulePack => GetRulePackDetail(owner, itemId, rulesetId),
            HubCatalogItemKinds.RuleProfile => GetRuleProfileDetail(owner, itemId, rulesetId),
            HubCatalogItemKinds.BuildKit => GetBuildKitDetail(owner, itemId, rulesetId),
            HubCatalogItemKinds.RuntimeLock => GetRuntimeLockDetail(owner, itemId, rulesetId),
            _ => null
        };
    }

    private IEnumerable<HubCatalogItem> EnumerateAllItems(OwnerScope owner)
    {
        foreach (IRulesetPlugin plugin in _rulesetPluginRegistry.All)
        {
            string rulesetId = plugin.Id.NormalizedValue;

            foreach (RulePackRegistryEntry entry in _rulePackRegistryService.List(owner, rulesetId))
            {
                yield return ToCatalogItem(rulesetId, entry);
            }

            foreach (BuildKitRegistryEntry entry in _buildKitRegistryService.List(owner, rulesetId))
            {
                yield return ToCatalogItem(rulesetId, entry);
            }
        }

        foreach (RuleProfileRegistryEntry entry in _ruleProfileRegistryService.List(owner))
        {
            yield return ToCatalogItem(entry);
        }

        foreach (RuntimeLockRegistryEntry entry in _runtimeLockRegistryService.List(owner).Entries)
        {
            yield return ToCatalogItem(entry);
        }
    }

    private HubProjectDetailProjection? GetRulePackDetail(OwnerScope owner, string itemId, string? rulesetId)
    {
        foreach (string candidateRulesetId in EnumerateRulesetIds(rulesetId))
        {
            RulePackRegistryEntry? entry = _rulePackRegistryService.Get(owner, itemId, candidateRulesetId);
            if (entry is null)
            {
                continue;
            }

            return new HubProjectDetailProjection(
                Summary: ToCatalogItem(candidateRulesetId, entry),
                OwnerId: entry.Publication.OwnerId,
                CatalogKind: null,
                PublicationStatus: entry.Publication.PublicationStatus,
                ReviewState: entry.Publication.Review.State,
                RuntimeFingerprint: null,
                Facts:
                [
                    new HubProjectDetailFact("install-state", "Install State", entry.Install.State),
                    new HubProjectDetailFact("engine-api", "Engine API", entry.Manifest.EngineApiVersion),
                    new HubProjectDetailFact("asset-count", "Assets", entry.Manifest.Assets.Count.ToString()),
                    new HubProjectDetailFact("capability-count", "Capabilities", entry.Manifest.Capabilities.Count.ToString()),
                    new HubProjectDetailFact("execution-policy-count", "Execution Policies", entry.Manifest.ExecutionPolicies.Count.ToString())
                ],
                Dependencies:
                [
                    .. entry.Manifest.DependsOn.Select(reference =>
                        new HubProjectDependency(HubProjectDependencyKinds.DependsOn, HubCatalogItemKinds.RulePack, reference.Id, reference.Version)),
                    .. entry.Manifest.ConflictsWith.Select(reference =>
                        new HubProjectDependency(HubProjectDependencyKinds.ConflictsWith, HubCatalogItemKinds.RulePack, reference.Id, reference.Version))
                ],
                Actions:
                [
                    new HubProjectAction("install-rulepack", "Install", HubProjectActionKinds.Install, LinkTarget: $"/hub/rulepacks/{entry.Manifest.PackId}/install"),
                    new HubProjectAction("open-rulepack", "Open Registry Entry", HubProjectActionKinds.OpenRegistry, LinkTarget: $"/hub/rulepacks/{entry.Manifest.PackId}")
                ]);
        }

        return null;
    }

    private HubProjectDetailProjection? GetRuleProfileDetail(OwnerScope owner, string itemId, string? rulesetId)
    {
        RuleProfileRegistryEntry? entry = _ruleProfileRegistryService.Get(owner, itemId, rulesetId);
        if (entry is null)
        {
            return null;
        }

        return new HubProjectDetailProjection(
            Summary: ToCatalogItem(entry),
            OwnerId: entry.Publication.OwnerId,
            CatalogKind: entry.Manifest.CatalogKind,
            PublicationStatus: entry.Publication.PublicationStatus,
            ReviewState: entry.Publication.Review.State,
            RuntimeFingerprint: entry.Manifest.RuntimeLock.RuntimeFingerprint,
            Facts:
            [
                new HubProjectDetailFact("install-state", "Install State", entry.Install.State),
                new HubProjectDetailFact("audience", "Audience", entry.Manifest.Audience),
                new HubProjectDetailFact("update-channel", "Update Channel", entry.Manifest.UpdateChannel),
                new HubProjectDetailFact("default-toggle-count", "Default Toggles", entry.Manifest.DefaultToggles.Count.ToString()),
                new HubProjectDetailFact("runtime-fingerprint", "Runtime Fingerprint", entry.Manifest.RuntimeLock.RuntimeFingerprint)
            ],
            Dependencies:
            [
                .. entry.Manifest.RulePacks.Select(selection =>
                    new HubProjectDependency(
                        HubProjectDependencyKinds.IncludesRulePack,
                        HubCatalogItemKinds.RulePack,
                        selection.RulePack.Id,
                        selection.RulePack.Version,
                        Notes: selection.Required ? "required" : "optional"))
            ],
            Actions:
            [
                new HubProjectAction("preview-profile-runtime", "Preview Runtime", HubProjectActionKinds.PreviewRuntime, LinkTarget: $"/api/profiles/{entry.Manifest.ProfileId}/preview"),
                new HubProjectAction("apply-profile", "Install & Apply", HubProjectActionKinds.Apply, LinkTarget: $"/hub/profiles/{entry.Manifest.ProfileId}/apply"),
                new HubProjectAction("inspect-profile-runtime", "Inspect Runtime", HubProjectActionKinds.InspectRuntime, LinkTarget: $"/hub/runtime/profiles/{entry.Manifest.ProfileId}")
            ]);
    }

    private HubProjectDetailProjection? GetBuildKitDetail(OwnerScope owner, string itemId, string? rulesetId)
    {
        foreach (string candidateRulesetId in EnumerateRulesetIds(rulesetId))
        {
            BuildKitRegistryEntry? entry = _buildKitRegistryService.Get(owner, itemId, candidateRulesetId);
            if (entry is null)
            {
                continue;
            }

            return new HubProjectDetailProjection(
                Summary: ToCatalogItem(candidateRulesetId, entry),
                OwnerId: entry.Owner.NormalizedValue,
                CatalogKind: null,
                PublicationStatus: entry.PublicationStatus,
                ReviewState: null,
                RuntimeFingerprint: null,
                Facts:
                [
                    new HubProjectDetailFact("prompt-count", "Prompts", entry.Manifest.Prompts.Count.ToString()),
                    new HubProjectDetailFact("action-count", "Actions", entry.Manifest.Actions.Count.ToString()),
                    new HubProjectDetailFact("runtime-requirement-count", "Runtime Requirements", entry.Manifest.RuntimeRequirements.Count.ToString())
                ],
                Dependencies:
                [
                    .. entry.Manifest.RuntimeRequirements.SelectMany(requirement => requirement.RequiredRulePacks.Select(reference =>
                        new HubProjectDependency(
                            HubProjectDependencyKinds.RequiresRulePack,
                            HubCatalogItemKinds.RulePack,
                            reference.Id,
                            reference.Version,
                            Notes: requirement.RulesetId))),
                    .. entry.Manifest.RuntimeRequirements.SelectMany(requirement => requirement.RequiredRuntimeFingerprints.Select(fingerprint =>
                        new HubProjectDependency(
                            HubProjectDependencyKinds.RequiresRuntimeFingerprint,
                            HubCatalogItemKinds.RuntimeLock,
                            fingerprint,
                            fingerprint,
                            Notes: requirement.RulesetId)))
                ],
                Actions:
                [
                    new HubProjectAction("apply-buildkit", "Apply BuildKit", HubProjectActionKinds.Apply, LinkTarget: $"/hub/buildkits/{entry.Manifest.BuildKitId}/apply"),
                    new HubProjectAction("open-buildkit", "Open Registry Entry", HubProjectActionKinds.OpenRegistry, LinkTarget: $"/hub/buildkits/{entry.Manifest.BuildKitId}")
                ]);
        }

        return null;
    }

    private HubProjectDetailProjection? GetRuntimeLockDetail(OwnerScope owner, string itemId, string? rulesetId)
    {
        RuntimeLockRegistryEntry? entry = _runtimeLockRegistryService.Get(owner, itemId, rulesetId);
        if (entry is null)
        {
            return null;
        }

        return new HubProjectDetailProjection(
            Summary: ToCatalogItem(entry),
            OwnerId: entry.Owner.NormalizedValue,
            CatalogKind: entry.CatalogKind,
            PublicationStatus: null,
            ReviewState: null,
            RuntimeFingerprint: entry.RuntimeLock.RuntimeFingerprint,
            Facts:
            [
                new HubProjectDetailFact("install-state", "Install State", entry.Install.State),
                new HubProjectDetailFact("engine-api", "Engine API", entry.RuntimeLock.EngineApiVersion),
                new HubProjectDetailFact("content-bundle-count", "Content Bundles", entry.RuntimeLock.ContentBundles.Count.ToString()),
                new HubProjectDetailFact("rulepack-count", "RulePacks", entry.RuntimeLock.RulePacks.Count.ToString()),
                new HubProjectDetailFact("provider-binding-count", "Provider Bindings", entry.RuntimeLock.ProviderBindings.Count.ToString())
            ],
            Dependencies:
            [
                .. entry.RuntimeLock.RulePacks.Select(reference =>
                    new HubProjectDependency(HubProjectDependencyKinds.IncludesRulePack, HubCatalogItemKinds.RulePack, reference.Id, reference.Version))
            ],
            Actions:
            [
                new HubProjectAction("install-runtime-lock", "Install Runtime Lock", HubProjectActionKinds.Install, LinkTarget: $"/hub/runtime-locks/{entry.LockId}/install"),
                new HubProjectAction("inspect-runtime-lock", "Inspect Runtime", HubProjectActionKinds.InspectRuntime, LinkTarget: $"/hub/runtime-locks/{entry.LockId}")
            ]);
    }

    private IEnumerable<string> EnumerateRulesetIds(string? rulesetId)
    {
        string? normalizedRulesetId = RulesetDefaults.NormalizeOptional(rulesetId);
        if (normalizedRulesetId is not null)
        {
            yield return normalizedRulesetId;
            yield break;
        }

        foreach (IRulesetPlugin plugin in _rulesetPluginRegistry.All)
        {
            yield return plugin.Id.NormalizedValue;
        }
    }

    private static HubCatalogItem ToCatalogItem(string rulesetId, RulePackRegistryEntry entry) => new(
        ItemId: entry.Manifest.PackId,
        Kind: HubCatalogItemKinds.RulePack,
        Title: entry.Manifest.Title,
        Description: entry.Manifest.Description,
        RulesetId: rulesetId,
        Visibility: entry.Publication.Visibility,
        TrustTier: entry.Manifest.TrustTier,
        LinkTarget: $"/hub/rulepacks/{entry.Manifest.PackId}",
        Version: entry.Manifest.Version,
        InstallState: entry.Install.State);

    private static HubCatalogItem ToCatalogItem(string rulesetId, BuildKitRegistryEntry entry) => new(
        ItemId: entry.Manifest.BuildKitId,
        Kind: HubCatalogItemKinds.BuildKit,
        Title: entry.Manifest.Title,
        Description: entry.Manifest.Description,
        RulesetId: rulesetId,
        Visibility: entry.Visibility,
        TrustTier: entry.Manifest.TrustTier,
        LinkTarget: $"/hub/buildkits/{entry.Manifest.BuildKitId}",
        Version: entry.Manifest.Version);

    private static HubCatalogItem ToCatalogItem(RuleProfileRegistryEntry entry) => new(
        ItemId: entry.Manifest.ProfileId,
        Kind: HubCatalogItemKinds.RuleProfile,
        Title: entry.Manifest.Title,
        Description: entry.Manifest.Description,
        RulesetId: entry.Manifest.RulesetId,
        Visibility: entry.Publication.Visibility,
        TrustTier: ResolveTrustTier(entry.Publication.Visibility),
        LinkTarget: $"/hub/profiles/{entry.Manifest.ProfileId}",
        Version: entry.Manifest.RuntimeLock.RuntimeFingerprint,
        InstallState: entry.Install.State);

    private static HubCatalogItem ToCatalogItem(RuntimeLockRegistryEntry entry) => new(
        ItemId: entry.LockId,
        Kind: HubCatalogItemKinds.RuntimeLock,
        Title: entry.Title,
        Description: entry.Description ?? string.Empty,
        RulesetId: entry.RuntimeLock.RulesetId,
        Visibility: entry.Visibility,
        TrustTier: ResolveTrustTier(entry.Visibility),
        LinkTarget: $"/hub/runtime-locks/{entry.LockId}",
        Version: entry.RuntimeLock.RuntimeFingerprint,
        Installable: true,
        InstallState: entry.Install.State);

    private static string ResolveTrustTier(string visibility) =>
        string.Equals(visibility, ArtifactVisibilityModes.Public, StringComparison.Ordinal)
            ? ArtifactTrustTiers.Curated
            : ArtifactTrustTiers.LocalOnly;

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
