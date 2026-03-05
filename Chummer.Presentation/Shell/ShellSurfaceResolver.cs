using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
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

        string preferredRulesetId = RulesetDefaults.Normalize(shellState.PreferredRulesetId);
        string activeRulesetId = string.IsNullOrWhiteSpace(shellState.ActiveRulesetId)
            ? preferredRulesetId
            : RulesetDefaults.Normalize(shellState.ActiveRulesetId);
        string? activeTabId = string.IsNullOrWhiteSpace(overviewState.ActiveTabId)
            ? shellState.ActiveTabId
            : overviewState.ActiveTabId;
        CharacterWorkspaceId? activeWorkspaceId = shellState.ActiveWorkspaceId
            ?? overviewState.Session.ActiveWorkspaceId
            ?? overviewState.WorkspaceId;
        IReadOnlyList<OpenWorkspaceState> openWorkspaces = ResolveOpenWorkspaces(overviewState, shellState);

        var workspaceActions = _catalogResolver.ResolveWorkspaceActionsForTab(
                activeTabId,
                activeRulesetId)
            .Where(action => _availabilityEvaluator.IsWorkspaceActionEnabled(action, overviewState))
            .ToArray();

        var uiControls = _catalogResolver.ResolveDesktopUiControlsForTab(
                activeTabId,
                activeRulesetId)
            .Where(control => _availabilityEvaluator.IsUiControlEnabled(control, overviewState))
            .ToArray();

        ShellSurfaceState state = new(
            Commands: shellState.Commands,
            MenuRoots: shellState.MenuRoots,
            NavigationTabs: shellState.NavigationTabs,
            WorkspaceActions: workspaceActions,
            DesktopUiControls: uiControls,
            OpenWorkspaces: openWorkspaces,
            ActiveRulesetId: activeRulesetId,
            PreferredRulesetId: preferredRulesetId,
            ActiveWorkspaceId: activeWorkspaceId,
            ActiveTabId: activeTabId,
            LastCommandId: shellState.LastCommandId ?? overviewState.LastCommandId);

        return state with
        {
            OpenMenuId = shellState.OpenMenuId,
            Notice = shellState.Notice ?? overviewState.Notice,
            Error = shellState.Error ?? overviewState.Error
        };
    }

    private static IReadOnlyList<OpenWorkspaceState> ResolveOpenWorkspaces(CharacterOverviewState overviewState, ShellState shellState)
    {
        if (overviewState.Session.OpenWorkspaces.Count > 0)
        {
            return overviewState.Session.OpenWorkspaces;
        }

        return shellState.OpenWorkspaces
            .Select(workspace => new OpenWorkspaceState(
                Id: workspace.Id,
                Name: workspace.Name,
                Alias: workspace.Alias,
                LastOpenedUtc: workspace.LastOpenedUtc,
                RulesetId: RulesetDefaults.Normalize(workspace.RulesetId),
                HasSavedWorkspace: false))
            .ToArray();
    }
}
