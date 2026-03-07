using Chummer.Contracts.AI;
using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;

namespace Chummer.Application.AI;

public sealed class DefaultAiExplainService : IAiExplainService
{
    private readonly IAiDigestService _aiDigestService;
    private readonly IRulesetPluginRegistry _rulesetPluginRegistry;

    public DefaultAiExplainService(
        IAiDigestService aiDigestService,
        IRulesetPluginRegistry rulesetPluginRegistry)
    {
        _aiDigestService = aiDigestService;
        _rulesetPluginRegistry = rulesetPluginRegistry;
    }

    public AiExplainValueProjection? GetExplainValue(OwnerScope owner, AiExplainValueQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        string? characterId = NormalizeOptional(query.CharacterId);
        AiCharacterDigestProjection? characterDigest = characterId is null
            ? null
            : _aiDigestService.GetCharacterDigest(owner, characterId);
        string? runtimeFingerprint = NormalizeOptional(query.RuntimeFingerprint) ?? characterDigest?.RuntimeFingerprint;
        if (runtimeFingerprint is null)
        {
            return null;
        }

        string? rulesetId = RulesetDefaults.NormalizeOptional(query.RulesetId) ?? characterDigest?.RulesetId;
        AiRuntimeSummaryProjection? runtimeSummary = _aiDigestService.GetRuntimeSummary(owner, runtimeFingerprint, rulesetId);
        if (runtimeSummary is null)
        {
            return null;
        }

        IRulesetPlugin? plugin = _rulesetPluginRegistry.Resolve(runtimeSummary.RulesetId);
        if (plugin is null)
        {
            return null;
        }

        string? requestedCapabilityId = NormalizeOptional(query.CapabilityId) ?? NormalizeOptional(query.ExplainEntryId);
        if (requestedCapabilityId is null)
        {
            return null;
        }

        RulesetCapabilityDescriptor? descriptor = plugin.CapabilityDescriptors
            .GetCapabilityDescriptors()
            .FirstOrDefault(candidate => string.Equals(candidate.CapabilityId, requestedCapabilityId, StringComparison.Ordinal));
        if (descriptor is null)
        {
            return null;
        }

        string? providerId = runtimeSummary.ProviderBindings.GetValueOrDefault(descriptor.CapabilityId);
        RulesetCapabilityInvocationResult invocation = plugin.Capabilities
            .InvokeAsync(
                new RulesetCapabilityInvocationRequest(
                    CapabilityId: descriptor.CapabilityId,
                    InvocationKind: descriptor.InvocationKind,
                    Arguments: BuildInvocationArguments(runtimeSummary, characterDigest, query),
                    Options: new RulesetExecutionOptions(Explain: true),
                    ProviderId: providerId,
                    Source: AiExplainApiOperations.ExplainValue),
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        string explainEntryId = NormalizeOptional(query.ExplainEntryId) ?? descriptor.CapabilityId;
        string? packId = TryResolvePackId(providerId, runtimeSummary.RulePacks);
        string summary = invocation.Explain?.Messages.FirstOrDefault()
            ?? invocation.Diagnostics.FirstOrDefault()?.Message
            ?? $"{descriptor.Title} resolved through runtime '{runtimeSummary.Title}'.";

        return new AiExplainValueProjection(
            ExplainEntryId: explainEntryId,
            Kind: ResolveEntryKind(descriptor),
            Title: descriptor.Title,
            Summary: summary,
            RuntimeFingerprint: runtimeSummary.RuntimeFingerprint,
            RulesetId: runtimeSummary.RulesetId,
            CharacterId: characterDigest?.CharacterId,
            CapabilityId: descriptor.CapabilityId,
            InvocationKind: descriptor.InvocationKind,
            ProviderId: providerId,
            PackId: packId,
            Explainable: descriptor.Explainable,
            SessionSafe: descriptor.SessionSafe,
            ProviderGasBudget: descriptor.DefaultGasBudget.ProviderInstructionLimit,
            RequestGasBudget: descriptor.DefaultGasBudget.RequestInstructionLimit,
            Fragments: BuildFragments(runtimeSummary, characterDigest, descriptor, providerId, packId, invocation),
            Diagnostics: invocation.Diagnostics.Select(static diagnostic => diagnostic.Message).ToArray());
    }

    private static IReadOnlyList<RulesetCapabilityArgument> BuildInvocationArguments(
        AiRuntimeSummaryProjection runtimeSummary,
        AiCharacterDigestProjection? characterDigest,
        AiExplainValueQuery query)
    {
        List<RulesetCapabilityArgument> arguments =
        [
            new("runtimeFingerprint", RulesetCapabilityBridge.FromObject(runtimeSummary.RuntimeFingerprint)),
            new("rulesetId", RulesetCapabilityBridge.FromObject(runtimeSummary.RulesetId))
        ];

        if (characterDigest is not null)
        {
            arguments.Add(new RulesetCapabilityArgument("characterId", RulesetCapabilityBridge.FromObject(characterDigest.CharacterId)));
            arguments.Add(new RulesetCapabilityArgument("characterName", RulesetCapabilityBridge.FromObject(characterDigest.DisplayName)));
            arguments.Add(new RulesetCapabilityArgument("karma", RulesetCapabilityBridge.FromObject(characterDigest.Summary.Karma)));
        }

        string? explainEntryId = NormalizeOptional(query.ExplainEntryId);
        if (explainEntryId is not null)
        {
            arguments.Add(new RulesetCapabilityArgument("explainEntryId", RulesetCapabilityBridge.FromObject(explainEntryId)));
        }

        return arguments;
    }

    private static IReadOnlyList<AiExplainFragmentProjection> BuildFragments(
        AiRuntimeSummaryProjection runtimeSummary,
        AiCharacterDigestProjection? characterDigest,
        RulesetCapabilityDescriptor descriptor,
        string? providerId,
        string? packId,
        RulesetCapabilityInvocationResult invocation)
    {
        List<AiExplainFragmentProjection> fragments =
        [
            new(AiExplainFragmentKinds.Input, "Runtime", runtimeSummary.Title),
            new(AiExplainFragmentKinds.Constant, "Ruleset", runtimeSummary.RulesetId.ToUpperInvariant()),
            new(AiExplainFragmentKinds.Constant, "Capability", descriptor.CapabilityId),
            new(AiExplainFragmentKinds.Constant, "Invocation", descriptor.InvocationKind)
        ];

        if (characterDigest is not null)
        {
            fragments.Add(new AiExplainFragmentProjection(AiExplainFragmentKinds.Input, "Character", characterDigest.DisplayName));
        }

        if (providerId is not null)
        {
            fragments.Add(new AiExplainFragmentProjection(AiExplainFragmentKinds.ProviderStep, "Provider", providerId));
        }

        if (packId is not null)
        {
            fragments.Add(new AiExplainFragmentProjection(AiExplainFragmentKinds.ProviderStep, "RulePack", packId));
        }

        if (invocation.Explain is not null)
        {
            foreach (RulesetProviderTrace provider in invocation.Explain.Providers)
            {
                fragments.Add(new AiExplainFragmentProjection(
                    AiExplainFragmentKinds.ProviderStep,
                    provider.ProviderId,
                    $"{provider.GasUsage.ProviderInstructionsConsumed}/{provider.GasUsage.RequestInstructionsConsumed} gas"));

                foreach (RulesetExplainFragment explainFragment in provider.ExplainFragments)
                {
                    fragments.Add(new AiExplainFragmentProjection(
                        AiExplainFragmentKinds.Note,
                        explainFragment.Label,
                        FormatValue(explainFragment.Value ?? explainFragment.Reason ?? string.Empty)));
                }
            }
        }
        else
        {
            foreach (KeyValuePair<string, object?> output in RulesetCapabilityBridge.ToOutputDictionary(invocation.Output))
            {
                fragments.Add(new AiExplainFragmentProjection(
                    AiExplainFragmentKinds.Output,
                    output.Key,
                    FormatValue(output.Value)));
            }

            fragments.Add(new AiExplainFragmentProjection(
                AiExplainFragmentKinds.Note,
                "Explain Trace",
                "Capability metadata is available, but the active provider did not emit a live explain trace."));
        }

        foreach (RulesetCapabilityDiagnostic diagnostic in invocation.Diagnostics)
        {
            fragments.Add(new AiExplainFragmentProjection(
                AiExplainFragmentKinds.Warning,
                diagnostic.Code,
                diagnostic.Message));
        }

        return fragments;
    }

    private static string ResolveEntryKind(RulesetCapabilityDescriptor descriptor)
    {
        if (descriptor.SessionSafe)
        {
            return AiExplainEntryKinds.QuickActionAvailability;
        }

        return descriptor.Explainable
            ? AiExplainEntryKinds.DerivedValue
            : AiExplainEntryKinds.CapabilityDescriptor;
    }

    private static string? TryResolvePackId(string? providerId, IEnumerable<string> rulePacks)
    {
        if (providerId is null)
        {
            return null;
        }

        foreach (string rulePack in rulePacks)
        {
            string packId = rulePack.Split('@', 2)[0];
            if (providerId.StartsWith($"{packId}:", StringComparison.Ordinal)
                || providerId.StartsWith($"{packId}/", StringComparison.Ordinal)
                || string.Equals(providerId, packId, StringComparison.Ordinal))
            {
                return packId;
            }
        }

        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return "(null)";
        }

        return value switch
        {
            string stringValue => stringValue,
            IEnumerable<object?> values => string.Join(", ", values.Select(FormatValue)),
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
        };
    }
}
