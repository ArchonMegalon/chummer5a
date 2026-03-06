#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chummer.Application.Characters;
using Chummer.Application.Content;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Assets;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Content;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Session;
using Chummer.Contracts.Trackers;
using Chummer.Contracts.Workspaces;
using Chummer.Infrastructure.Xml;
using Chummer.Rulesets.Sr4;
using Chummer.Rulesets.Sr5;
using Chummer.Rulesets.Sr6;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class RulesetSeamContractsTests
{
    [TestMethod]
    public void Workspace_models_keep_native_format_defaults_but_require_explicit_ruleset_inputs()
    {
        WorkspaceDocument workspaceDocument = new("<character />", RulesetId: RulesetDefaults.Sr5);
        WorkspaceImportDocument importDocument = new("<character />", RulesetId: RulesetDefaults.Sr5);
        WorkspaceSaveReceipt saveReceipt = new(new CharacterWorkspaceId("ws-1"), DocumentLength: 128, RulesetId: RulesetDefaults.Sr5);
        WorkspaceDownloadReceipt downloadReceipt = new(
            new CharacterWorkspaceId("ws-1"),
            WorkspaceDocumentFormat.NativeXml,
            ContentBase64: "PGNoYXJhY3RlciAvPg==",
            FileName: "ws-1.chum5",
            DocumentLength: 128,
            RulesetId: RulesetDefaults.Sr5);
        WorkspaceListItem listItem = new(
            new CharacterWorkspaceId("ws-1"),
            Summary: new Chummer.Contracts.Characters.CharacterFileSummary(
                Name: "Test",
                Alias: "Alias",
                Metatype: "Human",
                BuildMethod: "Priority",
                CreatedVersion: "1.0",
                AppVersion: "1.0",
                Karma: 0m,
                Nuyen: 0m,
                Created: true),
            LastUpdatedUtc: DateTimeOffset.UtcNow,
            RulesetId: RulesetDefaults.Sr5);
        WorkspacePayloadEnvelope envelope = new(RulesetDefaults.Sr5, SchemaVersion: 1, PayloadKind: "workspace", Payload: "{}");

        Assert.AreEqual(RulesetDefaults.Sr5, workspaceDocument.State.RulesetId);
        Assert.AreEqual(WorkspaceDocumentFormat.NativeXml, workspaceDocument.Format);
        Assert.AreEqual(1, workspaceDocument.State.SchemaVersion);
        Assert.AreEqual("workspace", workspaceDocument.State.PayloadKind);
        Assert.AreEqual("<character />", workspaceDocument.State.Payload);
        Assert.AreEqual(RulesetDefaults.Sr5, workspaceDocument.PayloadEnvelope.RulesetId);
        Assert.AreEqual(RulesetDefaults.Sr5, importDocument.RulesetId);
        Assert.AreEqual(WorkspaceDocumentFormat.NativeXml, importDocument.Format);
        Assert.AreEqual(RulesetDefaults.Sr5, saveReceipt.RulesetId);
        Assert.AreEqual(RulesetDefaults.Sr5, downloadReceipt.RulesetId);
        Assert.AreEqual(RulesetDefaults.Sr5, listItem.RulesetId);
        Assert.AreEqual(RulesetDefaults.Sr5, envelope.RulesetId);
    }

    [TestMethod]
    public void Artifact_taxonomy_distinguishes_rulepacks_buildkits_content_bundles_and_runtime_locks()
    {
        ContentBundleDescriptor contentBundle = new(
            BundleId: "sr5-core",
            RulesetId: RulesetDefaults.Sr5,
            Version: "2026.03.06",
            Title: "SR5 Core Bundle",
            Description: "Official base data.",
            AssetPaths: ["data/", "lang/"]);
        RulePackManifest rulePack = new(
            PackId: "house-rules",
            Version: "1.2.0",
            Title: "House Rules",
            Author: "GM",
            Description: "Campaign-specific runtime changes.",
            Targets: [RulesetDefaults.Sr5],
            EngineApiVersion: "rulepack-v1",
            DependsOn: [],
            ConflictsWith: [],
            Visibility: ArtifactVisibilityModes.Shared,
            TrustTier: ArtifactTrustTiers.Private,
            Assets:
            [
                new RulePackAssetDescriptor(
                    Kind: RulePackAssetKinds.Xml,
                    Mode: RulePackAssetModes.MergeCatalog,
                    RelativePath: "data/qualities.xml",
                    Checksum: "sha256:abc")
            ],
            Capabilities:
            [
                new RulePackCapabilityDescriptor(
                    CapabilityId: RulePackCapabilityIds.ValidateCharacter,
                    AssetKind: RulePackAssetKinds.Lua,
                    AssetMode: RulePackAssetModes.AddProvider,
                    Explainable: true,
                    SessionSafe: false)
            ],
            ExecutionPolicies:
            [
                new RulePackExecutionPolicyHint(
                    Environment: RulePackExecutionEnvironments.HostedServer,
                    PolicyMode: RulePackExecutionPolicyModes.ReviewRequired,
                    MinimumTrustTier: ArtifactTrustTiers.Curated,
                    AllowedAssetModes: [RulePackAssetModes.AddProvider, RulePackAssetModes.WrapProvider])
            ]);
        BuildKitManifest buildKit = new(
            BuildKitId: "street-sam-starter",
            Version: "1.0.0",
            Title: "Street Sam Starter",
            Description: "Chargen starter kit.",
            Targets: [RulesetDefaults.Sr5],
            RuntimeRequirements:
            [
                new BuildKitRuntimeRequirement(
                    RulesetId: RulesetDefaults.Sr5,
                    RequiredRuntimeFingerprints: ["runtime-lock-sha256"],
                    RequiredRulePacks: [new ArtifactVersionReference("house-rules", "1.2.0")])
            ],
            Prompts:
            [
                new BuildKitPromptDescriptor(
                    PromptId: "weapon-focus",
                    Kind: BuildKitPromptKinds.Choice,
                    Label: "Preferred Combat Focus",
                    Options:
                    [
                        new BuildKitPromptOption("melee", "Melee"),
                        new BuildKitPromptOption("ranged", "Ranged")
                    ],
                    Required: true)
            ],
            Actions:
            [
                new BuildKitActionDescriptor(
                    ActionId: "grant-starting-bundle",
                    Kind: BuildKitActionKinds.AddBundle,
                    TargetId: "starter/street-sam",
                    PromptId: "weapon-focus",
                    Notes: "Apply the matching starter bundle.")
            ],
            Visibility: ArtifactVisibilityModes.Shared,
            TrustTier: ArtifactTrustTiers.Curated);
        ResolvedRuntimeLock runtimeLock = new(
            RulesetId: RulesetDefaults.Sr5,
            ContentBundles: [contentBundle],
            RulePacks: [new ArtifactVersionReference("house-rules", "1.2.0")],
            ProviderBindings: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["validate.character"] = "house-rules/validate.character"
            },
            EngineApiVersion: "rulepack-v1",
            RuntimeFingerprint: "runtime-lock-sha256");

        Assert.AreEqual("street-sam-starter", buildKit.BuildKitId);
        Assert.AreEqual(BuildKitPromptKinds.Choice, buildKit.Prompts[0].Kind);
        Assert.AreEqual(BuildKitActionKinds.AddBundle, buildKit.Actions[0].Kind);
        Assert.AreEqual("runtime-lock-sha256", buildKit.RuntimeRequirements[0].RequiredRuntimeFingerprints[0]);
        Assert.AreEqual("house-rules", rulePack.PackId);
        Assert.AreEqual("sr5-core", contentBundle.BundleId);
        Assert.AreEqual("runtime-lock-sha256", runtimeLock.RuntimeFingerprint);
        Assert.AreEqual(RulePackAssetModes.MergeCatalog, rulePack.Assets[0].Mode);
        Assert.AreEqual(RulePackAssetKinds.Xml, rulePack.Assets[0].Kind);
        Assert.AreEqual(RulePackCapabilityIds.ValidateCharacter, rulePack.Capabilities[0].CapabilityId);
        Assert.AreEqual(RulePackExecutionEnvironments.HostedServer, rulePack.ExecutionPolicies[0].Environment);
        Assert.AreEqual(ArtifactVisibilityModes.Shared, buildKit.Visibility);
        Assert.AreEqual(ArtifactTrustTiers.Private, rulePack.TrustTier);
    }

    [TestMethod]
    public void Session_taxonomy_distinguishes_ledger_snapshot_and_runtime_bundle()
    {
        ResolvedRuntimeLock runtimeLock = new(
            RulesetId: RulesetDefaults.Sr5,
            ContentBundles:
            [
                new ContentBundleDescriptor(
                    BundleId: "sr5-core",
                    RulesetId: RulesetDefaults.Sr5,
                    Version: "2026.03.06",
                    Title: "SR5 Core Bundle",
                    Description: "Official base data.",
                    AssetPaths: ["data/", "lang/"])
            ],
            RulePacks: [new ArtifactVersionReference("house-rules", "1.2.0")],
            ProviderBindings: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["session.quick-actions"] = "house-rules/session.quick-actions"
            },
            EngineApiVersion: "rulepack-v1",
            RuntimeFingerprint: "runtime-lock-sha256");
        CharacterVersionReference baseCharacterVersion = new(
            CharacterId: "char-1",
            VersionId: "charv-1",
            RulesetId: RulesetDefaults.Sr5,
            RuntimeFingerprint: runtimeLock.RuntimeFingerprint);
        CharacterVersion characterVersion = new(
            Reference: baseCharacterVersion,
            RuntimeLock: runtimeLock,
            PayloadEnvelope: new WorkspacePayloadEnvelope(
                RulesetDefaults.Sr5,
                SchemaVersion: 1,
                PayloadKind: "workspace",
                Payload: "<character />"),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Summary: new CharacterFileSummary(
                Name: "Prime Runner",
                Alias: "Cipher",
                Metatype: "Human",
                BuildMethod: "Priority",
                CreatedVersion: "1.0",
                AppVersion: "1.0",
                Karma: 0m,
                Nuyen: 0m,
                Created: true));
        SessionEvent sessionEvent = new(
            EventId: "evt-1",
            OverlayId: "overlay-1",
            BaseCharacterVersion: baseCharacterVersion,
            DeviceId: "device-1",
            ActorId: "user-1",
            Sequence: 1,
            EventType: SessionEventTypes.TrackerIncrement,
            PayloadJson: "{\"trackerId\":\"stun\",\"amount\":1}",
            CreatedAtUtc: DateTimeOffset.UtcNow);
        SessionLedger ledger = new(
            OverlayId: "overlay-1",
            BaseCharacterVersion: baseCharacterVersion,
            Events: [sessionEvent],
            BaselineSnapshotId: "snap-0",
            NextSequence: 2);
        SessionOverlaySnapshot snapshot = new(
            OverlayId: "overlay-1",
            BaseCharacterVersion: baseCharacterVersion,
            Trackers:
            [
                new TrackerSnapshot(
                    Definition: new TrackerDefinition(
                        TrackerId: "stun",
                        Category: TrackerCategories.Condition,
                        Label: "Stun",
                        DefaultValue: 0,
                        MinimumValue: 0,
                        MaximumValue: 10,
                        Thresholds:
                        [
                            new TrackerThresholdDefinition(
                                ThresholdId: "healthy",
                                Value: 3,
                                Label: "Healthy",
                                Status: "ok")
                        ]),
                    CurrentValue: 1,
                    ThresholdState: "healthy")
            ],
            ActiveEffects:
            [
                new SessionEffectState(
                    EffectId: "wounded",
                    Label: "Wounded",
                    IsActive: true,
                    SourceEventId: sessionEvent.EventId)
            ],
            PinnedQuickActions:
            [
                new SessionQuickActionPin(
                    ActionId: "second-wind",
                    Label: "Second Wind",
                    CapabilityId: "session.quick-actions")
            ],
            Notes: ["Took stun from suppressive fire."],
            SyncState: new SessionSyncState(
                Status: SessionSyncStatuses.PendingSync,
                PendingEventCount: 1,
                LastSyncedAtUtc: null));
        SessionRuntimeBundle runtimeBundle = new(
            BundleId: "session-bundle-1",
            BaseCharacterVersion: baseCharacterVersion,
            EngineApiVersion: "session-runtime-v1",
            SignedAtUtc: DateTimeOffset.UtcNow,
            Signature: "sig-1",
            QuickActions:
            [
                new SessionQuickActionPin(
                    ActionId: "second-wind",
                    Label: "Second Wind",
                    CapabilityId: "session.quick-actions")
            ],
            Trackers:
            [
                new TrackerDefinition(
                    TrackerId: "stun",
                    Category: TrackerCategories.Condition,
                    Label: "Stun",
                    DefaultValue: 0,
                    MinimumValue: 0,
                    MaximumValue: 10,
                    Thresholds:
                    [
                        new TrackerThresholdDefinition("healthy", 3, "Healthy", "ok"),
                        new TrackerThresholdDefinition("wounded", 6, "Wounded", "warn"),
                        new TrackerThresholdDefinition("critical", 9, "Critical", "critical")
                    ])
            ],
            ReducerBindings: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["tracker.increment"] = "session-runtime/stun.increment"
            });

        Assert.AreEqual("charv-1", characterVersion.Reference.VersionId);
        Assert.AreEqual("runtime-lock-sha256", characterVersion.RuntimeLock.RuntimeFingerprint);
        Assert.AreEqual("evt-1", ledger.Events[0].EventId);
        Assert.AreEqual(SessionEventTypes.TrackerIncrement, ledger.Events[0].EventType);
        Assert.AreEqual("overlay-1", snapshot.OverlayId);
        Assert.AreEqual("char-1", snapshot.BaseCharacterVersion.CharacterId);
        Assert.AreEqual(SessionSyncStatuses.PendingSync, snapshot.SyncState.Status);
        Assert.AreEqual(TrackerCategories.Condition, snapshot.Trackers[0].Definition.Category);
        Assert.AreEqual("session-bundle-1", runtimeBundle.BundleId);
        Assert.AreEqual("runtime-lock-sha256", runtimeBundle.BaseCharacterVersion.RuntimeFingerprint);
        Assert.AreEqual(10, runtimeBundle.Trackers[0].MaximumValue);
        Assert.AreEqual("session-runtime/stun.increment", runtimeBundle.ReducerBindings["tracker.increment"]);
    }

    [TestMethod]
    public void Linked_asset_taxonomy_distinguishes_contact_assets_from_character_sections()
    {
        LinkedAssetReference assetReference = new(
            AssetId: "contact-1",
            VersionId: "contactv-1",
            AssetType: "contact",
            Visibility: LinkedAssetVisibilityModes.Private);
        ContactAsset contactAsset = new(
            Reference: assetReference,
            Name: "Nines",
            Role: "Fixer",
            Location: "Seattle",
            Connection: 4,
            Loyalty: 3,
            Notes: "Reusable campaign contact.");
        CharacterContactLink contactLink = new(
            CharacterId: "char-1",
            Contact: assetReference,
            Overrides: new ContactLinkOverride(
                DisplayName: "Nines (Runner Team)",
                Loyalty: 4),
            IsFavorite: true);
        CharacterContactsSection contactsSection = new(
            Count: 1,
            Contacts:
            [
                new CharacterContactSummary(
                    Name: "Nines",
                    Role: "Fixer",
                    Location: "Seattle",
                    Connection: 4,
                    Loyalty: 3)
            ]);

        Assert.AreEqual("contact", contactAsset.Reference.AssetType);
        Assert.AreEqual("contact-1", contactLink.Contact.AssetId);
        Assert.IsTrue(contactLink.IsFavorite);
        Assert.AreEqual("Nines (Runner Team)", contactLink.Overrides.DisplayName);
        Assert.AreEqual(1, contactsSection.Count);
        Assert.AreEqual("Nines", contactsSection.Contacts[0].Name);
    }

    [TestMethod]
    public void Presentation_catalogs_support_ruleset_filtering_without_changing_sr5_defaults()
    {
        IReadOnlyList<AppCommandDefinition> sr5Commands = AppCommandCatalog.ForRuleset(null);
        IReadOnlyList<NavigationTabDefinition> sr5Tabs = NavigationTabCatalog.ForRuleset(RulesetDefaults.Sr5);
        IReadOnlyList<WorkspaceSurfaceActionDefinition> sr5Actions = WorkspaceSurfaceActionCatalog.ForRuleset(string.Empty);
        IReadOnlyList<DesktopUiControlDefinition> sr5Controls = DesktopUiControlCatalog.ForRuleset("SR5");

        Assert.IsGreaterThan(0, sr5Commands.Count);
        Assert.IsGreaterThan(0, sr5Tabs.Count);
        Assert.IsGreaterThan(0, sr5Actions.Count);
        Assert.IsGreaterThan(0, sr5Controls.Count);

        Assert.IsFalse(AppCommandCatalog.ForRuleset("sr6").Any());
        Assert.IsFalse(AppCommandCatalog.ForRuleset("sr4").Any());
        Assert.IsFalse(NavigationTabCatalog.ForRuleset("sr6").Any());
        Assert.IsFalse(NavigationTabCatalog.ForRuleset("sr4").Any());
        Assert.IsFalse(WorkspaceSurfaceActionCatalog.ForRuleset("sr6").Any());
        Assert.IsFalse(WorkspaceSurfaceActionCatalog.ForRuleset("sr4").Any());
        Assert.IsFalse(DesktopUiControlCatalog.ForRuleset("sr6").Any());
        Assert.IsFalse(DesktopUiControlCatalog.ForRuleset("sr4").Any());
        Assert.IsFalse(WorkspaceSurfaceActionCatalog.ForTab("tab-info", "sr6").Any());
        Assert.IsFalse(WorkspaceSurfaceActionCatalog.ForTab("tab-info", "sr4").Any());
        Assert.IsFalse(DesktopUiControlCatalog.ForTab("tab-info", "sr6").Any());
        Assert.IsFalse(DesktopUiControlCatalog.ForTab("tab-info", "sr4").Any());
    }

    [TestMethod]
    public void Ruleset_plugin_contracts_are_declared_for_serializer_shell_catalog_rule_and_script_hosts()
    {
        Assert.IsTrue(typeof(IRulesetPlugin).IsInterface);
        Assert.IsTrue(typeof(IRulesetPluginRegistry).IsInterface);
        Assert.IsTrue(typeof(IRulesetSelectionPolicy).IsInterface);
        Assert.IsTrue(typeof(IRulesetShellCatalogResolver).IsInterface);
        Assert.IsTrue(typeof(IRulesetSerializer).IsInterface);
        Assert.IsTrue(typeof(IRulesetShellDefinitionProvider).IsInterface);
        Assert.IsTrue(typeof(IRulesetCatalogProvider).IsInterface);
        Assert.IsTrue(typeof(IRulesetRuleHost).IsInterface);
        Assert.IsTrue(typeof(IRulesetScriptHost).IsInterface);
    }

    [TestMethod]
    public void Ruleset_explain_contracts_capture_provider_traces_and_gas_usage()
    {
        RulesetGasBudget gasBudget = new(
            ProviderInstructionLimit: 5000,
            RequestInstructionLimit: 20000,
            MemoryBytesLimit: 1_048_576,
            WallClockLimit: TimeSpan.FromSeconds(1));
        RulesetExecutionOptions options = new(
            Explain: true,
            GasBudget: gasBudget);
        RulesetExplainTrace explainTrace = new(
            SubjectId: "derive.stat",
            Providers:
            [
                new RulesetProviderTrace(
                    ProviderId: "sr5/derive.stat.body",
                    CapabilityId: RulePackCapabilityIds.DeriveStat,
                    PackId: "house-rules",
                    Success: true,
                    ExplainFragments:
                    [
                        new RulesetExplainFragment(
                            Label: "Base Body",
                            Value: "3",
                            Reason: "Metatype base value.",
                            PackId: "sr5-core",
                            ProviderId: "sr5/derive.stat.body")
                    ],
                    GasUsage: new RulesetGasUsage(
                        ProviderInstructionsConsumed: 120,
                        RequestInstructionsConsumed: 120,
                        PeakMemoryBytes: 4096),
                    Messages: ["Derived body successfully."])
            ],
            Messages: ["Trace captured."],
            AggregateGasUsage: new RulesetGasUsage(
                ProviderInstructionsConsumed: 120,
                RequestInstructionsConsumed: 120,
                PeakMemoryBytes: 4096));
        RulesetRuleEvaluationRequest ruleRequest = new(
            RuleId: "derive.stat.body",
            Inputs: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["metatype"] = "human"
            },
            Options: options);
        RulesetRuleEvaluationResult ruleResult = new(
            Success: true,
            Outputs: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["value"] = 3
            },
            Messages: ["ok"],
            Explain: explainTrace);
        RulesetExplainTrace scriptExplainTrace = new(
            SubjectId: "session.quick-actions",
            Providers:
            [
                new RulesetProviderTrace(
                    ProviderId: "sr5/session.quick-actions",
                    CapabilityId: RulePackCapabilityIds.SessionQuickActions,
                    PackId: "house-rules",
                    Success: true,
                    ExplainFragments:
                    [
                        new RulesetExplainFragment(
                            Label: "Quick Action Set",
                            Value: "2",
                            Reason: "Pinned quick actions are session-safe.",
                            PackId: "house-rules",
                            ProviderId: "sr5/session.quick-actions")
                    ],
                    GasUsage: new RulesetGasUsage(
                        ProviderInstructionsConsumed: 80,
                        RequestInstructionsConsumed: 80,
                        PeakMemoryBytes: 2048),
                    Messages: ["Prepared quick actions."])
            ],
            Messages: ["Script trace captured."],
            AggregateGasUsage: new RulesetGasUsage(
                ProviderInstructionsConsumed: 80,
                RequestInstructionsConsumed: 80,
                PeakMemoryBytes: 2048));
        RulesetScriptExecutionRequest scriptRequest = new(
            ScriptId: "sr5/session.quick-actions",
            ScriptSource: "-- compiled provider reference",
            Inputs: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["runtimeFingerprint"] = "runtime-lock-sha256"
            },
            Options: options);
        RulesetScriptExecutionResult scriptResult = new(
            Success: true,
            Error: null,
            Outputs: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["quickActions"] = 2
            },
            Explain: scriptExplainTrace);

        Assert.IsTrue(ruleRequest.Options?.Explain);
        Assert.AreEqual(5000, ruleRequest.Options?.GasBudget?.ProviderInstructionLimit);
        Assert.AreEqual(RulePackCapabilityIds.DeriveStat, ruleResult.Explain?.Providers[0].CapabilityId);
        Assert.AreEqual(120, ruleResult.Explain?.AggregateGasUsage.RequestInstructionsConsumed);
        Assert.AreEqual("derive.stat", ruleResult.Explain?.SubjectId);
        Assert.IsTrue(scriptRequest.Options?.Explain);
        Assert.AreEqual("sr5/session.quick-actions", scriptResult.Explain?.Providers[0].ProviderId);
        Assert.AreEqual("house-rules", scriptResult.Explain?.Providers[0].PackId);
    }

    [TestMethod]
    public void Ruleset_defaults_expose_sr4_sr5_and_sr6_ids()
    {
        Assert.AreEqual(string.Empty, RulesetId.Default.NormalizedValue);
        Assert.AreEqual("sr4", new RulesetId(RulesetDefaults.Sr4).NormalizedValue);
        Assert.AreEqual("sr5", new RulesetId(RulesetDefaults.Sr5).NormalizedValue);
        Assert.AreEqual("sr6", new RulesetId(RulesetDefaults.Sr6).NormalizedValue);
    }

    [TestMethod]
    public void Ruleset_defaults_only_expose_explicit_normalization_helpers()
    {
        Assert.IsNull(typeof(RulesetDefaults).GetMethod("Normalize", [typeof(string)]));
        Assert.IsNotNull(typeof(RulesetDefaults).GetMethod(nameof(RulesetDefaults.NormalizeOptional)));
        Assert.IsNotNull(typeof(RulesetDefaults).GetMethod(nameof(RulesetDefaults.NormalizeRequired)));
        Assert.IsNull(typeof(RulesetDefaults).GetMethod("NormalizeOrDefault", [typeof(string), typeof(string)]));
        Assert.IsNull(RulesetDefaults.NormalizeOptional(" "));
        Assert.AreEqual(RulesetDefaults.Sr4, RulesetDefaults.NormalizeRequired(" SR4 "));
        Assert.AreEqual(
            RulesetDefaults.Sr6,
            RulesetDefaults.NormalizeOptional(null) ?? RulesetDefaults.NormalizeRequired(RulesetDefaults.Sr6));
    }

    [TestMethod]
    public void Ruleset_workspace_codecs_require_explicit_ruleset_id_for_wrap_import()
    {
        IRulesetWorkspaceCodec[] codecs =
        [
            new Sr4WorkspaceCodec(),
            new Sr5WorkspaceCodec(
                new XmlCharacterFileQueries(new CharacterFileService()),
                new XmlCharacterSectionQueries(new CharacterSectionService()),
                new XmlCharacterMetadataCommands(new CharacterFileService())),
            new Sr6WorkspaceCodec()
        ];

        foreach (IRulesetWorkspaceCodec codec in codecs)
        {
            Assert.ThrowsExactly<ArgumentException>(() => codec.WrapImport(
                string.Empty,
                new WorkspaceImportDocument("<character />", string.Empty)));
        }
    }

    [TestMethod]
    public void Sr4_ruleset_registration_is_opt_in()
    {
        ServiceCollection services = new();
        services.AddSr4Ruleset();

        using ServiceProvider provider = services.BuildServiceProvider();
        IRulesetPlugin[] plugins = provider.GetServices<IRulesetPlugin>().ToArray();
        IRulesetWorkspaceCodec[] codecs = provider.GetServices<IRulesetWorkspaceCodec>().ToArray();

        Assert.IsTrue(plugins.Any(plugin => string.Equals(plugin.Id.NormalizedValue, RulesetDefaults.Sr4, StringComparison.Ordinal)));
        Assert.IsTrue(codecs.Any(codec => string.Equals(codec.RulesetId, RulesetDefaults.Sr4, StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Sr5_plugin_adapters_expose_existing_shell_catalogs_without_behavior_change()
    {
        Sr5RulesetPlugin plugin = new();

        Assert.AreEqual(RulesetDefaults.Sr5, plugin.Id.NormalizedValue);
        Assert.AreEqual("Shadowrun 5", plugin.DisplayName);
        Assert.AreEqual(RulesetDefaults.Sr5, plugin.Serializer.RulesetId.NormalizedValue);
        Assert.AreEqual(1, plugin.Serializer.SchemaVersion);

        WorkspacePayloadEnvelope envelope = plugin.Serializer.Wrap("workspace", "{}");
        Assert.AreEqual(RulesetDefaults.Sr5, envelope.RulesetId);
        Assert.AreEqual("workspace", envelope.PayloadKind);
        Assert.AreEqual("{}", envelope.Payload);

        Assert.IsGreaterThan(0, plugin.ShellDefinitions.GetCommands().Count);
        Assert.IsGreaterThan(0, plugin.ShellDefinitions.GetNavigationTabs().Count);
        Assert.IsGreaterThan(0, plugin.Catalogs.GetWorkspaceActions().Count);
        Assert.IsGreaterThan(0, plugin.Catalogs.GetDesktopUiControls().Count);

        RulesetRuleEvaluationResult ruleResult = await plugin.Rules.EvaluateAsync(
            new RulesetRuleEvaluationRequest(
                RuleId: "sr5.noop",
                Inputs: new Dictionary<string, object?> { ["karma"] = 12 }),
            CancellationToken.None);
        Assert.IsTrue(ruleResult.Success);
        Assert.IsTrue(ruleResult.Outputs.ContainsKey("karma"));

        RulesetScriptExecutionResult scriptResult = await plugin.Scripts.ExecuteAsync(
            new RulesetScriptExecutionRequest(
                ScriptId: "sr5.noop",
                ScriptSource: "-- noop",
                Inputs: new Dictionary<string, object?> { ["nuyen"] = 5000 }),
            CancellationToken.None);
        Assert.IsTrue(scriptResult.Success);
        Assert.AreEqual("noop", scriptResult.Outputs["mode"]);
    }

    [TestMethod]
    public async Task Sr6_plugin_skeleton_exposes_independent_catalogs_and_codec_contracts()
    {
        Sr6RulesetPlugin plugin = new();
        Sr6WorkspaceCodec codec = new();

        Assert.AreEqual(RulesetDefaults.Sr6, plugin.Id.NormalizedValue);
        Assert.AreEqual("Shadowrun 6", plugin.DisplayName);
        Assert.AreEqual(RulesetDefaults.Sr6, plugin.Serializer.RulesetId.NormalizedValue);
        Assert.AreEqual(Sr6WorkspaceCodec.SchemaVersion, plugin.Serializer.SchemaVersion);
        Assert.AreEqual(RulesetDefaults.Sr6, codec.RulesetId);
        Assert.AreEqual(Sr6WorkspaceCodec.Sr6PayloadKind, codec.PayloadKind);

        WorkspacePayloadEnvelope wrapped = codec.WrapImport(
            RulesetDefaults.Sr6,
            new WorkspaceImportDocument("<character><name>Switchback</name><alias>Ghost</alias></character>", RulesetDefaults.Sr6));
        CharacterFileSummary summary = codec.ParseSummary(wrapped);

        Assert.AreEqual(RulesetDefaults.Sr6, wrapped.RulesetId);
        Assert.AreEqual("Switchback", summary.Name);
        Assert.AreEqual("Ghost", summary.Alias);
        Assert.IsGreaterThan(0, plugin.ShellDefinitions.GetCommands().Count);
        Assert.IsGreaterThan(0, plugin.ShellDefinitions.GetNavigationTabs().Count);
        Assert.IsGreaterThan(0, plugin.Catalogs.GetWorkspaceActions().Count);
        Assert.IsGreaterThan(0, plugin.Catalogs.GetDesktopUiControls().Count);

        WorkspaceDownloadReceipt download = codec.BuildDownload(
            new CharacterWorkspaceId("ws-sr6"),
            wrapped,
            WorkspaceDocumentFormat.NativeXml);
        Assert.AreEqual("ws-sr6.chum6", download.FileName);

        RulesetRuleEvaluationResult ruleResult = await plugin.Rules.EvaluateAsync(
            new RulesetRuleEvaluationRequest(
                RuleId: "sr6.noop",
                Inputs: new Dictionary<string, object?> { ["edge"] = 2 }),
            CancellationToken.None);
        Assert.IsTrue(ruleResult.Success);
        Assert.IsTrue(ruleResult.Outputs.ContainsKey("edge"));

        RulesetScriptExecutionResult scriptResult = await plugin.Scripts.ExecuteAsync(
            new RulesetScriptExecutionRequest(
                ScriptId: "sr6.noop",
                ScriptSource: "// noop",
                Inputs: new Dictionary<string, object?> { ["essence"] = 5.8m }),
            CancellationToken.None);
        Assert.IsTrue(scriptResult.Success);
        Assert.AreEqual(RulesetDefaults.Sr6, scriptResult.Outputs["rulesetId"]);
    }

    [TestMethod]
    public async Task Sr4_plugin_scaffold_exposes_independent_catalogs_and_codec_contracts()
    {
        Sr4RulesetPlugin plugin = new();
        Sr4WorkspaceCodec codec = new();

        Assert.AreEqual(RulesetDefaults.Sr4, plugin.Id.NormalizedValue);
        Assert.AreEqual("Shadowrun 4", plugin.DisplayName);
        Assert.AreEqual(RulesetDefaults.Sr4, plugin.Serializer.RulesetId.NormalizedValue);
        Assert.AreEqual(Sr4WorkspaceCodec.SchemaVersion, plugin.Serializer.SchemaVersion);
        Assert.AreEqual(RulesetDefaults.Sr4, codec.RulesetId);
        Assert.AreEqual(Sr4WorkspaceCodec.Sr4PayloadKind, codec.PayloadKind);

        WorkspacePayloadEnvelope wrapped = codec.WrapImport(
            RulesetDefaults.Sr4,
            new WorkspaceImportDocument("<character><name>Ghost</name><alias>Switchback</alias></character>", RulesetDefaults.Sr4));
        CharacterFileSummary summary = codec.ParseSummary(wrapped);

        Assert.AreEqual(RulesetDefaults.Sr4, wrapped.RulesetId);
        Assert.AreEqual("Ghost", summary.Name);
        Assert.AreEqual("Switchback", summary.Alias);
        Assert.IsGreaterThan(0, plugin.ShellDefinitions.GetCommands().Count);
        Assert.IsGreaterThan(0, plugin.ShellDefinitions.GetNavigationTabs().Count);
        Assert.IsGreaterThan(0, plugin.Catalogs.GetWorkspaceActions().Count);
        Assert.IsGreaterThan(0, plugin.Catalogs.GetDesktopUiControls().Count);

        WorkspaceDownloadReceipt download = codec.BuildDownload(
            new CharacterWorkspaceId("ws-sr4"),
            wrapped,
            WorkspaceDocumentFormat.NativeXml);
        Assert.AreEqual("ws-sr4.chum4", download.FileName);

        RulesetRuleEvaluationResult ruleResult = await plugin.Rules.EvaluateAsync(
            new RulesetRuleEvaluationRequest(
                RuleId: "sr4.noop",
                Inputs: new Dictionary<string, object?> { ["essence"] = 5.5m }),
            CancellationToken.None);
        Assert.IsTrue(ruleResult.Success);
        Assert.IsTrue(ruleResult.Outputs.ContainsKey("essence"));

        RulesetScriptExecutionResult scriptResult = await plugin.Scripts.ExecuteAsync(
            new RulesetScriptExecutionRequest(
                ScriptId: "sr4.noop",
                ScriptSource: "-- noop",
                Inputs: new Dictionary<string, object?> { ["karma"] = 9 }),
            CancellationToken.None);
        Assert.IsTrue(scriptResult.Success);
        Assert.AreEqual(RulesetDefaults.Sr4, scriptResult.Outputs["rulesetId"]);
    }
}
