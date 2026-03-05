using Chummer.Contracts.Presentation;

namespace Chummer.Contracts.Rulesets;

public interface IRulesetPluginRegistry
{
    IReadOnlyList<IRulesetPlugin> All { get; }

    IRulesetPlugin? Resolve(string? rulesetId);
}

public sealed class RulesetPluginRegistry : IRulesetPluginRegistry
{
    private readonly IReadOnlyList<IRulesetPlugin> _all;
    private readonly IReadOnlyDictionary<string, IRulesetPlugin> _pluginsByRuleset;

    public RulesetPluginRegistry(IEnumerable<IRulesetPlugin>? plugins)
    {
        _all = plugins?.ToArray() ?? [];

        Dictionary<string, IRulesetPlugin> pluginsByRuleset = new(StringComparer.Ordinal);
        foreach (IRulesetPlugin plugin in _all)
        {
            pluginsByRuleset[plugin.Id.NormalizedValue] = plugin;
        }

        _pluginsByRuleset = pluginsByRuleset;
    }

    public IReadOnlyList<IRulesetPlugin> All => _all;

    public IRulesetPlugin? Resolve(string? rulesetId)
    {
        string normalizedRulesetId = RulesetDefaults.Normalize(rulesetId);
        return _pluginsByRuleset.GetValueOrDefault(normalizedRulesetId);
    }
}

public interface IRulesetShellCatalogResolver
{
    IReadOnlyList<AppCommandDefinition> ResolveCommands(string? rulesetId);

    IReadOnlyList<NavigationTabDefinition> ResolveNavigationTabs(string? rulesetId);

    IReadOnlyList<WorkspaceSurfaceActionDefinition> ResolveWorkspaceActionsForTab(string? tabId, string? rulesetId);

    IReadOnlyList<DesktopUiControlDefinition> ResolveDesktopUiControlsForTab(string? tabId, string? rulesetId);
}

public sealed class RulesetShellCatalogResolverService : IRulesetShellCatalogResolver
{
    private readonly IRulesetPluginRegistry _pluginRegistry;

    public RulesetShellCatalogResolverService(IRulesetPluginRegistry pluginRegistry)
    {
        _pluginRegistry = pluginRegistry;
    }

    public IReadOnlyList<AppCommandDefinition> ResolveCommands(string? rulesetId)
    {
        IRulesetPlugin? plugin = _pluginRegistry.Resolve(rulesetId);
        return plugin is null
            ? AppCommandCatalog.ForRuleset(rulesetId)
            : plugin.ShellDefinitions.GetCommands();
    }

    public IReadOnlyList<NavigationTabDefinition> ResolveNavigationTabs(string? rulesetId)
    {
        IRulesetPlugin? plugin = _pluginRegistry.Resolve(rulesetId);
        return plugin is null
            ? NavigationTabCatalog.ForRuleset(rulesetId)
            : plugin.ShellDefinitions.GetNavigationTabs();
    }

    public IReadOnlyList<WorkspaceSurfaceActionDefinition> ResolveWorkspaceActionsForTab(string? tabId, string? rulesetId)
    {
        IRulesetPlugin? plugin = _pluginRegistry.Resolve(rulesetId);
        if (plugin is null)
            return WorkspaceSurfaceActionCatalog.ForTab(tabId, rulesetId);

        return SelectTabActions(plugin.Catalogs.GetWorkspaceActions(), tabId);
    }

    public IReadOnlyList<DesktopUiControlDefinition> ResolveDesktopUiControlsForTab(string? tabId, string? rulesetId)
    {
        IRulesetPlugin? plugin = _pluginRegistry.Resolve(rulesetId);
        if (plugin is null)
            return DesktopUiControlCatalog.ForTab(tabId, rulesetId);

        return SelectTabControls(plugin.Catalogs.GetDesktopUiControls(), tabId);
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
