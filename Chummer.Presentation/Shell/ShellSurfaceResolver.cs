using Chummer.Contracts.Rulesets;
using Chummer.Presentation.Overview;

namespace Chummer.Presentation.Shell;

public sealed class ShellSurfaceResolver : IShellSurfaceResolver
{
    private readonly IRulesetShellCatalogResolver _catalogResolver;
    private readonly ICommandAvailabilityEvaluator _availabilityEvaluator;

    public ShellSurfaceResolver(
        IRulesetShellCatalogResolver catalogResolver,
        ICommandAvailabilityEvaluator availabilityEvaluator)
    {
        _catalogResolver = catalogResolver;
        _availabilityEvaluator = availabilityEvaluator;
    }

    public ShellSurfaceState Resolve(CharacterOverviewState overviewState, ShellState shellState)
    {
        ArgumentNullException.ThrowIfNull(overviewState);
        ArgumentNullException.ThrowIfNull(shellState);

        var workspaceActions = _catalogResolver.ResolveWorkspaceActionsForTab(
                overviewState.ActiveTabId,
                shellState.ActiveRulesetId)
            .Where(action => _availabilityEvaluator.IsWorkspaceActionEnabled(action, overviewState))
            .ToArray();

        var uiControls = _catalogResolver.ResolveDesktopUiControlsForTab(
                overviewState.ActiveTabId,
                shellState.ActiveRulesetId)
            .Where(control => _availabilityEvaluator.IsUiControlEnabled(control, overviewState))
            .ToArray();

        return new ShellSurfaceState(
            WorkspaceActions: workspaceActions,
            DesktopUiControls: uiControls);
    }
}
