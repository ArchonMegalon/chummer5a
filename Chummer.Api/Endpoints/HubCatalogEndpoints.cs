using Chummer.Application.Hub;
using Chummer.Application.Owners;
using Chummer.Contracts.Hub;
using Chummer.Contracts.Presentation;

namespace Chummer.Api.Endpoints;

public static class HubCatalogEndpoints
{
    public static IEndpointRouteBuilder MapHubCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/hub/search", (BrowseQuery query, IHubCatalogService hubCatalogService, IOwnerContextAccessor ownerContextAccessor) =>
            Results.Ok(hubCatalogService.Search(ownerContextAccessor.Current, query)));

        app.MapGet("/api/hub/projects/{kind}/{itemId}", (string kind, string itemId, string? ruleset, IHubCatalogService hubCatalogService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            HubProjectDetailProjection? detail = hubCatalogService.GetProjectDetail(ownerContextAccessor.Current, kind, itemId, ruleset);
            return detail is null
                ? Results.NotFound(new
                {
                    error = "hub_project_not_found",
                    kind,
                    itemId
                })
                : Results.Ok(detail);
        });

        return app;
    }
}
