using Chummer.Contracts.Presentation;

namespace Chummer.Api.Endpoints;

public static class NavigationEndpoints
{
    public static IEndpointRouteBuilder MapNavigationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/navigation-tabs", () =>
        {
            IReadOnlyList<NavigationTabDefinition> tabs = NavigationTabCatalog.All;
            return Results.Ok(new NavigationTabCatalogResponse(
                Count: tabs.Count,
                Tabs: tabs));
        });

        return app;
    }
}
