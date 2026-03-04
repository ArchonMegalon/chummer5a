using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public sealed class CharacterOverviewPresenter : ICharacterOverviewPresenter
{
    private readonly IChummerClient _client;
    private readonly IWorkspaceSessionPresenter _workspaceSessionPresenter;
    private readonly IDesktopDialogFactory _dialogFactory;
    private readonly IOverviewCommandDispatcher _commandDispatcher;
    private readonly IDialogCoordinator _dialogCoordinator;
    private readonly IWorkspaceOverviewLoader _workspaceOverviewLoader;
    private readonly IWorkspaceSectionRenderer _workspaceSectionRenderer;
    private readonly IWorkspacePersistenceService _workspacePersistenceService;
    private readonly Dictionary<string, WorkspaceViewState> _workspaceViews = new(StringComparer.Ordinal);
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
        IWorkspacePersistenceService? workspacePersistenceService = null)
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
            Task<IReadOnlyList<AppCommandDefinition>> commandsTask = _client.GetCommandsAsync(ct);
            Task<IReadOnlyList<NavigationTabDefinition>> tabsTask = _client.GetNavigationTabsAsync(ct);
            Task<IReadOnlyList<WorkspaceListItem>> workspacesTask = _client.ListWorkspacesAsync(ct);
            await Task.WhenAll(commandsTask, tabsTask, workspacesTask);

            WorkspaceSessionState session = _workspaceSessionPresenter.Restore(workspacesTask.Result);

            Publish(State with
            {
                IsBusy = false,
                Error = null,
                Session = session,
                Commands = commandsTask.Result,
                NavigationTabs = tabsTask.Result,
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

    public async Task ImportAsync(WorkspaceImportDocument document, CancellationToken ct)
    {
        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            WorkspaceImportResult imported = await _client.ImportAsync(document, ct);
            await LoadWorkspaceAsync(imported.Id, ct);
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

    public async Task LoadAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            await LoadWorkspaceAsync(id, ct);
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

    public async Task SwitchWorkspaceAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            Publish(State with { Error = "Workspace id is required." });
            return;
        }

        if (!_workspaceSessionPresenter.Contains(id))
        {
            await LoadAsync(id, ct);
            return;
        }

        await LoadAsync(id, ct);
    }

    public async Task CloseWorkspaceAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            Publish(State with { Error = "Workspace id is required." });
            return;
        }

        bool closed;
        try
        {
            closed = await _client.CloseWorkspaceAsync(id, ct);
        }
        catch
        {
            closed = false;
        }

        bool closedActiveWorkspace = _currentWorkspace is { } activeWorkspace
            && string.Equals(activeWorkspace.Value, id.Value, StringComparison.Ordinal);
        WorkspaceSessionState session = _workspaceSessionPresenter.Close(id);
        _workspaceViews.Remove(id.Value);

        if (session.OpenWorkspaces.Count == 0)
        {
            _currentWorkspace = null;
            Publish(CharacterOverviewState.Empty with
            {
                Session = session,
                Commands = State.Commands,
                NavigationTabs = State.NavigationTabs,
                LastCommandId = State.LastCommandId,
                Notice = closed
                    ? "Closed active workspace."
                    : "Active workspace was already closed.",
                Preferences = State.Preferences,
                OpenWorkspaces = session.OpenWorkspaces
            });
            return;
        }

        if (closedActiveWorkspace && session.ActiveWorkspaceId is { } nextWorkspace)
        {
            await LoadWorkspaceAsync(nextWorkspace, ct, session, updateSession: false);
            Publish(State with
            {
                Notice = closed
                    ? $"Closed active workspace. Switched to '{nextWorkspace.Value}'."
                    : $"Active workspace was already closed. Switched to '{nextWorkspace.Value}'."
            });
            return;
        }

        Publish(State with
        {
            Session = session,
            OpenWorkspaces = session.OpenWorkspaces,
            Error = null,
            Notice = closed
                ? $"Closed workspace '{id.Value}'."
                : $"Workspace '{id.Value}' was already closed."
        });
    }

    public async Task ExecuteCommandAsync(string commandId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            Publish(State with { Error = "Command id is required." });
            return;
        }

        Publish(State with
        {
            LastCommandId = commandId,
            Error = null
        });

        OverviewCommandExecutionContext context = new(
            State: State,
            CurrentWorkspace: _currentWorkspace,
            DialogFactory: _dialogFactory,
            Publish: Publish,
            SaveAsync: SaveAsync,
            LoadAsync: LoadAsync,
            CreateResetState: CreateWorkspaceResetState,
            CloseAllAsync: CloseAllWorkspacesAsync,
            CloseWorkspaceAsync: CloseWorkspaceAsync);

        await _commandDispatcher.DispatchAsync(commandId, context, ct);
    }

    public Task HandleUiControlAsync(string controlId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(controlId))
        {
            Publish(State with { Error = "UI control id is required." });
            return Task.CompletedTask;
        }

        Publish(State with
        {
            Error = null,
            ActiveDialog = _dialogFactory.CreateUiControlDialog(controlId, State.Preferences)
        });

        return Task.CompletedTask;
    }

    public async Task ExecuteWorkspaceActionAsync(WorkspaceSurfaceActionDefinition action, CancellationToken ct)
    {
        if (action is null)
        {
            Publish(State with { Error = "Workspace action is required." });
            return;
        }

        if (action.RequiresOpenCharacter && _currentWorkspace is null)
        {
            Publish(State with { Error = "No workspace loaded." });
            return;
        }

        switch (action.Kind)
        {
            case WorkspaceSurfaceActionKind.Section:
                await LoadSectionAsync(action.TargetId, action.TabId, action.Id, ct);
                return;
            case WorkspaceSurfaceActionKind.Summary:
                await RenderSummaryAction(action, ct);
                return;
            case WorkspaceSurfaceActionKind.Validate:
                await RenderValidateAction(action, ct);
                return;
            case WorkspaceSurfaceActionKind.Metadata:
                Publish(State with
                {
                    ActiveTabId = action.TabId,
                    ActiveActionId = action.Id,
                    Error = null,
                    ActiveDialog = _dialogFactory.CreateMetadataDialog(State.Profile, State.Preferences)
                });
                return;
            case WorkspaceSurfaceActionKind.Command:
                await ExecuteCommandAsync(action.TargetId, ct);
                Publish(State with
                {
                    ActiveTabId = action.TabId,
                    ActiveActionId = action.Id
                });
                return;
            default:
                Publish(State with { Error = $"Unsupported workspace action kind '{action.Kind}'." });
                return;
        }
    }

    public Task UpdateDialogFieldAsync(string fieldId, string? value, CancellationToken ct)
    {
        DesktopDialogState? dialog = State.ActiveDialog;
        if (dialog is null)
            return Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(fieldId))
        {
            Publish(State with { Error = "Dialog field id is required." });
            return Task.CompletedTask;
        }

        DesktopDialogField[] updatedFields = dialog.Fields
            .Select(field => string.Equals(field.Id, fieldId, StringComparison.Ordinal)
                ? field with { Value = DesktopDialogFieldValueParser.Normalize(field, value) }
                : field)
            .ToArray();
        Publish(State with
        {
            ActiveDialog = dialog with { Fields = updatedFields },
            Error = null
        });
        return Task.CompletedTask;
    }

    public async Task ExecuteDialogActionAsync(string actionId, CancellationToken ct)
    {
        DialogCoordinationContext context = new(
            State: State,
            Publish: Publish,
            UpdateMetadataAsync: UpdateMetadataAsync,
            GetState: () => State);

        await _dialogCoordinator.CoordinateAsync(actionId, context, ct);
    }

    public Task CloseDialogAsync(CancellationToken ct)
    {
        Publish(State with
        {
            ActiveDialog = null,
            Error = null
        });
        return Task.CompletedTask;
    }

    public async Task SelectTabAsync(string tabId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tabId))
        {
            Publish(State with { Error = "Tab id is required." });
            return;
        }

        if (_currentWorkspace is null)
        {
            Publish(State with { Error = "No workspace loaded." });
            return;
        }

        NavigationTabDefinition? tab = State.NavigationTabs.FirstOrDefault(item => string.Equals(item.Id, tabId, StringComparison.Ordinal));
        if (tab is null)
        {
            Publish(State with { Error = $"Unknown tab '{tabId}'." });
            return;
        }

        await LoadSectionAsync(tab.SectionId, tab.Id, $"{tab.Id}:{tab.SectionId}", ct);
    }

    public async Task UpdateMetadataAsync(UpdateWorkspaceMetadata command, CancellationToken ct)
    {
        if (_currentWorkspace is null)
        {
            Publish(State with
            {
                Error = "No workspace loaded."
            });
            return;
        }

        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            WorkspaceMetadataUpdateResult result = await _workspacePersistenceService.UpdateMetadataAsync(
                _client,
                _currentWorkspace.Value,
                command,
                State.Preferences,
                ct);
            if (!result.Success || result.Profile is null)
            {
                Publish(State with
                {
                    IsBusy = false,
                    Error = result.Error
                });
                return;
            }

            WorkspaceSessionState session = _workspaceSessionPresenter.SetSavedStatus(_currentWorkspace.Value, hasSavedWorkspace: false);
            Publish(State with
            {
                IsBusy = false,
                Error = null,
                Session = session,
                OpenWorkspaces = session.OpenWorkspaces,
                WorkspaceId = _currentWorkspace,
                Profile = result.Profile,
                Preferences = result.Preferences,
                HasSavedWorkspace = false
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

    public async Task SaveAsync(CancellationToken ct)
    {
        if (_currentWorkspace is null)
        {
            Publish(State with
            {
                Error = "No workspace loaded."
            });
            return;
        }

        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            WorkspaceSaveResult result = await _workspacePersistenceService.SaveAsync(_client, _currentWorkspace.Value, ct);
            if (!result.Success)
            {
                Publish(State with
                {
                    IsBusy = false,
                    Error = result.Error
                });
                return;
            }

            WorkspaceSessionState session = _workspaceSessionPresenter.SetSavedStatus(_currentWorkspace.Value, hasSavedWorkspace: true);
            Publish(State with
            {
                IsBusy = false,
                Error = null,
                Session = session,
                OpenWorkspaces = session.OpenWorkspaces,
                WorkspaceId = _currentWorkspace,
                HasSavedWorkspace = true,
                Notice = "Workspace saved."
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

    private async Task LoadSectionAsync(string sectionId, string? tabId, string? actionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sectionId))
        {
            Publish(State with { Error = "Section id is required." });
            return;
        }

        if (_currentWorkspace is null)
        {
            Publish(State with { Error = "No workspace loaded." });
            return;
        }

        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            WorkspaceSectionRenderResult section = await _workspaceSectionRenderer.RenderSectionAsync(
                _client,
                _currentWorkspace.Value,
                sectionId,
                tabId,
                actionId,
                State.ActiveTabId,
                State.ActiveActionId,
                ct);
            Publish(State with
            {
                IsBusy = false,
                Error = null,
                ActiveTabId = section.ActiveTabId,
                ActiveActionId = section.ActiveActionId,
                ActiveSectionId = section.ActiveSectionId,
                ActiveSectionJson = section.ActiveSectionJson,
                ActiveSectionRows = section.ActiveSectionRows
            });
            CaptureWorkspaceView();
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

    private async Task RenderSummaryAction(WorkspaceSurfaceActionDefinition action, CancellationToken ct)
    {
        if (_currentWorkspace is null)
        {
            Publish(State with { Error = "No workspace loaded." });
            return;
        }

        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            WorkspaceSectionRenderResult summary = await _workspaceSectionRenderer.RenderSummaryAsync(
                _client,
                _currentWorkspace.Value,
                action,
                ct);
            Publish(State with
            {
                IsBusy = false,
                Error = null,
                ActiveTabId = summary.ActiveTabId,
                ActiveActionId = summary.ActiveActionId,
                ActiveSectionId = summary.ActiveSectionId,
                ActiveSectionJson = summary.ActiveSectionJson,
                ActiveSectionRows = summary.ActiveSectionRows
            });
            CaptureWorkspaceView();
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

    private async Task RenderValidateAction(WorkspaceSurfaceActionDefinition action, CancellationToken ct)
    {
        if (_currentWorkspace is null)
        {
            Publish(State with { Error = "No workspace loaded." });
            return;
        }

        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            WorkspaceSectionRenderResult validation = await _workspaceSectionRenderer.RenderValidationAsync(
                _client,
                _currentWorkspace.Value,
                action,
                ct);
            Publish(State with
            {
                IsBusy = false,
                Error = null,
                ActiveTabId = validation.ActiveTabId,
                ActiveActionId = validation.ActiveActionId,
                ActiveSectionId = validation.ActiveSectionId,
                ActiveSectionJson = validation.ActiveSectionJson,
                ActiveSectionRows = validation.ActiveSectionRows
            });
            CaptureWorkspaceView();
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


    private async Task LoadWorkspaceAsync(
        CharacterWorkspaceId id,
        CancellationToken ct,
        WorkspaceSessionState? sessionSeed = null,
        bool updateSession = true)
    {
        CaptureWorkspaceView();
        WorkspaceOverviewLoadResult loadedOverview = await _workspaceOverviewLoader.LoadAsync(_client, id, ct);

        WorkspaceSessionState session;
        if (sessionSeed is not null)
        {
            session = _workspaceSessionPresenter.Switch(id);
            if (session.ActiveWorkspaceId is null
                || !string.Equals(session.ActiveWorkspaceId.Value.Value, id.Value, StringComparison.Ordinal))
            {
                session = _workspaceSessionPresenter.Open(id, loadedOverview.Profile);
            }
        }
        else if (updateSession)
        {
            session = _workspaceSessionPresenter.Open(id, loadedOverview.Profile);
        }
        else
        {
            session = _workspaceSessionPresenter.Switch(id);
            if (session.ActiveWorkspaceId is null
                || !string.Equals(session.ActiveWorkspaceId.Value.Value, id.Value, StringComparison.Ordinal))
            {
                session = _workspaceSessionPresenter.Open(id, loadedOverview.Profile);
            }
        }

        WorkspaceViewState? restoredView = RestoreWorkspaceView(id);
        bool hasSavedWorkspace = restoredView?.HasSavedWorkspace ?? false;
        session = _workspaceSessionPresenter.SetSavedStatus(id, hasSavedWorkspace);
        _currentWorkspace = id;

        Publish(new CharacterOverviewState(
            IsBusy: false,
            Error: null,
            Session: session,
            WorkspaceId: id,
            OpenWorkspaces: session.OpenWorkspaces,
            Profile: loadedOverview.Profile,
            Progress: loadedOverview.Progress,
            Skills: loadedOverview.Skills,
            Rules: loadedOverview.Rules,
            Build: loadedOverview.Build,
            Movement: loadedOverview.Movement,
            Awakening: loadedOverview.Awakening,
            ActiveTabId: restoredView?.ActiveTabId,
            ActiveActionId: restoredView?.ActiveActionId,
            ActiveSectionId: restoredView?.ActiveSectionId,
            ActiveSectionJson: restoredView?.ActiveSectionJson,
            ActiveSectionRows: restoredView?.ActiveSectionRows ?? [],
            LastCommandId: State.LastCommandId,
            Notice: State.Notice,
            ActiveDialog: null,
            Preferences: State.Preferences,
            Commands: State.Commands,
            NavigationTabs: State.NavigationTabs,
            HasSavedWorkspace: hasSavedWorkspace));
    }

    private async Task CloseAllWorkspacesAsync(CancellationToken ct, string notice)
    {
        CaptureWorkspaceView();
        CharacterWorkspaceId[] workspaceIdsToClose = _workspaceSessionPresenter.State.OpenWorkspaces
            .GroupBy(workspace => workspace.Id.Value, StringComparer.Ordinal)
            .Select(group => group.First().Id)
            .ToArray();

        foreach (CharacterWorkspaceId workspaceId in workspaceIdsToClose)
        {
            try
            {
                await _client.CloseWorkspaceAsync(workspaceId, ct);
            }
            catch
            {
                // Keep resetting local shell state even if a close request fails remotely.
            }
        }

        WorkspaceSessionState session = _workspaceSessionPresenter.CloseAll();
        _currentWorkspace = null;
        Publish(CharacterOverviewState.Empty with
        {
            Session = session,
            Commands = State.Commands,
            NavigationTabs = State.NavigationTabs,
            LastCommandId = State.LastCommandId,
            Notice = notice,
            Preferences = State.Preferences,
            OpenWorkspaces = session.OpenWorkspaces
        });
    }

    private CharacterOverviewState CreateWorkspaceResetState(string commandId, string notice)
    {
        CaptureWorkspaceView();
        _currentWorkspace = null;
        WorkspaceSessionState session = _workspaceSessionPresenter.ClearActive();
        return CharacterOverviewState.Empty with
        {
            Session = session,
            Commands = State.Commands,
            NavigationTabs = State.NavigationTabs,
            LastCommandId = commandId,
            Notice = notice,
            Preferences = State.Preferences,
            OpenWorkspaces = session.OpenWorkspaces
        };
    }

    private void CaptureWorkspaceView()
    {
        if (_currentWorkspace is null)
            return;

        _workspaceViews[_currentWorkspace.Value.Value] = new WorkspaceViewState(
            ActiveTabId: State.ActiveTabId,
            ActiveActionId: State.ActiveActionId,
            ActiveSectionId: State.ActiveSectionId,
            ActiveSectionJson: State.ActiveSectionJson,
            ActiveSectionRows: State.ActiveSectionRows.ToArray(),
            HasSavedWorkspace: State.HasSavedWorkspace);
    }

    private WorkspaceViewState? RestoreWorkspaceView(CharacterWorkspaceId id)
    {
        return _workspaceViews.TryGetValue(id.Value, out WorkspaceViewState? view)
            ? view
            : null;
    }

    private void Publish(CharacterOverviewState state)
    {
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed record WorkspaceViewState(
        string? ActiveTabId,
        string? ActiveActionId,
        string? ActiveSectionId,
        string? ActiveSectionJson,
        IReadOnlyList<SectionRowState> ActiveSectionRows,
        bool HasSavedWorkspace);
}
