using Avalonia.Controls;
using Chummer.Avalonia.Controls;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;

namespace Chummer.Avalonia;

public partial class MainWindow : Window
{
    private readonly ICharacterOverviewPresenter _presenter;
    private readonly IShellPresenter _shellPresenter;
    private readonly ICommandAvailabilityEvaluator _commandAvailabilityEvaluator;
    private readonly IShellSurfaceResolver _shellSurfaceResolver;
    private readonly CharacterOverviewViewModelAdapter _adapter;
    private readonly ToolStripControl _toolStrip;
    private readonly WorkspaceStripControl _workspaceStrip;
    private readonly SummaryHeaderControl _summaryHeader;
    private readonly ShellMenuBarControl _menuBar;
    private readonly NavigatorPaneControl _navigatorPane;
    private readonly SectionHostControl _sectionHost;
    private readonly CommandDialogPaneControl _commandDialogPane;
    private readonly StatusStripControl _statusStrip;
    private DesktopDialogWindow? _dialogWindow;
    private long _lastDownloadVersionHandled;

    public MainWindow(
        ICharacterOverviewPresenter presenter,
        IShellPresenter shellPresenter,
        ICommandAvailabilityEvaluator commandAvailabilityEvaluator,
        IShellSurfaceResolver shellSurfaceResolver,
        CharacterOverviewViewModelAdapter adapter)
    {
        InitializeComponent();

        _presenter = presenter;
        _shellPresenter = shellPresenter;
        _commandAvailabilityEvaluator = commandAvailabilityEvaluator;
        _shellSurfaceResolver = shellSurfaceResolver;
        _adapter = adapter;
        _adapter.Updated += (_, _) => RefreshState();
        _shellPresenter.StateChanged += ShellPresenter_OnStateChanged;

        _toolStrip = ToolStripControl;
        _toolStrip.ImportFileRequested += ToolStrip_OnImportFileRequested;
        _toolStrip.ImportRawRequested += ToolStrip_OnImportRawRequested;
        _toolStrip.SaveRequested += ToolStrip_OnSaveRequested;
        _toolStrip.CloseWorkspaceRequested += ToolStrip_OnCloseWorkspaceRequested;
        _workspaceStrip = WorkspaceStripControl;
        _menuBar = ShellMenuBarControl;
        _menuBar.MenuSelected += MenuBar_OnMenuSelected;
        _summaryHeader = SummaryHeaderControl;
        _navigatorPane = NavigatorPaneControl;
        _navigatorPane.WorkspaceSelected += NavigatorPane_OnWorkspaceSelected;
        _navigatorPane.NavigationTabSelected += NavigatorPane_OnNavigationTabSelected;
        _navigatorPane.SectionActionSelected += NavigatorPane_OnSectionActionSelected;
        _navigatorPane.UiControlSelected += NavigatorPane_OnUiControlSelected;
        _sectionHost = SectionHostControl;
        _commandDialogPane = CommandDialogPaneControl;
        _commandDialogPane.CommandSelected += CommandDialogPane_OnCommandSelected;
        _commandDialogPane.DialogActionSelected += CommandDialogPane_OnDialogActionSelected;
        _statusStrip = StatusStripControl;

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
}
