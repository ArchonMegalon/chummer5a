using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
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
        ShellState shellState = _shellPresenter.State;
        int openWorkspaceCount = state.Session.OpenWorkspaces.Count;
        CharacterWorkspaceId? activeWorkspaceId = state.Session.ActiveWorkspaceId ?? state.WorkspaceId;
        OpenWorkspaceState? activeWorkspace = state.Session.OpenWorkspaces
            .FirstOrDefault(workspace => string.Equals(workspace.Id.Value, activeWorkspaceId?.Value, StringComparison.Ordinal));
        string activeWorkspaceSaveStatus = activeWorkspace is null
            ? "n/a"
            : activeWorkspace.HasSavedWorkspace ? "saved" : "unsaved";

        _statusText.Text = state.Error is null
            ? $"State: {(state.IsBusy ? "busy" : "ready")}, workspace={(activeWorkspaceId?.Value ?? "none")}, open={openWorkspaceCount}, saved={state.HasSavedWorkspace}, last-command={(state.LastCommandId ?? "none")}" 
            : $"State: error - {state.Error}";
        _noticeText.Text = $"Notice: {(state.Notice ?? "Ready.")}";
        _workspaceText.Text = $"Workspace: {(activeWorkspaceId?.Value ?? "none")} (open: {openWorkspaceCount}, {activeWorkspaceSaveStatus})";

        _nameValue.Text = state.Profile?.Name ?? "-";
        _aliasValue.Text = state.Profile?.Alias ?? "-";
        _karmaValue.Text = state.Progress?.Karma.ToString() ?? "-";
        _skillsValue.Text = state.Skills?.Count.ToString() ?? "-";

        _charStateText.Text = $"Character: {(activeWorkspaceId is null ? "none" : "loaded")}";
        _serviceStateText.Text = $"Service: {(state.Error is null ? "online" : "error")}";
        _timeStateText.Text = $"Time: {DateTimeOffset.UtcNow:u}";
        _complianceStateText.Text = $"Prefs: {state.Preferences.UiScalePercent}%/{state.Preferences.Theme}/{state.Preferences.Language}";
        UpdateMenuButtonStates(shellState, state.IsBusy);

        IEnumerable<AppCommandDefinition> visibleCommands = shellState.Commands
            .Where(command => !string.Equals(command.Group, "menu", StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(shellState.OpenMenuId))
        {
            visibleCommands = visibleCommands.Where(command => string.Equals(command.Group, shellState.OpenMenuId, StringComparison.Ordinal));
        }

        CommandListItem[] commands = visibleCommands
            .Select(command => new CommandListItem(
                command.Id,
                command.Group,
                _commandAvailabilityEvaluator.IsCommandEnabled(command, state)))
            .ToArray();

        _suppressCommandSelectionEvent = true;
        _commandsList.ItemsSource = commands;
        _commandsList.SelectedItem = commands.FirstOrDefault(item => string.Equals(item.Id, state.LastCommandId, StringComparison.Ordinal));
        _suppressCommandSelectionEvent = false;

        WorkspaceListItem[] openWorkspaces = state.Session.OpenWorkspaces
            .Select(workspace => new WorkspaceListItem(
                workspace.Id.Value,
                workspace.Name,
                workspace.Alias,
                workspace.HasSavedWorkspace,
                Enabled: !state.IsBusy))
            .ToArray();

        _suppressWorkspaceSelectionEvent = true;
        _openWorkspacesList.ItemsSource = openWorkspaces;
        _openWorkspacesList.SelectedItem = openWorkspaces.FirstOrDefault(item =>
            string.Equals(item.Id, activeWorkspaceId?.Value, StringComparison.Ordinal));
        _suppressWorkspaceSelectionEvent = false;

        TabListItem[] tabs = shellState.NavigationTabs
            .Select(tab => new TabListItem(
                tab.Id,
                tab.Label,
                tab.SectionId,
                tab.Group,
                _commandAvailabilityEvaluator.IsNavigationTabEnabled(tab, state)))
            .ToArray();

        _suppressTabSelectionEvent = true;
        _navigationTabsList.ItemsSource = tabs;
        _navigationTabsList.SelectedItem = tabs.FirstOrDefault(item => string.Equals(item.Id, state.ActiveTabId, StringComparison.Ordinal));
        _suppressTabSelectionEvent = false;

        WorkspaceSurfaceActionDefinition[] actions = WorkspaceSurfaceActionCatalog.ForTab(state.ActiveTabId)
            .Where(action => _commandAvailabilityEvaluator.IsWorkspaceActionEnabled(action, state))
            .ToArray();
        SectionActionListItem[] sectionActionItems = actions
            .Select(action => new SectionActionListItem(action))
            .ToArray();
        _suppressSectionActionSelectionEvent = true;
        _sectionActionsList.ItemsSource = sectionActionItems;
        _sectionActionsList.SelectedItem = sectionActionItems.FirstOrDefault(item => string.Equals(item.Id, state.ActiveActionId, StringComparison.Ordinal));
        _suppressSectionActionSelectionEvent = false;

        DesktopUiControlDefinition[] uiControls = DesktopUiControlCatalog.ForTab(state.ActiveTabId)
            .Where(control => _commandAvailabilityEvaluator.IsUiControlEnabled(control, state))
            .ToArray();
        _suppressUiControlSelectionEvent = true;
        _uiControlsList.ItemsSource = uiControls.Select(control => new UiControlListItem(control.Id, control.Label)).ToArray();
        _uiControlsList.SelectedItem = null;
        _suppressUiControlSelectionEvent = false;

        _sectionPreviewBox.Text = state.ActiveSectionJson ?? string.Empty;
        _sectionRowsList.ItemsSource = state.ActiveSectionRows
            .Select(row => new SectionRowListItem(row.Path, row.Value))
            .ToArray();

        if (state.ActiveDialog is null)
        {
            _dialogTitleText.Text = "(none)";
            _dialogMessageText.Text = "(none)";
            _dialogFieldsList.ItemsSource = Array.Empty<DialogFieldListItem>();
            _dialogActionsList.ItemsSource = Array.Empty<DialogActionListItem>();
        }
        else
        {
            _dialogTitleText.Text = state.ActiveDialog.Title;
            _dialogMessageText.Text = state.ActiveDialog.Message ?? "(none)";
            _dialogFieldsList.ItemsSource = state.ActiveDialog.Fields
                .Select(field => new DialogFieldListItem(field.Id, field.Label, field.Value))
                .ToArray();
            _suppressDialogActionSelectionEvent = true;
            _dialogActionsList.ItemsSource = state.ActiveDialog.Actions
                .Select(action => new DialogActionListItem(action.Id, action.Label, action.IsPrimary))
                .ToArray();
            _dialogActionsList.SelectedItem = null;
            _suppressDialogActionSelectionEvent = false;
        }

        SyncDialogWindow(state);
    }

    private void UpdateMenuButtonStates(ShellState shellState, bool isBusy)
    {
        HashSet<string> knownMenus = shellState.MenuRoots
            .Select(menu => menu.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (Button menuButton in _menuButtons)
        {
            string menuId = menuButton.Content?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
            bool known = knownMenus.Contains(menuId);
            bool active = known && string.Equals(shellState.OpenMenuId, menuId, StringComparison.Ordinal);

            menuButton.IsEnabled = known && !isBusy;
            menuButton.Classes.Set("active-menu", active);
        }
    }

    private sealed record WorkspaceListItem(
        string Id,
        string Name,
        string Alias,
        bool HasSavedWorkspace,
        bool Enabled)
    {
        public override string ToString()
        {
            string label = string.IsNullOrWhiteSpace(Alias) ? Name : $"{Name} ({Alias})";
            string saveTag = HasSavedWorkspace ? "saved" : "unsaved";
            return $"{label} [{Id}] [{saveTag}] {(Enabled ? "enabled" : "disabled")}";
        }
    }

    private sealed record TabListItem(
        string Id,
        string Label,
        string SectionId,
        string Group,
        bool Enabled)
    {
        public override string ToString()
        {
            return $"{Label} ({Id}) -> {SectionId}";
        }
    }

    private sealed record CommandListItem(
        string Id,
        string Group,
        bool Enabled)
    {
        public override string ToString()
        {
            return $"{Id} [{Group}] {(Enabled ? "enabled" : "disabled")}";
        }
    }

    private sealed record UiControlListItem(string Id, string Label)
    {
        public override string ToString()
        {
            return $"{Label} ({Id})";
        }
    }

    private sealed record SectionActionListItem(WorkspaceSurfaceActionDefinition Action)
    {
        public string Id => Action.Id;

        public override string ToString()
        {
            return $"{Action.Label} [{Action.Kind}]";
        }
    }

    private sealed record DialogFieldListItem(string Id, string Label, string Value)
    {
        public override string ToString()
        {
            return $"{Label}: {Value}";
        }
    }

    private sealed record SectionRowListItem(string Path, string Value)
    {
        public override string ToString()
        {
            return $"{Path} = {Value}";
        }
    }

    private sealed record DialogActionListItem(string Id, string Label, bool IsPrimary)
    {
        public override string ToString()
        {
            return $"{Label} ({Id}){(IsPrimary ? " *" : string.Empty)}";
        }
    }
}
