using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Shell;

namespace Chummer.Presentation.Overview;

public sealed partial class CharacterOverviewPresenter : ICharacterOverviewPresenter
{
    private readonly IChummerClient _client;
    private readonly IWorkspaceSessionPresenter _workspaceSessionPresenter;
    private readonly IDesktopDialogFactory _dialogFactory;
    private readonly IOverviewCommandDispatcher _commandDispatcher;
    private readonly IDialogCoordinator _dialogCoordinator;
    private readonly IWorkspaceOverviewLoader _workspaceOverviewLoader;
    private readonly IWorkspaceSectionRenderer _workspaceSectionRenderer;
    private readonly IWorkspacePersistenceService _workspacePersistenceService;
    private readonly IWorkspaceViewStateStore _workspaceViewStateStore;
    private readonly IWorkspaceShellStateFactory _workspaceShellStateFactory;
    private readonly IWorkspaceRemoteCloseService _workspaceRemoteCloseService;
    private readonly IWorkspaceSessionActivationService _workspaceSessionActivationService;
    private readonly IWorkspaceOverviewStateFactory _workspaceOverviewStateFactory;
    private readonly IShellBootstrapDataProvider _bootstrapDataProvider;
    private CharacterWorkspaceId? _currentWorkspace;

    public CharacterOverviewPresenter(
        IChummerClient client,
        IWorkspaceSessionManager? workspaceSessionManager = null,
        IDesktopDialogFactory? dialogFactory = null,
        IWorkspaceSessionPresenter? workspaceSessionPresenter = null,
        IOverviewCommandDispatcher? commandDispatcher = null,
        IDialogCoordinator? dialogCoordinator = null,
        IWorkspaceOverviewLoader? workspaceOverviewLoader = null,
        IWorkspaceSectionRenderer? workspaceSectionRenderer = null,
        IWorkspacePersistenceService? workspacePersistenceService = null,
        IWorkspaceViewStateStore? workspaceViewStateStore = null,
        IWorkspaceShellStateFactory? workspaceShellStateFactory = null,
        IWorkspaceRemoteCloseService? workspaceRemoteCloseService = null,
        IWorkspaceSessionActivationService? workspaceSessionActivationService = null,
        IWorkspaceOverviewStateFactory? workspaceOverviewStateFactory = null,
        IShellBootstrapDataProvider? bootstrapDataProvider = null)
    {
        _client = client;
        IWorkspaceSessionManager manager = workspaceSessionManager ?? new WorkspaceSessionManager();
        _workspaceSessionPresenter = workspaceSessionPresenter ?? new WorkspaceSessionPresenter(manager);
        _dialogFactory = dialogFactory ?? new DesktopDialogFactory();
        _commandDispatcher = commandDispatcher ?? new OverviewCommandDispatcher();
        _dialogCoordinator = dialogCoordinator ?? new DialogCoordinator();
        _workspaceOverviewLoader = workspaceOverviewLoader ?? new WorkspaceOverviewLoader();
        _workspaceSectionRenderer = workspaceSectionRenderer ?? new WorkspaceSectionRenderer();
        _workspacePersistenceService = workspacePersistenceService ?? new WorkspacePersistenceService();
        _workspaceViewStateStore = workspaceViewStateStore ?? new WorkspaceViewStateStore();
        _workspaceShellStateFactory = workspaceShellStateFactory ?? new WorkspaceShellStateFactory();
        _workspaceRemoteCloseService = workspaceRemoteCloseService ?? new WorkspaceRemoteCloseService();
        _workspaceSessionActivationService = workspaceSessionActivationService ?? new WorkspaceSessionActivationService();
        _workspaceOverviewStateFactory = workspaceOverviewStateFactory ?? new WorkspaceOverviewStateFactory();
        _bootstrapDataProvider = bootstrapDataProvider ?? new ShellBootstrapDataProvider(client);
    }

    public CharacterOverviewState State { get; private set; } = CharacterOverviewState.Empty;

    public event EventHandler? StateChanged;

    public async Task InitializeAsync(CancellationToken ct)
    {
        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            ShellBootstrapData bootstrap = await _bootstrapDataProvider.GetAsync(ct);
            WorkspaceSessionState session = _workspaceSessionPresenter.Restore(bootstrap.Workspaces);

            Publish(State with
            {
                IsBusy = false,
                Error = null,
                Session = session,
                Commands = bootstrap.Commands,
                NavigationTabs = bootstrap.NavigationTabs,
                OpenWorkspaces = session.OpenWorkspaces,
                Notice = session.OpenWorkspaces.Count == 0
                    ? State.Notice
                    : $"Restored {session.OpenWorkspaces.Count} workspace(s)."
            });
        }
        catch (Exception ex)
        {
            Publish(State with
            {
                IsBusy = false,
                Error = ex.Message
            });
        }
    }

    private void Publish(CharacterOverviewState state)
    {
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
