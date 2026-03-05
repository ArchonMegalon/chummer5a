using Chummer.Avalonia.Controls;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;

namespace Chummer.Avalonia;

internal static class MainWindowShellFrameProjector
{
    public static MainWindowShellFrame Project(
        CharacterOverviewState state,
        ShellSurfaceState shellSurface,
        ICommandAvailabilityEvaluator commandAvailabilityEvaluator)
    {
        ActiveWorkspaceContext workspaceContext = ResolveActiveWorkspaceContext(shellSurface);
        IReadOnlyDictionary<string, WorkspaceSurfaceActionDefinition> workspaceActionsById = BuildWorkspaceActionLookup(shellSurface.WorkspaceActions);

        return new MainWindowShellFrame(
            ToolStripStatusText: BuildToolStripStatusText(state, shellSurface, workspaceContext),
            ChromeState: new MainWindowChromeState(
                WorkspaceStrip: new WorkspaceStripState(
                    $"Workspace: {(workspaceContext.ActiveWorkspaceId?.Value ?? "none")} (open: {workspaceContext.OpenWorkspaceCount}, {workspaceContext.ActiveWorkspaceSaveStatus})"),
                SummaryHeader: new SummaryHeaderState(
                    Name: state.Profile?.Name,
                    Alias: state.Profile?.Alias,
                    Karma: state.Progress?.Karma.ToString(),
                    Skills: state.Skills?.Count.ToString()),
                StatusStrip: new StatusStripState(
                    CharacterState: $"Character: {(workspaceContext.ActiveWorkspaceId is null ? "none" : "loaded")}",
                    ServiceState: $"Service: {(shellSurface.Error is null ? "online" : "error")}",
                    TimeState: $"Time: {DateTimeOffset.UtcNow:u}",
                    ComplianceState: $"Ruleset: {shellSurface.ActiveRulesetId} | Prefs: {state.Preferences.UiScalePercent}%/{state.Preferences.Theme}/{state.Preferences.Language}")),
            SectionHostState: new SectionHostState(
                Notice: $"Notice: {(shellSurface.Notice ?? "Ready.")}",
                PreviewJson: state.ActiveSectionJson ?? string.Empty,
                Rows: state.ActiveSectionRows
                    .Select(row => new SectionRowDisplayItem(row.Path, row.Value))
                    .ToArray()),
            KnownMenuIds: shellSurface.MenuRoots.Select(menu => menu.Id).ToArray(),
            OpenMenuId: shellSurface.OpenMenuId,
            IsBusy: state.IsBusy,
            Commands: ProjectCommands(state, shellSurface, commandAvailabilityEvaluator),
            LastCommandId: shellSurface.LastCommandId,
            NavigatorPaneState: new NavigatorPaneState(
                OpenWorkspaces: ProjectOpenWorkspaces(state, shellSurface),
                SelectedWorkspaceId: shellSurface.ActiveWorkspaceId?.Value,
                NavigationTabs: ProjectNavigationTabs(state, shellSurface, commandAvailabilityEvaluator),
                ActiveTabId: shellSurface.ActiveTabId,
                SectionActions: ProjectSectionActions(shellSurface),
                ActiveActionId: state.ActiveActionId,
                UiControls: ProjectUiControls(shellSurface)),
            WorkspaceActionsById: workspaceActionsById);
    }

    private static string BuildToolStripStatusText(
        CharacterOverviewState state,
        ShellSurfaceState shellSurface,
        ActiveWorkspaceContext workspaceContext)
    {
        if (shellSurface.Error is not null)
        {
            return $"State: error - {shellSurface.Error}";
        }

        return $"State: {(state.IsBusy ? "busy" : "ready")}, workspace={(workspaceContext.ActiveWorkspaceId?.Value ?? "none")}, open={workspaceContext.OpenWorkspaceCount}, saved={state.HasSavedWorkspace}, last-command={(shellSurface.LastCommandId ?? "none")}";
    }

