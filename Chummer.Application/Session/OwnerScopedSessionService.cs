using System.Security.Cryptography;
using System.Text;
using Chummer.Application.Content;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Session;
using Chummer.Contracts.Trackers;

namespace Chummer.Application.Session;

public sealed class OwnerScopedSessionService : ISessionService
{
    private const string BundleKeyId = "session-runtime-bundle-v1";
    private static readonly TimeSpan BundleLifetime = TimeSpan.FromDays(7);
    private static readonly TimeSpan ExpiringSoonThreshold = TimeSpan.FromDays(1);

    private readonly IRulePackRegistryService _rulePackRegistryService;
    private readonly IRuleProfileApplicationService _ruleProfileApplicationService;
    private readonly IRuleProfileRegistryService _ruleProfileRegistryService;
    private readonly IRulesetSelectionPolicy _rulesetSelectionPolicy;
    private readonly ISessionProfileSelectionStore _profileSelectionStore;
    private readonly ISessionRuntimeBundleStore _runtimeBundleStore;

    public OwnerScopedSessionService(
        IRuleProfileRegistryService ruleProfileRegistryService,
        IRuleProfileApplicationService ruleProfileApplicationService,
        IRulePackRegistryService rulePackRegistryService,
        IRulesetSelectionPolicy rulesetSelectionPolicy,
        ISessionProfileSelectionStore profileSelectionStore,
        ISessionRuntimeBundleStore runtimeBundleStore)
    {
        _ruleProfileRegistryService = ruleProfileRegistryService;
        _ruleProfileApplicationService = ruleProfileApplicationService;
        _rulePackRegistryService = rulePackRegistryService;
        _rulesetSelectionPolicy = rulesetSelectionPolicy;
        _profileSelectionStore = profileSelectionStore;
        _runtimeBundleStore = runtimeBundleStore;
    }

    public SessionApiResult<SessionCharacterCatalog> ListCharacters(OwnerScope owner)
        => NotImplemented<SessionCharacterCatalog>(owner, SessionApiOperations.ListCharacters);

    public SessionApiResult<SessionDashboardProjection> GetCharacterProjection(OwnerScope owner, string characterId)
        => NotImplemented<SessionDashboardProjection>(owner, SessionApiOperations.GetCharacterProjection, characterId);

    public SessionApiResult<SessionOverlaySnapshot> ApplyCharacterPatches(OwnerScope owner, string characterId, SessionPatchRequest? request)
        => NotImplemented<SessionOverlaySnapshot>(owner, SessionApiOperations.ApplyCharacterPatches, characterId);

    public SessionApiResult<SessionSyncReceipt> SyncCharacterLedger(OwnerScope owner, string characterId, SessionSyncBatch? batch)
        => NotImplemented<SessionSyncReceipt>(owner, SessionApiOperations.SyncCharacterLedger, characterId);

    public SessionApiResult<SessionProfileCatalog> ListProfiles(OwnerScope owner)
    {
        string defaultProfileId = $"official.{_rulesetSelectionPolicy.GetDefaultRulesetId()}.core";
        SessionProfileBinding? activeBinding = _profileSelectionStore.List(owner)
            .OrderByDescending(binding => binding.SelectedAtUtc)
            .FirstOrDefault();
        SessionProfileListItem[] profiles = _ruleProfileRegistryService.List(owner)
            .Select(entry => CreateProfileListItem(owner, entry))
            .OrderBy(profile => profile.Title, StringComparer.Ordinal)
            .ToArray();

        return SessionApiResult<SessionProfileCatalog>.Implemented(
            new SessionProfileCatalog(
                Profiles: profiles,
                ActiveProfileId: activeBinding?.ProfileId ?? defaultProfileId));
    }

    public SessionApiResult<SessionRuntimeBundleIssueReceipt> GetRuntimeBundle(OwnerScope owner, string characterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);

