#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Linq;
using Chummer.Application.Content;
using Chummer.Application.Session;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Session;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public sealed class SessionServiceTests
{
    [TestMethod]
    public void Owner_scoped_session_service_preserves_not_implemented_sync_boundary()
    {
        OwnerScopedSessionService service = CreateService();
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

    [TestMethod]
    public void Owner_scoped_session_service_lists_profiles_and_uses_default_core_profile_when_no_selection_exists()
    {
        OwnerScopedSessionService service = CreateService();

        SessionApiResult<SessionProfileCatalog> result = service.ListProfiles(OwnerScope.LocalSingleUser);

        Assert.IsTrue(result.IsImplemented);
        Assert.IsNotNull(result.Payload);
        Assert.AreEqual("official.sr5.core", result.Payload.ActiveProfileId);
        Assert.HasCount(2, result.Payload.Profiles);
        Assert.IsTrue(result.Payload.Profiles.Any(profile => profile.ProfileId == "official.sr5.core" && profile.SessionReady));
        Assert.IsTrue(result.Payload.Profiles.Any(profile => profile.ProfileId == "campaign.sr5.ready" && profile.SessionReady));
    }

    [TestMethod]
    public void Owner_scoped_session_service_projects_unselected_runtime_state_when_no_profile_binding_exists()
    {
        OwnerScopedSessionService service = CreateService();

        SessionApiResult<SessionRuntimeStatusProjection> result = service.GetRuntimeState(OwnerScope.LocalSingleUser, "char-9");

        Assert.IsTrue(result.IsImplemented);
        Assert.IsNotNull(result.Payload);
        Assert.AreEqual(SessionRuntimeSelectionStates.Unselected, result.Payload.SelectionState);
        Assert.AreEqual(SessionRuntimeBundleFreshnessStates.Missing, result.Payload.BundleFreshness);
        Assert.IsTrue(result.Payload.RequiresBundleRefresh);
    }

    [TestMethod]
    public void Owner_scoped_session_service_blocks_runtime_bundle_when_profile_has_not_been_selected()
    {
        OwnerScopedSessionService service = CreateService();

        SessionApiResult<SessionRuntimeBundleIssueReceipt> result = service.GetRuntimeBundle(OwnerScope.LocalSingleUser, "char-9");

        Assert.IsTrue(result.IsImplemented);
        Assert.IsNotNull(result.Payload);
        Assert.AreEqual(SessionRuntimeBundleIssueOutcomes.Blocked, result.Payload.Outcome);
        Assert.AreEqual(SessionRuntimeBundleTrustStates.MissingKey, result.Payload.Diagnostics[0].State);
    }

    [TestMethod]
    public void Owner_scoped_session_service_projects_selected_runtime_state_and_bundle_freshness()
    {
        InMemorySessionProfileSelectionStore selectionStore = new();
        InMemorySessionRuntimeBundleStore runtimeBundleStore = new();
        OwnerScopedSessionService service = CreateService(selectionStore: selectionStore, runtimeBundleStore: runtimeBundleStore);

        SessionApiResult<SessionProfileSelectionReceipt> selection = service.SelectProfile(
            OwnerScope.LocalSingleUser,
            "char-1",
            new SessionProfileSelectionRequest("campaign.sr5.ready"));
        SessionApiResult<SessionRuntimeStatusProjection> beforeBundle = service.GetRuntimeState(OwnerScope.LocalSingleUser, "char-1");
        SessionApiResult<SessionRuntimeBundleIssueReceipt> bundle = service.GetRuntimeBundle(OwnerScope.LocalSingleUser, "char-1");
        SessionApiResult<SessionRuntimeStatusProjection> afterBundle = service.GetRuntimeState(OwnerScope.LocalSingleUser, "char-1");

        Assert.IsTrue(selection.IsImplemented);
        Assert.IsNotNull(selection.Payload);
        Assert.AreEqual(SessionProfileSelectionOutcomes.Selected, selection.Payload.Outcome);

        Assert.IsTrue(beforeBundle.IsImplemented);
        Assert.IsNotNull(beforeBundle.Payload);
        Assert.AreEqual(SessionRuntimeSelectionStates.Selected, beforeBundle.Payload.SelectionState);
        Assert.AreEqual(SessionRuntimeBundleFreshnessStates.Missing, beforeBundle.Payload.BundleFreshness);
        Assert.IsTrue(beforeBundle.Payload.RequiresBundleRefresh);
        Assert.AreEqual("campaign.sr5.ready", beforeBundle.Payload.ProfileId);

        Assert.IsTrue(bundle.IsImplemented);
        Assert.IsNotNull(bundle.Payload);
        Assert.AreEqual(SessionRuntimeBundleIssueOutcomes.Issued, bundle.Payload.Outcome);

        Assert.IsTrue(afterBundle.IsImplemented);
        Assert.IsNotNull(afterBundle.Payload);
        Assert.AreEqual(SessionRuntimeSelectionStates.Selected, afterBundle.Payload.SelectionState);
        Assert.AreEqual(SessionRuntimeBundleFreshnessStates.Current, afterBundle.Payload.BundleFreshness);
        Assert.IsFalse(afterBundle.Payload.RequiresBundleRefresh);
        Assert.AreEqual(SessionRuntimeBundleTrustStates.Trusted, afterBundle.Payload.BundleTrustState);
        Assert.AreEqual(bundle.Payload.Bundle.BundleId, afterBundle.Payload.BundleId);
    }

    [TestMethod]
    public void Owner_scoped_session_service_selects_profile_and_reuses_cached_runtime_bundle()
    {
        InMemorySessionProfileSelectionStore selectionStore = new();
        InMemorySessionRuntimeBundleStore runtimeBundleStore = new();
        OwnerScopedSessionService service = CreateService(selectionStore: selectionStore, runtimeBundleStore: runtimeBundleStore);

        SessionApiResult<SessionProfileSelectionReceipt> selection = service.SelectProfile(
            OwnerScope.LocalSingleUser,
            "char-1",
            new SessionProfileSelectionRequest("campaign.sr5.ready"));

        Assert.IsTrue(selection.IsImplemented);
        Assert.IsNotNull(selection.Payload);
        Assert.AreEqual(SessionProfileSelectionOutcomes.Selected, selection.Payload.Outcome);
        Assert.AreEqual("campaign.sr5.ready", selection.Payload.ProfileId);

        SessionApiResult<SessionRuntimeBundleIssueReceipt> firstBundle = service.GetRuntimeBundle(OwnerScope.LocalSingleUser, "char-1");
        SessionApiResult<SessionRuntimeBundleIssueReceipt> secondBundle = service.GetRuntimeBundle(OwnerScope.LocalSingleUser, "char-1");

        Assert.IsTrue(firstBundle.IsImplemented);
        Assert.IsNotNull(firstBundle.Payload);
        Assert.AreEqual(SessionRuntimeBundleIssueOutcomes.Issued, firstBundle.Payload.Outcome);
        Assert.AreEqual(SessionRuntimeBundleDeliveryModes.Inline, firstBundle.Payload.DeliveryMode);
        Assert.AreEqual("char-1", firstBundle.Payload.Bundle.BaseCharacterVersion.CharacterId);
        Assert.AreEqual("sr5", firstBundle.Payload.Bundle.BaseCharacterVersion.RulesetId);
        Assert.AreEqual("runtime-campaign-sr5-ready", firstBundle.Payload.Bundle.BaseCharacterVersion.RuntimeFingerprint);

        Assert.IsTrue(secondBundle.IsImplemented);
        Assert.IsNotNull(secondBundle.Payload);
        Assert.AreEqual(SessionRuntimeBundleDeliveryModes.Cached, secondBundle.Payload.DeliveryMode);
        Assert.AreEqual(firstBundle.Payload.Bundle.BundleId, secondBundle.Payload.Bundle.BundleId);
        Assert.HasCount(1, runtimeBundleStore.Records);
        Assert.HasCount(1, selectionStore.Bindings);
    }

    [TestMethod]
    public void Owner_scoped_session_service_lists_only_session_ready_rulepacks()
    {
        OwnerScopedSessionService service = CreateService();

        SessionApiResult<RulePackCatalog> result = service.ListRulePacks(OwnerScope.LocalSingleUser);

        Assert.IsTrue(result.IsImplemented);
        Assert.IsNotNull(result.Payload);
        Assert.HasCount(1, result.Payload.InstalledRulePacks);
        Assert.AreEqual("campaign.ready.pack", result.Payload.InstalledRulePacks[0].PackId);
    }

    private static OwnerScopedSessionService CreateService(
        ISessionProfileSelectionStore? selectionStore = null,
        ISessionRuntimeBundleStore? runtimeBundleStore = null)
    {
        StubRulePackRegistryService rulePackRegistry = new(
        [
            CreateRulePackEntry("campaign.ready.pack", sessionReady: true),
            CreateRulePackEntry("campaign.blocked.pack", sessionReady: false)
        ]);
        StubRuleProfileRegistryService ruleProfileRegistry = new(
        [
            CreateProfileEntry("official.sr5.core", "SR5 Core", [], "runtime-official-sr5-core"),
            CreateProfileEntry(
                "campaign.sr5.ready",
                "Campaign Ready",
                [new RuleProfilePackSelection(new ArtifactVersionReference("campaign.ready.pack", "1.0.0"))],
                "runtime-campaign-sr5-ready")
        ]);

        return new OwnerScopedSessionService(
            ruleProfileRegistry,
            new StubRuleProfileApplicationService(ruleProfileRegistry),
            rulePackRegistry,
            new StubRulesetSelectionPolicy(),
            selectionStore ?? new InMemorySessionProfileSelectionStore(),
            runtimeBundleStore ?? new InMemorySessionRuntimeBundleStore());
    }

    private static RuleProfileRegistryEntry CreateProfileEntry(
        string profileId,
        string title,
        IReadOnlyList<RuleProfilePackSelection> rulePacks,
        string runtimeFingerprint)
    {
        RuleProfileManifest manifest = new(
            ProfileId: profileId,
            Title: title,
            Description: title,
            RulesetId: RulesetDefaults.Sr5,
            Audience: RuleProfileAudienceKinds.General,
            CatalogKind: RuleProfileCatalogKinds.Curated,
            RulePacks: rulePacks,
            DefaultToggles: [],
            RuntimeLock: new ResolvedRuntimeLock(
                RulesetId: RulesetDefaults.Sr5,
                ContentBundles:
                [
                    new ContentBundleDescriptor(
                        BundleId: "official.sr5.base",
                        RulesetId: RulesetDefaults.Sr5,
                        Version: "schema-1",
                        Title: "SR5 Base",
                        Description: "Base bundle",
                        AssetPaths: ["data/", "lang/"])
                ],
                RulePacks: rulePacks.Select(selection => selection.RulePack).ToArray(),
                ProviderBindings: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [RulePackCapabilityIds.SessionQuickActions] = $"{profileId}:session.quick-actions"
                },
                EngineApiVersion: "rulepack-v1",
                RuntimeFingerprint: runtimeFingerprint),
            UpdateChannel: RuleProfileUpdateChannels.Stable,
            Notes: null);
        return new RuleProfileRegistryEntry(
            manifest,
            new RuleProfilePublicationMetadata(
                OwnerId: OwnerScope.LocalSingleUser.NormalizedValue,
                Visibility: ArtifactVisibilityModes.Private,
                PublicationStatus: RuleProfilePublicationStatuses.Published,
                Review: new RulePackReviewDecision(RulePackReviewStates.NotRequired),
                Shares: []),
            new ArtifactInstallState(ArtifactInstallStates.Available),
            RegistryEntrySourceKinds.PersistedManifest);
    }

    private static RulePackRegistryEntry CreateRulePackEntry(string packId, bool sessionReady)
    {
        RulePackExecutionPolicyHint[] executionPolicies = sessionReady
            ?
            [
                new RulePackExecutionPolicyHint(
                    Environment: RulePackExecutionEnvironments.SessionRuntimeBundle,
                    PolicyMode: RulePackExecutionPolicyModes.Allow,
                    MinimumTrustTier: ArtifactTrustTiers.Private,
                    AllowedAssetModes: [RulePackAssetModes.MergeCatalog, RulePackAssetModes.WrapProvider])
            ]
            :
            [
                new RulePackExecutionPolicyHint(
                    Environment: RulePackExecutionEnvironments.SessionRuntimeBundle,
                    PolicyMode: RulePackExecutionPolicyModes.Deny,
                    MinimumTrustTier: ArtifactTrustTiers.Private,
                    AllowedAssetModes: [])
            ];
        RulePackManifest manifest = new(
            PackId: packId,
            Version: "1.0.0",
            Title: packId,
            Author: "test",
            Description: packId,
            Targets: [RulesetDefaults.Sr5],
            EngineApiVersion: "rulepack-v1",
            DependsOn: [],
            ConflictsWith: [],
            Visibility: ArtifactVisibilityModes.Private,
            TrustTier: ArtifactTrustTiers.Private,
            Assets: [],
            Capabilities:
            [
                new RulePackCapabilityDescriptor(
                    CapabilityId: RulePackCapabilityIds.SessionQuickActions,
                    AssetKind: RulePackAssetKinds.Lua,
                    AssetMode: RulePackAssetModes.WrapProvider,
                    Explainable: true,
                    SessionSafe: sessionReady)
            ],
            ExecutionPolicies: executionPolicies);
        return new RulePackRegistryEntry(
            manifest,
            new RulePackPublicationMetadata(
                OwnerId: OwnerScope.LocalSingleUser.NormalizedValue,
                Visibility: ArtifactVisibilityModes.Private,
                PublicationStatus: RulePackPublicationStatuses.Published,
                Review: new RulePackReviewDecision(RulePackReviewStates.NotRequired),
                Shares: []),
            new ArtifactInstallState(ArtifactInstallStates.Installed),
            RegistryEntrySourceKinds.PersistedManifest);
    }

    private sealed class StubRulesetSelectionPolicy : IRulesetSelectionPolicy
    {
        public string GetDefaultRulesetId() => RulesetDefaults.Sr5;
    }

    private sealed class StubRuleProfileRegistryService : IRuleProfileRegistryService
    {
        private readonly IReadOnlyList<RuleProfileRegistryEntry> _entries;

        public StubRuleProfileRegistryService(IReadOnlyList<RuleProfileRegistryEntry> entries)
        {
            _entries = entries;
        }

        public IReadOnlyList<RuleProfileRegistryEntry> List(OwnerScope owner, string? rulesetId = null)
        {
            string? normalizedRulesetId = RulesetDefaults.NormalizeOptional(rulesetId);
            return _entries
                .Where(entry => normalizedRulesetId is null || string.Equals(entry.Manifest.RulesetId, normalizedRulesetId, StringComparison.Ordinal))
                .ToArray();
        }

        public RuleProfileRegistryEntry? Get(OwnerScope owner, string profileId, string? rulesetId = null)
        {
            string? normalizedRulesetId = RulesetDefaults.NormalizeOptional(rulesetId);
            return _entries.FirstOrDefault(entry =>
                string.Equals(entry.Manifest.ProfileId, profileId, StringComparison.Ordinal)
                && (normalizedRulesetId is null || string.Equals(entry.Manifest.RulesetId, normalizedRulesetId, StringComparison.Ordinal)));
        }
    }

    private sealed class StubRuleProfileApplicationService : IRuleProfileApplicationService
    {
        private readonly IRuleProfileRegistryService _registryService;

        public StubRuleProfileApplicationService(IRuleProfileRegistryService registryService)
        {
            _registryService = registryService;
        }

        public RuleProfilePreviewReceipt? Preview(OwnerScope owner, string profileId, RuleProfileApplyTarget target, string? rulesetId = null)
        {
            RuleProfileRegistryEntry? entry = _registryService.Get(owner, profileId, rulesetId);
            return entry is null
                ? null
                : new RuleProfilePreviewReceipt(
                    ProfileId: profileId,
                    Target: target,
                    RuntimeLock: entry.Manifest.RuntimeLock,
                    Changes: [],
                    Warnings: [],
                    RequiresConfirmation: false);
        }

        public RuleProfileApplyReceipt? Apply(OwnerScope owner, string profileId, RuleProfileApplyTarget target, string? rulesetId = null)
        {
            RuleProfileRegistryEntry? entry = _registryService.Get(owner, profileId, rulesetId);
            if (entry is null)
            {
                return null;
            }

            RuleProfilePreviewReceipt preview = Preview(owner, profileId, target, rulesetId)!;
            return new RuleProfileApplyReceipt(
                ProfileId: profileId,
                Target: target,
                Outcome: RuleProfileApplyOutcomes.Applied,
                Preview: preview,
                InstallReceipt: new RuntimeLockInstallReceipt(
                    TargetKind: target.TargetKind,
                    TargetId: target.TargetId,
                    Outcome: RuntimeLockInstallOutcomes.Installed,
                    RuntimeLock: entry.Manifest.RuntimeLock,
                    InstalledAtUtc: DateTimeOffset.UtcNow,
                    RebindNotices: [],
                    RequiresSessionReplay: true));
        }
    }

    private sealed class StubRulePackRegistryService : IRulePackRegistryService
    {
        private readonly IReadOnlyList<RulePackRegistryEntry> _entries;

        public StubRulePackRegistryService(IReadOnlyList<RulePackRegistryEntry> entries)
        {
            _entries = entries;
        }

        public IReadOnlyList<RulePackRegistryEntry> List(OwnerScope owner, string? rulesetId = null)
        {
            string? normalizedRulesetId = RulesetDefaults.NormalizeOptional(rulesetId);
            return _entries
                .Where(entry => normalizedRulesetId is null || entry.Manifest.Targets.Contains(normalizedRulesetId, StringComparer.Ordinal))
                .ToArray();
        }

        public RulePackRegistryEntry? Get(OwnerScope owner, string packId, string? rulesetId = null)
        {
            string? normalizedRulesetId = RulesetDefaults.NormalizeOptional(rulesetId);
            return _entries.FirstOrDefault(entry =>
                string.Equals(entry.Manifest.PackId, packId, StringComparison.Ordinal)
                && (normalizedRulesetId is null || entry.Manifest.Targets.Contains(normalizedRulesetId, StringComparer.Ordinal)));
        }
    }

    private sealed class InMemorySessionProfileSelectionStore : ISessionProfileSelectionStore
    {
        public List<SessionProfileBinding> Bindings { get; } = [];

        public IReadOnlyList<SessionProfileBinding> List(OwnerScope owner) => Bindings.ToArray();

        public SessionProfileBinding? Get(OwnerScope owner, string characterId)
            => Bindings.FirstOrDefault(binding => string.Equals(binding.CharacterId, characterId, StringComparison.Ordinal));

        public SessionProfileBinding Upsert(OwnerScope owner, SessionProfileBinding binding)
        {
            int existingIndex = Bindings.FindIndex(current => string.Equals(current.CharacterId, binding.CharacterId, StringComparison.Ordinal));
            if (existingIndex >= 0)
            {
                Bindings[existingIndex] = binding;
            }
            else
            {
                Bindings.Add(binding);
            }

            return binding;
        }
    }

    private sealed class InMemorySessionRuntimeBundleStore : ISessionRuntimeBundleStore
    {
        public List<SessionRuntimeBundleRecord> Records { get; } = [];

        public SessionRuntimeBundleRecord? Get(OwnerScope owner, string characterId)
            => Records.FirstOrDefault(record => string.Equals(record.CharacterId, characterId, StringComparison.Ordinal));

        public SessionRuntimeBundleRecord Upsert(OwnerScope owner, SessionRuntimeBundleRecord record)
        {
            int existingIndex = Records.FindIndex(current => string.Equals(current.CharacterId, record.CharacterId, StringComparison.Ordinal));
            if (existingIndex >= 0)
            {
                Records[existingIndex] = record;
            }
            else
            {
                Records.Add(record);
            }

            return record;
        }
    }
}
