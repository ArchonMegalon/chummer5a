using Chummer.Application.Hub;
using Chummer.Application.Owners;
using Chummer.Contracts.Content;
using Chummer.Contracts.Hub;
using Chummer.Contracts.Presentation;

namespace Chummer.Api.Endpoints;

public static class HubCatalogEndpoints
{
    public static IEndpointRouteBuilder MapHubCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/hub/search", (BrowseQuery query, IHubCatalogService hubCatalogService, IOwnerContextAccessor ownerContextAccessor) =>
            Results.Ok(hubCatalogService.Search(ownerContextAccessor.Current, query)))
            .AllowPublicApiKeyBypass();

        app.MapGet("/api/hub/projects/{kind}/{itemId}", (string kind, string itemId, string? ruleset, IHubCatalogService hubCatalogService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            if (ValidateProjectKind(kind) is { } invalidKindResult)
            {
                return invalidKindResult;
            }

            HubProjectDetailProjection? detail = hubCatalogService.GetProjectDetail(ownerContextAccessor.Current, kind, itemId, ruleset);
            return detail is null
                ? Results.NotFound(new
                {
                    error = "hub_project_not_found",
                    kind,
                    itemId
                })
                : Results.Ok(detail);
        }).AllowPublicApiKeyBypass();

        app.MapPost("/api/hub/projects/{kind}/{itemId}/install-preview", (string kind, string itemId, string? ruleset, RuleProfileApplyTarget target, IHubInstallPreviewService hubInstallPreviewService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            if (ValidateProjectKind(kind) is { } invalidKindResult)
            {
                return invalidKindResult;
            }

            HubProjectInstallPreviewReceipt? preview = hubInstallPreviewService.Preview(ownerContextAccessor.Current, kind, itemId, target, ruleset);
            return preview is null
                ? Results.NotFound(new
                {
                    error = "hub_project_not_found",
                    kind,
                    itemId
                })
                : Results.Ok(preview);
        }).AllowPublicApiKeyBypass();

        app.MapGet("/api/hub/projects/{kind}/{itemId}/compatibility", (string kind, string itemId, string? ruleset, IHubProjectCompatibilityService hubProjectCompatibilityService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            if (ValidateProjectKind(kind) is { } invalidKindResult)
            {
                return invalidKindResult;
            }

            HubProjectCompatibilityMatrix? matrix = hubProjectCompatibilityService.GetMatrix(ownerContextAccessor.Current, kind, itemId, ruleset);
            return matrix is null
                ? Results.NotFound(new
                {
                    error = "hub_project_not_found",
                    kind,
                    itemId
                })
                : Results.Ok(matrix);
        }).AllowPublicApiKeyBypass();

        return app;
    }

    private static IResult? ValidateProjectKind(string kind)
    {
        if (HubCatalogItemKinds.IsDefined(kind))
        {
            return null;
        }

        return Results.BadRequest(new
        {
            error = "hub_project_kind_invalid",
            kind,
            allowedKinds = HubCatalogItemKinds.All
        });
    }
}
