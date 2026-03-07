using Chummer.Contracts.Presentation;

namespace Chummer.Contracts.Hub;

public static class HubCatalogItemKinds
{
    public const string RulePack = "rulepack";
    public const string RuleProfile = "ruleprofile";
    public const string BuildKit = "buildkit";
    public const string RuntimeLock = "runtime-lock";
}

public static class HubCatalogFacetIds
{
    public const string Kind = "kind";
    public const string Ruleset = "ruleset";
    public const string Visibility = "visibility";
    public const string Trust = "trust";
}

public static class HubCatalogSortIds
{
    public const string Title = "title";
    public const string Kind = "kind";
    public const string Ruleset = "ruleset";
}

public sealed record HubCatalogItem(
    string ItemId,
    string Kind,
    string Title,
    string Description,
    string RulesetId,
    string Visibility,
    string TrustTier,
    string LinkTarget,
    string? Version = null,
    bool Installable = true,
    string? InstallState = null,
    HubReviewSummary? OwnerReview = null,
    HubReviewAggregateSummary? AggregateReview = null);

public sealed record HubCatalogResultPage(
    BrowseQuery Query,
    IReadOnlyList<HubCatalogItem> Items,
    IReadOnlyList<FacetDefinition> Facets,
    IReadOnlyList<SortDefinition> Sorts,
    int TotalCount);
