using System.Linq;
using Avalonia.Threading;
using Chummer.Avalonia.Controls;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;

namespace Chummer.Avalonia;

public partial class MainWindow
{
    private void ShellPresenter_OnStateChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(RefreshState);
    }

    private void RefreshState()
    {
        CharacterOverviewState state = _adapter.State;
        ShellSurfaceState shellSurface = _shellSurfaceResolver.Resolve(state, _shellPresenter.State);
        ActiveWorkspaceContext workspaceContext = ResolveActiveWorkspaceContext(shellSurface, state);
        UpdateHeaderState(state, shellSurface, workspaceContext);
        RefreshCommands(state, shellSurface);
        RefreshOpenWorkspaces(state, shellSurface);
        RefreshNavigationTabs(state, shellSurface);
        RefreshSectionActions(state, shellSurface);
        RefreshUiControls(shellSurface);
        RefreshSectionPreview(state);
        RefreshDialogState(state);

        SyncDialogWindow(state);
        DispatchPendingDownload(state);
    }

    private static ActiveWorkspaceContext ResolveActiveWorkspaceContext(ShellSurfaceState shellSurface, CharacterOverviewState state)
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

    private void UpdateHeaderState(
        CharacterOverviewState state,
        ShellSurfaceState shellSurface,
        ActiveWorkspaceContext workspaceContext)
    {
        string statusText = shellSurface.Error is null
            ? $"State: {(state.IsBusy ? "busy" : "ready")}, workspace={(workspaceContext.ActiveWorkspaceId?.Value ?? "none")}, open={workspaceContext.OpenWorkspaceCount}, saved={state.HasSavedWorkspace}, last-command={(shellSurface.LastCommandId ?? "none")}"
            : $"State: error - {shellSurface.Error}";
        _toolStrip.SetStatusText(statusText);
        _sectionHost.SetNotice($"Notice: {(shellSurface.Notice ?? "Ready.")}");
        _workspaceStrip.SetWorkspaceText($"Workspace: {(workspaceContext.ActiveWorkspaceId?.Value ?? "none")} (open: {workspaceContext.OpenWorkspaceCount}, {workspaceContext.ActiveWorkspaceSaveStatus})");

        _summaryHeader.SetValues(
            name: state.Profile?.Name,
            alias: state.Profile?.Alias,
            karma: state.Progress?.Karma.ToString(),
            skills: state.Skills?.Count.ToString());

        _statusStrip.SetValues(
            characterState: $"Character: {(workspaceContext.ActiveWorkspaceId is null ? "none" : "loaded")}",
            serviceState: $"Service: {(shellSurface.Error is null ? "online" : "error")}",
            timeState: $"Time: {DateTimeOffset.UtcNow:u}",
            complianceState: $"Ruleset: {shellSurface.ActiveRulesetId} | Prefs: {state.Preferences.UiScalePercent}%/{state.Preferences.Theme}/{state.Preferences.Language}");
        UpdateMenuButtonStates(shellSurface, state.IsBusy);
    }

    private void RefreshCommands(CharacterOverviewState state, ShellSurfaceState shellSurface)
    {
        IEnumerable<AppCommandDefinition> visibleCommands = shellSurface.Commands
            .Where(command => !string.Equals(command.Group, "menu", StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(shellSurface.OpenMenuId))
        {
            visibleCommands = visibleCommands.Where(command => string.Equals(command.Group, shellSurface.OpenMenuId, StringComparison.Ordinal));
        }

        CommandPaletteItem[] commands = visibleCommands
            .Select(command => new CommandPaletteItem(
                command.Id,
                command.Group,
                _commandAvailabilityEvaluator.IsCommandEnabled(command, state)))
            .ToArray();

        _commandDialogPane.SetCommands(commands, shellSurface.LastCommandId);
    }

    private void RefreshOpenWorkspaces(CharacterOverviewState state, ShellSurfaceState shellSurface)
    {
        NavigatorWorkspaceItem[] openWorkspaces = shellSurface.OpenWorkspaces
            .Select(workspace => new NavigatorWorkspaceItem(
                workspace.Id.Value,
                workspace.Name,
                workspace.Alias,
                workspace.HasSavedWorkspace,
                Enabled: !state.IsBusy))
            .ToArray();

        _navigatorPane.SetOpenWorkspaces(openWorkspaces, shellSurface.ActiveWorkspaceId?.Value);
    }

    private void RefreshNavigationTabs(CharacterOverviewState state, ShellSurfaceState shellSurface)
    {
        NavigatorTabItem[] tabs = shellSurface.NavigationTabs
            .Select(tab => new NavigatorTabItem(
                tab.Id,
                tab.Label,
                tab.SectionId,
                tab.Group,
                _commandAvailabilityEvaluator.IsNavigationTabEnabled(tab, state)))
            .ToArray();

        _navigatorPane.SetNavigationTabs(tabs, shellSurface.ActiveTabId);
    }

    private void RefreshSectionActions(CharacterOverviewState state, ShellSurfaceState shellSurface)
    {
        NavigatorSectionActionItem[] sectionActionItems = shellSurface.WorkspaceActions
            .Select(action => new NavigatorSectionActionItem(
                action.Id,
                action.Label,
                action.Kind))
            .ToArray();
        _navigatorPane.SetSectionActions(sectionActionItems, state.ActiveActionId);
    }

    private void RefreshUiControls(ShellSurfaceState shellSurface)
    {
        NavigatorUiControlItem[] controls = shellSurface.DesktopUiControls
            .Select(control => new NavigatorUiControlItem(control.Id, control.Label))
            .ToArray();
        _navigatorPane.SetUiControls(controls);
    }

    private void RefreshSectionPreview(CharacterOverviewState state)
    {
        SectionRowDisplayItem[] rows = state.ActiveSectionRows
            .Select(row => new SectionRowDisplayItem(row.Path, row.Value))
            .ToArray();
        _sectionHost.SetSectionPreview(state.ActiveSectionJson ?? string.Empty, rows);
    }

    private void RefreshDialogState(CharacterOverviewState state)
    {
        if (state.ActiveDialog is null)
        {
            _commandDialogPane.SetDialog(
                title: null,
                message: null,
                fields: Array.Empty<DialogFieldDisplayItem>(),
                actions: Array.Empty<DialogActionDisplayItem>());
            return;
        }

        DialogFieldDisplayItem[] fields = state.ActiveDialog.Fields
            .Select(field => new DialogFieldDisplayItem(field.Id, field.Label, field.Value))
            .ToArray();
        DialogActionDisplayItem[] actions = state.ActiveDialog.Actions
            .Select(action => new DialogActionDisplayItem(action.Id, action.Label, action.IsPrimary))
            .ToArray();
        _commandDialogPane.SetDialog(
            title: state.ActiveDialog.Title,
            message: state.ActiveDialog.Message,
            fields: fields,
            actions: actions);
    }

    private void UpdateMenuButtonStates(ShellSurfaceState shellSurface, bool isBusy)
    {
        _menuBar.SetMenuState(
            openMenuId: shellSurface.OpenMenuId,
            knownMenuIds: shellSurface.MenuRoots.Select(menu => menu.Id),
            isBusy: isBusy);
    }

    private sealed record ActiveWorkspaceContext(
        CharacterWorkspaceId? ActiveWorkspaceId,
        int OpenWorkspaceCount,
        string ActiveWorkspaceSaveStatus);
}
