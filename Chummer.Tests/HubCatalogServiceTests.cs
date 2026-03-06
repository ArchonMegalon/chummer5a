#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chummer.Application.Content;
using Chummer.Application.Hub;
using Chummer.Contracts.Content;
using Chummer.Contracts.Hub;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Rulesets.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class HubCatalogServiceTests
{
    [TestMethod]
    public void Hub_catalog_service_aggregates_rulepacks_buildkits_profiles_and_runtime_locks()
    {
        DefaultHubCatalogService service = CreateService();

        HubCatalogResultPage page = service.Search(
            OwnerScope.LocalSingleUser,
            new BrowseQuery(
                QueryText: string.Empty,
                FacetSelections: new Dictionary<string, IReadOnlyList<string>>(),
                SortId: HubCatalogSortIds.Title));

        Assert.AreEqual(5, page.TotalCount);
        Assert.IsTrue(page.Items.Any(item => item.Kind == HubCatalogItemKinds.RulePack));
        Assert.IsTrue(page.Items.Any(item => item.Kind == HubCatalogItemKinds.RuleProfile));
        Assert.IsTrue(page.Items.Any(item => item.Kind == HubCatalogItemKinds.BuildKit));
        Assert.IsTrue(page.Items.Any(item => item.Kind == HubCatalogItemKinds.RuntimeLock));
        Assert.IsTrue(page.Facets.Any(facet => facet.FacetId == HubCatalogFacetIds.Kind));
    }

    [TestMethod]
    public void Hub_catalog_service_returns_project_details_for_registered_catalog_kinds()
    {
        DefaultHubCatalogService service = CreateService();

        HubProjectDetailProjection? rulePack = service.GetProjectDetail(OwnerScope.LocalSingleUser, HubCatalogItemKinds.RulePack, "house-rules", RulesetDefaults.Sr5);
        HubProjectDetailProjection? buildKit = service.GetProjectDetail(OwnerScope.LocalSingleUser, HubCatalogItemKinds.BuildKit, "street-sam-starter", RulesetDefaults.Sr5);
        HubProjectDetailProjection? ruleProfile = service.GetProjectDetail(OwnerScope.LocalSingleUser, HubCatalogItemKinds.RuleProfile, "official.sr5.core", RulesetDefaults.Sr5);
        HubProjectDetailProjection? runtimeLock = service.GetProjectDetail(OwnerScope.LocalSingleUser, HubCatalogItemKinds.RuntimeLock, "sha256:core", RulesetDefaults.Sr5);

        Assert.IsNotNull(rulePack);
        Assert.AreEqual(HubCatalogItemKinds.RulePack, rulePack.Summary.Kind);
        Assert.AreEqual(RulePackPublicationStatuses.Published, rulePack.PublicationStatus);
        Assert.IsTrue(rulePack.Facts.Any(fact => fact.FactId == "engine-api"));

        Assert.IsNotNull(buildKit);
        Assert.AreEqual(HubCatalogItemKinds.BuildKit, buildKit.Summary.Kind);
        Assert.AreEqual(BuildKitPublicationStatuses.Published, buildKit.PublicationStatus);
        Assert.IsTrue(buildKit.Dependencies.Any(dependency => dependency.Kind == HubProjectDependencyKinds.RequiresRulePack));

        Assert.IsNotNull(ruleProfile);
        Assert.AreEqual(HubCatalogItemKinds.RuleProfile, ruleProfile.Summary.Kind);
        Assert.AreEqual("sha256:core", ruleProfile.RuntimeFingerprint);
        Assert.AreEqual(ArtifactInstallStates.Available, ruleProfile.Summary.InstallState);
        Assert.IsTrue(ruleProfile.Actions.Any(action => action.Kind == HubProjectActionKinds.InspectRuntime));

        Assert.IsNotNull(runtimeLock);
        Assert.AreEqual(HubCatalogItemKinds.RuntimeLock, runtimeLock.Summary.Kind);
        Assert.AreEqual(RuntimeLockCatalogKinds.Published, runtimeLock.CatalogKind);
        Assert.AreEqual("sha256:core", runtimeLock.RuntimeFingerprint);
    }

    private static DefaultHubCatalogService CreateService() => new(
        new RulesetPluginRegistry(
        [
            new HubRulesetPluginStub(RulesetDefaults.Sr5, "Shadowrun Fifth Edition"),
            new HubRulesetPluginStub(RulesetDefaults.Sr6, "Shadowrun Sixth Edition")
        ]),
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
                    Assets: [],
                    Capabilities: [],
                    ExecutionPolicies: []),
                new RulePackPublicationMetadata(
                    OwnerId: "local-single-user",
                    Visibility: ArtifactVisibilityModes.LocalOnly,
                    PublicationStatus: RulePackPublicationStatuses.Published,
                    Review: new RulePackReviewDecision(RulePackReviewStates.NotRequired),
                    Shares: []),
                new ArtifactInstallState(ArtifactInstallStates.Installed))
        ]),
        new RuleProfileRegistryServiceStub(
        [
            new RuleProfileRegistryEntry(
                new RuleProfileManifest(
                    ProfileId: "official.sr5.core",
                    Title: "Official SR5 Core",
                    Description: "Curated runtime.",
                    RulesetId: RulesetDefaults.Sr5,
                    Audience: RuleProfileAudienceKinds.General,
                    CatalogKind: RuleProfileCatalogKinds.Official,
                    RulePacks: [],
                    DefaultToggles: [],
                    RuntimeLock: new ResolvedRuntimeLock(
                        RulesetId: RulesetDefaults.Sr5,
                        ContentBundles: [],
                        RulePacks: [],
                        ProviderBindings: new Dictionary<string, string>(),
                        EngineApiVersion: "rulepack-v1",
                        RuntimeFingerprint: "sha256:core"),
                    UpdateChannel: RuleProfileUpdateChannels.Stable),
                new RuleProfilePublicationMetadata(
                    OwnerId: "system",
                    Visibility: ArtifactVisibilityModes.Public,
                    PublicationStatus: RuleProfilePublicationStatuses.Published,
                    Review: new RulePackReviewDecision(RulePackReviewStates.NotRequired),
                    Shares: []),
                new ArtifactInstallState(ArtifactInstallStates.Available))
        ]),
        new BuildKitRegistryServiceStub(
        [
            new BuildKitRegistryEntry(
                new BuildKitManifest(
                    BuildKitId: "street-sam-starter",
                    Version: "1.0.0",
                    Title: "Street Sam Starter",
                    Description: "Starter template.",
                    Targets: [RulesetDefaults.Sr5],
                    RuntimeRequirements:
                    [
                        new BuildKitRuntimeRequirement(
                            RulesetId: RulesetDefaults.Sr5,
                            RequiredRuntimeFingerprints: ["sha256:core"],
                            RequiredRulePacks: [new ArtifactVersionReference("house-rules", "1.0.0")])
                    ],
                    Prompts:
                    [
                        new BuildKitPromptDescriptor(
                            PromptId: "focus",
                            Kind: BuildKitPromptKinds.Choice,
                            Label: "Combat Focus",
                            Options: [new BuildKitPromptOption("street-sam", "Street Sam")],
                            Required: true)
                    ],
                    Actions:
                    [
                        new BuildKitActionDescriptor(
                            ActionId: "starter-bundle",
                            Kind: BuildKitActionKinds.AddBundle,
                            TargetId: "starter-bundle")
                    ],
                    Visibility: ArtifactVisibilityModes.Public,
                    TrustTier: ArtifactTrustTiers.Curated),
                Owner: new OwnerScope("system"),
                Visibility: ArtifactVisibilityModes.Public,
                PublicationStatus: BuildKitPublicationStatuses.Published,
                UpdatedAtUtc: DateTimeOffset.UtcNow)
        ]),
        new RuntimeLockRegistryServiceStub(
            new RuntimeLockRegistryPage(
            [
                new RuntimeLockRegistryEntry(
                    LockId: "sha256:core",
                    Owner: new OwnerScope("system"),
                    Title: "Official SR5 Core Runtime Lock",
                    Visibility: ArtifactVisibilityModes.Public,
                    CatalogKind: RuntimeLockCatalogKinds.Published,
                    RuntimeLock: new ResolvedRuntimeLock(
                        RulesetId: RulesetDefaults.Sr5,
                        ContentBundles: [],
                        RulePacks: [],
                        ProviderBindings: new Dictionary<string, string>(),
                        EngineApiVersion: "rulepack-v1",
                        RuntimeFingerprint: "sha256:core"),
                    UpdatedAtUtc: DateTimeOffset.UtcNow)
            ],
                TotalCount: 1)));

    private sealed class RulePackRegistryServiceStub : IRulePackRegistryService
    {
        private readonly IReadOnlyList<RulePackRegistryEntry> _entries;

        public RulePackRegistryServiceStub(IReadOnlyList<RulePackRegistryEntry> entries)
        {
            _entries = entries;
        }

        public IReadOnlyList<RulePackRegistryEntry> List(OwnerScope owner, string? rulesetId = null)
        {
            if (string.IsNullOrWhiteSpace(rulesetId))
            {
                return _entries;
            }

            return _entries.Where(entry => entry.Manifest.Targets.Contains(rulesetId, StringComparer.Ordinal)).ToArray();
        }

        public RulePackRegistryEntry? Get(OwnerScope owner, string packId, string? rulesetId = null) =>
            _entries.FirstOrDefault(entry => entry.Manifest.PackId == packId);
    }

    private sealed class RuleProfileRegistryServiceStub : IRuleProfileRegistryService
    {
        private readonly IReadOnlyList<RuleProfileRegistryEntry> _entries;

        public RuleProfileRegistryServiceStub(IReadOnlyList<RuleProfileRegistryEntry> entries)
        {
            _entries = entries;
        }

        public IReadOnlyList<RuleProfileRegistryEntry> List(OwnerScope owner, string? rulesetId = null) => _entries;

        public RuleProfileRegistryEntry? Get(OwnerScope owner, string profileId, string? rulesetId = null) =>
            _entries.FirstOrDefault(entry => entry.Manifest.ProfileId == profileId);
    }

    private sealed class RuntimeLockRegistryServiceStub : IRuntimeLockRegistryService
    {
        private readonly RuntimeLockRegistryPage _page;

        public RuntimeLockRegistryServiceStub(RuntimeLockRegistryPage page)
        {
            _page = page;
        }

        public RuntimeLockRegistryPage List(OwnerScope owner, string? rulesetId = null) => _page;

        public RuntimeLockRegistryEntry? Get(OwnerScope owner, string lockId, string? rulesetId = null) =>
            _page.Entries.FirstOrDefault(entry => entry.LockId == lockId);
    }

    private sealed class BuildKitRegistryServiceStub : IBuildKitRegistryService
    {
        private readonly IReadOnlyList<BuildKitRegistryEntry> _entries;

        public BuildKitRegistryServiceStub(IReadOnlyList<BuildKitRegistryEntry> entries)
        {
            _entries = entries;
        }

        public IReadOnlyList<BuildKitRegistryEntry> List(OwnerScope owner, string? rulesetId = null) => _entries;

        public BuildKitRegistryEntry? Get(OwnerScope owner, string buildKitId, string? rulesetId = null) =>
            _entries.FirstOrDefault(entry => entry.Manifest.BuildKitId == buildKitId);
    }

    private sealed class HubRulesetPluginStub : IRulesetPlugin
    {
        public HubRulesetPluginStub(string rulesetId, string displayName)
        {
            Id = new RulesetId(rulesetId);
            DisplayName = displayName;
            Serializer = new HubRulesetSerializerStub(Id);
            ShellDefinitions = new HubShellDefinitionProviderStub();
            Catalogs = new HubCatalogProviderStub();
            Rules = new HubRuleHostStub();
            Scripts = new HubScriptHostStub();
        }

        public RulesetId Id { get; }

        public string DisplayName { get; }

        public IRulesetSerializer Serializer { get; }

        public IRulesetShellDefinitionProvider ShellDefinitions { get; }

        public IRulesetCatalogProvider Catalogs { get; }

        public IRulesetRuleHost Rules { get; }

        public IRulesetScriptHost Scripts { get; }
    }

    private sealed class HubRulesetSerializerStub : IRulesetSerializer
    {
        public HubRulesetSerializerStub(RulesetId rulesetId)
        {
            RulesetId = rulesetId;
        }

        public RulesetId RulesetId { get; }

        public int SchemaVersion => 1;

        public WorkspacePayloadEnvelope Wrap(string payloadKind, string payload) => new(RulesetId.NormalizedValue, SchemaVersion, payloadKind, payload);
    }

    private sealed class HubShellDefinitionProviderStub : IRulesetShellDefinitionProvider
    {
        public IReadOnlyList<AppCommandDefinition> GetCommands() => [];

        public IReadOnlyList<NavigationTabDefinition> GetNavigationTabs() => [];
    }

    private sealed class HubCatalogProviderStub : IRulesetCatalogProvider
    {
        public IReadOnlyList<WorkspaceSurfaceActionDefinition> GetWorkspaceActions() => [];

        public IReadOnlyList<DesktopUiControlDefinition> GetDesktopUiControls() => [];
    }

    private sealed class HubRuleHostStub : IRulesetRuleHost
    {
        public ValueTask<RulesetRuleEvaluationResult> EvaluateAsync(RulesetRuleEvaluationRequest request, CancellationToken ct) =>
            ValueTask.FromResult(new RulesetRuleEvaluationResult(true, new Dictionary<string, object?>(), []));
    }

    private sealed class HubScriptHostStub : IRulesetScriptHost
    {
        public ValueTask<RulesetScriptExecutionResult> ExecuteAsync(RulesetScriptExecutionRequest request, CancellationToken ct) =>
            ValueTask.FromResult(new RulesetScriptExecutionResult(true, null, new Dictionary<string, object?>()));
    }
}