        SessionProfileBinding? binding = _profileSelectionStore.Get(owner, characterId);
        if (binding is null)
        {
            return SessionApiResult<SessionRuntimeBundleIssueReceipt>.Implemented(
                CreateBlockedBundleReceipt(characterId, "No session profile has been selected for this character yet."));
        }

        RuleProfileRegistryEntry? profile = _ruleProfileRegistryService.Get(owner, binding.ProfileId, binding.RulesetId);
        if (profile is null)
        {
            return SessionApiResult<SessionRuntimeBundleIssueReceipt>.Implemented(
                CreateBlockedBundleReceipt(characterId, $"Session profile '{binding.ProfileId}' is no longer available."));
        }

        if (!IsSessionReady(owner, profile))
        {
            return SessionApiResult<SessionRuntimeBundleIssueReceipt>.Implemented(
                CreateBlockedBundleReceipt(characterId, $"Session profile '{profile.Manifest.ProfileId}' is not session-ready."));
        }

        SessionRuntimeBundleRecord? existingRecord = _runtimeBundleStore.Get(owner, characterId);
        if (existingRecord is not null
            && string.Equals(existingRecord.ProfileId, profile.Manifest.ProfileId, StringComparison.Ordinal)
            && string.Equals(existingRecord.Receipt.Bundle.BaseCharacterVersion.RuntimeFingerprint, profile.Manifest.RuntimeLock.RuntimeFingerprint, StringComparison.Ordinal)
            && existingRecord.Receipt.SignatureEnvelope.ExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            SessionRuntimeBundleIssueReceipt cachedReceipt = existingRecord.Receipt with
            {
                DeliveryMode = SessionRuntimeBundleDeliveryModes.Cached,
                Diagnostics = UpdateDiagnostics(existingRecord.Receipt.SignatureEnvelope, existingRecord.Receipt.Diagnostics)
            };
            _runtimeBundleStore.Upsert(owner, existingRecord with { Receipt = cachedReceipt });
            return SessionApiResult<SessionRuntimeBundleIssueReceipt>.Implemented(cachedReceipt);
        }

