using Chummer.Application.Owners;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Session;

namespace Chummer.Api.Endpoints;

public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/session/characters", (IOwnerContextAccessor ownerContextAccessor) =>
            NotImplemented(SessionApiOperations.ListCharacters, ownerContextAccessor.Current));

        app.MapGet("/api/session/characters/{characterId}", (string characterId, IOwnerContextAccessor ownerContextAccessor) =>
            NotImplemented(SessionApiOperations.GetCharacterProjection, ownerContextAccessor.Current, characterId));

        app.MapPost("/api/session/characters/{characterId}/patches", (string characterId, IOwnerContextAccessor ownerContextAccessor) =>
            NotImplemented(SessionApiOperations.ApplyCharacterPatches, ownerContextAccessor.Current, characterId));

        app.MapPost("/api/session/characters/{characterId}/sync", (string characterId, IOwnerContextAccessor ownerContextAccessor) =>
            NotImplemented(SessionApiOperations.SyncCharacterLedger, ownerContextAccessor.Current, characterId));

        app.MapGet("/api/session/rulepacks", (IOwnerContextAccessor ownerContextAccessor) =>
            NotImplemented(SessionApiOperations.ListRulePacks, ownerContextAccessor.Current));

        app.MapPost("/api/session/pins", (IOwnerContextAccessor ownerContextAccessor) =>
            NotImplemented(SessionApiOperations.UpdatePins, ownerContextAccessor.Current));

        return app;
    }

    private static IResult NotImplemented(string operation, OwnerScope owner, string? characterId = null)
    {
        return Results.Json(
            new SessionNotImplementedReceipt(
                Error: "session_not_implemented",
                Operation: operation,
                Message: "The dedicated session/mobile API seam exists, but this operation is not implemented yet.",
                CharacterId: string.IsNullOrWhiteSpace(characterId) ? null : characterId,
                OwnerId: owner.NormalizedValue),
            statusCode: StatusCodes.Status501NotImplemented);
    }
}
