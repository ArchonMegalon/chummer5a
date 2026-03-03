using System.Net.Http;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

        RefreshState();
    }

    protected override void OnClosed(EventArgs e)
    {
        _adapter.Dispose();
        base.OnClosed(e);
    }

    private async void ImportButton_OnClick(object? sender, RoutedEventArgs e)
    {
        string xml = _xmlInputBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(xml))
        {
            _statusText.Text = "State: paste XML content before importing.";
            return;
        }

        await _adapter.ImportAsync(xml, CancellationToken.None);
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
