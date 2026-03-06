#nullable enable annotations

using Chummer.Application.Session;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Session;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class SessionServiceTests
{
    [TestMethod]
    public void Not_implemented_session_service_returns_owner_scoped_character_list_receipt()
    {
        NotImplementedSessionService service = new();

        SessionApiResult<SessionCharacterCatalog> result = service.ListCharacters(OwnerScope.LocalSingleUser);

        Assert.IsFalse(result.IsImplemented);
        Assert.IsNotNull(result.NotImplemented);
        Assert.AreEqual(SessionApiOperations.ListCharacters, result.NotImplemented.Operation);
        Assert.AreEqual(OwnerScope.LocalSingleUser.NormalizedValue, result.NotImplemented.OwnerId);
    }

    [TestMethod]
    public void Not_implemented_session_service_preserves_character_identity_on_sync_receipt()
    {
        NotImplementedSessionService service = new();
        SessionSyncBatch batch = new(
            OverlayId: "overlay-1",
            BaseCharacterVersion: new CharacterVersionReference("char-7", "ver-2", "sr5", "runtime-1"),
            Events: [],
            ClientCursor: "cursor-1");

        SessionApiResult<SessionSyncReceipt> result = service.SyncCharacterLedger(OwnerScope.LocalSingleUser, "char-7", batch);

        Assert.IsFalse(result.IsImplemented);
        Assert.IsNotNull(result.NotImplemented);
        Assert.AreEqual(SessionApiOperations.SyncCharacterLedger, result.NotImplemented.Operation);
        Assert.AreEqual("char-7", result.NotImplemented.CharacterId);
    }
}
