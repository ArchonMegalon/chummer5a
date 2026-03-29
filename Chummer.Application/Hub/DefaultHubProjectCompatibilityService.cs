using Chummer.Application.Content;
using Chummer.Contracts.Content;
using Chummer.Contracts.Hub;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;

namespace Chummer.Application.Hub;

public sealed class DefaultHubProjectCompatibilityService : IHubProjectCompatibilityService
{
    private readonly IRulesetPluginRegistry _rulesetPluginRegistry;
    private readonly IRulePackRegistryService _rulePackRegistryService;
    private readonly IRuleProfileRegistryService _ruleProfileRegistryService;
    private readonly IBuildKitRegistryService _buildKitRegistryService;
    private readonly IRuntimeInspectorService _runtimeInspectorService;
    private readonly IRuntimeLockRegistryService _runtimeLockRegistryService;

    public DefaultHubProjectCompatibilityService(
        IRulesetPluginRegistry rulesetPluginRegistry,
        IRulePackRegistryService rulePackRegistryService,
        IRuleProfileRegistryService ruleProfileRegistryService,
        IBuildKitRegistryService buildKitRegistryService,
        IRuntimeInspectorService runtimeInspectorService,
        IRuntimeLockRegistryService runtimeLockRegistryService)
    {
        _rulesetPluginRegistry = rulesetPluginRegistry;
        _rulePackRegistryService = rulePackRegistryService;
        _ruleProfileRegistryService = ruleProfileRegistryService;
        _buildKitRegistryService = buildKitRegistryService;
        _runtimeInspectorService = runtimeInspectorService;
        _runtimeLockRegistryService = runtimeLockRegistryService;
    }

    public HubProjectCompatibilityMatrix? GetMatrix(OwnerScope owner, string kind, string itemId, string? rulesetId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        string normalizedKind = HubCatalogItemKinds.NormalizeRequired(kind);

        return normalizedKind switch
        {
            HubCatalogItemKinds.RulePack => GetRulePackMatrix(owner, itemId, rulesetId),
            HubCatalogItemKinds.RuleProfile => GetRuleProfileMatrix(owner, itemId, rulesetId),
            HubCatalogItemKinds.BuildKit => GetBuildKitMatrix(owner, itemId, rulesetId),
            HubCatalogItemKinds.RuntimeLock => GetRuntimeLockMatrix(owner, itemId, rulesetId),
            _ => null
        };
    }

