using Chummer.Contracts.Presentation;

namespace Chummer.Contracts.Rulesets;

public interface IRulesetPluginRegistry
{
    IReadOnlyList<IRulesetPlugin> All { get; }

    IRulesetPlugin? Resolve(string? rulesetId);
}

public interface IRulesetSelectionPolicy
{
    string GetDefaultRulesetId();
}

public interface IRulesetShellCatalogResolver
{
    IReadOnlyList<AppCommandDefinition> ResolveCommands(string? rulesetId);

    IReadOnlyList<NavigationTabDefinition> ResolveNavigationTabs(string? rulesetId);

    IReadOnlyList<WorkspaceSurfaceActionDefinition> ResolveWorkspaceActionsForTab(string? tabId, string? rulesetId);

    IReadOnlyList<DesktopUiControlDefinition> ResolveDesktopUiControlsForTab(string? tabId, string? rulesetId);
}
