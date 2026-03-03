using System.Net.Http;
using System.Text;
using System.Linq;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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
    private readonly TextBlock _nameValue;
    private readonly TextBlock _aliasValue;
    private readonly TextBlock _karmaValue;
    private readonly TextBlock _skillsValue;
    private readonly ListBox _commandsList;
    private readonly ListBox _navigationTabsList;
    private readonly TextBox _sectionPreviewBox;
    private bool _suppressCommandSelectionEvent;
    private bool _suppressTabSelectionEvent;

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
        _nameValue = this.FindControl<TextBlock>("NameValue")!;
        _aliasValue = this.FindControl<TextBlock>("AliasValue")!;
        _karmaValue = this.FindControl<TextBlock>("KarmaValue")!;
        _skillsValue = this.FindControl<TextBlock>("SkillsValue")!;
        _commandsList = this.FindControl<ListBox>("CommandsList")!;
        _commandsList.SelectionChanged += CommandsList_OnSelectionChanged;
        _navigationTabsList = this.FindControl<ListBox>("NavigationTabsList")!;
        _sectionPreviewBox = this.FindControl<TextBox>("SectionPreviewBox")!;
        _navigationTabsList.SelectionChanged += NavigationTabsList_OnSelectionChanged;

        RefreshState();
        Opened += OnOpened;
    }

    protected override void OnClosed(EventArgs e)
    {
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

        _nameValue.Text = state.Profile?.Name ?? "-";
        _aliasValue.Text = state.Profile?.Alias ?? "-";
        _karmaValue.Text = state.Progress?.Karma.ToString() ?? "-";
        _skillsValue.Text = state.Skills?.Count.ToString() ?? "-";

        bool hasWorkspace = state.WorkspaceId is not null;
        CommandListItem[] commands = state.Commands
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

        _sectionPreviewBox.Text = state.ActiveSectionJson ?? string.Empty;
    }

    private async void CommandsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressCommandSelectionEvent)
            return;

        if (_commandsList.SelectedItem is not CommandListItem command || !command.Enabled)
            return;

        await _adapter.ExecuteCommandAsync(command.Id, CancellationToken.None);
    }

    private async void NavigationTabsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressTabSelectionEvent)
            return;

        if (_navigationTabsList.SelectedItem is not TabListItem tab || !tab.Enabled)
            return;

        await _adapter.SelectTabAsync(tab.Id, CancellationToken.None);
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

    private sealed record TabListItem(
        string Id,
        string Label,
        string SectionId,
        string Group,
        bool Enabled)
    {
        public override string ToString()
        {
            return $"{Id} -> {SectionId} [{Group}] {(Enabled ? "enabled" : "disabled")}";
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
}