        DateTimeOffset signedAtUtc = DateTimeOffset.UtcNow;
        SessionRuntimeBundle bundle = CreateBundle(characterId, profile, signedAtUtc);
        SessionRuntimeBundleSignatureEnvelope signatureEnvelope = CreateSignatureEnvelope(owner, characterId, profile, bundle, signedAtUtc);
        SessionRuntimeBundleIssueReceipt receipt = new(
            Outcome: existingRecord is null
                ? SessionRuntimeBundleIssueOutcomes.Issued
                : SessionRuntimeBundleIssueOutcomes.Rotated,
            Bundle: bundle,
            SignatureEnvelope: signatureEnvelope,
            DeliveryMode: SessionRuntimeBundleDeliveryModes.Inline,
            Diagnostics: UpdateDiagnostics(signatureEnvelope, []));
        _runtimeBundleStore.Upsert(
            owner,
            new SessionRuntimeBundleRecord(
                CharacterId: characterId.Trim(),
                ProfileId: profile.Manifest.ProfileId,
                RulesetId: profile.Manifest.RulesetId,
                Receipt: receipt,
                IssuedAtUtc: signedAtUtc));
        return SessionApiResult<SessionRuntimeBundleIssueReceipt>.Implemented(receipt);
    }

    public SessionApiResult<SessionProfileSelectionReceipt> SelectProfile(OwnerScope owner, string characterId, SessionProfileSelectionRequest? request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);

        string? requestedProfileId = NormalizeOptional(request?.ProfileId);
        if (requestedProfileId is null)
        {
            return SessionApiResult<SessionProfileSelectionReceipt>.Implemented(
                new SessionProfileSelectionReceipt(
                    CharacterId: characterId.Trim(),
                    ProfileId: string.Empty,
                    RuntimeFingerprint: string.Empty,
                    Outcome: SessionProfileSelectionOutcomes.Blocked,
                    DeferredReason: "A session profile id is required."));
        }

        RuleProfileRegistryEntry? profile = _ruleProfileRegistryService.Get(owner, requestedProfileId);
        if (profile is null)
        {
            return SessionApiResult<SessionProfileSelectionReceipt>.Implemented(
                new SessionProfileSelectionReceipt(
                    CharacterId: characterId.Trim(),
                    ProfileId: requestedProfileId,
                    RuntimeFingerprint: string.Empty,
                    Outcome: SessionProfileSelectionOutcomes.Blocked,
                    DeferredReason: $"Session profile '{requestedProfileId}' was not found."));
        }

        if (!IsSessionReady(owner, profile))
        {
            return SessionApiResult<SessionProfileSelectionReceipt>.Implemented(
                new SessionProfileSelectionReceipt(
                    CharacterId: characterId.Trim(),
                    ProfileId: profile.Manifest.ProfileId,
                    RuntimeFingerprint: profile.Manifest.RuntimeLock.RuntimeFingerprint,
                    Outcome: SessionProfileSelectionOutcomes.Blocked,
                    DeferredReason: $"Session profile '{profile.Manifest.ProfileId}' is not session-ready."));
        }

        RuleProfileApplyReceipt? applyReceipt = _ruleProfileApplicationService.Apply(
            owner,
            profile.Manifest.ProfileId,
            new RuleProfileApplyTarget(
                TargetKind: RuleProfileApplyTargetKinds.SessionLedger,
                TargetId: characterId.Trim()),
            profile.Manifest.RulesetId);
        if (applyReceipt is null || string.Equals(applyReceipt.Outcome, RuleProfileApplyOutcomes.Blocked, StringComparison.Ordinal))
        {
            return SessionApiResult<SessionProfileSelectionReceipt>.Implemented(
                new SessionProfileSelectionReceipt(
                    CharacterId: characterId.Trim(),
                    ProfileId: profile.Manifest.ProfileId,
                    RuntimeFingerprint: profile.Manifest.RuntimeLock.RuntimeFingerprint,
                    Outcome: SessionProfileSelectionOutcomes.Blocked,
                    DeferredReason: $"Session profile '{profile.Manifest.ProfileId}' could not be applied."));
        }

        SessionProfileBinding? existingBinding = _profileSelectionStore.Get(owner, characterId);
        _profileSelectionStore.Upsert(
            owner,
            new SessionProfileBinding(
                CharacterId: characterId.Trim(),
                ProfileId: profile.Manifest.ProfileId,
                RulesetId: profile.Manifest.RulesetId,
                RuntimeFingerprint: profile.Manifest.RuntimeLock.RuntimeFingerprint,
                SelectedAtUtc: DateTimeOffset.UtcNow));

        bool requiresBundleRefresh = existingBinding is not null
            && !string.Equals(existingBinding.RuntimeFingerprint, profile.Manifest.RuntimeLock.RuntimeFingerprint, StringComparison.Ordinal);

        return SessionApiResult<SessionProfileSelectionReceipt>.Implemented(
            new SessionProfileSelectionReceipt(
                CharacterId: characterId.Trim(),
                ProfileId: profile.Manifest.ProfileId,
                RuntimeFingerprint: profile.Manifest.RuntimeLock.RuntimeFingerprint,
                Outcome: SessionProfileSelectionOutcomes.Selected,
                RequiresBundleRefresh: requiresBundleRefresh));
    }

    public SessionApiResult<RulePackCatalog> ListRulePacks(OwnerScope owner)
    {
        RulePackManifest[] sessionReadyPacks = _rulePackRegistryService.List(owner)
            .Where(entry => IsSessionReady(entry))
            .Select(entry => entry.Manifest)
            .OrderBy(manifest => manifest.Title, StringComparer.Ordinal)
            .ToArray();
        return SessionApiResult<RulePackCatalog>.Implemented(new RulePackCatalog(sessionReadyPacks));
    }

    public SessionApiResult<SessionOverlaySnapshot> UpdatePins(OwnerScope owner, SessionPinUpdateRequest? request)
        => NotImplemented<SessionOverlaySnapshot>(owner, SessionApiOperations.UpdatePins, request?.BaseCharacterVersion.CharacterId);

    private SessionProfileListItem CreateProfileListItem(OwnerScope owner, RuleProfileRegistryEntry entry)
    {
        return new SessionProfileListItem(
            ProfileId: entry.Manifest.ProfileId,
            Title: entry.Manifest.Title,
            RulesetId: entry.Manifest.RulesetId,
            RuntimeFingerprint: entry.Manifest.RuntimeLock.RuntimeFingerprint,
            UpdateChannel: entry.Manifest.UpdateChannel,
            SessionReady: IsSessionReady(owner, entry),
            Audience: entry.Manifest.Audience);
    }

    private bool IsSessionReady(OwnerScope owner, RuleProfileRegistryEntry profile)
    {
        if (profile.Manifest.RulePacks.Count == 0)
        {
            return true;
        }

        return profile.Manifest.RulePacks.All(selection =>
        {
            RulePackRegistryEntry? rulePack = _rulePackRegistryService.Get(owner, selection.RulePack.Id, profile.Manifest.RulesetId);
            return rulePack is not null && IsSessionReady(rulePack);
        });
    }

    private static bool IsSessionReady(RulePackRegistryEntry entry)
    {
        if (entry.Manifest.ExecutionPolicies.Count == 0)
        {
            return false;
        }

        return entry.Manifest.ExecutionPolicies.Any(policy =>
            string.Equals(policy.Environment, RulePackExecutionEnvironments.SessionRuntimeBundle, StringComparison.Ordinal)
            && !string.Equals(policy.PolicyMode, RulePackExecutionPolicyModes.Deny, StringComparison.Ordinal));
    }

    private static SessionRuntimeBundleIssueReceipt CreateBlockedBundleReceipt(string characterId, string message)
    {
        CharacterVersionReference baseCharacterVersion = new(
            CharacterId: characterId.Trim(),
            VersionId: "unbound",
            RulesetId: string.Empty,
            RuntimeFingerprint: string.Empty);
        SessionRuntimeBundle bundle = new(
            BundleId: string.Empty,
            BaseCharacterVersion: baseCharacterVersion,
            EngineApiVersion: string.Empty,
            SignedAtUtc: DateTimeOffset.MinValue,
            Signature: string.Empty,
            QuickActions: [],
            Trackers: [],
            ReducerBindings: new Dictionary<string, string>(StringComparer.Ordinal));
        SessionRuntimeBundleSignatureEnvelope signatureEnvelope = new(
            BundleId: string.Empty,
            KeyId: string.Empty,
            Signature: string.Empty,
            SignedAtUtc: DateTimeOffset.MinValue,
            ExpiresAtUtc: DateTimeOffset.MinValue);
        return new SessionRuntimeBundleIssueReceipt(
            Outcome: SessionRuntimeBundleIssueOutcomes.Blocked,
            Bundle: bundle,
            SignatureEnvelope: signatureEnvelope,
            DeliveryMode: SessionRuntimeBundleDeliveryModes.Inline,
            Diagnostics:
            [
                new SessionRuntimeBundleTrustDiagnostic(
                    State: SessionRuntimeBundleTrustStates.MissingKey,
                    Message: message)
            ]);
    }

    private static SessionRuntimeBundle CreateBundle(
        string characterId,
        RuleProfileRegistryEntry profile,
        DateTimeOffset signedAtUtc)
    {
        string bundleId = ComputeHash($"{characterId.Trim()}|{profile.Manifest.ProfileId}|{profile.Manifest.RuntimeLock.RuntimeFingerprint}");
        CharacterVersionReference baseCharacterVersion = new(
            CharacterId: characterId.Trim(),
            VersionId: $"session:{profile.Manifest.ProfileId}",
            RulesetId: profile.Manifest.RulesetId,
            RuntimeFingerprint: profile.Manifest.RuntimeLock.RuntimeFingerprint);
        SessionQuickActionPin[] quickActions = profile.Manifest.RuntimeLock.ProviderBindings
            .Where(binding => string.Equals(binding.Key, RulePackCapabilityIds.SessionQuickActions, StringComparison.Ordinal))
            .Select(binding => new SessionQuickActionPin(
                ActionId: binding.Value,
                Label: binding.Value,
                CapabilityId: binding.Key))
            .ToArray();

        return new SessionRuntimeBundle(
            BundleId: bundleId,
            BaseCharacterVersion: baseCharacterVersion,
            EngineApiVersion: profile.Manifest.RuntimeLock.EngineApiVersion,
            SignedAtUtc: signedAtUtc,
            Signature: ComputeHash($"{bundleId}|{profile.Manifest.RuntimeLock.RuntimeFingerprint}|{signedAtUtc:O}"),
            QuickActions: quickActions,
            Trackers: Array.Empty<TrackerDefinition>(),
            ReducerBindings: new Dictionary<string, string>(profile.Manifest.RuntimeLock.ProviderBindings, StringComparer.Ordinal));
    }

    private static SessionRuntimeBundleSignatureEnvelope CreateSignatureEnvelope(
        OwnerScope owner,
        string characterId,
        RuleProfileRegistryEntry profile,
        SessionRuntimeBundle bundle,
        DateTimeOffset signedAtUtc)
    {
        return new SessionRuntimeBundleSignatureEnvelope(
            BundleId: bundle.BundleId,
            KeyId: BundleKeyId,
            Signature: ComputeHash($"{owner.NormalizedValue}|{characterId.Trim()}|{profile.Manifest.ProfileId}|{bundle.BundleId}|{bundle.Signature}"),
            SignedAtUtc: signedAtUtc,
            ExpiresAtUtc: signedAtUtc.Add(BundleLifetime));
    }

    private static IReadOnlyList<SessionRuntimeBundleTrustDiagnostic> UpdateDiagnostics(
        SessionRuntimeBundleSignatureEnvelope signatureEnvelope,
        IReadOnlyList<SessionRuntimeBundleTrustDiagnostic> existingDiagnostics)
    {
        List<SessionRuntimeBundleTrustDiagnostic> diagnostics =
        [
            new(
                State: SessionRuntimeBundleTrustStates.Trusted,
                Message: "Runtime bundle signature is valid for the current owner-scoped session profile selection.",
                KeyId: signatureEnvelope.KeyId,
                RuntimeFingerprint: null)
        ];

        if (signatureEnvelope.ExpiresAtUtc != DateTimeOffset.MinValue
            && signatureEnvelope.ExpiresAtUtc - DateTimeOffset.UtcNow <= ExpiringSoonThreshold)
        {
            diagnostics.Add(new SessionRuntimeBundleTrustDiagnostic(
                State: SessionRuntimeBundleTrustStates.ExpiringSoon,
                Message: "Runtime bundle signature is nearing expiry and should be refreshed soon.",
                KeyId: signatureEnvelope.KeyId,
                RuntimeFingerprint: null));
        }

        foreach (SessionRuntimeBundleTrustDiagnostic diagnostic in existingDiagnostics)
        {
            if (diagnostics.Any(current =>
                    string.Equals(current.State, diagnostic.State, StringComparison.Ordinal)
                    && string.Equals(current.Message, diagnostic.Message, StringComparison.Ordinal)))
            {
                continue;
            }

            diagnostics.Add(diagnostic);
        }

        return diagnostics;
    }

    private static string ComputeHash(string input)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private static SessionApiResult<T> NotImplemented<T>(OwnerScope owner, string operation, string? characterId = null)
        => SessionApiResult<T>.FromNotImplemented(
            new SessionNotImplementedReceipt(
                Error: "session_not_implemented",
                Operation: operation,
                Message: "The dedicated session/mobile surface is not implemented yet.",
                CharacterId: string.IsNullOrWhiteSpace(characterId) ? null : characterId,
                OwnerId: owner.NormalizedValue));
}