    private static ActiveWorkspaceContext ResolveActiveWorkspaceContext(ShellSurfaceState shellSurface)
    {
        int openWorkspaceCount = shellSurface.OpenWorkspaces.Count;
        CharacterWorkspaceId? activeWorkspaceId = shellSurface.ActiveWorkspaceId;
        OpenWorkspaceState? activeWorkspace = shellSurface.OpenWorkspaces
            .FirstOrDefault(workspace => string.Equals(workspace.Id.Value, activeWorkspaceId?.Value, StringComparison.Ordinal));
        string activeWorkspaceSaveStatus = activeWorkspace is null
            ? "n/a"
            : activeWorkspace.HasSavedWorkspace ? "saved" : "unsaved";
        return new ActiveWorkspaceContext(activeWorkspaceId, openWorkspaceCount, activeWorkspaceSaveStatus);
    }

    private static CommandPaletteItem[] ProjectCommands(
        CharacterOverviewState state,
        ShellSurfaceState shellSurface,
        ICommandAvailabilityEvaluator commandAvailabilityEvaluator)
    {
        IEnumerable<AppCommandDefinition> visibleCommands = shellSurface.Commands
            .Where(command => !string.Equals(command.Group, "menu", StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(shellSurface.OpenMenuId))
        {
            visibleCommands = visibleCommands.Where(command => string.Equals(command.Group, shellSurface.OpenMenuId, StringComparison.Ordinal));
        }

        return visibleCommands
            .Select(command => new CommandPaletteItem(
                command.Id,
                command.Group,
                commandAvailabilityEvaluator.IsCommandEnabled(command, state)))
            .ToArray();
    }

    private static NavigatorWorkspaceItem[] ProjectOpenWorkspaces(CharacterOverviewState state, ShellSurfaceState shellSurface)
    {
        return shellSurface.OpenWorkspaces
            .Select(workspace => new NavigatorWorkspaceItem(
                workspace.Id.Value,
                workspace.Name,
                workspace.Alias,
                workspace.HasSavedWorkspace,
                Enabled: !state.IsBusy))
            .ToArray();
    }

    private static NavigatorTabItem[] ProjectNavigationTabs(
        CharacterOverviewState state,
        ShellSurfaceState shellSurface,
        ICommandAvailabilityEvaluator commandAvailabilityEvaluator)
    {
        return shellSurface.NavigationTabs
            .Select(tab => new NavigatorTabItem(
                tab.Id,
                tab.Label,
                tab.SectionId,
                tab.Group,
                commandAvailabilityEvaluator.IsNavigationTabEnabled(tab, state)))
            .ToArray();
    }

    private static NavigatorSectionActionItem[] ProjectSectionActions(ShellSurfaceState shellSurface)
    {
        return shellSurface.WorkspaceActions
            .Select(action => new NavigatorSectionActionItem(
                action.Id,
                action.Label,
                action.Kind))
            .ToArray();
    }

    private static NavigatorUiControlItem[] ProjectUiControls(ShellSurfaceState shellSurface)
    {
        return shellSurface.DesktopUiControls
            .Select(control => new NavigatorUiControlItem(control.Id, control.Label))
            .ToArray();
    }

    private static IReadOnlyDictionary<string, WorkspaceSurfaceActionDefinition> BuildWorkspaceActionLookup(
        IReadOnlyList<WorkspaceSurfaceActionDefinition> workspaceActions)
    {
        var lookup = new Dictionary<string, WorkspaceSurfaceActionDefinition>(StringComparer.Ordinal);
        foreach (WorkspaceSurfaceActionDefinition action in workspaceActions)
        {
            lookup[action.Id] = action;
        }

        return lookup;
    }

    private sealed record ActiveWorkspaceContext(
        CharacterWorkspaceId? ActiveWorkspaceId,
        int OpenWorkspaceCount,
        string ActiveWorkspaceSaveStatus);
}

internal sealed record MainWindowShellFrame(
    string ToolStripStatusText,
    MainWindowChromeState ChromeState,
    SectionHostState SectionHostState,
    IReadOnlyList<string> KnownMenuIds,
    string? OpenMenuId,
    bool IsBusy,
    CommandPaletteItem[] Commands,
    string? LastCommandId,
    NavigatorPaneState NavigatorPaneState,
    IReadOnlyDictionary<string, WorkspaceSurfaceActionDefinition> WorkspaceActionsById);

internal sealed record MainWindowChromeState(
    WorkspaceStripState WorkspaceStrip,
    SummaryHeaderState SummaryHeader,
    StatusStripState StatusStrip);
