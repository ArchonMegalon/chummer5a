using Chummer.Application.Owners;
using Chummer.Contracts.Content;
using Chummer.Contracts.Session;
using Chummer.Infrastructure.Owners;
using Chummer.Presentation;

namespace Chummer.Desktop.Runtime;

public sealed class InProcessSessionClient : ISessionClient
{
    private readonly IOwnerContextAccessor _ownerContextAccessor;

    public InProcessSessionClient(IOwnerContextAccessor? ownerContextAccessor = null)
    {
        _ownerContextAccessor = ownerContextAccessor ?? new LocalOwnerContextAccessor();
    }

    public Task<SessionApiResult<SessionCharacterCatalog>> ListCharactersAsync(CancellationToken ct)
        => Task.FromResult(NotImplemented<SessionCharacterCatalog>(SessionApiOperations.ListCharacters));

    public Task<SessionApiResult<SessionDashboardProjection>> GetCharacterProjectionAsync(string characterId, CancellationToken ct)
        => Task.FromResult(NotImplemented<SessionDashboardProjection>(SessionApiOperations.GetCharacterProjection, characterId));

    public Task<SessionApiResult<SessionOverlaySnapshot>> ApplyCharacterPatchesAsync(string characterId, SessionPatchRequest request, CancellationToken ct)
        => Task.FromResult(NotImplemented<SessionOverlaySnapshot>(SessionApiOperations.ApplyCharacterPatches, characterId));

    public Task<SessionApiResult<SessionSyncReceipt>> SyncCharacterLedgerAsync(string characterId, SessionSyncBatch batch, CancellationToken ct)
        => Task.FromResult(NotImplemented<SessionSyncReceipt>(SessionApiOperations.SyncCharacterLedger, characterId));

    public Task<SessionApiResult<RulePackCatalog>> ListRulePacksAsync(CancellationToken ct)
        => Task.FromResult(NotImplemented<RulePackCatalog>(SessionApiOperations.ListRulePacks));

    public Task<SessionApiResult<SessionOverlaySnapshot>> UpdatePinsAsync(SessionPinUpdateRequest request, CancellationToken ct)
        => Task.FromResult(NotImplemented<SessionOverlaySnapshot>(SessionApiOperations.UpdatePins, request.BaseCharacterVersion.CharacterId));

    private SessionApiResult<T> NotImplemented<T>(string operation, string? characterId = null)
        => SessionApiResult<T>.FromNotImplemented(
            new SessionNotImplementedReceipt(
                Error: "session_not_implemented",
                Operation: operation,
                Message: "The dedicated session/mobile surface is not implemented yet.",
                CharacterId: characterId,
                OwnerId: _ownerContextAccessor.Current.NormalizedValue));
}
