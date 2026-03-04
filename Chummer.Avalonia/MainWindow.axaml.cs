using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation;
using Chummer.Presentation.Overview;

namespace Chummer.Avalonia;

public partial class MainWindow : Window
{
    private readonly CharacterOverviewPresenter _presenter;
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
    private bool _suppressTabSelectionEvent;
    private bool _suppressSectionActionSelectionEvent;
    private bool _suppressUiControlSelectionEvent;
    private bool _suppressDialogActionSelectionEvent;
    private string? _activeMenuGroup;

    public MainWindow()
    {
        InitializeComponent();

        HttpClient httpClient = new()
        {
            BaseAddress = ResolveApiBaseAddress(),
            Timeout = TimeSpan.FromSeconds(20)
        };

        _presenter = new CharacterOverviewPresenter(new HttpChummerClient(httpClient));
        _adapter = new CharacterOverviewViewModelAdapter(_presenter);
        _adapter.Updated += (_, _) => RefreshState();

        _xmlInputBox = this.FindControl<TextBox>("XmlInputBox")!;
        _statusText = this.FindControl<TextBlock>("StatusText")!;
        _noticeText = this.FindControl<TextBlock>("NoticeText")!;
        _workspaceText = this.FindControl<TextBlock>("WorkspaceText")!;
        _nameValue = this.FindControl<TextBlock>("NameValue")!;
        _aliasValue = this.FindControl<TextBlock>("AliasValue")!;
        _karmaValue = this.FindControl<TextBlock>("KarmaValue")!;
        _skillsValue = this.FindControl<TextBlock>("SkillsValue")!;
        _charStateText = this.FindControl<TextBlock>("CharStateText")!;
        _serviceStateText = this.FindControl<TextBlock>("ServiceStateText")!;
        _timeStateText = this.FindControl<TextBlock>("TimeStateText")!;
        _complianceStateText = this.FindControl<TextBlock>("ComplianceStateText")!;
        _commandsList = this.FindControl<ListBox>("CommandsList")!;
        _commandsList.SelectionChanged += CommandsList_OnSelectionChanged;
        _navigationTabsList = this.FindControl<ListBox>("NavigationTabsList")!;
        _navigationTabsList.SelectionChanged += NavigationTabsList_OnSelectionChanged;
        _sectionActionsList = this.FindControl<ListBox>("SectionActionsList")!;
        _sectionActionsList.SelectionChanged += SectionActionsList_OnSelectionChanged;
        _uiControlsList = this.FindControl<ListBox>("UiControlsList")!;
        _uiControlsList.SelectionChanged += UiControlsList_OnSelectionChanged;
        _sectionRowsList = this.FindControl<ListBox>("SectionRowsList")!;
        _sectionPreviewBox = this.FindControl<TextBox>("SectionPreviewBox")!;
        _dialogTitleText = this.FindControl<TextBlock>("DialogTitleText")!;
        _dialogMessageText = this.FindControl<TextBlock>("DialogMessageText")!;
        _dialogFieldsList = this.FindControl<ListBox>("DialogFieldsList")!;
        _dialogActionsList = this.FindControl<ListBox>("DialogActionsList")!;
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

        await _adapter.ImportAsync(Encoding.UTF8.GetBytes(importText), CancellationToken.None);
    }

