using Chummer.Contracts.Characters;

namespace Chummer.Contracts.Session;

public static class SessionApiOperations
{
    public const string ListCharacters = "list-characters";
    public const string GetCharacterProjection = "get-character-projection";
    public const string ApplyCharacterPatches = "apply-character-patches";
    public const string SyncCharacterLedger = "sync-character-ledger";
    public const string ListRulePacks = "list-rulepacks";
    public const string UpdatePins = "update-pins";
}

public sealed record SessionCharacterListItem(
    string CharacterId,
    string DisplayName,
    string RulesetId,
    string RuntimeFingerprint);

public sealed record SessionCharacterCatalog(
    IReadOnlyList<SessionCharacterListItem> Characters);

public sealed record SessionPatchRequest(
    string OverlayId,
    CharacterVersionReference BaseCharacterVersion,
    IReadOnlyList<SessionEvent> Events);

public sealed record SessionPinUpdateRequest(
    string OverlayId,
    CharacterVersionReference BaseCharacterVersion,
    IReadOnlyList<SessionQuickActionPin> Pins);

public sealed record SessionNotImplementedReceipt(
    string Error,
    string Operation,
    string Message,
    string? CharacterId = null,
    string? OwnerId = null);

public sealed record SessionApiResult<T>(
    T? Payload = default,
    SessionNotImplementedReceipt? NotImplemented = null)
{
    public bool IsImplemented => NotImplemented is null;

    public static SessionApiResult<T> Implemented(T payload)
        => new(payload, null);

    public static SessionApiResult<T> FromNotImplemented(SessionNotImplementedReceipt receipt)
        => new(default, receipt);
}
