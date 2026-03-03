using System.Net.Http;
using System.Text;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
    private readonly TextBlock _nameValue;
    private readonly TextBlock _aliasValue;
    private readonly TextBlock _karmaValue;
    private readonly TextBlock _skillsValue;
    private readonly ListBox _commandsList;

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
            _statusText.Text = "State: paste document content before importing.";
            return;
        }

        await _adapter.ImportAsync(Encoding.UTF8.GetBytes(importText), CancellationToken.None);
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
            ? $"State: {(state.IsBusy ? "busy" : "ready")}, workspace={(state.WorkspaceId?.Value ?? "none")}, saved={state.HasSavedWorkspace}"
            : $"State: error - {state.Error}";

        _nameValue.Text = state.Profile?.Name ?? "-";
        _aliasValue.Text = state.Profile?.Alias ?? "-";
        _karmaValue.Text = state.Progress?.Karma.ToString() ?? "-";
        _skillsValue.Text = state.Skills?.Count.ToString() ?? "-";

        bool hasWorkspace = state.WorkspaceId is not null;
        _commandsList.ItemsSource = state.Commands
            .Select(command => ToCommandLine(command, hasWorkspace))
            .ToArray();
    }

    private static string ToCommandLine(AppCommandDefinition command, bool hasWorkspace)
    {
        bool enabled = command.EnabledByDefault && (!command.RequiresOpenCharacter || hasWorkspace);
        return $"{command.Id} [{command.Group}] {(enabled ? "enabled" : "disabled")}";
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
}
