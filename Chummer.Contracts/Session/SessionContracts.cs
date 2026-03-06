using Chummer.Contracts.Characters;

namespace Chummer.Contracts.Session;

public static class SessionEventTypes
{
    public const string TrackerIncrement = "tracker.increment";
    public const string TrackerDecrement = "tracker.decrement";
    public const string ResourceSpend = "resource.spend";
    public const string ResourceRestore = "resource.restore";
    public const string AmmoSpend = "ammo.spend";
    public const string AmmoReload = "ammo.reload";
    public const string EffectAdd = "effect.add";
    public const string EffectRemove = "effect.remove";
    public const string QuickActionPin = "quickaction.pin";
    public const string QuickActionUnpin = "quickaction.unpin";
    public const string NoteAppend = "note.append";
    public const string NoteReplace = "note.replace";
    public const string SelectionSet = "selection.set";
}

public static class SessionSyncStatuses
{
    public const string LocalOnly = "local-only";
    public const string PendingSync = "pending-sync";
    public const string Synced = "synced";
    public const string Replayed = "replayed";
    public const string Conflict = "conflict";
}

public sealed record SessionEvent(
    string EventId,
    string OverlayId,
    CharacterVersionReference BaseCharacterVersion,
    string DeviceId,
    string ActorId,
    long Sequence,
    string EventType,
    string PayloadJson,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? AppliedAtUtc = null,
    string? ParentEventId = null,
    string? SyncCursor = null);

public sealed record SessionLedger(
    string OverlayId,
    CharacterVersionReference BaseCharacterVersion,
    IReadOnlyList<SessionEvent> Events,
    string? BaselineSnapshotId = null,
    long NextSequence = 0);

public sealed record SessionTrackerDefinition(
    string TrackerId,
    string Label,
    int DefaultValue,
    int? MinimumValue,
    int? MaximumValue,
    IReadOnlyList<int> Thresholds);

public sealed record SessionTrackerValue(
    string TrackerId,
    string Label,
    int CurrentValue,
    int? MinimumValue,
    int? MaximumValue,
    string? ThresholdState = null);

public sealed record SessionEffectState(
    string EffectId,
    string Label,
    bool IsActive,
    string? SourceEventId = null);

public sealed record SessionQuickActionPin(
    string ActionId,
    string Label,
    string CapabilityId,
    bool IsPinned = true);

public sealed record SessionSyncState(
    string Status,
    int PendingEventCount,
    DateTimeOffset? LastSyncedAtUtc,
    bool WasReplayed = false,
    bool RuntimeFingerprintMismatch = false);

public sealed record SessionOverlaySnapshot(
    string OverlayId,
    CharacterVersionReference BaseCharacterVersion,
    IReadOnlyList<SessionTrackerValue> Trackers,
    IReadOnlyList<SessionEffectState> ActiveEffects,
    IReadOnlyList<SessionQuickActionPin> PinnedQuickActions,
    IReadOnlyList<string> Notes,
    SessionSyncState SyncState);

public sealed record SessionRuntimeBundle(
    string BundleId,
    CharacterVersionReference BaseCharacterVersion,
    string EngineApiVersion,
    DateTimeOffset SignedAtUtc,
    string Signature,
    IReadOnlyList<SessionQuickActionPin> QuickActions,
    IReadOnlyList<SessionTrackerDefinition> Trackers,
    IReadOnlyDictionary<string, string> ReducerBindings);
