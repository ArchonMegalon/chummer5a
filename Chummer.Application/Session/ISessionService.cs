using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Session;

namespace Chummer.Application.Session;

public interface ISessionService
{
    SessionApiResult<SessionCharacterCatalog> ListCharacters(OwnerScope owner);

    SessionApiResult<SessionDashboardProjection> GetCharacterProjection(OwnerScope owner, string characterId);

    SessionApiResult<SessionOverlaySnapshot> ApplyCharacterPatches(OwnerScope owner, string characterId, SessionPatchRequest? request);

    SessionApiResult<SessionSyncReceipt> SyncCharacterLedger(OwnerScope owner, string characterId, SessionSyncBatch? batch);

    SessionApiResult<RulePackCatalog> ListRulePacks(OwnerScope owner);

    SessionApiResult<SessionOverlaySnapshot> UpdatePins(OwnerScope owner, SessionPinUpdateRequest? request);
}
