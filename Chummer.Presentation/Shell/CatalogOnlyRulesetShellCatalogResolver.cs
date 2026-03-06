using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;

namespace Chummer.Presentation.Shell;

internal sealed class CatalogOnlyRulesetShellCatalogResolver : IRulesetShellCatalogResolver
{
    public IReadOnlyList<AppCommandDefinition> ResolveCommands(string? rulesetId)
    {
        return AppCommandCatalog.ForRuleset(rulesetId);
    }

    public IReadOnlyList<NavigationTabDefinition> ResolveNavigationTabs(string? rulesetId)
    {
        return NavigationTabCatalog.ForRuleset(rulesetId);
    }

    public IReadOnlyList<WorkspaceSurfaceActionDefinition> ResolveWorkspaceActionsForTab(string? tabId, string? rulesetId)
    {
        return WorkspaceSurfaceActionCatalog.ForTab(tabId, rulesetId);
    }

    public IReadOnlyList<DesktopUiControlDefinition> ResolveDesktopUiControlsForTab(string? tabId, string? rulesetId)
    {
        return DesktopUiControlCatalog.ForTab(tabId, rulesetId);
    }
}
