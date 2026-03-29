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
    private readonly INpcVaultRegistryService _npcVaultRegistryService;
    private readonly IRuntimeInspectorService _runtimeInspectorService;
    private readonly IRuntimeLockRegistryService _runtimeLockRegistryService;

    public DefaultHubProjectCompatibilityService(
        IRulesetPluginRegistry rulesetPluginRegistry,
        IRulePackRegistryService rulePackRegistryService,
        IRuleProfileRegistryService ruleProfileRegistryService,
        IBuildKitRegistryService buildKitRegistryService,
        INpcVaultRegistryService npcVaultRegistryService,
        IRuntimeInspectorService runtimeInspectorService,
        IRuntimeLockRegistryService runtimeLockRegistryService)
    {
        _rulesetPluginRegistry = rulesetPluginRegistry;
        _rulePackRegistryService = rulePackRegistryService;
        _ruleProfileRegistryService = ruleProfileRegistryService;
        _buildKitRegistryService = buildKitRegistryService;
        _npcVaultRegistryService = npcVaultRegistryService;
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
            HubCatalogItemKinds.NpcEntry => GetNpcEntryMatrix(owner, itemId, rulesetId),
            HubCatalogItemKinds.NpcPack => GetNpcPackMatrix(owner, itemId, rulesetId),
            HubCatalogItemKinds.EncounterPack => GetEncounterPackMatrix(owner, itemId, rulesetId),
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

    private HubProjectCompatibilityMatrix? GetNpcEntryMatrix(OwnerScope owner, string itemId, string? rulesetId)
    {
        foreach (string candidateRulesetId in EnumerateRulesetIds(rulesetId))
        {
            NpcEntryRegistryEntry? entry = _npcVaultRegistryService.GetEntry(owner, itemId, candidateRulesetId);
            if (entry is null)
            {
                continue;
            }

            string sessionRuntimeState = ResolveNpcSessionRuntimeState(entry.Manifest.SessionReady, entry.Manifest.GmBoardReady);
            List<HubProjectCompatibilityRow> rows =
            [
                CreateRulesetRow(candidateRulesetId),
                new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Visibility, "Visibility", HubProjectCompatibilityStates.Informational, entry.Manifest.Visibility),
                new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Trust, "Trust Tier", HubProjectCompatibilityStates.Informational, entry.Manifest.TrustTier),
                new HubProjectCompatibilityRow(
                    HubProjectCompatibilityRowKinds.SessionRuntime,
                    "Session Runtime Bundle",
                    sessionRuntimeState,
                    ResolveNpcSessionRuntimeValue(sessionRuntimeState),
                    RequiredValue: RulePackExecutionEnvironments.SessionRuntimeBundle,
                    Notes: DescribeNpcEntrySessionRuntime(entry)),
                CreateNarrativeRow(
                    HubProjectCompatibilityRowKinds.CampaignReturn,
                    sessionRuntimeState,
                    DescribeNpcEntryCampaignReturn(entry, sessionRuntimeState)),
                CreateNarrativeRow(
                    HubProjectCompatibilityRowKinds.SupportClosure,
                    sessionRuntimeState,
                    DescribeNpcEntrySupportClosure(entry, sessionRuntimeState))
            ];

            if (!string.IsNullOrWhiteSpace(entry.Manifest.RuntimeFingerprint))
            {
                rows.Insert(3, new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.RuntimeFingerprint, "Runtime Fingerprint", HubProjectCompatibilityStates.Informational, entry.Manifest.RuntimeFingerprint!));
            }

            return new HubProjectCompatibilityMatrix(
                Kind: HubCatalogItemKinds.NpcEntry,
                ItemId: itemId,
                Rows: rows.ToArray(),
                GeneratedAtUtc: DateTimeOffset.UtcNow,
                Capabilities: []);
        }

        return null;
    }

    private HubProjectCompatibilityMatrix? GetNpcPackMatrix(OwnerScope owner, string itemId, string? rulesetId)
    {
        foreach (string candidateRulesetId in EnumerateRulesetIds(rulesetId))
        {
            NpcPackRegistryEntry? entry = _npcVaultRegistryService.GetPack(owner, itemId, candidateRulesetId);
            if (entry is null)
            {
                continue;
            }

            string sessionRuntimeState = ResolveNpcSessionRuntimeState(entry.Manifest.SessionReady, entry.Manifest.GmBoardReady);
            return new HubProjectCompatibilityMatrix(
                Kind: HubCatalogItemKinds.NpcPack,
                ItemId: itemId,
                Rows:
                [
                    CreateRulesetRow(candidateRulesetId),
                    new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Visibility, "Visibility", HubProjectCompatibilityStates.Informational, entry.Manifest.Visibility),
                    new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Trust, "Trust Tier", HubProjectCompatibilityStates.Informational, entry.Manifest.TrustTier),
                    new HubProjectCompatibilityRow(
                        HubProjectCompatibilityRowKinds.SessionRuntime,
                        "Session Runtime Bundle",
                        sessionRuntimeState,
                        ResolveNpcSessionRuntimeValue(sessionRuntimeState),
                        RequiredValue: RulePackExecutionEnvironments.SessionRuntimeBundle,
                        Notes: DescribeNpcPackSessionRuntime(entry)),
                    CreateNarrativeRow(
                        HubProjectCompatibilityRowKinds.CampaignReturn,
                        sessionRuntimeState,
                        DescribeNpcPackCampaignReturn(entry, sessionRuntimeState)),
                    CreateNarrativeRow(
                        HubProjectCompatibilityRowKinds.SupportClosure,
                        sessionRuntimeState,
                        DescribeNpcPackSupportClosure(entry, sessionRuntimeState))
                ],
                GeneratedAtUtc: DateTimeOffset.UtcNow,
                Capabilities: []);
        }

        return null;
    }

    private HubProjectCompatibilityMatrix? GetEncounterPackMatrix(OwnerScope owner, string itemId, string? rulesetId)
    {
        foreach (string candidateRulesetId in EnumerateRulesetIds(rulesetId))
        {
            EncounterPackRegistryEntry? entry = _npcVaultRegistryService.GetEncounterPack(owner, itemId, candidateRulesetId);
            if (entry is null)
            {
                continue;
            }

            string sessionRuntimeState = ResolveNpcSessionRuntimeState(entry.Manifest.SessionReady, entry.Manifest.GmBoardReady);
            return new HubProjectCompatibilityMatrix(
                Kind: HubCatalogItemKinds.EncounterPack,
                ItemId: itemId,
                Rows:
                [
                    CreateRulesetRow(candidateRulesetId),
                    new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Visibility, "Visibility", HubProjectCompatibilityStates.Informational, entry.Manifest.Visibility),
                    new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Trust, "Trust Tier", HubProjectCompatibilityStates.Informational, entry.Manifest.TrustTier),
                    new HubProjectCompatibilityRow(
                        HubProjectCompatibilityRowKinds.SessionRuntime,
                        "Session Runtime Bundle",
                        sessionRuntimeState,
                        ResolveNpcSessionRuntimeValue(sessionRuntimeState),
                        RequiredValue: RulePackExecutionEnvironments.SessionRuntimeBundle,
                        Notes: DescribeEncounterPackSessionRuntime(entry)),
                    CreateNarrativeRow(
                        HubProjectCompatibilityRowKinds.CampaignReturn,
                        sessionRuntimeState,
                        DescribeEncounterPackCampaignReturn(entry, sessionRuntimeState)),
                    CreateNarrativeRow(
                        HubProjectCompatibilityRowKinds.SupportClosure,
                        sessionRuntimeState,
                        DescribeEncounterPackSupportClosure(entry, sessionRuntimeState))
                ],
                GeneratedAtUtc: DateTimeOffset.UtcNow,
                Capabilities: []);
        }

        return null;
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

    private static string ResolveNpcSessionRuntimeState(bool sessionReady, bool gmBoardReady)
    {
        if (sessionReady && gmBoardReady)
        {
            return HubProjectCompatibilityStates.Compatible;
        }

        if (sessionReady || gmBoardReady)
        {
            return HubProjectCompatibilityStates.ReviewRequired;
        }

        return HubProjectCompatibilityStates.Blocked;
    }

    private static string ResolveNpcSessionRuntimeValue(string sessionRuntimeState) =>
        sessionRuntimeState switch
        {
            HubProjectCompatibilityStates.Compatible => "campaign-bindable",
            HubProjectCompatibilityStates.ReviewRequired => "prep-review-required",
            _ => "prep-not-ready"
        };

    private static string DescribeNpcEntrySessionRuntime(NpcEntryRegistryEntry entry)
    {
        string faction = string.IsNullOrWhiteSpace(entry.Manifest.Faction) ? "independent" : entry.Manifest.Faction!;
        string runtime = string.IsNullOrWhiteSpace(entry.Manifest.RuntimeFingerprint)
            ? "without an explicit runtime fingerprint yet"
            : $"on runtime {entry.Manifest.RuntimeFingerprint}";
        return $"{entry.Manifest.Title} is tagged as threat tier {entry.Manifest.ThreatTier} for faction {faction} {runtime}; session-ready={entry.Manifest.SessionReady} and gm-board-ready={entry.Manifest.GmBoardReady}.";
    }

    private static string DescribeNpcEntryCampaignReturn(NpcEntryRegistryEntry entry, string sessionRuntimeState)
    {
        if (string.Equals(sessionRuntimeState, HubProjectCompatibilityStates.Compatible, StringComparison.Ordinal))
        {
            return $"Campaign return can bind {entry.Manifest.Title} back into a governed prep lane because both the session packet and GM board evidence are already grounded.";
        }

        return $"Campaign return should wait until {entry.Manifest.Title} is both session-ready and GM-board-ready before it is rebound into the next governed run.";
    }

    private static string DescribeNpcEntrySupportClosure(NpcEntryRegistryEntry entry, string sessionRuntimeState)
    {
        if (string.Equals(sessionRuntimeState, HubProjectCompatibilityStates.Compatible, StringComparison.Ordinal))
        {
            return $"Support closure can cite threat tier {entry.Manifest.ThreatTier}, faction {entry.Manifest.Faction ?? "independent"}, and the current prep packet as the governed opposition receipt.";
        }

        return $"Support closure should stay open until {entry.Manifest.Title} carries both session-ready and GM-board-ready posture.";
    }

    private static string DescribeNpcPackSessionRuntime(NpcPackRegistryEntry entry)
    {
        int entryTypeCount = entry.Manifest.Entries.Count;
        int totalSeatCount = entry.Manifest.Entries.Sum(static member => Math.Max(1, member.Quantity));
        return $"{entry.Manifest.Title} stages {totalSeatCount} opposition seat(s) across {entryTypeCount} entry type(s); session-ready={entry.Manifest.SessionReady} and gm-board-ready={entry.Manifest.GmBoardReady}.";
    }

    private static string DescribeNpcPackCampaignReturn(NpcPackRegistryEntry entry, string sessionRuntimeState)
    {
        if (string.Equals(sessionRuntimeState, HubProjectCompatibilityStates.Compatible, StringComparison.Ordinal))
        {
            return $"Campaign return can reopen with {entry.Manifest.Title} because the packet already preserves its governed opposition roster for the next prep lane.";
        }

        return $"Campaign return should wait until {entry.Manifest.Title} is reviewed for both session handoff and GM board reuse.";
    }

    private static string DescribeNpcPackSupportClosure(NpcPackRegistryEntry entry, string sessionRuntimeState)
    {
        int totalSeatCount = entry.Manifest.Entries.Sum(static member => Math.Max(1, member.Quantity));
        if (string.Equals(sessionRuntimeState, HubProjectCompatibilityStates.Compatible, StringComparison.Ordinal))
        {
            return $"Support closure can cite {entry.Manifest.Title} and its {totalSeatCount} prepared opposition seat(s) as the reusable prep receipt.";
        }

        return $"Support closure should stay review-aware until {entry.Manifest.Title} is both session-ready and GM-board-ready.";
    }

    private static string DescribeEncounterPackSessionRuntime(EncounterPackRegistryEntry entry)
    {
        int participantTypeCount = entry.Manifest.Participants.Count;
        int totalParticipantCount = entry.Manifest.Participants.Sum(static participant => Math.Max(1, participant.Quantity));
        int roleCount = entry.Manifest.Participants
            .Select(static participant => participant.Role)
            .Where(static role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.Ordinal)
            .Count();
        return $"{entry.Manifest.Title} stages {totalParticipantCount} opposition seat(s) across {participantTypeCount} participant type(s) and {roleCount} explicit role lane(s); session-ready={entry.Manifest.SessionReady} and gm-board-ready={entry.Manifest.GmBoardReady}.";
    }

    private static string DescribeEncounterPackCampaignReturn(EncounterPackRegistryEntry entry, string sessionRuntimeState)
    {
        if (string.Equals(sessionRuntimeState, HubProjectCompatibilityStates.Compatible, StringComparison.Ordinal))
        {
            return $"Campaign return can rebind {entry.Manifest.Title} because the encounter packet already carries governed role and quantity truth for the next session lane.";
        }

        return $"Campaign return should wait until {entry.Manifest.Title} clears both session and GM board prep posture before it is rebound.";
    }

    private static string DescribeEncounterPackSupportClosure(EncounterPackRegistryEntry entry, string sessionRuntimeState)
    {
        int roleCount = entry.Manifest.Participants
            .Select(static participant => participant.Role)
            .Where(static role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.Ordinal)
            .Count();
        if (string.Equals(sessionRuntimeState, HubProjectCompatibilityStates.Compatible, StringComparison.Ordinal))
        {
            return $"Support closure can cite {entry.Manifest.Title}, its {roleCount} explicit role lane(s), and the governed encounter packet as the reusable GM prep receipt.";
        }

        return $"Support closure should remain open until {entry.Manifest.Title} is fully grounded for both session and GM board use.";
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