    private async void ImportFileButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!StorageProvider.CanOpen)
        {
            _statusText.Text = "State: file picker unavailable on this platform.";
            return;
        }

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
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        await _adapter.InitializeAsync(CancellationToken.None);
    }

    private async void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await _presenter.SaveAsync(CancellationToken.None);
    }

    private void RefreshState()
    {
        CharacterOverviewState state = _adapter.State;

        _statusText.Text = state.Error is null
            ? $"State: {(state.IsBusy ? "busy" : "ready")}, workspace={(state.WorkspaceId?.Value ?? "none")}, saved={state.HasSavedWorkspace}, last-command={(state.LastCommandId ?? "none")}"
            : $"State: error - {state.Error}";
        _noticeText.Text = $"Notice: {(state.Notice ?? "Ready.")}";
        _workspaceText.Text = $"Workspace: {(state.WorkspaceId?.Value ?? "none")}";

        _nameValue.Text = state.Profile?.Name ?? "-";
        _aliasValue.Text = state.Profile?.Alias ?? "-";
        _karmaValue.Text = state.Progress?.Karma.ToString() ?? "-";
        _skillsValue.Text = state.Skills?.Count.ToString() ?? "-";

        _charStateText.Text = $"Character: {(state.WorkspaceId is null ? "none" : "loaded")}";
        _serviceStateText.Text = $"Service: {(state.Error is null ? "online" : "error")}";
        _timeStateText.Text = $"Time: {DateTimeOffset.UtcNow:u}";
        _complianceStateText.Text = $"Prefs: {state.Preferences.UiScalePercent}%/{state.Preferences.Theme}/{state.Preferences.Language}";

        bool hasWorkspace = state.WorkspaceId is not null;
        IEnumerable<AppCommandDefinition> visibleCommands = state.Commands
            .Where(command => !string.Equals(command.Group, "menu", StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(_activeMenuGroup))
        {
            visibleCommands = visibleCommands.Where(command => string.Equals(command.Group, _activeMenuGroup, StringComparison.Ordinal));
        }

        CommandListItem[] commands = visibleCommands
            .Select(command => new CommandListItem(
                command.Id,
                command.Group,
                command.EnabledByDefault && (!command.RequiresOpenCharacter || hasWorkspace)))
            .ToArray();

        _suppressCommandSelectionEvent = true;
        _commandsList.ItemsSource = commands;
        _commandsList.SelectedItem = commands.FirstOrDefault(item => string.Equals(item.Id, state.LastCommandId, StringComparison.Ordinal));
        _suppressCommandSelectionEvent = false;

        TabListItem[] tabs = state.NavigationTabs
            .Select(tab => new TabListItem(
                tab.Id,
                tab.Label,
                tab.SectionId,
                tab.Group,
                tab.EnabledByDefault && (!tab.RequiresOpenCharacter || hasWorkspace)))
            .ToArray();

        _suppressTabSelectionEvent = true;
        _navigationTabsList.ItemsSource = tabs;
        _navigationTabsList.SelectedItem = tabs.FirstOrDefault(item => string.Equals(item.Id, state.ActiveTabId, StringComparison.Ordinal));
        _suppressTabSelectionEvent = false;

        WorkspaceSurfaceActionDefinition[] actions = WorkspaceSurfaceActionCatalog.ForTab(state.ActiveTabId)
            .Where(action => action.EnabledByDefault && (!action.RequiresOpenCharacter || hasWorkspace))
            .ToArray();
        SectionActionListItem[] sectionActionItems = actions
            .Select(action => new SectionActionListItem(action))
            .ToArray();
        _suppressSectionActionSelectionEvent = true;
        _sectionActionsList.ItemsSource = sectionActionItems;
        _sectionActionsList.SelectedItem = sectionActionItems.FirstOrDefault(item => string.Equals(item.Id, state.ActiveActionId, StringComparison.Ordinal));
        _suppressSectionActionSelectionEvent = false;

        DesktopUiControlDefinition[] uiControls = DesktopUiControlCatalog.ForTab(state.ActiveTabId)
            .Where(control => control.EnabledByDefault && (!control.RequiresOpenCharacter || hasWorkspace))
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
        _activeMenuGroup = string.Equals(_activeMenuGroup, menuId, StringComparison.Ordinal) ? null : menuId;
        RefreshState();
    }

    private async void CommandsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressCommandSelectionEvent)
            return;

        if (_commandsList.SelectedItem is not CommandListItem command || !command.Enabled)
            return;

        await _adapter.ExecuteCommandAsync(command.Id, CancellationToken.None);
        _suppressCommandSelectionEvent = true;
        _commandsList.SelectedItem = null;
        _suppressCommandSelectionEvent = false;
    }

    private async void NavigationTabsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressTabSelectionEvent)
            return;

        if (_navigationTabsList.SelectedItem is not TabListItem tab || !tab.Enabled)
            return;

        await _adapter.SelectTabAsync(tab.Id, CancellationToken.None);
    }

    private async void SectionActionsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSectionActionSelectionEvent)
            return;

        if (_sectionActionsList.SelectedItem is not SectionActionListItem action)
            return;

        await _adapter.ExecuteWorkspaceActionAsync(action.Action, CancellationToken.None);
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

        await _adapter.HandleUiControlAsync(control.Id, CancellationToken.None);
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

        await _adapter.ExecuteDialogActionAsync(action.Id, CancellationToken.None);
        _suppressDialogActionSelectionEvent = true;
        _dialogActionsList.SelectedItem = null;
        _suppressDialogActionSelectionEvent = false;
    }

    private static Uri ResolveApiBaseAddress()
    {
        string? configured = Environment.GetEnvironmentVariable("CHUMMER_API_BASE_URL");
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = "http://127.0.0.1:8088";
        }

        if (!Uri.TryCreate(configured, UriKind.Absolute, out Uri? uri))
        {
            throw new InvalidOperationException($"Invalid CHUMMER_API_BASE_URL: '{configured}'");
        }

        return uri;
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
