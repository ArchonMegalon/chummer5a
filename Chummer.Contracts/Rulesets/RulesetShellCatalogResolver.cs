using Chummer.Contracts.Presentation;

namespace Chummer.Contracts.Rulesets;

public static class RulesetShellCatalogResolver
{
    public static IReadOnlyList<AppCommandDefinition> ResolveCommands(
        string? rulesetId,
        IEnumerable<IRulesetPlugin>? plugins = null)
    {
        string normalizedRulesetId = RulesetDefaults.Normalize(rulesetId);
        IRulesetPlugin? plugin = ResolvePlugin(normalizedRulesetId, plugins);
        return plugin is null
            ? AppCommandCatalog.ForRuleset(normalizedRulesetId)
            : plugin.ShellDefinitions.GetCommands();
    }

    public static IReadOnlyList<NavigationTabDefinition> ResolveNavigationTabs(
        string? rulesetId,
        IEnumerable<IRulesetPlugin>? plugins = null)
    {
        string normalizedRulesetId = RulesetDefaults.Normalize(rulesetId);
        IRulesetPlugin? plugin = ResolvePlugin(normalizedRulesetId, plugins);
        return plugin is null
            ? NavigationTabCatalog.ForRuleset(normalizedRulesetId)
            : plugin.ShellDefinitions.GetNavigationTabs();
    }

    private static IRulesetPlugin? ResolvePlugin(string normalizedRulesetId, IEnumerable<IRulesetPlugin>? plugins)
    {
        if (plugins is null)
            return null;

        IRulesetPlugin? matchedPlugin = null;
        foreach (IRulesetPlugin plugin in plugins)
        {
            if (string.Equals(plugin.Id.NormalizedValue, normalizedRulesetId, StringComparison.Ordinal))
            {
                matchedPlugin = plugin;
            }
        }

        return matchedPlugin;
    }
}
