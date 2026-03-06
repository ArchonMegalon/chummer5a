using Chummer.Application.Owners;
using Chummer.Application.Session;
using Chummer.Contracts.Content;
using Chummer.Contracts.Session;
using Chummer.Infrastructure.Owners;
using Chummer.Presentation;

namespace Chummer.Desktop.Runtime;

public sealed class InProcessSessionClient : ISessionClient
{
    private readonly ISessionService _sessionService;
    private readonly IOwnerContextAccessor _ownerContextAccessor;

    public InProcessSessionClient(ISessionService? sessionService = null, IOwnerContextAccessor? ownerContextAccessor = null)
    {
        _sessionService = sessionService ?? new NotImplementedSessionService();
        _ownerContextAccessor = ownerContextAccessor ?? new LocalOwnerContextAccessor();
    }

    public Task<SessionApiResult<SessionCharacterCatalog>> ListCharactersAsync(CancellationToken ct)
        => Task.FromResult(_sessionService.ListCharacters(_ownerContextAccessor.Current));

    public Task<SessionApiResult<SessionDashboardProjection>> GetCharacterProjectionAsync(string characterId, CancellationToken ct)
        => Task.FromResult(_sessionService.GetCharacterProjection(_ownerContextAccessor.Current, characterId));

    public Task<SessionApiResult<SessionOverlaySnapshot>> ApplyCharacterPatchesAsync(string characterId, SessionPatchRequest request, CancellationToken ct)
        => Task.FromResult(_sessionService.ApplyCharacterPatches(_ownerContextAccessor.Current, characterId, request));

    public Task<SessionApiResult<SessionSyncReceipt>> SyncCharacterLedgerAsync(string characterId, SessionSyncBatch batch, CancellationToken ct)
        => Task.FromResult(_sessionService.SyncCharacterLedger(_ownerContextAccessor.Current, characterId, batch));

    public Task<SessionApiResult<RulePackCatalog>> ListRulePacksAsync(CancellationToken ct)
        => Task.FromResult(_sessionService.ListRulePacks(_ownerContextAccessor.Current));

    public Task<SessionApiResult<SessionOverlaySnapshot>> UpdatePinsAsync(SessionPinUpdateRequest request, CancellationToken ct)
        => Task.FromResult(_sessionService.UpdatePins(_ownerContextAccessor.Current, request));
}
