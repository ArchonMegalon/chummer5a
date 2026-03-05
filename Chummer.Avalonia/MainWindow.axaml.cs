using Avalonia.Controls;
using Chummer.Contracts.Rulesets;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;

namespace Chummer.Avalonia;

public partial class MainWindow : Window
{
    private readonly ICharacterOverviewPresenter _presenter;
    private readonly IShellPresenter _shellPresenter;
    private readonly ICommandAvailabilityEvaluator _commandAvailabilityEvaluator;
    private readonly IRulesetShellCatalogResolver _shellCatalogResolver;
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
    private readonly Button[] _menuButtons;
    private DesktopDialogWindow? _dialogWindow;
    private bool _suppressCommandSelectionEvent;
    private bool _suppressWorkspaceSelectionEvent;
    private bool _suppressTabSelectionEvent;
    private bool _suppressSectionActionSelectionEvent;
    private bool _suppressUiControlSelectionEvent;
    private bool _suppressDialogActionSelectionEvent;
    private long _lastDownloadVersionHandled;

    public MainWindow(
        ICharacterOverviewPresenter presenter,
        IShellPresenter shellPresenter,
        ICommandAvailabilityEvaluator commandAvailabilityEvaluator,
        IRulesetShellCatalogResolver shellCatalogResolver,
        CharacterOverviewViewModelAdapter adapter)
    {
        InitializeComponent();

        _presenter = presenter;
        _shellPresenter = shellPresenter;
        _commandAvailabilityEvaluator = commandAvailabilityEvaluator;
        _shellCatalogResolver = shellCatalogResolver;
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
        _menuButtons =
        [
            MenuFileButton,
            MenuEditButton,
            MenuSpecialButton,
            MenuToolsButton,
            MenuWindowsButton,
            MenuHelpButton
        ];

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
