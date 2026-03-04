using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public sealed partial class CharacterOverviewPresenter
{
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
        _workspaceViewStateStore.Remove(id);

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
        _workspaceViewStateStore.Clear();
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
}
