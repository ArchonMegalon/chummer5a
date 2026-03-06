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
    private readonly IRuntimeLockRegistryService _runtimeLockRegistryService;

    public DefaultHubProjectCompatibilityService(
        IRulesetPluginRegistry rulesetPluginRegistry,
        IRulePackRegistryService rulePackRegistryService,
        IRuleProfileRegistryService ruleProfileRegistryService,
        IBuildKitRegistryService buildKitRegistryService,
        IRuntimeLockRegistryService runtimeLockRegistryService)
    {
        _rulesetPluginRegistry = rulesetPluginRegistry;
        _rulePackRegistryService = rulePackRegistryService;
        _ruleProfileRegistryService = ruleProfileRegistryService;
        _buildKitRegistryService = buildKitRegistryService;
        _runtimeLockRegistryService = runtimeLockRegistryService;
    }

    public HubProjectCompatibilityMatrix? GetMatrix(OwnerScope owner, string kind, string itemId, string? rulesetId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        return kind.Trim() switch
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

            return new HubProjectCompatibilityMatrix(
                Kind: HubCatalogItemKinds.RulePack,
                ItemId: itemId,
                Rows:
                [
                    CreateRulesetRow(candidateRulesetId),
                    new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.EngineApi, "Engine API", HubProjectCompatibilityStates.Informational, entry.Manifest.EngineApiVersion),
                    new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Visibility, "Visibility", HubProjectCompatibilityStates.Informational, entry.Publication.Visibility),
                    new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Trust, "Trust Tier", HubProjectCompatibilityStates.Informational, entry.Manifest.TrustTier),
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
                GeneratedAtUtc: DateTimeOffset.UtcNow);
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

        return new HubProjectCompatibilityMatrix(
            Kind: HubCatalogItemKinds.RuleProfile,
            ItemId: itemId,
            Rows:
            [
                CreateRulesetRow(entry.Manifest.RulesetId),
                new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.EngineApi, "Engine API", HubProjectCompatibilityStates.Informational, entry.Manifest.RuntimeLock.EngineApiVersion),
                new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Visibility, "Visibility", HubProjectCompatibilityStates.Informational, entry.Publication.Visibility),
                new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.Trust, "Trust Tier", HubProjectCompatibilityStates.Informational, ResolveTrustTier(entry.Publication.Visibility)),
                new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.RuntimeFingerprint, "Runtime Fingerprint", HubProjectCompatibilityStates.Informational, entry.Manifest.RuntimeLock.RuntimeFingerprint),
                new HubProjectCompatibilityRow(
                    HubProjectCompatibilityRowKinds.SessionRuntime,
                    "Session Runtime Bundle",
                    sessionReady ? HubProjectCompatibilityStates.Compatible : HubProjectCompatibilityStates.ReviewRequired,
                    sessionReady ? "session-ready" : "session-review-required",
                    RequiredValue: RulePackExecutionEnvironments.SessionRuntimeBundle,
                    Notes: $"{entry.Manifest.RulePacks.Count} selected RulePack(s)")
            ],
            GeneratedAtUtc: DateTimeOffset.UtcNow);
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
                        entry.Manifest.RuntimeRequirements.Count == 0 ? HubProjectCompatibilityStates.Compatible : HubProjectCompatibilityStates.ReviewRequired,
                        entry.Manifest.RuntimeRequirements.Count.ToString(),
                        Notes: "BuildKits may require a campaign or profile runtime."),
                    new HubProjectCompatibilityRow(
                        HubProjectCompatibilityRowKinds.SessionRuntime,
                        "Session Runtime Bundle",
                        HubProjectCompatibilityStates.Blocked,
                        "workbench-only",
                        RequiredValue: RulePackExecutionEnvironments.SessionRuntimeBundle,
                        Notes: "BuildKits are create/career templates, not session-runtime inputs.")
                ],
                GeneratedAtUtc: DateTimeOffset.UtcNow);
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
                new HubProjectCompatibilityRow(HubProjectCompatibilityRowKinds.RuntimeFingerprint, "Runtime Fingerprint", HubProjectCompatibilityStates.Informational, entry.RuntimeLock.RuntimeFingerprint),
                new HubProjectCompatibilityRow(
                    HubProjectCompatibilityRowKinds.SessionRuntime,
                    "Session Runtime Bundle",
                    HubProjectCompatibilityStates.Compatible,
                    "bundle-ready",
                    RequiredValue: RulePackExecutionEnvironments.SessionRuntimeBundle,
                    Notes: $"{entry.RuntimeLock.RulePacks.Count} RulePack(s) resolved")
            ],
            GeneratedAtUtc: DateTimeOffset.UtcNow);
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

    private static string ResolveTrustTier(string visibility) =>
        string.Equals(visibility, ArtifactVisibilityModes.Public, StringComparison.Ordinal)
            ? ArtifactTrustTiers.Curated
            : ArtifactTrustTiers.LocalOnly;
}
