using Chummer.Application.Hub;
using Chummer.Application.Owners;
using Chummer.Contracts.Presentation;

namespace Chummer.Api.Endpoints;

public static class HubCatalogEndpoints
{
    public static IEndpointRouteBuilder MapHubCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/hub/search", (BrowseQuery query, IHubCatalogService hubCatalogService, IOwnerContextAccessor ownerContextAccessor) =>
            Results.Ok(hubCatalogService.Search(ownerContextAccessor.Current, query)));

        return app;
    }
}
