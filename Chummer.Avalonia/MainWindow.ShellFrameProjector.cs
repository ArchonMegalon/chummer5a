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
        ActiveWorkspaceContext workspaceContext = ResolveActiveWorkspaceContext(shellSurface, state);
        IReadOnlyDictionary<string, WorkspaceSurfaceActionDefinition> workspaceActionsById = BuildWorkspaceActionLookup(shellSurface.WorkspaceActions);

        return new MainWindowShellFrame(
            ToolStripStatusText: BuildToolStripStatusText(state, shellSurface, workspaceContext),
            NoticeText: $"Notice: {(shellSurface.Notice ?? "Ready.")}",
            WorkspaceStripText: $"Workspace: {(workspaceContext.ActiveWorkspaceId?.Value ?? "none")} (open: {workspaceContext.OpenWorkspaceCount}, {workspaceContext.ActiveWorkspaceSaveStatus})",
            SummaryName: state.Profile?.Name,
            SummaryAlias: state.Profile?.Alias,
            SummaryKarma: state.Progress?.Karma.ToString(),
            SummarySkills: state.Skills?.Count.ToString(),
            CharacterStatusText: $"Character: {(workspaceContext.ActiveWorkspaceId is null ? "none" : "loaded")}",
            ServiceStatusText: $"Service: {(shellSurface.Error is null ? "online" : "error")}",
            TimeStatusText: $"Time: {DateTimeOffset.UtcNow:u}",
            ComplianceStatusText: $"Ruleset: {shellSurface.ActiveRulesetId} | Prefs: {state.Preferences.UiScalePercent}%/{state.Preferences.Theme}/{state.Preferences.Language}",
            KnownMenuIds: shellSurface.MenuRoots.Select(menu => menu.Id).ToArray(),
            OpenMenuId: shellSurface.OpenMenuId,
            IsBusy: state.IsBusy,
            Commands: ProjectCommands(state, shellSurface, commandAvailabilityEvaluator),
            LastCommandId: shellSurface.LastCommandId,
            OpenWorkspaces: ProjectOpenWorkspaces(state, shellSurface),
            SelectedWorkspaceId: shellSurface.ActiveWorkspaceId?.Value,
            NavigationTabs: ProjectNavigationTabs(state, shellSurface, commandAvailabilityEvaluator),
            ActiveTabId: shellSurface.ActiveTabId,
            SectionActions: ProjectSectionActions(shellSurface),
            ActiveActionId: state.ActiveActionId,
            UiControls: ProjectUiControls(shellSurface),
            SectionPreviewJson: state.ActiveSectionJson ?? string.Empty,
            SectionRows: state.ActiveSectionRows
                .Select(row => new SectionRowDisplayItem(row.Path, row.Value))
                .ToArray(),
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

    private static ActiveWorkspaceContext ResolveActiveWorkspaceContext(
        ShellSurfaceState shellSurface,
        CharacterOverviewState state)
    {
        int openWorkspaceCount = shellSurface.OpenWorkspaces.Count;
        CharacterWorkspaceId? activeWorkspaceId = shellSurface.ActiveWorkspaceId ?? state.WorkspaceId;
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
    string NoticeText,
    string WorkspaceStripText,
    string? SummaryName,
    string? SummaryAlias,
    string? SummaryKarma,
    string? SummarySkills,
    string CharacterStatusText,
    string ServiceStatusText,
    string TimeStatusText,
    string ComplianceStatusText,
    IReadOnlyList<string> KnownMenuIds,
    string? OpenMenuId,
    bool IsBusy,
    CommandPaletteItem[] Commands,
    string? LastCommandId,
    NavigatorWorkspaceItem[] OpenWorkspaces,
    string? SelectedWorkspaceId,
    NavigatorTabItem[] NavigationTabs,
    string? ActiveTabId,
    NavigatorSectionActionItem[] SectionActions,
    string? ActiveActionId,
    NavigatorUiControlItem[] UiControls,
    string SectionPreviewJson,
    SectionRowDisplayItem[] SectionRows,
    IReadOnlyDictionary<string, WorkspaceSurfaceActionDefinition> WorkspaceActionsById);
