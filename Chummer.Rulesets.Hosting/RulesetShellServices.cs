using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;

namespace Chummer.Contracts.Rulesets;

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
        string? normalizedRulesetId = RulesetDefaults.NormalizeOptional(rulesetId);
        return normalizedRulesetId is null
            ? null
            : _pluginsByRuleset.GetValueOrDefault(normalizedRulesetId);
    }
}

public sealed class DefaultRulesetSelectionPolicy : IRulesetSelectionPolicy
{
    private readonly IRulesetPluginRegistry _pluginRegistry;

    public DefaultRulesetSelectionPolicy(IRulesetPluginRegistry pluginRegistry)
    {
        _pluginRegistry = pluginRegistry;
    }

    public string GetDefaultRulesetId()
    {
        return _pluginRegistry.All
            .Select(plugin => plugin.Id.NormalizedValue)
            .FirstOrDefault(rulesetId => !string.IsNullOrWhiteSpace(rulesetId))
            ?? string.Empty;
    }
}

public sealed class RulesetShellCatalogResolverService : IRulesetShellCatalogResolver
{
    private readonly IRulesetPluginRegistry _pluginRegistry;
    private readonly IRulesetSelectionPolicy _rulesetSelectionPolicy;

    public RulesetShellCatalogResolverService(
        IRulesetPluginRegistry pluginRegistry,
        IRulesetSelectionPolicy? rulesetSelectionPolicy = null)
    {
        _pluginRegistry = pluginRegistry;
        _rulesetSelectionPolicy = rulesetSelectionPolicy ?? new DefaultRulesetSelectionPolicy(pluginRegistry);
    }

    public IReadOnlyList<AppCommandDefinition> ResolveCommands(string? rulesetId)
    {
        return ResolveRequiredPlugin(rulesetId).ShellDefinitions.GetCommands();
    }

    public IReadOnlyList<NavigationTabDefinition> ResolveNavigationTabs(string? rulesetId)
    {
        return ResolveRequiredPlugin(rulesetId).ShellDefinitions.GetNavigationTabs();
    }

    public IReadOnlyList<WorkspaceSurfaceActionDefinition> ResolveWorkspaceActionsForTab(string? tabId, string? rulesetId)
    {
        return SelectTabActions(ResolveRequiredPlugin(rulesetId).Catalogs.GetWorkspaceActions(), tabId);
    }

    public IReadOnlyList<DesktopUiControlDefinition> ResolveDesktopUiControlsForTab(string? tabId, string? rulesetId)
    {
        return SelectTabControls(ResolveRequiredPlugin(rulesetId).Catalogs.GetDesktopUiControls(), tabId);
    }

    private IRulesetPlugin ResolveRequiredPlugin(string? requestedRulesetId)
    {
        string effectiveRulesetId = RulesetDefaults.NormalizeOptional(requestedRulesetId)
            ?? RulesetDefaults.NormalizeOptional(_rulesetSelectionPolicy.GetDefaultRulesetId())
            ?? throw new InvalidOperationException("No ruleset plugin is registered to provide shell metadata.");

        return _pluginRegistry.Resolve(effectiveRulesetId)
            ?? throw new InvalidOperationException(
                $"No ruleset plugin is registered for ruleset '{effectiveRulesetId}'.");
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