    private HubProjectCompatibilityMatrix? GetRulePackMatrix(OwnerScope owner, string itemId, string? rulesetId)
    {
        foreach (string candidateRulesetId in EnumerateRulesetIds(rulesetId))
        {
            RulePackRegistryEntry? entry = _rulePackRegistryService.Get(owner, itemId, candidateRulesetId);
            if (entry is null)
            {
                continue;
            }

            RulePackExecutionPolicyHint? sessionPolicy = entry.Manifest.ExecutionPolicies
                .FirstOrDefault(policy => string.Equals(policy.Environment, RulePackExecutionEnvironments.SessionRuntimeBundle, StringComparison.Ordinal));
            RulePackExecutionPolicyHint? hostedPolicy = entry.Manifest.ExecutionPolicies
                .FirstOrDefault(policy => string.Equals(policy.Environment, RulePackExecutionEnvironments.HostedServer, StringComparison.Ordinal));
            bool hasSessionSafeCapability = entry.Manifest.Capabilities.Any(capability => capability.SessionSafe);
            HubProjectCapabilityDescriptorProjection[] capabilities = BuildRulePackCapabilities(candidateRulesetId, entry);

            return new HubProjectCompatibilityMatrix(
                Kind: HubCatalogItemKinds.RulePack,
                ItemId: itemId,
                Rows:
                [
                    CreateRulesetRow(candidateRulesetId),
                    new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.EngineApi, "Engine API", HubProjectCompatibilityStates.Informational, entry.Manifest.EngineApiVersion),
                    new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Visibility, "Visibility", HubProjectCompatibilityStates.Informational, entry.Publication.Visibility),
                    new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Trust, "Trust Tier", HubProjectCompatibilityStates.Informational, entry.Manifest.TrustTier),
                    CreateCapabilitiesRow(capabilities),
                    new HubProjectCompatibilityRow(
                        HubProjectCompatibilityRowKinds.SessionRuntime,
                        "Session Runtime Bundle",
                        ResolveExecutionState(sessionPolicy?.PolicyMode, hasSessionSafeCapability),
                        hasSessionSafeCapability ? "session-safe" : "not-session-safe",
                        RequiredValue: RulePackExecutionEnvironments.SessionRuntimeBundle,
                        Notes: sessionPolicy?.PolicyMode),
                    new HubProjectCompatibilityRow(
                        HubProjectCompatibilityRowKinds.HostedPublic,
                        "Hosted/Public Runtime",
                        ResolveExecutionState(hostedPolicy?.PolicyMode, false),
                        hostedPolicy?.PolicyMode ?? "not-declared",
                        RequiredValue: RulePackExecutionEnvironments.HostedServer,
                        Notes: hostedPolicy?.MinimumTrustTier)
                ],
                GeneratedAtUtc: DateTimeOffset.UtcNow,
                Capabilities: capabilities);
        }

        return null;
    }

    private HubProjectCompatibilityMatrix? GetRuleProfileMatrix(OwnerScope owner, string itemId, string? rulesetId)
    {
        RuleProfileRegistryEntry? entry = _ruleProfileRegistryService.Get(owner, itemId, rulesetId);
        if (entry is null)
        {
            return null;
        }

        RulePackRegistryEntry[] resolvedRulePacks = entry.Manifest.RulePacks
            .Select(selection => _rulePackRegistryService.Get(owner, selection.RulePack.Id, entry.Manifest.RulesetId))
            .OfType<RulePackRegistryEntry>()
            .ToArray();
        bool sessionReady = resolvedRulePacks.Length == 0 || resolvedRulePacks.All(IsSessionReadyRulePack);
        RuntimeInspectorProjection? runtimeProjection = _runtimeInspectorService.GetProfileProjection(owner, itemId, entry.Manifest.RulesetId);
        bool requiresRuntimeReview = runtimeProjection?.CompatibilityDiagnostics.Any(diagnostic =>
            !string.Equals(diagnostic.State, RuntimeLockCompatibilityStates.Compatible, StringComparison.Ordinal)) ?? false;
        HubProjectCapabilityDescriptorProjection[] capabilities = BuildRuntimeCapabilities(
            entry.Manifest.RulesetId,
            entry.Manifest.RuntimeLock.ProviderBindings,
            entry.Manifest.RuntimeLock.RulePacks.Select(reference => reference.Id));

        return new HubProjectCompatibilityMatrix(
            Kind: HubCatalogItemKinds.RuleProfile,
            ItemId: itemId,
            Rows:
            [
                CreateRulesetRow(entry.Manifest.RulesetId),
                new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.EngineApi, "Engine API", HubProjectCompatibilityStates.Informational, entry.Manifest.RuntimeLock.EngineApiVersion),
                new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Visibility, "Visibility", HubProjectCompatibilityStates.Informational, entry.Publication.Visibility),
                new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Trust, "Trust Tier", HubProjectCompatibilityStates.Informational, ResolveTrustTier(entry.Publication.Visibility)),
                CreateCapabilitiesRow(capabilities),
                new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.RuntimeFingerprint, "Runtime Fingerprint", HubProjectCompatibilityStates.Informational, entry.Manifest.RuntimeLock.RuntimeFingerprint),
                new HubProjectCompatibilityRow(
                    HubProjectCompatibilityRowKinds.SessionRuntime,
                    "Session Runtime Bundle",
                    sessionReady && !requiresRuntimeReview ? HubProjectCompatibilityStates.Compatible : HubProjectCompatibilityStates.ReviewRequired,
                    sessionReady && !requiresRuntimeReview ? "session-ready" : "session-review-required",
                    RequiredValue: RulePackExecutionEnvironments.SessionRuntimeBundle,
                    Notes: ResolveRuleProfileSessionRuntimeNotes(entry, runtimeProjection)),
                CreateNarrativeRow(
                    HubProjectCompatibilityRowKinds.CampaignReturn,
                    sessionReady && !requiresRuntimeReview ? HubProjectCompatibilityStates.Compatible : HubProjectCompatibilityStates.ReviewRequired,
                    DescribeRuleProfileCampaignReturn(entry, runtimeProjection)),
                CreateNarrativeRow(
                    HubProjectCompatibilityRowKinds.SupportClosure,
                    sessionReady && !requiresRuntimeReview ? HubProjectCompatibilityStates.Compatible : HubProjectCompatibilityStates.ReviewRequired,
                    DescribeRuleProfileSupportClosure(entry, runtimeProjection))
            ],
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Capabilities: capabilities);
    }

    private HubProjectCompatibilityMatrix? GetBuildKitMatrix(OwnerScope owner, string itemId, string? rulesetId)
    {
        foreach (string candidateRulesetId in EnumerateRulesetIds(rulesetId))
        {
            BuildKitRegistryEntry? entry = _buildKitRegistryService.Get(owner, itemId, candidateRulesetId);
            if (entry is null)
            {
                continue;
            }

            BuildKitCompatibilityReceipt compatibilityReceipt = BuildKitCompatibilityReceiptBuilder.Create(entry.Manifest);

            return new HubProjectCompatibilityMatrix(
                Kind: HubCatalogItemKinds.BuildKit,
                ItemId: itemId,
                Rows:
                [
                    CreateRulesetRow(candidateRulesetId),
                    new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Visibility, "Visibility", HubProjectCompatibilityStates.Informational, entry.Visibility),
                    new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Trust, "Trust Tier", HubProjectCompatibilityStates.Informational, entry.Manifest.TrustTier),
                    new HubProjectCompatibilityRow(
                        HubProjectCompatibilityRowKinds.RuntimeRequirements,
                        "Runtime Requirements",
                        compatibilityReceipt.RequiresRuntimeReview ? HubProjectCompatibilityStates.ReviewRequired : HubProjectCompatibilityStates.Compatible,
                        compatibilityReceipt.RuntimeRequirements.Count.ToString(),
                        Notes: compatibilityReceipt.RuntimeCompatibilitySummary),
                    new HubProjectCompatibilityRow(
                        HubProjectCompatibilityRowKinds.SessionRuntime,
                        "Session Runtime Bundle",
                        HubProjectCompatibilityStates.Blocked,
                        "workbench-first",
                        RequiredValue: RulePackExecutionEnvironments.SessionRuntimeBundle,
                        Notes: $"{compatibilityReceipt.SessionRuntimeSummary} Next safe action: {compatibilityReceipt.NextSafeActionSummary}"),
                    CreateNarrativeRow(
                        HubProjectCompatibilityRowKinds.CampaignReturn,
                        compatibilityReceipt.RequiresRuntimeReview ? HubProjectCompatibilityStates.ReviewRequired : HubProjectCompatibilityStates.Compatible,
                        compatibilityReceipt.CampaignReturnSummary),
                    CreateNarrativeRow(
                        HubProjectCompatibilityRowKinds.SupportClosure,
                        compatibilityReceipt.RequiresRuntimeReview ? HubProjectCompatibilityStates.ReviewRequired : HubProjectCompatibilityStates.Compatible,
                        compatibilityReceipt.SupportClosureSummary)
                ],
                GeneratedAtUtc: DateTimeOffset.UtcNow,
                Capabilities: []);
        }

        return null;
    }

    private HubProjectCompatibilityMatrix? GetRuntimeLockMatrix(OwnerScope owner, string itemId, string? rulesetId)
    {
        RuntimeLockRegistryEntry? entry = _runtimeLockRegistryService.Get(owner, itemId, rulesetId);
        if (entry is null)
        {
            return null;
        }

        HubProjectCapabilityDescriptorProjection[] capabilities = BuildRuntimeCapabilities(
            entry.RuntimeLock.RulesetId,
            entry.RuntimeLock.ProviderBindings,
            entry.RuntimeLock.RulePacks.Select(reference => reference.Id));

        return new HubProjectCompatibilityMatrix(
            Kind: HubCatalogItemKinds.RuntimeLock,
            ItemId: itemId,
            Rows:
            [
                CreateRulesetRow(entry.RuntimeLock.RulesetId),
                new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.EngineApi, "Engine API", HubProjectCompatibilityStates.Informational, entry.RuntimeLock.EngineApiVersion),
                new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Visibility, "Visibility", HubProjectCompatibilityStates.Informational, entry.Visibility),
                new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Trust, "Trust Tier", HubProjectCompatibilityStates.Informational, ResolveTrustTier(entry.Visibility)),
                new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.InstallState, "Install State", HubProjectCompatibilityStates.Informational, entry.Install.State, Notes: entry.Install.InstalledTargetId),
                CreateCapabilitiesRow(capabilities),
                new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.RuntimeFingerprint, "Runtime Fingerprint", HubProjectCompatibilityStates.Informational, entry.RuntimeLock.RuntimeFingerprint),
                new HubProjectCompatibilityRow(
                    HubProjectCompatibilityRowKinds.SessionRuntime,
                    "Session Runtime Bundle",
                    HubProjectCompatibilityStates.Compatible,
                    "bundle-ready",
                    RequiredValue: RulePackExecutionEnvironments.SessionRuntimeBundle,
                    Notes: $"{entry.RuntimeLock.RulePacks.Count} RulePack(s) resolved"),
                CreateNarrativeRow(
                    HubProjectCompatibilityRowKinds.CampaignReturn,
                    HubProjectCompatibilityStates.Compatible,
                    DescribeRuntimeLockCampaignReturn(entry)),
                CreateNarrativeRow(
                    HubProjectCompatibilityRowKinds.SupportClosure,
                    HubProjectCompatibilityStates.Compatible,
                    DescribeRuntimeLockSupportClosure(entry))
            ],
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Capabilities: capabilities);
    }

    private IEnumerable<string> EnumerateRulesetIds(string? rulesetId)
    {
        string? normalizedRulesetId = RulesetDefaults.NormalizeOptional(rulesetId);
        if (normalizedRulesetId is not null)
        {
            yield return normalizedRulesetId;
            yield break;
        }

        foreach (IRulesetPlugin plugin in _rulesetPluginRegistry.All)
        {
            yield return plugin.Id.NormalizedValue;
        }
    }

    private static HubProjectCompatibilityRow CreateRulesetRow(string rulesetId) =>
        new(HubProjectCompatibilityRowKinds.Ruleset, "Ruleset", HubProjectCompatibilityStates.Compatible, rulesetId);

    private static HubProjectCompatibilityRow CreateCapabilitiesRow(IReadOnlyList<HubProjectCapabilityDescriptorProjection> capabilities) =>
        new(
            HubProjectCompatibilityRowKinds.Capabilities,
            "Capabilities",
            HubProjectCompatibilityStates.Informational,
            capabilities.Count.ToString(),
            Notes: capabilities.Count == 0
                ? "No typed capability descriptors are published for this runtime."
                : $"{capabilities.Count(capability => capability.SessionSafe)} session-safe; {capabilities.Count(capability => capability.Explainable)} explainable");

    private static HubProjectCompatibilityRow CreateNarrativeRow(string kind, string state, string notes) =>
        new(
            kind,
            kind switch
            {
                HubProjectCompatibilityRowKinds.CampaignReturn => "Campaign Return",
                HubProjectCompatibilityRowKinds.SupportClosure => "Support Closure",
                _ => kind
            },
            state,
            state,
            Notes: notes);

    private static string ResolveExecutionState(string? policyMode, bool sessionSafe)
    {
        if (sessionSafe)
        {
            return HubProjectCompatibilityStates.Compatible;
        }

        return policyMode switch
        {
            RulePackExecutionPolicyModes.Allow => HubProjectCompatibilityStates.Compatible,
            RulePackExecutionPolicyModes.ReviewRequired => HubProjectCompatibilityStates.ReviewRequired,
            _ => HubProjectCompatibilityStates.Blocked
        };
    }

    private static bool IsSessionReadyRulePack(RulePackRegistryEntry entry)
    {
        return entry.Manifest.Capabilities.Any(capability => capability.SessionSafe)
            || entry.Manifest.ExecutionPolicies.Any(policy =>
                string.Equals(policy.Environment, RulePackExecutionEnvironments.SessionRuntimeBundle, StringComparison.Ordinal)
                && !string.Equals(policy.PolicyMode, RulePackExecutionPolicyModes.Deny, StringComparison.Ordinal));
    }

    private static string ResolveRuleProfileSessionRuntimeNotes(RuleProfileRegistryEntry entry, RuntimeInspectorProjection? runtimeProjection)
    {
        string defaultNotes = $"{entry.Manifest.RulePacks.Count} selected RulePack(s)";
        if (runtimeProjection is null)
        {
            return defaultNotes;
        }

        string[] reviewMessages = runtimeProjection.CompatibilityDiagnostics
            .Where(diagnostic => !string.Equals(diagnostic.State, RuntimeLockCompatibilityStates.Compatible, StringComparison.Ordinal))
            .Select(diagnostic => diagnostic.Message)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return reviewMessages.Length == 0
            ? defaultNotes
            : string.Join(" ", reviewMessages);
    }

    private static string DescribeRuleProfileCampaignReturn(RuleProfileRegistryEntry entry, RuntimeInspectorProjection? runtimeProjection)
    {
        string? issueSummary = ResolveCompatibilityIssueSummary(runtimeProjection);
        if (issueSummary is not null)
        {
            return $"Campaign return should wait until runtime '{entry.Manifest.RuntimeLock.RuntimeFingerprint}' and the selected rule environment are aligned again. {issueSummary}";
        }

        return $"Campaign return can reopen through the selected workspace or campaign lane once runtime '{entry.Manifest.RuntimeLock.RuntimeFingerprint}' and {entry.Manifest.RulePacks.Count} selected RulePack(s) stay aligned.";
    }

    private static string DescribeRuleProfileSupportClosure(RuleProfileRegistryEntry entry, RuntimeInspectorProjection? runtimeProjection)
    {
        string? issueSummary = ResolveCompatibilityIssueSummary(runtimeProjection);
        if (issueSummary is not null)
        {
            return $"Support closure should cite runtime '{entry.Manifest.RuntimeLock.RuntimeFingerprint}' with the current compatibility warnings before reopening play. {issueSummary}";
        }

        return $"Support closure can cite runtime '{entry.Manifest.RuntimeLock.RuntimeFingerprint}' and {entry.Manifest.RulePacks.Count} selected RulePack(s) as the grounded recovery contract.";
    }

    private static string DescribeRuntimeLockCampaignReturn(RuntimeLockRegistryEntry entry)
    {
        return $"Campaign return can reopen through the selected workspace or campaign lane once runtime '{entry.RuntimeLock.RuntimeFingerprint}' and {entry.RuntimeLock.RulePacks.Count} RulePack(s) stay aligned.";
    }

    private static string DescribeRuntimeLockSupportClosure(RuntimeLockRegistryEntry entry)
    {
        return $"Support closure can cite runtime '{entry.RuntimeLock.RuntimeFingerprint}' and {entry.RuntimeLock.RulePacks.Count} RulePack(s) as the grounded recovery contract.";
    }

    private static string? ResolveCompatibilityIssueSummary(RuntimeInspectorProjection? runtimeProjection)
    {
        if (runtimeProjection is null)
        {
            return null;
        }

        string[] reviewMessages = runtimeProjection.CompatibilityDiagnostics
            .Where(diagnostic => !string.Equals(diagnostic.State, RuntimeLockCompatibilityStates.Compatible, StringComparison.Ordinal))
            .Select(diagnostic => diagnostic.Message)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return reviewMessages.Length == 0
            ? null
            : string.Join(" ", reviewMessages);
    }

    private HubProjectCapabilityDescriptorProjection[] BuildRulePackCapabilities(string rulesetId, RulePackRegistryEntry entry)
    {
        IReadOnlyDictionary<string, RulesetCapabilityDescriptor> rulesetDescriptors = GetRulesetCapabilityDescriptors(rulesetId);

        return entry.Manifest.Capabilities
            .OrderBy(capability => capability.CapabilityId, StringComparer.Ordinal)
            .Select(capability =>
            {
                rulesetDescriptors.TryGetValue(capability.CapabilityId, out RulesetCapabilityDescriptor? descriptor);
                return new HubProjectCapabilityDescriptorProjection(
                    CapabilityId: capability.CapabilityId,
                    InvocationKind: descriptor?.InvocationKind,
                    Title: descriptor?.Title,
                    Explainable: capability.Explainable || descriptor?.Explainable == true,
                    SessionSafe: capability.SessionSafe || descriptor?.SessionSafe == true,
                    DefaultGasBudget: descriptor?.DefaultGasBudget,
                    MaximumGasBudget: descriptor?.MaximumGasBudget,
                    PackId: entry.Manifest.PackId,
                    AssetKind: capability.AssetKind,
                    AssetMode: capability.AssetMode);
            })
            .ToArray();
    }

    private HubProjectCapabilityDescriptorProjection[] BuildRuntimeCapabilities(
        string rulesetId,
        IReadOnlyDictionary<string, string> providerBindings,
        IEnumerable<string> packIds)
    {
        return GetRulesetCapabilityDescriptors(rulesetId)
            .Values
            .OrderBy(descriptor => descriptor.CapabilityId, StringComparer.Ordinal)
            .Select(descriptor =>
            {
                string? providerId = providerBindings.GetValueOrDefault(descriptor.CapabilityId);
                return new HubProjectCapabilityDescriptorProjection(
                    CapabilityId: descriptor.CapabilityId,
                    InvocationKind: descriptor.InvocationKind,
                    Title: descriptor.Title,
                    Explainable: descriptor.Explainable,
                    SessionSafe: descriptor.SessionSafe,
                    DefaultGasBudget: descriptor.DefaultGasBudget,
                    MaximumGasBudget: descriptor.MaximumGasBudget,
                    ProviderId: providerId,
                    PackId: providerId is null ? null : TryResolvePackId(providerId, packIds));
            })
            .ToArray();
    }

    private IReadOnlyDictionary<string, RulesetCapabilityDescriptor> GetRulesetCapabilityDescriptors(string rulesetId)
    {
        IRulesetPlugin? plugin = _rulesetPluginRegistry.Resolve(rulesetId);
        if (plugin is null)
        {
            return new Dictionary<string, RulesetCapabilityDescriptor>(StringComparer.Ordinal);
        }

        return plugin.CapabilityDescriptors
            .GetCapabilityDescriptors()
            .ToDictionary(descriptor => descriptor.CapabilityId, descriptor => descriptor, StringComparer.Ordinal);
    }

    private static string? TryResolvePackId(string providerId, IEnumerable<string> packIds)
    {
        foreach (string packId in packIds)
        {
            if (providerId.StartsWith($"{packId}/", StringComparison.Ordinal))
            {
                return packId;
            }
        }

        return null;
    }

    private static string ResolveTrustTier(string visibility) =>
        string.Equals(visibility, ArtifactVisibilityModes.Public, StringComparison.Ordinal)
            ? ArtifactTrustTiers.Curated
            : ArtifactTrustTiers.LocalOnly;
}
