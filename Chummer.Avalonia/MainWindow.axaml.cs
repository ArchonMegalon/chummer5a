using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;

namespace Chummer.Avalonia;

public partial class MainWindow : Window
{
    private readonly ICharacterOverviewPresenter _presenter;
    private readonly IShellPresenter _shellPresenter;
    private readonly ICommandAvailabilityEvaluator _commandAvailabilityEvaluator;
    private readonly CharacterOverviewViewModelAdapter _adapter;
    private readonly TextBox _xmlInputBox;
    private readonly TextBlock _statusText;
    private readonly TextBlock _noticeText;
    private readonly TextBlock _workspaceText;
    private readonly TextBlock _nameValue;
    private readonly TextBlock _aliasValue;
    private readonly TextBlock _karmaValue;
    private readonly TextBlock _skillsValue;
    private readonly TextBlock _charStateText;
    private readonly TextBlock _serviceStateText;
    private readonly TextBlock _timeStateText;
    private readonly TextBlock _complianceStateText;
    private readonly ListBox _commandsList;
    private readonly ListBox _openWorkspacesList;
    private readonly ListBox _navigationTabsList;
    private readonly ListBox _sectionActionsList;
    private readonly ListBox _uiControlsList;
    private readonly ListBox _sectionRowsList;
    private readonly TextBox _sectionPreviewBox;
    private readonly TextBlock _dialogTitleText;
    private readonly TextBlock _dialogMessageText;
    private readonly ListBox _dialogFieldsList;
    private readonly ListBox _dialogActionsList;
    private DesktopDialogWindow? _dialogWindow;
    private bool _suppressCommandSelectionEvent;
    private bool _suppressWorkspaceSelectionEvent;
    private bool _suppressTabSelectionEvent;
    private bool _suppressSectionActionSelectionEvent;
    private bool _suppressUiControlSelectionEvent;
    private bool _suppressDialogActionSelectionEvent;

    public MainWindow(
        ICharacterOverviewPresenter presenter,
        IShellPresenter shellPresenter,
        ICommandAvailabilityEvaluator commandAvailabilityEvaluator,
        CharacterOverviewViewModelAdapter adapter)
    {
        InitializeComponent();

        _presenter = presenter;
        _shellPresenter = shellPresenter;
        _commandAvailabilityEvaluator = commandAvailabilityEvaluator;
        _adapter = adapter;
        _adapter.Updated += (_, _) => RefreshState();
        _shellPresenter.StateChanged += ShellPresenter_OnStateChanged;

        _xmlInputBox = XmlInputBox;
        _statusText = StatusText;
        _noticeText = NoticeText;
        _workspaceText = WorkspaceText;
        _nameValue = NameValue;
        _aliasValue = AliasValue;
        _karmaValue = KarmaValue;
        _skillsValue = SkillsValue;
        _charStateText = CharStateText;
        _serviceStateText = ServiceStateText;
        _timeStateText = TimeStateText;
        _complianceStateText = ComplianceStateText;
        _commandsList = CommandsList;
        _commandsList.SelectionChanged += CommandsList_OnSelectionChanged;
        _openWorkspacesList = OpenWorkspacesList;
        _openWorkspacesList.SelectionChanged += OpenWorkspacesList_OnSelectionChanged;
        _navigationTabsList = NavigationTabsList;
        _navigationTabsList.SelectionChanged += NavigationTabsList_OnSelectionChanged;
        _sectionActionsList = SectionActionsList;
        _sectionActionsList.SelectionChanged += SectionActionsList_OnSelectionChanged;
        _uiControlsList = UiControlsList;
        _uiControlsList.SelectionChanged += UiControlsList_OnSelectionChanged;
        _sectionRowsList = SectionRowsList;
        _sectionPreviewBox = SectionPreviewBox;
        _dialogTitleText = DialogTitleText;
        _dialogMessageText = DialogMessageText;
        _dialogFieldsList = DialogFieldsList;
        _dialogActionsList = DialogActionsList;
        _dialogActionsList.SelectionChanged += DialogActionsList_OnSelectionChanged;

        RefreshState();
        Opened += OnOpened;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_dialogWindow is not null)
        {
            _dialogWindow.CloseFromPresenter();
            _dialogWindow = null;
        }

