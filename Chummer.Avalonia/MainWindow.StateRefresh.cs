using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
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
        ActiveWorkspaceContext workspaceContext = ResolveActiveWorkspaceContext(state);
        UpdateHeaderState(state, shellState, workspaceContext);
        RefreshCommands(state, shellState);
        RefreshOpenWorkspaces(state, workspaceContext.ActiveWorkspaceId);
        RefreshNavigationTabs(state, shellState);
        RefreshSectionActions(state, shellState);
        RefreshUiControls(state, shellState);
        RefreshSectionPreview(state);
        RefreshDialogState(state);

        SyncDialogWindow(state);
        DispatchPendingDownload(state);
    }

    private static ActiveWorkspaceContext ResolveActiveWorkspaceContext(CharacterOverviewState state)
    {
        int openWorkspaceCount = state.Session.OpenWorkspaces.Count;
        CharacterWorkspaceId? activeWorkspaceId = state.Session.ActiveWorkspaceId ?? state.WorkspaceId;
        OpenWorkspaceState? activeWorkspace = state.Session.OpenWorkspaces
            .FirstOrDefault(workspace => string.Equals(workspace.Id.Value, activeWorkspaceId?.Value, StringComparison.Ordinal));
        string activeWorkspaceSaveStatus = activeWorkspace is null
            ? "n/a"
            : activeWorkspace.HasSavedWorkspace ? "saved" : "unsaved";
        return new ActiveWorkspaceContext(activeWorkspaceId, openWorkspaceCount, activeWorkspaceSaveStatus);
    }

    private void UpdateHeaderState(CharacterOverviewState state, ShellState shellState, ActiveWorkspaceContext workspaceContext)
    {
        _statusText.Text = state.Error is null
            ? $"State: {(state.IsBusy ? "busy" : "ready")}, workspace={(workspaceContext.ActiveWorkspaceId?.Value ?? "none")}, open={workspaceContext.OpenWorkspaceCount}, saved={state.HasSavedWorkspace}, last-command={(state.LastCommandId ?? "none")}"
            : $"State: error - {state.Error}";
        _noticeText.Text = $"Notice: {(state.Notice ?? "Ready.")}";
        _workspaceText.Text = $"Workspace: {(workspaceContext.ActiveWorkspaceId?.Value ?? "none")} (open: {workspaceContext.OpenWorkspaceCount}, {workspaceContext.ActiveWorkspaceSaveStatus})";

        _nameValue.Text = state.Profile?.Name ?? "-";
        _aliasValue.Text = state.Profile?.Alias ?? "-";
        _karmaValue.Text = state.Progress?.Karma.ToString() ?? "-";
        _skillsValue.Text = state.Skills?.Count.ToString() ?? "-";

        _charStateText.Text = $"Character: {(workspaceContext.ActiveWorkspaceId is null ? "none" : "loaded")}";
        _serviceStateText.Text = $"Service: {(state.Error is null ? "online" : "error")}";
        _timeStateText.Text = $"Time: {DateTimeOffset.UtcNow:u}";
        _complianceStateText.Text = $"Prefs: {state.Preferences.UiScalePercent}%/{state.Preferences.Theme}/{state.Preferences.Language}";
        UpdateMenuButtonStates(shellState, state.IsBusy);
    }

    private void RefreshCommands(CharacterOverviewState state, ShellState shellState)
    {
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
    }

    private void RefreshOpenWorkspaces(CharacterOverviewState state, CharacterWorkspaceId? activeWorkspaceId)
    {
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
    }

    private void RefreshNavigationTabs(CharacterOverviewState state, ShellState shellState)
    {
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
    }

    private void RefreshSectionActions(CharacterOverviewState state, ShellState shellState)
    {
        WorkspaceSurfaceActionDefinition[] actions = RulesetShellCatalogResolver.ResolveWorkspaceActionsForTab(state.ActiveTabId, shellState.ActiveRulesetId, _rulesetPlugins)
            .Where(action => _commandAvailabilityEvaluator.IsWorkspaceActionEnabled(action, state))
            .ToArray();
        SectionActionListItem[] sectionActionItems = actions
            .Select(action => new SectionActionListItem(action))
            .ToArray();
        _suppressSectionActionSelectionEvent = true;
        _sectionActionsList.ItemsSource = sectionActionItems;
        _sectionActionsList.SelectedItem = sectionActionItems.FirstOrDefault(item => string.Equals(item.Id, state.ActiveActionId, StringComparison.Ordinal));
        _suppressSectionActionSelectionEvent = false;
    }

    private void RefreshUiControls(CharacterOverviewState state, ShellState shellState)
    {
        DesktopUiControlDefinition[] uiControls = RulesetShellCatalogResolver.ResolveDesktopUiControlsForTab(state.ActiveTabId, shellState.ActiveRulesetId, _rulesetPlugins)
            .Where(control => _commandAvailabilityEvaluator.IsUiControlEnabled(control, state))
            .ToArray();
        _suppressUiControlSelectionEvent = true;
        _uiControlsList.ItemsSource = uiControls.Select(control => new UiControlListItem(control.Id, control.Label)).ToArray();
        _uiControlsList.SelectedItem = null;
        _suppressUiControlSelectionEvent = false;
    }

    private void RefreshSectionPreview(CharacterOverviewState state)
    {
        _sectionPreviewBox.Text = state.ActiveSectionJson ?? string.Empty;
        _sectionRowsList.ItemsSource = state.ActiveSectionRows
            .Select(row => new SectionRowListItem(row.Path, row.Value))
            .ToArray();
    }

    private void RefreshDialogState(CharacterOverviewState state)
    {
        if (state.ActiveDialog is null)
        {
            _dialogTitleText.Text = "(none)";
            _dialogMessageText.Text = "(none)";
            _dialogFieldsList.ItemsSource = Array.Empty<DialogFieldListItem>();
            _dialogActionsList.ItemsSource = Array.Empty<DialogActionListItem>();
            return;
        }

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

    private void DispatchPendingDownload(CharacterOverviewState state)
    {
        WorkspaceDownloadReceipt? pendingDownload = state.PendingDownload;
        if (pendingDownload is null || state.PendingDownloadVersion <= _lastDownloadVersionHandled)
            return;

        long pendingVersion = state.PendingDownloadVersion;
        _lastDownloadVersionHandled = pendingVersion;
        _ = RunUiActionAsync(
            () => SavePendingDownloadAsync(pendingDownload, pendingVersion),
            "save-as download");
    }

    private async Task SavePendingDownloadAsync(WorkspaceDownloadReceipt pendingDownload, long pendingVersion)
    {
        if (pendingVersion < _lastDownloadVersionHandled)
            return;

        if (!StorageProvider.CanSave)
        {
            _noticeText.Text = "Notice: save-as requested but file save is unavailable on this platform.";
            return;
        }

        IReadOnlyList<FilePickerFileType> fileTypes =
        [
            new FilePickerFileType("Chummer Character Files")
            {
                Patterns = ["*.chum5", "*.xml"],
                MimeTypes = ["application/xml"]
            }
        ];

        IStorageFile? targetFile = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Character As",
            SuggestedFileName = pendingDownload.FileName,
            FileTypeChoices = fileTypes,
            ShowOverwritePrompt = true
        });

        if (targetFile is null)
        {
            _noticeText.Text = "Notice: save-as canceled.";
            return;
        }

        byte[] payloadBytes = Convert.FromBase64String(pendingDownload.ContentBase64);
        await using Stream output = await targetFile.OpenWriteAsync();
        if (output.CanSeek)
            output.SetLength(0);

        await output.WriteAsync(payloadBytes, CancellationToken.None);
        await output.FlushAsync(CancellationToken.None);
        _noticeText.Text = $"Notice: downloaded {pendingDownload.FileName} to {targetFile.Name}.";
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

    private sealed record ActiveWorkspaceContext(
        CharacterWorkspaceId? ActiveWorkspaceId,
        int OpenWorkspaceCount,
        string ActiveWorkspaceSaveStatus);
}
