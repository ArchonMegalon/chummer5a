using Chummer.Contracts.Presentation;
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

        string preferredRulesetId = ResolveRulesetId(
            shellState.PreferredRulesetId,
            shellState.OpenWorkspaces.Select(workspace => workspace.RulesetId),
            shellState.Commands.Select(command => command.RulesetId),
            shellState.NavigationTabs.Select(tab => tab.RulesetId));
        string activeRulesetId = ResolveRulesetId(
            shellState.ActiveRulesetId,
            shellState.OpenWorkspaces.Select(workspace => workspace.RulesetId),
            [preferredRulesetId]);
        string? activeTabId = shellState.ActiveTabId;
        CharacterWorkspaceId? activeWorkspaceId = shellState.ActiveWorkspaceId;
        IReadOnlyList<OpenWorkspaceState> openWorkspaces = shellState.OpenWorkspaces
            .Select(workspace => new OpenWorkspaceState(
                Id: workspace.Id,
                Name: workspace.Name,
                Alias: workspace.Alias,
                LastOpenedUtc: workspace.LastOpenedUtc,
                RulesetId: RulesetDefaults.NormalizeOptional(workspace.RulesetId) ?? string.Empty,
                HasSavedWorkspace: workspace.HasSavedWorkspace))
            .ToArray();

        WorkspaceSurfaceActionDefinition[] workspaceActions = string.IsNullOrWhiteSpace(activeRulesetId)
            ? []
            : _catalogResolver.ResolveWorkspaceActionsForTab(
                    activeTabId,
                    activeRulesetId)
                .Where(action => _availabilityEvaluator.IsWorkspaceActionEnabled(action, overviewState))
                .ToArray();

        DesktopUiControlDefinition[] uiControls = string.IsNullOrWhiteSpace(activeRulesetId)
            ? []
            : _catalogResolver.ResolveDesktopUiControlsForTab(
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
            LastCommandId: shellState.LastCommandId);

        return state with
        {
            OpenMenuId = shellState.OpenMenuId,
            Notice = shellState.Notice,
            Error = shellState.Error
        };
    }

    private static string ResolveRulesetId(
        string? preferredCandidate,
        IEnumerable<string?> primaryCandidates,
        IEnumerable<string?> secondaryCandidates,
        IEnumerable<string?>? tertiaryCandidates = null)
    {
        return RulesetDefaults.NormalizeOptional(preferredCandidate)
            ?? primaryCandidates.Select(RulesetDefaults.NormalizeOptional).FirstOrDefault(candidate => candidate is not null)
            ?? secondaryCandidates.Select(RulesetDefaults.NormalizeOptional).FirstOrDefault(candidate => candidate is not null)
            ?? (tertiaryCandidates?.Select(RulesetDefaults.NormalizeOptional).FirstOrDefault(candidate => candidate is not null))
            ?? string.Empty;
    }
}
