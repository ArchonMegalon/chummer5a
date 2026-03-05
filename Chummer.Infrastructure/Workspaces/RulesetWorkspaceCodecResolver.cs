using Chummer.Application.Workspaces;
using Chummer.Contracts.Rulesets;

namespace Chummer.Infrastructure.Workspaces;

public sealed class RulesetWorkspaceCodecResolver : IRulesetWorkspaceCodecResolver
{
    private readonly IReadOnlyDictionary<string, IRulesetWorkspaceCodec> _codecsByRuleset;
    private readonly IRulesetWorkspaceCodec? _fallbackCodec;

    public RulesetWorkspaceCodecResolver(IEnumerable<IRulesetWorkspaceCodec> codecs)
    {
        Dictionary<string, IRulesetWorkspaceCodec> map = new(StringComparer.Ordinal);
        IRulesetWorkspaceCodec? fallback = null;
        foreach (IRulesetWorkspaceCodec codec in codecs)
        {
            string normalizedRulesetId = RulesetDefaults.Normalize(codec.RulesetId);
            map[normalizedRulesetId] = codec;
            fallback ??= codec;
        }

        _codecsByRuleset = map;
        _fallbackCodec = fallback;
    }

    public IRulesetWorkspaceCodec Resolve(string? rulesetId)
    {
        string normalizedRulesetId = RulesetDefaults.Normalize(rulesetId);
        if (_codecsByRuleset.TryGetValue(normalizedRulesetId, out IRulesetWorkspaceCodec? codec))
        {
            return codec;
        }

        if (_codecsByRuleset.TryGetValue(RulesetDefaults.Sr5, out IRulesetWorkspaceCodec? sr5Codec))
        {
            return sr5Codec;
        }

        return _fallbackCodec
            ?? throw new InvalidOperationException("No workspace codecs are registered.");
    }
}
