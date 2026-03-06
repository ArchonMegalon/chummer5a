#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chummer.Application.Content;
using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Rulesets.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class RuleProfileRegistryServiceTests
{
    [TestMethod]
    public void Default_registry_service_projects_core_and_overlay_profiles_from_rulesets_and_rulepacks()
    {
        RulesetPluginRegistry pluginRegistry =
            new([
                new StubRulesetPlugin(RulesetDefaults.Sr5, "Shadowrun Fifth Edition", schemaVersion: 5),
                new StubRulesetPlugin(RulesetDefaults.Sr6, "Shadowrun Sixth Edition", schemaVersion: 6)
            ]);
        DefaultRuleProfileRegistryService service = new(
            pluginRegistry,
            new RulePackRegistryServiceStub(
            [
                new RulePackRegistryEntry(
                    new RulePackManifest(
                        PackId: "house-rules",
                        Version: "1.0.0",
                        Title: "House Rules",
                        Author: "GM",
                        Description: "Campaign overlay.",
                        Targets: [RulesetDefaults.Sr5],
                        EngineApiVersion: "rulepack-v1",
                        DependsOn: [],
                        ConflictsWith: [],
                        Visibility: ArtifactVisibilityModes.LocalOnly,
                        TrustTier: ArtifactTrustTiers.LocalOnly,
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
                                CapabilityId: RulePackCapabilityIds.ContentCatalog,
                                AssetKind: RulePackAssetKinds.Xml,
                                AssetMode: RulePackAssetModes.MergeCatalog)
                        ],
                        ExecutionPolicies: []),
                    new RulePackPublicationMetadata(
                        OwnerId: "system",
                        Visibility: ArtifactVisibilityModes.LocalOnly,
                        PublicationStatus: RulePackPublicationStatuses.Published,
                        Review: new RulePackReviewDecision(RulePackReviewStates.NotRequired),
                        Shares: []))
            ]),
            new DefaultRuntimeFingerprintService());

        IReadOnlyList<RuleProfileRegistryEntry> entries = service.List(OwnerScope.LocalSingleUser);

        Assert.HasCount(3, entries);
        Assert.IsNotNull(entries.SingleOrDefault(entry => string.Equals(entry.Manifest.ProfileId, "official.sr5.core", StringComparison.Ordinal)));
        RuleProfileRegistryEntry? overlayProfile = entries.SingleOrDefault(entry => string.Equals(entry.Manifest.ProfileId, "local.sr5.current-overlays", StringComparison.Ordinal));
        Assert.IsNotNull(overlayProfile);
        Assert.HasCount(1, overlayProfile.Manifest.RulePacks);
        Assert.AreEqual("house-rules", overlayProfile.Manifest.RulePacks[0].RulePack.Id);
        Assert.AreEqual(RuleProfileCatalogKinds.Personal, overlayProfile.Manifest.CatalogKind);
    }

    [TestMethod]
    public void Default_registry_service_returns_null_for_unknown_profile()
    {
        DefaultRuleProfileRegistryService service = new(
            new RulesetPluginRegistry([new StubRulesetPlugin(RulesetDefaults.Sr5, "Shadowrun Fifth Edition", schemaVersion: 5)]),
            new RulePackRegistryServiceStub([]),
            new DefaultRuntimeFingerprintService());

        RuleProfileRegistryEntry? entry = service.Get(OwnerScope.LocalSingleUser, "missing-profile", RulesetDefaults.Sr5);

        Assert.IsNull(entry);
    }

    [TestMethod]
    public void Default_registry_service_runtime_fingerprint_tracks_rulepack_asset_checksums()
    {
        DefaultRuleProfileRegistryService checksumA = CreateServiceWithRulePackChecksum("sha256:abc");
        DefaultRuleProfileRegistryService checksumB = CreateServiceWithRulePackChecksum("sha256:def");

        string fingerprintA = checksumA.Get(OwnerScope.LocalSingleUser, "local.sr5.current-overlays", RulesetDefaults.Sr5)!.Manifest.RuntimeLock.RuntimeFingerprint;
        string fingerprintB = checksumB.Get(OwnerScope.LocalSingleUser, "local.sr5.current-overlays", RulesetDefaults.Sr5)!.Manifest.RuntimeLock.RuntimeFingerprint;

        Assert.AreNotEqual(fingerprintA, fingerprintB);
    }

    private static DefaultRuleProfileRegistryService CreateServiceWithRulePackChecksum(string checksum)
    {
        return new DefaultRuleProfileRegistryService(
            new RulesetPluginRegistry([new StubRulesetPlugin(RulesetDefaults.Sr5, "Shadowrun Fifth Edition", schemaVersion: 5)]),
            new RulePackRegistryServiceStub(
            [
                new RulePackRegistryEntry(
                    new RulePackManifest(
                        PackId: "house-rules",
                        Version: "1.0.0",
                        Title: "House Rules",
                        Author: "GM",
                        Description: "Campaign overlay.",
                        Targets: [RulesetDefaults.Sr5],
                        EngineApiVersion: "rulepack-v1",
                        DependsOn: [],
                        ConflictsWith: [],
                        Visibility: ArtifactVisibilityModes.LocalOnly,
                        TrustTier: ArtifactTrustTiers.LocalOnly,
                        Assets:
                        [
                            new RulePackAssetDescriptor(
                                Kind: RulePackAssetKinds.Xml,
                                Mode: RulePackAssetModes.MergeCatalog,
                                RelativePath: "data/qualities.xml",
                                Checksum: checksum)
                        ],
                        Capabilities:
                        [
                            new RulePackCapabilityDescriptor(
                                CapabilityId: RulePackCapabilityIds.ContentCatalog,
                                AssetKind: RulePackAssetKinds.Xml,
                                AssetMode: RulePackAssetModes.MergeCatalog)
                        ],
                        ExecutionPolicies: []),
                    new RulePackPublicationMetadata(
                        OwnerId: "system",
                        Visibility: ArtifactVisibilityModes.LocalOnly,
                        PublicationStatus: RulePackPublicationStatuses.Published,
                        Review: new RulePackReviewDecision(RulePackReviewStates.NotRequired),
                        Shares: []))
            ]),
            new DefaultRuntimeFingerprintService());
    }

    private sealed class RulePackRegistryServiceStub : IRulePackRegistryService
    {
        private readonly IReadOnlyList<RulePackRegistryEntry> _entries;

        public RulePackRegistryServiceStub(IReadOnlyList<RulePackRegistryEntry> entries)
        {
            _entries = entries;
        }

        public IReadOnlyList<RulePackRegistryEntry> List(OwnerScope owner, string? rulesetId = null)
        {
            string? normalizedRulesetId = RulesetDefaults.NormalizeOptional(rulesetId);
            return normalizedRulesetId is null
                ? _entries
                : _entries.Where(entry => entry.Manifest.Targets.Contains(normalizedRulesetId, StringComparer.Ordinal)).ToArray();
        }

        public RulePackRegistryEntry? Get(OwnerScope owner, string packId, string? rulesetId = null)
        {
            return List(owner, rulesetId)
                .FirstOrDefault(entry => string.Equals(entry.Manifest.PackId, packId, StringComparison.Ordinal));
        }
    }

    private sealed class StubRulesetPlugin : IRulesetPlugin
    {
        public StubRulesetPlugin(string id, string displayName, int schemaVersion)
        {
            Id = new RulesetId(id);
            DisplayName = displayName;
            Serializer = new StubSerializer(Id, schemaVersion);
            ShellDefinitions = new StubShellDefinitions();
            Catalogs = new StubCatalogs();
            Rules = new StubRuleHost();
            Scripts = new StubScriptHost();
        }

        public RulesetId Id { get; }

        public string DisplayName { get; }

        public IRulesetSerializer Serializer { get; }

        public IRulesetShellDefinitionProvider ShellDefinitions { get; }

        public IRulesetCatalogProvider Catalogs { get; }

        public IRulesetRuleHost Rules { get; }

        public IRulesetScriptHost Scripts { get; }
    }

    private sealed class StubSerializer : IRulesetSerializer
    {
        public StubSerializer(RulesetId rulesetId, int schemaVersion)
        {
            RulesetId = rulesetId;
            SchemaVersion = schemaVersion;
        }

        public RulesetId RulesetId { get; }

        public int SchemaVersion { get; }

        public WorkspacePayloadEnvelope Wrap(string payloadKind, string payload) => new(RulesetId.NormalizedValue, SchemaVersion, payloadKind, payload);
    }

    private sealed class StubShellDefinitions : IRulesetShellDefinitionProvider
    {
        public IReadOnlyList<AppCommandDefinition> GetCommands() => [];

        public IReadOnlyList<NavigationTabDefinition> GetNavigationTabs() => [];
    }

    private sealed class StubCatalogs : IRulesetCatalogProvider
    {
        public IReadOnlyList<WorkspaceSurfaceActionDefinition> GetWorkspaceActions() => [];

        public IReadOnlyList<DesktopUiControlDefinition> GetDesktopUiControls() => [];
    }

    private sealed class StubRuleHost : IRulesetRuleHost
    {
        public ValueTask<RulesetRuleEvaluationResult> EvaluateAsync(RulesetRuleEvaluationRequest request, CancellationToken ct) =>
            ValueTask.FromResult(new RulesetRuleEvaluationResult(true, new Dictionary<string, object?>(), []));
    }

    private sealed class StubScriptHost : IRulesetScriptHost
    {
        public ValueTask<RulesetScriptExecutionResult> ExecuteAsync(RulesetScriptExecutionRequest request, CancellationToken ct) =>
            ValueTask.FromResult(new RulesetScriptExecutionResult(true, null, new Dictionary<string, object?>()));
    }
}
