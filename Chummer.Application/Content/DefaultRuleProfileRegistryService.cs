using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;

namespace Chummer.Application.Content;

public sealed class DefaultRuleProfileRegistryService : IRuleProfileRegistryService
{
    private const string EngineApiVersion = "rulepack-v1";
    private const string SystemOwnerId = "system";
    private readonly IRuleProfileInstallStateStore _installStateStore;
    private readonly IRulesetPluginRegistry _pluginRegistry;
    private readonly IRuleProfilePublicationStore _publicationStore;
    private readonly IRulePackRegistryService _rulePackRegistryService;
    private readonly IRuntimeFingerprintService _runtimeFingerprintService;

    public DefaultRuleProfileRegistryService(
        IRulesetPluginRegistry pluginRegistry,
        IRulePackRegistryService rulePackRegistryService,
        IRuleProfilePublicationStore publicationStore,
        IRuleProfileInstallStateStore installStateStore,
        IRuntimeFingerprintService runtimeFingerprintService)
    {
        _pluginRegistry = pluginRegistry;
        _rulePackRegistryService = rulePackRegistryService;
        _publicationStore = publicationStore;
        _installStateStore = installStateStore;
        _runtimeFingerprintService = runtimeFingerprintService;
    }

    public IReadOnlyList<RuleProfileRegistryEntry> List(OwnerScope owner, string? rulesetId = null)
    {
        IReadOnlyList<IRulesetPlugin> plugins = SelectPlugins(rulesetId);

        return plugins
            .SelectMany(plugin => BuildProfiles(plugin, owner))
            .ToArray();
    }

    public RuleProfileRegistryEntry? Get(OwnerScope owner, string profileId, string? rulesetId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);

