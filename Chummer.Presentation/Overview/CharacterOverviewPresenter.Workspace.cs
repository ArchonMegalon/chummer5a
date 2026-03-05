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
            await LoadWorkspaceAsync(imported.Id, ct, rulesetId: imported.RulesetId);
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

        if (_currentWorkspace is { } activeWorkspace
            && string.Equals(activeWorkspace.Value, id.Value, StringComparison.Ordinal))
        {
            Publish(State with
            {
                Error = null,
                Notice = $"Workspace '{id.Value}' is already active."
            });
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

        bool closed = await _workspaceRemoteCloseService.TryCloseAsync(_client, id, ct);

        bool closedActiveWorkspace = _currentWorkspace is { } activeWorkspace
            && string.Equals(activeWorkspace.Value, id.Value, StringComparison.Ordinal);
        WorkspaceSessionState session = _workspaceSessionPresenter.Close(id);
        _workspaceViewStateStore.Remove(id);

        if (session.OpenWorkspaces.Count == 0)
        {
            _currentWorkspace = null;
            Publish(_workspaceShellStateFactory.CreateEmptyShellState(
                State,
                session,
                closed ? "Closed active workspace." : "Active workspace was already closed."));
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
        bool updateSession = true,
        string? rulesetId = null)
    {
        CaptureWorkspaceView();
        WorkspaceOverviewLoadResult loadedOverview = await _workspaceOverviewLoader.LoadAsync(_client, id, ct);

        WorkspaceSessionState session = _workspaceSessionActivationService.Activate(
            _workspaceSessionPresenter,
            id,
            loadedOverview.Profile,
            sessionSeed,
            updateSession,
            rulesetId);

        WorkspaceViewState? restoredView = RestoreWorkspaceView(id);
        bool hasSavedWorkspace = restoredView?.HasSavedWorkspace ?? false;
        session = _workspaceSessionPresenter.SetSavedStatus(id, hasSavedWorkspace);
        _currentWorkspace = id;

        Publish(_workspaceOverviewStateFactory.CreateLoadedState(
            State,
            id,
            session,
            loadedOverview,
            restoredView,
            hasSavedWorkspace));
    }

    private async Task CloseAllWorkspacesAsync(CancellationToken ct, string notice)
    {
        CaptureWorkspaceView();
        CharacterWorkspaceId[] workspaceIdsToClose = _workspaceSessionPresenter.State.OpenWorkspaces
            .GroupBy(workspace => workspace.Id.Value, StringComparer.Ordinal)
            .Select(group => group.First().Id)
            .ToArray();

        await _workspaceRemoteCloseService.CloseManyIgnoringFailuresAsync(_client, workspaceIdsToClose, ct);

        WorkspaceSessionState session = _workspaceSessionPresenter.CloseAll();
        _workspaceViewStateStore.Clear();
        _currentWorkspace = null;
        Publish(_workspaceShellStateFactory.CreateEmptyShellState(State, session, notice));
    }

    private CharacterOverviewState CreateWorkspaceResetState(string commandId, string notice)
    {
        CaptureWorkspaceView();
        _currentWorkspace = null;
        WorkspaceSessionState session = _workspaceSessionPresenter.ClearActive();
        return _workspaceShellStateFactory.CreateEmptyShellState(State, session, notice, commandId);
    }
}
