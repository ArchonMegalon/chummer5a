using Chummer.Contracts.Content;
using Chummer.Contracts.Session;

namespace Chummer.Presentation;

public interface ISessionClient
{
    Task<SessionApiResult<SessionCharacterCatalog>> ListCharactersAsync(CancellationToken ct);

    Task<SessionApiResult<SessionDashboardProjection>> GetCharacterProjectionAsync(string characterId, CancellationToken ct);

    Task<SessionApiResult<SessionOverlaySnapshot>> ApplyCharacterPatchesAsync(string characterId, SessionPatchRequest request, CancellationToken ct);

    Task<SessionApiResult<SessionSyncReceipt>> SyncCharacterLedgerAsync(string characterId, SessionSyncBatch batch, CancellationToken ct);

    Task<SessionApiResult<RulePackCatalog>> ListRulePacksAsync(CancellationToken ct);

    Task<SessionApiResult<SessionOverlaySnapshot>> UpdatePinsAsync(SessionPinUpdateRequest request, CancellationToken ct);
}
