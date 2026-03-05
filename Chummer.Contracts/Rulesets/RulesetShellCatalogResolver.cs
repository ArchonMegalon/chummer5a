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

    public static IReadOnlyList<WorkspaceSurfaceActionDefinition> ResolveWorkspaceActionsForTab(
        string? tabId,
        string? rulesetId,
        IEnumerable<IRulesetPlugin>? plugins = null)
    {
        string normalizedRulesetId = RulesetDefaults.Normalize(rulesetId);
        IRulesetPlugin? plugin = ResolvePlugin(normalizedRulesetId, plugins);
        if (plugin is null)
            return WorkspaceSurfaceActionCatalog.ForTab(tabId, normalizedRulesetId);

        return SelectTabActions(plugin.Catalogs.GetWorkspaceActions(), tabId);
    }

    public static IReadOnlyList<DesktopUiControlDefinition> ResolveDesktopUiControlsForTab(
        string? tabId,
        string? rulesetId,
        IEnumerable<IRulesetPlugin>? plugins = null)
    {
        string normalizedRulesetId = RulesetDefaults.Normalize(rulesetId);
        IRulesetPlugin? plugin = ResolvePlugin(normalizedRulesetId, plugins);
        if (plugin is null)
            return DesktopUiControlCatalog.ForTab(tabId, normalizedRulesetId);

        return SelectTabControls(plugin.Catalogs.GetDesktopUiControls(), tabId);
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

    private static IReadOnlyList<WorkspaceSurfaceActionDefinition> SelectTabActions(
        IReadOnlyList<WorkspaceSurfaceActionDefinition> actions,
        string? tabId)
    {
        string effectiveTabId = string.IsNullOrWhiteSpace(tabId) ? "tab-info" : tabId;

        WorkspaceSurfaceActionDefinition[] tabActions = actions
            .Where(action => string.Equals(action.TabId, effectiveTabId, StringComparison.Ordinal))
            .ToArray();
        if (tabActions.Length > 0)
            return tabActions;

        return actions
            .Where(action => string.Equals(action.TabId, "tab-info", StringComparison.Ordinal))
            .ToArray();
    }

    private static IReadOnlyList<DesktopUiControlDefinition> SelectTabControls(
        IReadOnlyList<DesktopUiControlDefinition> controls,
        string? tabId)
    {
        string effectiveTabId = string.IsNullOrWhiteSpace(tabId) ? "tab-info" : tabId;

        DesktopUiControlDefinition[] tabControls = controls
            .Where(control => string.Equals(control.TabId, effectiveTabId, StringComparison.Ordinal))
            .ToArray();
        if (tabControls.Length > 0)
            return tabControls;

        return controls
            .Where(control => string.Equals(control.TabId, "tab-info", StringComparison.Ordinal))
            .ToArray();
    }
}
