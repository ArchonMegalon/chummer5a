using System.Text.Json;
using Chummer.Contracts.AI;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Content;
using Chummer.Contracts.Session;
using Microsoft.AspNetCore.Components;

namespace Chummer.Session.Web.Components.Pages;

public partial class Home : ComponentBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string BrowserReplicaId = "browser-local";
    private const string BrowserActorId = "browser-user";
    private const string BrowserDeviceId = "browser-device";
    private const string NotesSemanticKey = "notes";
    private const string PinsSemanticKey = "pins";
    private const int CoachAuditCount = 3;

    private string _characterId = "char-1";
    private string _trackerSemanticKey = "stun";
    private string _noteText = "Marked local table note.";
    private string _pinActionId = "action.reload";
    private string _pinActionLabel = "Reload";
    private string _pinActionCapabilityId = "session.quick-actions";
    private SessionCharacterCatalog? _characterCatalog;
    private SessionProfileCatalog? _profileCatalog;
    private SessionRuntimeStatusProjection? _runtimeState;
    private SessionRuntimeBundleIssueReceipt? _bundleReceipt;
    private SessionRuntimeBundleRefreshReceipt? _bundleRefreshReceipt;
    private RulePackCatalog? _rulePackCatalog;
    private AiGatewayStatusProjection? _coachStatus;
    private IReadOnlyList<AiProviderHealthProjection> _coachProviderHealth = [];
    private IReadOnlyList<AiConversationAuditSummary> _coachAudits = [];
    private SessionNotImplementedReceipt? _lastNotImplemented;
    private CachedClientPayload<SessionCharacterCatalog>? _cachedCharacterCatalog;
    private CachedClientPayload<SessionProfileCatalog>? _cachedProfileCatalog;
    private CachedClientPayload<RulePackCatalog>? _cachedRulePackCatalog;
    private CachedClientPayload<SessionRuntimeStatusProjection>? _cachedRuntimeState;
    private CachedClientPayload<SessionRuntimeBundleIssueReceipt>? _cachedBundleReceipt;
    private CachedClientPayload<SessionLedger>? _cachedLedger;
    private CachedClientPayload<SessionReplicaState>? _cachedReplicaState;
    private ClientStorageQuotaEstimate? _storageQuota;
    private string? _statusMessage;
    private string? _errorMessage;
    private string? _coachErrorMessage;
    private string? _storageQuotaError;
    private bool _isBusy;
    private bool _loadedFromOfflineCache;

    [Inject]
    private BrowserSessionApiClient SessionApi { get; set; } = default!;

    [Inject]
    private BrowserSessionCoachApiClient SessionCoachApi { get; set; } = default!;

    [Inject]
    private ISessionOfflineCacheService SessionCache { get; set; } = default!;

    private string EffectiveCharacterId => _characterId.Trim();

    private string EffectiveOfflineOverlayId => CanQueryCharacter
        ? BuildOfflineOverlayId(EffectiveCharacterId)
        : string.Empty;

    private bool CanQueryCharacter => !string.IsNullOrWhiteSpace(EffectiveCharacterId);

    private string? EffectiveCoachRuntimeFingerprint => _runtimeState?.RuntimeFingerprint ?? SelectedCharacter?.RuntimeFingerprint;

    private string BuildCoachLaunchUri()
        => AiCoachLaunchQuery.BuildRelativeUri(
            "/coach/",
            new AiCoachLaunchContext(
                RouteType: AiRouteTypes.Coach,
                RuntimeFingerprint: EffectiveCoachRuntimeFingerprint,
                CharacterId: CanQueryCharacter ? EffectiveCharacterId : null,
                RulesetId: _runtimeState?.RulesetId ?? SelectedCharacter?.RulesetId));

    private string BuildCoachLaunchUri(AiConversationAuditSummary audit)
        => AiCoachLaunchQuery.BuildRelativeUri(
            "/coach/",
            new AiCoachLaunchContext(
                RouteType: audit.RouteType,
                ConversationId: audit.ConversationId,
                RuntimeFingerprint: audit.RuntimeFingerprint ?? EffectiveCoachRuntimeFingerprint,
                CharacterId: audit.CharacterId ?? (CanQueryCharacter ? EffectiveCharacterId : null),
                WorkspaceId: audit.WorkspaceId,
                RulesetId: _runtimeState?.RulesetId ?? SelectedCharacter?.RulesetId));

    private SessionCharacterListItem? SelectedCharacter => _characterCatalog?.Characters
        .FirstOrDefault(character => string.Equals(character.CharacterId, EffectiveCharacterId, StringComparison.Ordinal));

    private SessionRuntimeBundleIssueReceipt? VisibleBundleReceipt => _bundleReceipt ?? _cachedBundleReceipt?.Payload;

    private bool ShowingCachedBundleReceipt => _bundleReceipt is null && _cachedBundleReceipt is not null;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await ReloadAllAsync();
    }

    private Task ReloadAllAsync()
        => RunBusyAsync(
            "Loading session catalog, runtime state, and bundle metadata.",
            async () =>
            {
                _bundleReceipt = null;
                _bundleRefreshReceipt = null;

                await LoadCharactersCoreAsync();
                await LoadProfilesCoreAsync();
                await LoadRulePacksCoreAsync();

                if (CanQueryCharacter)
                {
                    await LoadRuntimeStateCoreAsync();
                    await LoadCachedBundleCoreAsync();
                    await LoadOfflineStateCoreAsync();
                }

                await LoadCoachSidecarCoreAsync();
                await LoadStorageQuotaCoreAsync();

                if (!_loadedFromOfflineCache)
                {
                    _statusMessage = "Session profile catalog, runtime state, and session-safe RulePack inventory are current.";
                }
            });

    private Task LoadCharactersAsync()
        => RunBusyAsync("Refreshing the session character catalog.", async () =>
        {
            await LoadCharactersCoreAsync();
            await LoadCoachSidecarCoreAsync();
            await LoadStorageQuotaCoreAsync();

            if (!_loadedFromOfflineCache)
            {
                _statusMessage = "Session character catalog refreshed.";
            }
        });

    private Task LoadProfilesAsync()
        => RunBusyAsync("Refreshing the session profile catalog.", async () =>
        {
            await LoadProfilesCoreAsync();
            await LoadStorageQuotaCoreAsync();

            if (!_loadedFromOfflineCache)
            {
                _statusMessage = "Session profile catalog refreshed.";
            }
        });

    private Task LoadRulePacksAsync()
        => RunBusyAsync("Refreshing the session-ready RulePack inventory.", async () =>
        {
            await LoadRulePacksCoreAsync();
            await LoadStorageQuotaCoreAsync();

            if (!_loadedFromOfflineCache)
            {
                _statusMessage = "Session-ready RulePack inventory refreshed.";
            }
        });

    private Task LoadRuntimeStateAsync()
        => RunBusyAsync("Loading session runtime state.", async () =>
        {
            await LoadRuntimeStateCoreAsync();
            await LoadCachedBundleCoreAsync();
            await LoadOfflineStateCoreAsync();
            await LoadCoachSidecarCoreAsync();
            await LoadStorageQuotaCoreAsync();

            if (!_loadedFromOfflineCache)
            {
                _statusMessage = $"Loaded runtime state for '{EffectiveCharacterId}'.";
            }
        });

    private Task LoadStorageQuotaAsync()
        => RunBusyAsync("Refreshing browser storage status.", async () =>
        {
            await LoadOfflineStateCoreAsync();
            await LoadStorageQuotaCoreAsync();
            _statusMessage = "Browser storage status refreshed.";
        });

    private Task LoadCoachSidecarAsync()
        => RunBusyAsync("Refreshing the Coach sidecar summary.", async () =>
        {
            await LoadCoachSidecarCoreAsync();
            _statusMessage = "Coach sidecar status and recent guidance refreshed.";
        });

    private Task QueueTrackerIncrementAsync()
        => QueueTrackerDeltaAsync(1);

    private Task QueueTrackerDecrementAsync()
        => QueueTrackerDeltaAsync(-1);

    private Task QueueNoteAsync()
        => RunBusyAsync("Queuing a local session note.", async () =>
        {
            string? note = NormalizeOptionalText(_noteText);
            if (note is null)
            {
                _errorMessage = "Enter a note before queueing it into the offline overlay.";
                return;
            }

            if (!await EnsureOfflineOverlayReadyAsync())
            {
                return;
            }

            SessionLedger ledger = _cachedLedger!.Payload;
            SessionReplicaState replicaState = _cachedReplicaState!.Payload;
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            SessionEvent sessionEvent = CreateLocalEvent(
                ledger,
                ledger.BaseCharacterVersion,
                SessionEventTypes.NoteAppend,
                JsonSerializer.Serialize(new LocalNotePayload(note), JsonOptions),
                timestamp);

            SessionLedger updatedLedger = AppendEvent(ledger, sessionEvent);
            SessionReplicaState updatedReplica = UpdateSequenceReplicaValue(replicaState, NotesSemanticKey, note, timestamp);

            _cachedLedger = await SessionCache.CacheLedgerAsync(updatedLedger, CancellationToken.None);
            _cachedReplicaState = await SessionCache.CacheReplicaStateAsync(updatedReplica, CancellationToken.None);
            _noteText = string.Empty;
            _statusMessage = $"Queued local note '{note}' for overlay '{updatedLedger.OverlayId}'.";

            await LoadStorageQuotaCoreAsync();
        });

    private Task QueuePinAsync()
        => QueuePinChangeAsync(pin: true);

    private Task QueueUnpinAsync()
        => QueuePinChangeAsync(pin: false);

    private Task ApplyProfileAsync(string profileId)
        => RunBusyAsync($"Selecting session profile '{profileId}'.", async () =>
        {
            if (!CanQueryCharacter)
            {
                _errorMessage = "Enter a character id before selecting a session profile.";
                return;
            }

            BrowserSessionApiCallResult<SessionProfileSelectionReceipt> result = await SessionApi.SelectProfileAsync(
                EffectiveCharacterId,
                new SessionProfileSelectionRequest(profileId));

            if (!TryCaptureResult(result, receipt =>
                {
                    _bundleReceipt = null;
                    _bundleRefreshReceipt = null;
                    _statusMessage = $"Profile '{receipt.ProfileId}' is now selected for '{receipt.CharacterId}'.";
                }))
            {
                return;
            }

            await LoadProfilesCoreAsync();
            await LoadRuntimeStateCoreAsync();
            await LoadCachedBundleCoreAsync();
            await LoadCoachSidecarCoreAsync();
            await LoadStorageQuotaCoreAsync();
        });

    private Task IssueRuntimeBundleAsync()
        => RunBusyAsync("Issuing a session runtime bundle.", async () =>
        {
            if (!CanQueryCharacter)
            {
                _errorMessage = "Enter a character id before requesting a runtime bundle.";
                return;
            }

            BrowserSessionApiCallResult<SessionRuntimeBundleIssueReceipt> result = await SessionApi.GetRuntimeBundleAsync(EffectiveCharacterId);
            if (!TryCaptureResult(result, receipt => _bundleReceipt = receipt))
            {
                CachedClientPayload<SessionRuntimeBundleIssueReceipt>? cachedBundle = await SessionCache.GetRuntimeBundleAsync(EffectiveCharacterId);
                if (cachedBundle is not null)
                {
                    _cachedBundleReceipt = cachedBundle;
                    _bundleReceipt = null;
                    _bundleRefreshReceipt = null;
                    ApplyCachedFallback(cachedBundle, "Runtime bundle request failed against the active API path.");
                }

                return;
            }

            SessionRuntimeBundleIssueReceipt issuedBundle = _bundleReceipt!;
            _bundleRefreshReceipt = null;
            _cachedBundleReceipt = await SessionCache.CacheRuntimeBundleAsync(EffectiveCharacterId, issuedBundle, CancellationToken.None);
            await EnsureOfflineOverlayStateAsync(issuedBundle);
            _statusMessage = $"Bundle '{issuedBundle.Bundle.BundleId}' was returned with outcome '{issuedBundle.Outcome}'.";
            await LoadRuntimeStateCoreAsync();
            await LoadCoachSidecarCoreAsync();
            await LoadStorageQuotaCoreAsync();
        });

    private Task RefreshRuntimeBundleAsync()
        => RunBusyAsync("Refreshing the current session runtime bundle.", async () =>
        {
            if (!CanQueryCharacter)
            {
                _errorMessage = "Enter a character id before refreshing a runtime bundle.";
                return;
            }

            BrowserSessionApiCallResult<SessionRuntimeBundleRefreshReceipt> result = await SessionApi.RefreshRuntimeBundleAsync(EffectiveCharacterId);
            if (!TryCaptureResult(result, receipt => _bundleRefreshReceipt = receipt))
            {
                await LoadCachedBundleCoreAsync();
                if (_cachedBundleReceipt is not null)
                {
                    ApplyCachedFallback(_cachedBundleReceipt, "Runtime bundle refresh failed against the active API path.");
                }

                return;
            }

            BrowserSessionApiCallResult<SessionRuntimeBundleIssueReceipt> currentBundle = await SessionApi.GetRuntimeBundleAsync(EffectiveCharacterId);
            if (currentBundle.IsImplemented && currentBundle.IsSuccess && currentBundle.Payload is not null)
            {
                _bundleReceipt = currentBundle.Payload;
                _cachedBundleReceipt = await SessionCache.CacheRuntimeBundleAsync(EffectiveCharacterId, currentBundle.Payload, CancellationToken.None);
                await EnsureOfflineOverlayStateAsync(currentBundle.Payload);
            }
            else
            {
                await LoadCachedBundleCoreAsync();
            }

            _statusMessage = $"Bundle refresh completed with outcome '{_bundleRefreshReceipt!.Outcome}'.";
            await LoadRuntimeStateCoreAsync();
            await LoadOfflineStateCoreAsync();
            await LoadCoachSidecarCoreAsync();
            await LoadStorageQuotaCoreAsync();
        });

    private async Task LoadCoachSidecarCoreAsync()
    {
        BrowserSessionCoachApiCallResult<AiGatewayStatusProjection> statusResult = await SessionCoachApi.GetStatusAsync();
        if (!TryCaptureCoachResult(statusResult, payload => _coachStatus = payload))
        {
            return;
        }

        BrowserSessionCoachApiCallResult<AiProviderHealthProjection[]> providerResult = await SessionCoachApi.ListProviderHealthAsync(AiRouteTypes.Coach);
        if (!TryCaptureCoachResult(providerResult, payload => _coachProviderHealth = payload))
        {
            return;
        }

        BrowserSessionCoachApiCallResult<AiConversationAuditCatalogPage> auditResult = await SessionCoachApi.ListConversationAuditsAsync(
            AiRouteTypes.Coach,
            CanQueryCharacter ? EffectiveCharacterId : null,
            EffectiveCoachRuntimeFingerprint,
            CoachAuditCount);
        TryCaptureCoachResult(auditResult, payload => _coachAudits = payload.Items);
    }

    private async Task LoadProfilesCoreAsync()
    {
        BrowserSessionApiCallResult<SessionProfileCatalog> result = await SessionApi.ListProfilesAsync();
        if (TryCaptureResult(result, payload => _profileCatalog = payload))
        {
            _cachedProfileCatalog = await SessionCache.CacheProfileCatalogAsync(_profileCatalog!, CancellationToken.None);
            return;
        }

        CachedClientPayload<SessionProfileCatalog>? cachedCatalog = await SessionCache.GetProfileCatalogAsync();
        if (cachedCatalog is null)
        {
            return;
        }

        _cachedProfileCatalog = cachedCatalog;
        _profileCatalog = cachedCatalog.Payload;
        ApplyCachedFallback(cachedCatalog, "Session profile catalog unavailable from the active API path.");
    }

    private async Task LoadCharactersCoreAsync()
    {
        BrowserSessionApiCallResult<SessionCharacterCatalog> result = await SessionApi.ListCharactersAsync();
        if (TryCaptureResult(result, payload =>
            {
                _characterCatalog = payload;
                ApplyCharacterSelection(payload);
            }))
        {
            _cachedCharacterCatalog = await SessionCache.CacheCharacterCatalogAsync(_characterCatalog!, CancellationToken.None);
            return;
        }

        CachedClientPayload<SessionCharacterCatalog>? cachedCatalog = await SessionCache.GetCharacterCatalogAsync();
        if (cachedCatalog is null)
        {
            return;
        }

        _cachedCharacterCatalog = cachedCatalog;
        _characterCatalog = cachedCatalog.Payload;
        ApplyCharacterSelection(cachedCatalog.Payload);
        ApplyCachedFallback(cachedCatalog, "Session character catalog unavailable from the active API path.");
    }

    private async Task LoadRulePacksCoreAsync()
    {
        BrowserSessionApiCallResult<RulePackCatalog> result = await SessionApi.ListRulePacksAsync();
        if (TryCaptureResult(result, payload => _rulePackCatalog = payload))
        {
            _cachedRulePackCatalog = await SessionCache.CacheRulePackCatalogAsync(_rulePackCatalog!, CancellationToken.None);
            return;
        }

        CachedClientPayload<RulePackCatalog>? cachedCatalog = await SessionCache.GetRulePackCatalogAsync();
        if (cachedCatalog is null)
        {
            return;
        }

        _cachedRulePackCatalog = cachedCatalog;
        _rulePackCatalog = cachedCatalog.Payload;
        ApplyCachedFallback(cachedCatalog, "Session-ready RulePack inventory unavailable from the active API path.");
    }

    private async Task LoadRuntimeStateCoreAsync()
    {
        BrowserSessionApiCallResult<SessionRuntimeStatusProjection> result = await SessionApi.GetRuntimeStateAsync(EffectiveCharacterId);
        if (TryCaptureResult(result, payload => _runtimeState = payload))
        {
            _cachedRuntimeState = await SessionCache.CacheRuntimeStateAsync(EffectiveCharacterId, _runtimeState!, CancellationToken.None);
            return;
        }

        CachedClientPayload<SessionRuntimeStatusProjection>? cachedRuntimeState = await SessionCache.GetRuntimeStateAsync(EffectiveCharacterId);
        if (cachedRuntimeState is null)
        {
            return;
        }

        _cachedRuntimeState = cachedRuntimeState;
        _runtimeState = cachedRuntimeState.Payload;
        ApplyCachedFallback(cachedRuntimeState, $"Runtime state for '{EffectiveCharacterId}' unavailable from the active API path.");
    }

    private Task LoadCachedBundleCoreAsync()
        => LoadCachedBundleCoreAsync(EffectiveCharacterId);

    private async Task LoadCachedBundleCoreAsync(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            _cachedBundleReceipt = null;
            return;
        }

        _cachedBundleReceipt = await SessionCache.GetRuntimeBundleAsync(characterId, CancellationToken.None);
    }

    private async Task LoadStorageQuotaCoreAsync()
    {
        try
        {
            _storageQuotaError = null;
            _storageQuota = await SessionCache.GetStorageQuotaAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _storageQuota = null;
            _storageQuotaError = ex.Message;
        }
    }

    private async Task LoadOfflineStateCoreAsync()
    {
        if (!CanQueryCharacter)
        {
            _cachedLedger = null;
            _cachedReplicaState = null;
            return;
        }

        _cachedLedger = await SessionCache.GetLedgerAsync(EffectiveOfflineOverlayId, CancellationToken.None);
        _cachedReplicaState = await SessionCache.GetReplicaStateAsync(EffectiveOfflineOverlayId, CancellationToken.None);
    }

    private async Task EnsureOfflineOverlayStateAsync(SessionRuntimeBundleIssueReceipt receipt)
    {
        string overlayId = BuildOfflineOverlayId(receipt.Bundle.BaseCharacterVersion.CharacterId);

        _cachedLedger = await SessionCache.GetLedgerAsync(overlayId, CancellationToken.None);
        if (_cachedLedger is null)
        {
            _cachedLedger = await SessionCache.CacheLedgerAsync(
                new SessionLedger(
                    OverlayId: overlayId,
                    BaseCharacterVersion: receipt.Bundle.BaseCharacterVersion,
                    Events: [],
                    BaselineSnapshotId: null,
                    NextSequence: 0),
                CancellationToken.None);
        }

        _cachedReplicaState = await SessionCache.GetReplicaStateAsync(overlayId, CancellationToken.None);
        if (_cachedReplicaState is null)
        {
            _cachedReplicaState = await SessionCache.CacheReplicaStateAsync(
                new SessionReplicaState(
                    OverlayId: overlayId,
                    BaseCharacterVersion: receipt.Bundle.BaseCharacterVersion,
                    RuntimeFingerprint: receipt.Bundle.BaseCharacterVersion.RuntimeFingerprint,
                    ReplicaId: "browser-local",
                    ClockSummary: [],
                    Values: [],
                    UpdatedAtUtc: DateTimeOffset.UtcNow,
                    PendingOperationCount: 0),
                CancellationToken.None);
        }
    }

    private Task QueueTrackerDeltaAsync(int delta)
        => RunBusyAsync(
            delta >= 0
                ? "Queuing a local tracker increment."
                : "Queuing a local tracker decrement.",
            async () =>
            {
                string? semanticKey = NormalizeOptionalText(_trackerSemanticKey);
                if (semanticKey is null)
                {
                    _errorMessage = "Enter a tracker key before queueing a local tracker event.";
                    return;
                }

                if (!await EnsureOfflineOverlayReadyAsync())
                {
                    return;
                }

                SessionLedger ledger = _cachedLedger!.Payload;
                SessionReplicaState replicaState = _cachedReplicaState!.Payload;
                DateTimeOffset timestamp = DateTimeOffset.UtcNow;
                SessionEvent sessionEvent = CreateLocalEvent(
                    ledger,
                    ledger.BaseCharacterVersion,
                    delta >= 0 ? SessionEventTypes.TrackerIncrement : SessionEventTypes.TrackerDecrement,
                    JsonSerializer.Serialize(new LocalTrackerPayload(semanticKey, delta), JsonOptions),
                    timestamp);

                SessionLedger updatedLedger = AppendEvent(ledger, sessionEvent);
                SessionReplicaState updatedReplica = UpdatePnCounterReplicaValue(replicaState, $"tracker:{semanticKey}", delta, timestamp);

                _cachedLedger = await SessionCache.CacheLedgerAsync(updatedLedger, CancellationToken.None);
                _cachedReplicaState = await SessionCache.CacheReplicaStateAsync(updatedReplica, CancellationToken.None);
                _statusMessage = $"Queued local tracker delta {delta:+#;-#;0} for '{semanticKey}'.";

                await LoadStorageQuotaCoreAsync();
            });

    private Task QueuePinChangeAsync(bool pin)
        => RunBusyAsync(
            pin
                ? "Queuing a local quick-action pin."
                : "Queuing a local quick-action unpin.",
            async () =>
            {
                string? actionId = NormalizeOptionalText(_pinActionId);
                string? label = NormalizeOptionalText(_pinActionLabel);
                string? capabilityId = NormalizeOptionalText(_pinActionCapabilityId);
                if (actionId is null || label is null || capabilityId is null)
                {
                    _errorMessage = "Action id, label, and capability are required before queueing a local pin change.";
                    return;
                }

                if (!await EnsureOfflineOverlayReadyAsync())
                {
                    return;
                }

                SessionLedger ledger = _cachedLedger!.Payload;
                SessionReplicaState replicaState = _cachedReplicaState!.Payload;
                DateTimeOffset timestamp = DateTimeOffset.UtcNow;
                SessionQuickActionPin quickActionPin = new(actionId, label, capabilityId, IsPinned: pin);
                SessionEvent sessionEvent = CreateLocalEvent(
                    ledger,
                    ledger.BaseCharacterVersion,
                    pin ? SessionEventTypes.QuickActionPin : SessionEventTypes.QuickActionUnpin,
                    JsonSerializer.Serialize(quickActionPin, JsonOptions),
                    timestamp);

                SessionLedger updatedLedger = AppendEvent(ledger, sessionEvent);
                SessionReplicaState updatedReplica = UpdatePinsReplicaValue(replicaState, quickActionPin, pin, timestamp);

                _cachedLedger = await SessionCache.CacheLedgerAsync(updatedLedger, CancellationToken.None);
                _cachedReplicaState = await SessionCache.CacheReplicaStateAsync(updatedReplica, CancellationToken.None);
                _statusMessage = pin
                    ? $"Queued local pin for '{actionId}'."
                    : $"Queued local unpin for '{actionId}'.";

                await LoadStorageQuotaCoreAsync();
            });

    private async Task<bool> EnsureOfflineOverlayReadyAsync()
    {
        if (!CanQueryCharacter)
        {
            _errorMessage = "Enter a character id before queueing local session mutations.";
            return false;
        }

        SessionRuntimeBundleIssueReceipt? receipt = VisibleBundleReceipt;
        if (receipt is null)
        {
            await LoadCachedBundleCoreAsync();
            receipt = VisibleBundleReceipt;
        }

        if (receipt is null)
        {
            _errorMessage = "Issue a runtime bundle before queueing offline session mutations.";
            return false;
        }

        await EnsureOfflineOverlayStateAsync(receipt);
        return _cachedLedger is not null && _cachedReplicaState is not null;
    }

    private void ApplyCharacterSelection(SessionCharacterCatalog catalog)
    {
        if (catalog.Characters.Count == 0)
        {
            return;
        }

        bool hasCurrentCharacter = catalog.Characters.Any(character =>
            string.Equals(character.CharacterId, EffectiveCharacterId, StringComparison.Ordinal));
        if (!hasCurrentCharacter)
        {
            _characterId = catalog.Characters[0].CharacterId;
        }
    }

    private void ApplyCachedFallback<T>(CachedClientPayload<T> cachedPayload, string context)
    {
        _loadedFromOfflineCache = true;
        _lastNotImplemented = null;
        _errorMessage = null;
        _statusMessage = $"{context} Showing IndexedDB cache from {FormatTimestamp(cachedPayload.CachedAtUtc)}.";
    }

    private bool TryCaptureResult<T>(BrowserSessionApiCallResult<T> result, Action<T> apply)
    {
        _lastNotImplemented = null;
        _errorMessage = null;

        if (!result.IsImplemented)
        {
            _lastNotImplemented = result.NotImplemented;
            _statusMessage = result.NotImplemented?.Message;
            return false;
        }

        if (!result.IsSuccess)
        {
            _errorMessage = result.ErrorMessage ?? $"Session request failed with HTTP {result.StatusCode}.";
            return false;
        }

        if (result.Payload is null)
        {
            _errorMessage = $"Session request returned HTTP {result.StatusCode} without a payload.";
            return false;
        }

        apply(result.Payload);
        return true;
    }

    private bool TryCaptureCoachResult<T>(BrowserSessionCoachApiCallResult<T> result, Action<T> apply)
    {
        _coachErrorMessage = null;

        if (!result.IsImplemented)
        {
            _coachErrorMessage = result.NotImplemented?.Message ?? "Coach sidecar route is not implemented yet.";
            return false;
        }

        if (result.QuotaExceeded is not null)
        {
            _coachErrorMessage = result.QuotaExceeded.Message;
            return false;
        }

        if (!result.IsSuccess)
        {
            _coachErrorMessage = result.ErrorMessage ?? $"Coach request failed with HTTP {result.StatusCode}.";
            return false;
        }

        if (result.Payload is null)
        {
            _coachErrorMessage = $"Coach request returned HTTP {result.StatusCode} without a payload.";
            return false;
        }

        apply(result.Payload);
        return true;
    }

    private async Task RunBusyAsync(string busyMessage, Func<Task> operation)
    {
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;
        _loadedFromOfflineCache = false;
        _errorMessage = null;
        _lastNotImplemented = null;
        _statusMessage = busyMessage;
        StateHasChanged();

        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _isBusy = false;
            StateHasChanged();
        }
    }

    private string GetStatusChipClass()
    {
        if (_isBusy)
        {
            return "status-chip busy";
        }

        if (!string.IsNullOrWhiteSpace(_errorMessage))
        {
            return "status-chip error";
        }

        if (_lastNotImplemented is not null)
        {
            return "status-chip warn";
        }

        return "status-chip ok";
    }

    private string GetStatusChipText()
    {
        if (_isBusy)
        {
            return "syncing";
        }

        if (!string.IsNullOrWhiteSpace(_errorMessage))
        {
            return "error";
        }

        if (_lastNotImplemented is not null)
        {
            return "partial";
        }

        return "ready";
    }

    private static string FormatBool(bool value) => value ? "yes" : "no";

    private static string FormatString(string? value) => string.IsNullOrWhiteSpace(value) ? "n/a" : value;

    private static string FormatTimestamp(DateTimeOffset? value)
        => value?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "n/a";

    private static string FormatBytes(long? value)
    {
        if (!value.HasValue)
        {
            return "n/a";
        }

        double size = value.Value;
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        int unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.#} {units[unitIndex]}";
    }

    private static string FormatCacheTimestamp<T>(CachedClientPayload<T>? payload)
        => payload is null ? "n/a" : FormatTimestamp(payload.CachedAtUtc);

    private static string FormatStorageBackends(ClientStorageQuotaEstimate? estimate)
    {
        if (estimate is null)
        {
            return "n/a";
        }

        List<string> backends = [];
        if (estimate.IndexedDbAvailable)
        {
            backends.Add("IndexedDB");
        }

        if (estimate.OpfsAvailable)
        {
            backends.Add("OPFS");
        }

        return backends.Count == 0 ? "none" : string.Join(", ", backends);
    }

    private static string FormatPersistence(ClientStorageQuotaEstimate? estimate)
    {
        if (estimate is null)
        {
            return "n/a";
        }

        if (!estimate.PersistenceSupported)
        {
            return "best-effort";
        }

        return estimate.IsPersistent ? "persistent" : "best-effort";
    }

    private static string FormatQuotaUsage(ClientStorageQuotaEstimate? estimate)
    {
        if (estimate is null
            || !estimate.UsageBytes.HasValue
            || !estimate.QuotaBytes.HasValue
            || estimate.QuotaBytes.Value <= 0)
        {
            return "n/a";
        }

        double ratio = estimate.UsageBytes.Value / (double)estimate.QuotaBytes.Value;
        return $"{ratio * 100:0.#}%";
    }

    private static string BuildOfflineOverlayId(string characterId)
        => $"offline:{characterId.Trim()}";

    private AiRouteBudgetStatusProjection? GetCoachRouteBudgetStatus()
        => _coachStatus?.RouteBudgetStatuses?.FirstOrDefault(status => string.Equals(status.RouteType, AiRouteTypes.Coach, StringComparison.Ordinal));

    private IReadOnlyList<AiProviderHealthProjection> GetCoachProviders()
        => _coachProviderHealth
            .Where(provider => provider.AllowedRouteTypes.Contains(AiRouteTypes.Coach))
            .ToArray();

    private string DescribeCoachProviderTransport(AiProviderHealthProjection provider)
    {
        string readiness = provider.TransportMetadataConfigured
            ? "ready"
            : provider.TransportBaseUrlConfigured || provider.TransportModelConfigured
                ? "partial"
                : "missing";
        return $"{readiness} · base {(provider.TransportBaseUrlConfigured ? "yes" : "no")} · model {(provider.TransportModelConfigured ? "yes" : "no")}";
    }

    private string DescribeCoachProviderKeys(AiProviderHealthProjection provider)
        => $"primary {provider.PrimaryCredentialCount} / fallback {provider.FallbackCredentialCount}";

    private static string DescribeCoachProviderBinding(AiProviderHealthProjection provider)
    {
        string route = string.IsNullOrWhiteSpace(provider.LastRouteType)
            ? "route n/a"
            : $"route {provider.LastRouteType}";
        string binding = string.IsNullOrWhiteSpace(provider.LastCredentialTier)
            ? "binding none"
            : provider.LastCredentialSlotIndex is int slotIndex
                ? $"binding {provider.LastCredentialTier} / slot {slotIndex}"
                : $"binding {provider.LastCredentialTier}";
        return $"{route} · {binding}";
    }

    private static string GetCoachProviderHealthBadgeClass(AiProviderHealthProjection provider)
        => provider.CircuitState switch
        {
            AiProviderCircuitStates.Open => "badge error",
            AiProviderCircuitStates.Degraded => "badge warn",
            AiProviderCircuitStates.Closed => "badge good",
            _ => "badge"
        };

    private static string GetCoachCacheBadgeClass(AiCacheMetadata? cache)
        => cache?.Status switch
        {
            AiCacheStatuses.Hit => "badge good",
            AiCacheStatuses.Miss => "badge warn",
            _ => "badge"
        };

    private static string FormatCoachCacheStatus(AiCacheMetadata? cache)
        => cache?.Status switch
        {
            AiCacheStatuses.Hit => "cache hit",
            AiCacheStatuses.Miss => "cache miss",
            _ => "cache none"
        };

    private static string DescribeCoachCoverage(AiGroundingCoverage? coverage)
        => coverage is null
            ? "none"
            : $"{coverage.ScorePercent}% · {coverage.Summary}";

    private static string DescribeCoachBudgetSnapshot(AiBudgetSnapshot? budget)
        => budget is null
            ? "n/a"
            : $"{budget.MonthlyConsumed} / {budget.MonthlyAllowance} {budget.BudgetUnit} · burst {budget.CurrentBurstConsumed} / {budget.BurstLimitPerMinute}";

    private static string DescribeCoachRecommendationSummary(AiStructuredAnswer? structuredAnswer)
    {
        if (structuredAnswer is null || structuredAnswer.Recommendations.Count == 0)
        {
            return "none";
        }

        AiRecommendation top = structuredAnswer.Recommendations[0];
        return $"{structuredAnswer.Recommendations.Count} · {top.Title}";
    }

    private static string DescribeCoachEvidenceSummary(AiStructuredAnswer? structuredAnswer)
    {
        if (structuredAnswer is null || structuredAnswer.Evidence.Count == 0)
        {
            return "none";
        }

        AiEvidenceEntry lead = structuredAnswer.Evidence[0];
        return $"{structuredAnswer.Evidence.Count} · {lead.Title}";
    }

    private static string DescribeCoachRiskSummary(AiStructuredAnswer? structuredAnswer)
    {
        if (structuredAnswer is null || structuredAnswer.Risks.Count == 0)
        {
            return "none";
        }

        AiRiskEntry lead = structuredAnswer.Risks[0];
        return $"{structuredAnswer.Risks.Count} · {lead.Title}";
    }

    private static string DescribeCoachSourceSummary(AiStructuredAnswer? structuredAnswer)
        => structuredAnswer is null
            ? "0 sources / 0 action drafts"
            : $"{structuredAnswer.Sources.Count} sources / {structuredAnswer.ActionDrafts.Count} action drafts";

    private static string DescribeCoachRouteDecision(AiProviderRouteDecision? routeDecision, string providerId)
    {
        if (routeDecision is null)
        {
            return providerId;
        }

        string binding = DescribeCoachCredentialBinding(routeDecision);
        return string.Equals(binding, "none", StringComparison.Ordinal)
            ? $"{routeDecision.ProviderId} · {routeDecision.Reason}"
            : $"{routeDecision.ProviderId} · {routeDecision.Reason} · {binding}";
    }

    private static string DescribeCoachCredentialBinding(AiProviderRouteDecision routeDecision)
    {
        if (string.IsNullOrWhiteSpace(routeDecision.CredentialTier)
            || string.Equals(routeDecision.CredentialTier, AiProviderCredentialTiers.None, StringComparison.Ordinal))
        {
            return "none";
        }

        return routeDecision.CredentialSlotIndex is int slotIndex
            ? $"{routeDecision.CredentialTier} / slot {slotIndex}"
            : routeDecision.CredentialTier;
    }

    private static string FormatEventCount(CachedClientPayload<SessionLedger>? payload)
        => payload is null ? "n/a" : payload.Payload.Events.Count.ToString();

    private static string FormatPendingOperationCount(CachedClientPayload<SessionReplicaState>? payload)
        => payload is null ? "n/a" : payload.Payload.PendingOperationCount.ToString();

    private static IReadOnlyList<SessionEvent> GetRecentEvents(CachedClientPayload<SessionLedger>? payload)
        => payload?.Payload.Events
            .OrderByDescending(item => item.Sequence)
            .Take(5)
            .ToArray()
        ?? [];

    private static IReadOnlyList<SessionReplicaValue> GetRecentReplicaValues(CachedClientPayload<SessionReplicaState>? payload)
        => payload?.Payload.Values
            .Take(5)
            .ToArray()
        ?? [];

    private static string FormatPayloadPreview(string payloadJson)
        => payloadJson.Length <= 96 ? payloadJson : $"{payloadJson[..93]}...";

    private static string FormatEventSequence(long sequence)
        => sequence.ToString("D4");

    private static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private static SessionEvent CreateLocalEvent(
        SessionLedger ledger,
        CharacterVersionReference baseCharacterVersion,
        string eventType,
        string payloadJson,
        DateTimeOffset timestamp)
    {
        long nextSequence = ResolveNextSequence(ledger);

        return new SessionEvent(
            EventId: $"evt-{nextSequence:D4}",
            OverlayId: ledger.OverlayId,
            BaseCharacterVersion: baseCharacterVersion,
            DeviceId: BrowserDeviceId,
            ActorId: BrowserActorId,
            Sequence: nextSequence,
            EventType: eventType,
            PayloadJson: payloadJson,
            CreatedAtUtc: timestamp);
    }

    private static SessionLedger AppendEvent(SessionLedger ledger, SessionEvent sessionEvent)
    {
        SessionEvent[] events = [.. ledger.Events, sessionEvent];

        return new SessionLedger(
            OverlayId: ledger.OverlayId,
            BaseCharacterVersion: ledger.BaseCharacterVersion,
            Events: events,
            BaselineSnapshotId: ledger.BaselineSnapshotId,
            NextSequence: sessionEvent.Sequence);
    }

    private static SessionReplicaState UpdatePnCounterReplicaValue(
        SessionReplicaState replicaState,
        string semanticKey,
        int delta,
        DateTimeOffset timestamp)
    {
        int currentValue = ResolveCounterValue(replicaState.Values, semanticKey);
        SessionReplicaValue value = new(
            SemanticKey: semanticKey,
            ValueKind: SessionReplicaValueKinds.PnCounter,
            PayloadJson: JsonSerializer.Serialize(new LocalCounterPayload(currentValue + delta), JsonOptions));

        return UpdateReplicaState(replicaState, value, timestamp);
    }

    private static SessionReplicaState UpdateSequenceReplicaValue(
        SessionReplicaState replicaState,
        string semanticKey,
        string entry,
        DateTimeOffset timestamp)
    {
        List<string> items = ResolveSequenceEntries(replicaState.Values, semanticKey);
        items.Add(entry);

        SessionReplicaValue value = new(
            SemanticKey: semanticKey,
            ValueKind: SessionReplicaValueKinds.Sequence,
            PayloadJson: JsonSerializer.Serialize(new LocalSequencePayload(items), JsonOptions));

        return UpdateReplicaState(replicaState, value, timestamp);
    }

    private static SessionReplicaState UpdatePinsReplicaValue(
        SessionReplicaState replicaState,
        SessionQuickActionPin pin,
        bool isPinned,
        DateTimeOffset timestamp)
    {
        List<SessionQuickActionPin> pins = ResolvePins(replicaState.Values, PinsSemanticKey);
        pins.RemoveAll(existing => string.Equals(existing.ActionId, pin.ActionId, StringComparison.Ordinal));
        if (isPinned)
        {
            pins.Add(pin with { IsPinned = true });
        }

        SessionReplicaValue value = new(
            SemanticKey: PinsSemanticKey,
            ValueKind: SessionReplicaValueKinds.ObservedRemoveSet,
            PayloadJson: JsonSerializer.Serialize(new LocalPinSetPayload(pins), JsonOptions));

        return UpdateReplicaState(replicaState, value, timestamp);
    }

    private static SessionReplicaState UpdateReplicaState(
        SessionReplicaState replicaState,
        SessionReplicaValue updatedValue,
        DateTimeOffset timestamp)
    {
        List<SessionReplicaValue> values = replicaState.Values.ToList();
        int existingIndex = values.FindIndex(value => string.Equals(value.SemanticKey, updatedValue.SemanticKey, StringComparison.Ordinal));
        if (existingIndex >= 0)
        {
            values[existingIndex] = updatedValue;
        }
        else
        {
            values.Add(updatedValue);
        }

        SessionReplicaClock[] clocks = IncrementClock(replicaState.ClockSummary, timestamp);

        return new SessionReplicaState(
            OverlayId: replicaState.OverlayId,
            BaseCharacterVersion: replicaState.BaseCharacterVersion,
            RuntimeFingerprint: replicaState.RuntimeFingerprint,
            ReplicaId: replicaState.ReplicaId,
            ClockSummary: clocks,
            Values: values,
            UpdatedAtUtc: timestamp,
            PendingOperationCount: replicaState.PendingOperationCount + 1);
    }

    private static SessionReplicaClock[] IncrementClock(IReadOnlyList<SessionReplicaClock> clocks, DateTimeOffset timestamp)
    {
        List<SessionReplicaClock> updated = clocks.ToList();
        int existingIndex = updated.FindIndex(clock => string.Equals(clock.ReplicaId, BrowserReplicaId, StringComparison.Ordinal));
        if (existingIndex >= 0)
        {
            SessionReplicaClock existing = updated[existingIndex];
            updated[existingIndex] = existing with
            {
                LogicalClock = existing.LogicalClock + 1,
                UpdatedAtUtc = timestamp
            };
        }
        else
        {
            updated.Add(new SessionReplicaClock(BrowserReplicaId, 1, timestamp));
        }

        return [.. updated];
    }

    private static long ResolveNextSequence(SessionLedger ledger)
    {
        long lastSequence = ledger.Events.Count == 0
            ? 0
            : ledger.Events.Max(item => item.Sequence);
        return Math.Max(ledger.NextSequence, lastSequence) + 1;
    }

    private static int ResolveCounterValue(IReadOnlyList<SessionReplicaValue> values, string semanticKey)
    {
        SessionReplicaValue? existing = values.FirstOrDefault(value =>
            string.Equals(value.SemanticKey, semanticKey, StringComparison.Ordinal));
        if (existing is null)
        {
            return 0;
        }

        try
        {
            LocalCounterPayload? payload = JsonSerializer.Deserialize<LocalCounterPayload>(existing.PayloadJson, JsonOptions);
            return payload?.Value ?? 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    private static List<string> ResolveSequenceEntries(IReadOnlyList<SessionReplicaValue> values, string semanticKey)
    {
        SessionReplicaValue? existing = values.FirstOrDefault(value =>
            string.Equals(value.SemanticKey, semanticKey, StringComparison.Ordinal));
        if (existing is null)
        {
            return [];
        }

        try
        {
            LocalSequencePayload? payload = JsonSerializer.Deserialize<LocalSequencePayload>(existing.PayloadJson, JsonOptions);
            return payload?.Items.ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static List<SessionQuickActionPin> ResolvePins(IReadOnlyList<SessionReplicaValue> values, string semanticKey)
    {
        SessionReplicaValue? existing = values.FirstOrDefault(value =>
            string.Equals(value.SemanticKey, semanticKey, StringComparison.Ordinal));
        if (existing is null)
        {
            return [];
        }

        try
        {
            LocalPinSetPayload? payload = JsonSerializer.Deserialize<LocalPinSetPayload>(existing.PayloadJson, JsonOptions);
            return payload?.Pins.ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record LocalTrackerPayload(string TrackerKey, int Delta);

    private sealed record LocalNotePayload(string Note);

    private sealed record LocalCounterPayload(int Value);

    private sealed record LocalSequencePayload(IReadOnlyList<string> Items);

    private sealed record LocalPinSetPayload(IReadOnlyList<SessionQuickActionPin> Pins);
}
