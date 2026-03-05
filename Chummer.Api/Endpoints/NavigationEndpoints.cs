using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;

namespace Chummer.Api.Endpoints;

public static class NavigationEndpoints
{
    public static IEndpointRouteBuilder MapNavigationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/navigation-tabs", (string? ruleset) =>
        {
            IReadOnlyList<NavigationTabDefinition> tabs = NavigationTabCatalog.ForRuleset(RulesetDefaults.Normalize(ruleset));
            return Results.Ok(new NavigationTabCatalogResponse(
                Count: tabs.Count,
                Tabs: tabs));
        });

        return app;
    }
}
