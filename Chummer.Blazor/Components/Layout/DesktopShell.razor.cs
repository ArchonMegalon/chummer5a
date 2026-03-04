using Chummer.Contracts.Presentation;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;
using Microsoft.AspNetCore.Components;

namespace Chummer.Blazor.Components.Layout;

public partial class DesktopShell : IDisposable
{
    private CharacterOverviewStateBridge? _bridge;
    private const long MaxImportBytes = 8 * 1024 * 1024;
    private ElementReference _shellRoot;

    [Inject]
    public ICharacterOverviewPresenter Presenter { get; set; } = default!;

    [Inject]
    public ICommandAvailabilityEvaluator AvailabilityEvaluator { get; set; } = default!;

    [Inject]
    public IShellPresenter ShellPresenter { get; set; } = default!;

    private string RawImportXml { get; set; } = "<character><name>Demo</name><alias>Sample</alias><metatype>Human</metatype><buildmethod>Priority</buildmethod><created>True</created></character>";
    private string? ImportedFileName { get; set; }
    private string? ImportError { get; set; }
    private string LoadWorkspaceId { get; set; } = string.Empty;
    private string MetadataName { get; set; } = string.Empty;
    private string MetadataAlias { get; set; } = string.Empty;
    private string MetadataNotes { get; set; } = string.Empty;
    private string _lastUiUtc = DateTimeOffset.UtcNow.ToString("u");
    private bool _isDisposed;

    private CharacterOverviewState State => _bridge?.Current ?? Presenter.State;
    private ShellState ShellState => ShellPresenter.State;

    private IEnumerable<AppCommandDefinition> HeadCommands =>
        ShellState.Commands.Where(command => !string.Equals(command.Group, "menu", StringComparison.Ordinal));

    private IEnumerable<AppCommandDefinition> ToolStripCommands =>
        HeadCommands.Where(command => command.Group is "file" or "tools").Take(10);

    private IReadOnlyList<WorkspaceSurfaceActionDefinition> ActiveWorkspaceActions =>
        WorkspaceSurfaceActionCatalog.ForTab(State.ActiveTabId)
            .Where(action => AvailabilityEvaluator.IsWorkspaceActionEnabled(action, State))
            .ToArray();

    private IReadOnlyList<DesktopUiControlDefinition> ActiveUiControls =>
        DesktopUiControlCatalog.ForTab(State.ActiveTabId)
            .Where(control => AvailabilityEvaluator.IsUiControlEnabled(control, State))
            .ToArray();

    protected override async Task OnInitializedAsync()
    {
        ShellPresenter.StateChanged += OnShellStateChanged;
        await ShellPresenter.InitializeAsync(CancellationToken.None);

        _bridge = new CharacterOverviewStateBridge(Presenter, state =>
        {
            if (_isDisposed)
                return;

            _lastUiUtc = DateTimeOffset.UtcNow.ToString("u");
            _ = InvokeAsync(StateHasChanged);
        });
        await _bridge.InitializeAsync(CancellationToken.None);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _shellRoot.FocusAsync();
        }
    }

    private void OnShellStateChanged(object? sender, EventArgs e)
    {
        if (_isDisposed)
            return;

        _lastUiUtc = DateTimeOffset.UtcNow.ToString("u");
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _isDisposed = true;
        ShellPresenter.StateChanged -= OnShellStateChanged;
        _bridge?.Dispose();
    }
}
