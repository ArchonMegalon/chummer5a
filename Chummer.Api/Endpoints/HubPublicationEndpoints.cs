using Chummer.Application.Hub;
using Chummer.Application.Owners;
using Chummer.Contracts.Hub;

namespace Chummer.Api.Endpoints;

public static class HubPublicationEndpoints
{
    public static IEndpointRouteBuilder MapHubPublicationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/hub/publish/drafts", (string? kind, string? ruleset, string? state, IHubPublicationService hubPublicationService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            if (ValidateProjectKindOptional(kind) is { } invalidKindResult)
            {
                return invalidKindResult;
            }

            return ToResult(hubPublicationService.ListDrafts(ownerContextAccessor.Current, kind, ruleset, state));
        });

        app.MapGet("/api/hub/publish/drafts/{draftId}", (string draftId, IHubPublicationService hubPublicationService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            HubDraftDetailProjection? detail = hubPublicationService.GetDraft(ownerContextAccessor.Current, draftId).Payload;
            return detail is null
                ? Results.NotFound(new
                {
                    error = "hub_publish_draft_not_found",
                    draftId
                })
                : Results.Ok(detail);
        });

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

            return ToValidatedResult(() => hubPublicationService.CreateDraft(ownerContextAccessor.Current, request));
        });

        app.MapPut("/api/hub/publish/drafts/{draftId}", (string draftId, HubUpdateDraftRequest? request, IHubPublicationService hubPublicationService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            if (request is null)
            {
                return Results.BadRequest(new
                {
                    error = "hub_publish_request_required",
                    operation = HubPublicationOperations.UpdateDraft
                });
            }

            HubPublishDraftReceipt? updated;
            try
            {
                updated = hubPublicationService.UpdateDraft(ownerContextAccessor.Current, draftId, request).Payload;
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new
                {
                    error = "hub_publish_invalid_request",
                    operation = HubPublicationOperations.UpdateDraft,
                    message = exception.Message
                });
            }

            return updated is null
                ? Results.NotFound(new
                {
                    error = "hub_publish_draft_not_found",
                    draftId
                })
                : Results.Ok(updated);
        });

        app.MapPost("/api/hub/publish/drafts/{draftId}/archive", (string draftId, IHubPublicationService hubPublicationService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            HubPublishDraftReceipt? archived = hubPublicationService.ArchiveDraft(ownerContextAccessor.Current, draftId).Payload;
            return archived is null
                ? Results.NotFound(new
                {
                    error = "hub_publish_draft_not_found",
                    draftId
                })
                : Results.Ok(archived);
        });

        app.MapDelete("/api/hub/publish/drafts/{draftId}", (string draftId, IHubPublicationService hubPublicationService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            bool deleted = hubPublicationService.DeleteDraft(ownerContextAccessor.Current, draftId).Payload;
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new
                {
                    error = "hub_publish_draft_not_found",
                    draftId
                });
        });

        app.MapPost("/api/hub/publish/{kind}/{itemId}/submit", (string kind, string itemId, string? ruleset, HubSubmitProjectRequest? request, IHubPublicationService hubPublicationService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            if (ValidateProjectKindRequired(kind) is { } invalidKindResult)
            {
                return invalidKindResult;
            }

            return ToValidatedResult(() => hubPublicationService.SubmitForReview(ownerContextAccessor.Current, kind, itemId, ruleset, request));
        });

        app.MapGet("/api/hub/moderation/queue", (string? state, IHubModerationService hubModerationService, IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(hubModerationService.ListQueue(ownerContextAccessor.Current, state)));

        app.MapPost("/api/hub/moderation/queue/{caseId}/approve", (string caseId, HubModerationDecisionRequest? request, IHubModerationService hubModerationService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            HubModerationDecisionReceipt? approved = hubModerationService.Approve(ownerContextAccessor.Current, caseId, request).Payload;
            return approved is null
                ? Results.NotFound(new
                {
                    error = "hub_moderation_case_not_found",
                    caseId
                })
                : Results.Ok(approved);
        });

        app.MapPost("/api/hub/moderation/queue/{caseId}/reject", (string caseId, HubModerationDecisionRequest? request, IHubModerationService hubModerationService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            HubModerationDecisionReceipt? rejected = hubModerationService.Reject(ownerContextAccessor.Current, caseId, request).Payload;
            return rejected is null
                ? Results.NotFound(new
                {
                    error = "hub_moderation_case_not_found",
                    caseId
                })
                : Results.Ok(rejected);
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
            ?? throw new InvalidOperationException("Hub publication result was not implemented but did not include a receipt.");
        return Results.Json(receipt, statusCode: StatusCodes.Status501NotImplemented);
    }

    private static IResult ToValidatedResult<T>(Func<HubPublicationResult<T>> factory)
    {
        try
        {
            return ToResult(factory());
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "hub_publish_invalid_request",
                message = exception.Message
            });
        }
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
