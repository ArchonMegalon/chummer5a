using Chummer.Application.Hub;
using Chummer.Application.Owners;
using Chummer.Contracts.Hub;

namespace Chummer.Api.Endpoints;

public static class HubPublicationEndpoints
{
    public static IEndpointRouteBuilder MapHubPublicationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/hub/publish/drafts", (string? kind, string? ruleset, string? state, IHubPublicationService hubPublicationService, IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(hubPublicationService.ListDrafts(ownerContextAccessor.Current, kind, ruleset, state)));

        app.MapPost("/api/hub/publish/drafts", (HubPublishDraftRequest? request, IHubPublicationService hubPublicationService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            if (request is null)
            {
                return Results.BadRequest(new
                {
                    error = "hub_publish_request_required",
                    operation = HubPublicationOperations.CreateDraft
                });
            }

            return ToResult(hubPublicationService.CreateDraft(ownerContextAccessor.Current, request));
        });

        app.MapPost("/api/hub/publish/{kind}/{itemId}/submit", (string kind, string itemId, string? ruleset, HubSubmitProjectRequest? request, IHubPublicationService hubPublicationService, IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(hubPublicationService.SubmitForReview(ownerContextAccessor.Current, kind, itemId, ruleset, request)));

        app.MapGet("/api/hub/moderation/queue", (string? state, IHubModerationService hubModerationService, IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(hubModerationService.ListQueue(ownerContextAccessor.Current, state)));

        return app;
    }

    private static IResult ToResult<T>(HubPublicationResult<T> result)
    {
        if (result.IsImplemented)
        {
            return Results.Ok(result.Payload);
        }

        HubPublicationNotImplementedReceipt receipt = result.NotImplemented
            ?? throw new InvalidOperationException("Hub publication result was not implemented but did not include a receipt.");
        return Results.Json(receipt, statusCode: StatusCodes.Status501NotImplemented);
    }
}
