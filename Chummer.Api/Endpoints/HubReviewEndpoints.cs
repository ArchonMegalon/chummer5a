using Chummer.Application.Hub;
using Chummer.Application.Owners;
using Chummer.Contracts.Hub;

namespace Chummer.Api.Endpoints;

public static class HubReviewEndpoints
{
    public static IEndpointRouteBuilder MapHubReviewEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/hub/reviews", (string? kind, string? itemId, string? ruleset, IHubReviewService hubReviewService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            if (ValidateProjectKindOptional(kind) is { } invalidKindResult)
            {
                return invalidKindResult;
            }

            return ToResult(hubReviewService.ListReviews(ownerContextAccessor.Current, kind, itemId, ruleset));
        });

        app.MapPut("/api/hub/reviews/{kind}/{itemId}", (string kind, string itemId, HubUpsertReviewRequest? request, IHubReviewService hubReviewService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            if (ValidateProjectKindRequired(kind) is { } invalidKindResult)
            {
                return invalidKindResult;
            }

            if (request is null)
            {
                return Results.BadRequest(new
                {
                    error = "hub_review_request_required",
                    kind,
                    itemId
                });
            }

            return ToResult(hubReviewService.UpsertReview(ownerContextAccessor.Current, kind, itemId, request));
        });

        return app;
    }

    private static IResult ToResult<T>(HubPublicationResult<T> result)
    {
        if (result.IsImplemented)
        {
            return Results.Ok(result.Payload);
        }

        HubPublicationNotImplementedReceipt receipt = result.NotImplemented
            ?? throw new InvalidOperationException("Hub review result was not implemented but did not include a receipt.");
        return Results.Json(receipt, statusCode: StatusCodes.Status501NotImplemented);
    }

    private static IResult? ValidateProjectKindOptional(string? kind)
    {
        if (string.IsNullOrWhiteSpace(kind) || HubCatalogItemKinds.IsDefined(kind))
        {
            return null;
        }

        return CreateInvalidKindResult(kind);
    }

    private static IResult? ValidateProjectKindRequired(string kind)
        => HubCatalogItemKinds.IsDefined(kind)
            ? null
            : CreateInvalidKindResult(kind);

    private static IResult CreateInvalidKindResult(string? kind)
        => Results.BadRequest(new
        {
            error = "hub_project_kind_invalid",
            kind,
            allowedKinds = HubCatalogItemKinds.All
        });
}