        return List(owner, rulesetId)
            .FirstOrDefault(entry => string.Equals(entry.Manifest.ProfileId, profileId, StringComparison.Ordinal));
    }

    private IReadOnlyList<IRulesetPlugin> SelectPlugins(string? rulesetId)
    {
        string? normalizedRulesetId = RulesetDefaults.NormalizeOptional(rulesetId);
        if (normalizedRulesetId is null)
        {
            return _pluginRegistry.All;
        }

        IRulesetPlugin? plugin = _pluginRegistry.Resolve(normalizedRulesetId);
        return plugin is null ? [] : [plugin];
    }

    private IReadOnlyList<RuleProfileRegistryEntry> BuildProfiles(IRulesetPlugin plugin, OwnerScope owner)
    {
        string rulesetId = plugin.Id.NormalizedValue;
        IReadOnlyList<RulePackRegistryEntry> rulePacks = _rulePackRegistryService.List(owner, rulesetId);
        Dictionary<string, ArtifactInstallState> installStateLookup = _installStateStore.List(owner, rulesetId)
            .ToDictionary(
                record => CreatePublicationKey(record.ProfileId, record.RulesetId),
                record => record.Install,
                StringComparer.Ordinal);
        Dictionary<string, RuleProfilePublicationMetadata> publicationLookup = _publicationStore.List(owner, rulesetId)
            .ToDictionary(
                record => CreatePublicationKey(record.ProfileId, record.RulesetId),
                record => record.Publication,
                StringComparer.Ordinal);
        List<RuleProfileRegistryEntry> entries =
        [
            CreateCoreProfile(plugin, publicationLookup, installStateLookup)
        ];

        if (rulePacks.Count > 0)
        {
            entries.Add(CreateOverlayProfile(plugin, owner, rulePacks, publicationLookup, installStateLookup));
        }

        return entries;
    }

    private RuleProfileRegistryEntry CreateCoreProfile(
        IRulesetPlugin plugin,
        IReadOnlyDictionary<string, RuleProfilePublicationMetadata> publicationLookup,
        IReadOnlyDictionary<string, ArtifactInstallState> installStateLookup)
    {
        string rulesetId = plugin.Id.NormalizedValue;
        string profileId = $"official.{rulesetId}.core";
        ResolvedRuntimeLock runtimeLock = CreateRuntimeLock(
            plugin,
            rulePacks: []);

        RuleProfileManifest manifest = new(
            ProfileId: profileId,
            Title: $"{plugin.DisplayName} Core",
            Description: $"Curated core runtime for {plugin.DisplayName}.",
            RulesetId: rulesetId,
            Audience: RuleProfileAudienceKinds.General,
            CatalogKind: RuleProfileCatalogKinds.Official,
            RulePacks: [],
            DefaultToggles: [],
            RuntimeLock: runtimeLock,
            UpdateChannel: RuleProfileUpdateChannels.Stable,
            Notes: "Built-in baseline runtime profile.");
        RuleProfilePublicationMetadata publication = publicationLookup.TryGetValue(
            CreatePublicationKey(profileId, rulesetId),
            out RuleProfilePublicationMetadata? persisted)
            ? persisted
            : new RuleProfilePublicationMetadata(
                OwnerId: SystemOwnerId,
                Visibility: ArtifactVisibilityModes.Public,
                PublicationStatus: RuleProfilePublicationStatuses.Published,
                Review: new RulePackReviewDecision(RulePackReviewStates.NotRequired),
                Shares:
                [
                    new RulePackShareGrant(
                        SubjectKind: RulePackShareSubjectKinds.PublicCatalog,
                        SubjectId: "profiles",
                        AccessLevel: RulePackShareAccessLevels.Install)
                ]);
        ArtifactInstallState install = installStateLookup.TryGetValue(
            CreatePublicationKey(profileId, rulesetId),
            out ArtifactInstallState? persistedInstall)
            ? persistedInstall
            : new ArtifactInstallState(ArtifactInstallStates.Available);

        return new RuleProfileRegistryEntry(manifest, publication, install);
    }

    private RuleProfileRegistryEntry CreateOverlayProfile(
        IRulesetPlugin plugin,
        OwnerScope owner,
        IReadOnlyList<RulePackRegistryEntry> rulePacks,
        IReadOnlyDictionary<string, RuleProfilePublicationMetadata> publicationLookup,
        IReadOnlyDictionary<string, ArtifactInstallState> installStateLookup)
    {
        string rulesetId = plugin.Id.NormalizedValue;
        string profileId = $"local.{rulesetId}.current-overlays";
        RuleProfilePackSelection[] selections = rulePacks
            .Select(rulePack => new RuleProfilePackSelection(
                RulePack: new ArtifactVersionReference(rulePack.Manifest.PackId, rulePack.Manifest.Version),
                Required: true,
                EnabledByDefault: true))
            .ToArray();
        ResolvedRuntimeLock runtimeLock = CreateRuntimeLock(plugin, rulePacks);
        RuleProfileManifest manifest = new(
            ProfileId: profileId,
            Title: $"{plugin.DisplayName} Local Overlay Catalog",
            Description: $"Runtime profile composed from the current discovered RulePacks for {plugin.DisplayName}.",
            RulesetId: rulesetId,
            Audience: RuleProfileAudienceKinds.Advanced,
            CatalogKind: RuleProfileCatalogKinds.Personal,
            RulePacks: selections,
            DefaultToggles: [],
            RuntimeLock: runtimeLock,
            UpdateChannel: RuleProfileUpdateChannels.Preview,
            Notes: "Derived from the current local RulePack registry.");
        RuleProfilePublicationMetadata publication = publicationLookup.TryGetValue(
            CreatePublicationKey(profileId, rulesetId),
            out RuleProfilePublicationMetadata? persisted)
            ? persisted
            : new RuleProfilePublicationMetadata(
                OwnerId: owner.NormalizedValue,
                Visibility: ArtifactVisibilityModes.LocalOnly,
                PublicationStatus: RuleProfilePublicationStatuses.Published,
                Review: new RulePackReviewDecision(RulePackReviewStates.NotRequired),
                Shares: []);
        ArtifactInstallState install = installStateLookup.TryGetValue(
            CreatePublicationKey(profileId, rulesetId),
            out ArtifactInstallState? persistedInstall)
            ? persistedInstall
            : new ArtifactInstallState(
                ArtifactInstallStates.Available,
                RuntimeFingerprint: runtimeLock.RuntimeFingerprint);

        return new RuleProfileRegistryEntry(manifest, publication, install);
    }

    private ResolvedRuntimeLock CreateRuntimeLock(
        IRulesetPlugin plugin,
        IReadOnlyList<RulePackRegistryEntry> rulePacks)
    {
        string rulesetId = plugin.Id.NormalizedValue;
        ContentBundleDescriptor bundle = new(
            BundleId: $"official.{rulesetId}.base",
            RulesetId: rulesetId,
            Version: $"schema-{plugin.Serializer.SchemaVersion}",
            Title: $"{plugin.DisplayName} Base Content",
            Description: $"Built-in base content bundle for {plugin.DisplayName}.",
            AssetPaths: ["data/", "lang/"]);
        ArtifactVersionReference[] runtimeRulePacks = rulePacks
            .Select(rulePack => new ArtifactVersionReference(rulePack.Manifest.PackId, rulePack.Manifest.Version))
            .ToArray();
        Dictionary<string, string> providerBindings = rulePacks
            .SelectMany(
                rulePack => rulePack.Manifest.Capabilities.Select(capability => new
                {
                    capability.CapabilityId,
                    ProviderId = $"{rulePack.Manifest.PackId}/{capability.CapabilityId}"
                }))
            .GroupBy(binding => binding.CapabilityId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Last().ProviderId,
                StringComparer.Ordinal);
        string runtimeFingerprint = _runtimeFingerprintService.ComputeResolvedRuntimeFingerprint(
            rulesetId,
            [bundle],
            rulePacks,
            providerBindings,
            EngineApiVersion);

        return new ResolvedRuntimeLock(
            RulesetId: rulesetId,
            ContentBundles: [bundle],
            RulePacks: runtimeRulePacks,
            ProviderBindings: providerBindings,
            EngineApiVersion: EngineApiVersion,
            RuntimeFingerprint: runtimeFingerprint);
    }

    private static string CreatePublicationKey(string profileId, string rulesetId)
    {
        return $"{profileId}|{rulesetId}";
    }
}