        _shellPresenter.StateChanged -= ShellPresenter_OnStateChanged;
        _adapter.Dispose();
        base.OnClosed(e);
    }

    private async void ImportButton_OnClick(object? sender, RoutedEventArgs e)
    {
        string importText = _xmlInputBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(importText))
        {
            _statusText.Text = "State: provide debug XML content before importing.";
            return;
        }

        await RunUiActionAsync(
            () => _adapter.ImportAsync(Encoding.UTF8.GetBytes(importText), CancellationToken.None),
            "import debug XML");
    }

    private async void ImportFileButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!StorageProvider.CanOpen)
        {
            _statusText.Text = "State: file picker unavailable on this platform.";
            return;
        }

        await RunUiActionAsync(async () =>
        {
            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Character File",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Chummer Character Files")
                    {
                        Patterns = ["*.chum5", "*.xml"]
                    }
                ]
            });

            IStorageFile? file = files.FirstOrDefault();
            if (file is null)
                return;

            await using Stream stream = await file.OpenReadAsync();
            using MemoryStream memory = new();
            await stream.CopyToAsync(memory, CancellationToken.None);
            await _adapter.ImportAsync(memory.ToArray(), CancellationToken.None);
        }, "import character file");
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        await RunUiActionAsync(
            async () =>
            {
                await _shellPresenter.InitializeAsync(CancellationToken.None);
                await _adapter.InitializeAsync(CancellationToken.None);
            },
            "initialize desktop shell");
    }

    private async void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await RunUiActionAsync(
            () => _presenter.SaveAsync(CancellationToken.None),
            "save workspace");
    }

    private async void CloseWorkspaceButton_OnClick(object? sender, RoutedEventArgs e)
    {
        CharacterWorkspaceId? activeWorkspaceId = _adapter.State.Session.ActiveWorkspaceId ?? _adapter.State.WorkspaceId;
        if (activeWorkspaceId is null)
        {
            _statusText.Text = "State: no active workspace to close.";
            return;
        }

        await RunUiActionAsync(
            () => _adapter.CloseWorkspaceAsync(activeWorkspaceId.Value, CancellationToken.None),
            "close workspace");
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

    private async void MenuButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Content is null)
            return;

        string menuId = button.Content.ToString()!.Trim().ToLowerInvariant();
        await RunUiActionAsync(
            () => _shellPresenter.ToggleMenuAsync(menuId, CancellationToken.None),
            $"toggle menu '{menuId}'");
    }

    private void ShellPresenter_OnStateChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(RefreshState);
    }

    private async void CommandsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressCommandSelectionEvent)
            return;

        if (_commandsList.SelectedItem is not CommandListItem command || !command.Enabled)
            return;

        await RunUiActionAsync(
            async () =>
            {
                await _shellPresenter.ExecuteCommandAsync(command.Id, CancellationToken.None);
                await _adapter.ExecuteCommandAsync(command.Id, CancellationToken.None);
            },
            $"execute command '{command.Id}'");
        _suppressCommandSelectionEvent = true;
        _commandsList.SelectedItem = null;
        _suppressCommandSelectionEvent = false;
    }

    private async void OpenWorkspacesList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressWorkspaceSelectionEvent)
            return;

        if (_openWorkspacesList.SelectedItem is not WorkspaceListItem workspace || !workspace.Enabled)
            return;

        await RunUiActionAsync(
            () => _adapter.SwitchWorkspaceAsync(new CharacterWorkspaceId(workspace.Id), CancellationToken.None),
            $"switch workspace '{workspace.Id}'");
    }

    private async void NavigationTabsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressTabSelectionEvent)
            return;

        if (_navigationTabsList.SelectedItem is not TabListItem tab || !tab.Enabled)
            return;

        await RunUiActionAsync(
            async () =>
            {
                await _shellPresenter.SelectTabAsync(tab.Id, CancellationToken.None);
                if (!string.Equals(_shellPresenter.State.ActiveTabId, tab.Id, StringComparison.Ordinal))
                    return;

                await _adapter.SelectTabAsync(tab.Id, CancellationToken.None);
            },
            $"select tab '{tab.Id}'");
    }

    private async void SectionActionsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSectionActionSelectionEvent)
            return;

        if (_sectionActionsList.SelectedItem is not SectionActionListItem action)
            return;

        await RunUiActionAsync(
            () => _adapter.ExecuteWorkspaceActionAsync(action.Action, CancellationToken.None),
            $"execute workspace action '{action.Id}'");
        _suppressSectionActionSelectionEvent = true;
        _sectionActionsList.SelectedItem = null;
        _suppressSectionActionSelectionEvent = false;
    }

    private async void UiControlsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressUiControlSelectionEvent)
            return;

        if (_uiControlsList.SelectedItem is not UiControlListItem control)
            return;

        await RunUiActionAsync(
            () => _adapter.HandleUiControlAsync(control.Id, CancellationToken.None),
            $"handle desktop control '{control.Id}'");
        _suppressUiControlSelectionEvent = true;
        _uiControlsList.SelectedItem = null;
        _suppressUiControlSelectionEvent = false;
    }

    private async void DialogActionsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressDialogActionSelectionEvent)
            return;

        if (_dialogActionsList.SelectedItem is not DialogActionListItem action)
            return;

        await RunUiActionAsync(
            () => _adapter.ExecuteDialogActionAsync(action.Id, CancellationToken.None),
            $"execute dialog action '{action.Id}'");
        _suppressDialogActionSelectionEvent = true;
        _dialogActionsList.SelectedItem = null;
        _suppressDialogActionSelectionEvent = false;
    }

    private async void Window_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        string? commandId = e.Key switch
        {
            Key.S => "save_character",
            Key.W => "close_window",
            Key.G => "global_settings",
            _ => null
        };

        if (string.IsNullOrWhiteSpace(commandId))
            return;

        e.Handled = true;
        await RunUiActionAsync(
            async () =>
            {
                await _shellPresenter.ExecuteCommandAsync(commandId, CancellationToken.None);
                await _adapter.ExecuteCommandAsync(commandId, CancellationToken.None);
            },
            $"execute hotkey command '{commandId}'");
    }

    private void SyncDialogWindow(CharacterOverviewState state)
    {
        if (state.ActiveDialog is null)
        {
            if (_dialogWindow is not null)
            {
                _dialogWindow.CloseFromPresenter();
                _dialogWindow = null;
            }

            return;
        }

        if (_dialogWindow is null)
        {
            _dialogWindow = new DesktopDialogWindow(_adapter);
            _dialogWindow.Closed += DialogWindow_OnClosed;
        }

        _dialogWindow.BindDialog(state.ActiveDialog);
        if (!_dialogWindow.IsVisible)
        {
            _dialogWindow.Show(this);
        }
    }

    private void DialogWindow_OnClosed(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, _dialogWindow))
        {
            _dialogWindow = null;
        }
    }

    private async Task RunUiActionAsync(Func<Task> operation, string operationName)
    {
        try
        {
            await operation();
        }
        catch (OperationCanceledException)
        {
            // UI-triggered operations are best-effort; canceled actions should not fault the window thread.
        }
        catch (Exception ex)
        {
            _statusText.Text = $"State: error - {operationName} failed: {ex.Message}";
            _noticeText.Text = $"Notice: {operationName} failed.";
            _serviceStateText.Text = "Service: error";
            _timeStateText.Text = $"Time: {DateTimeOffset.UtcNow:u}";
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
