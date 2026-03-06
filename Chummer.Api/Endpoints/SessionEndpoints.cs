using Chummer.Application.Session;
using Chummer.Application.Owners;
using Chummer.Contracts.Session;

namespace Chummer.Api.Endpoints;

public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/session/characters", (ISessionService sessionService, IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(sessionService.ListCharacters(ownerContextAccessor.Current)));

        app.MapGet("/api/session/characters/{characterId}", (string characterId, ISessionService sessionService, IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(sessionService.GetCharacterProjection(ownerContextAccessor.Current, characterId)));

        app.MapPost("/api/session/characters/{characterId}/patches", (string characterId, SessionPatchRequest? request, ISessionService sessionService, IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(sessionService.ApplyCharacterPatches(ownerContextAccessor.Current, characterId, request)));

        app.MapPost("/api/session/characters/{characterId}/sync", (string characterId, SessionSyncBatch? batch, ISessionService sessionService, IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(sessionService.SyncCharacterLedger(ownerContextAccessor.Current, characterId, batch)));

        app.MapGet("/api/session/profiles", (ISessionService sessionService, IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(sessionService.ListProfiles(ownerContextAccessor.Current)));

        app.MapGet("/api/session/characters/{characterId}/runtime-bundle", (string characterId, ISessionService sessionService, IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(sessionService.GetRuntimeBundle(ownerContextAccessor.Current, characterId)));

        app.MapPost("/api/session/characters/{characterId}/profile", (string characterId, SessionProfileSelectionRequest? request, ISessionService sessionService, IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(sessionService.SelectProfile(ownerContextAccessor.Current, characterId, request)));

        app.MapGet("/api/session/rulepacks", (ISessionService sessionService, IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(sessionService.ListRulePacks(ownerContextAccessor.Current)));

        app.MapPost("/api/session/pins", (SessionPinUpdateRequest? request, ISessionService sessionService, IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(sessionService.UpdatePins(ownerContextAccessor.Current, request)));

        return app;
    }

    private static IResult ToResult<T>(SessionApiResult<T> result)
    {
        if (result.IsImplemented)
        {
            return Results.Ok(result.Payload);
        }

        SessionNotImplementedReceipt receipt = result.NotImplemented
            ?? throw new InvalidOperationException("Session API result was not implemented but did not include a receipt.");
        return Results.Json(receipt, statusCode: StatusCodes.Status501NotImplemented);
    }
}
