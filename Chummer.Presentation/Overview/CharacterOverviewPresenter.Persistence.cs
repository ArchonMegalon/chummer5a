using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public sealed partial class CharacterOverviewPresenter
{
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
            Error = null,
            PendingDownload = null
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
                Notice = "Workspace saved.",
                PendingDownload = null
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

    public async Task DownloadAsync(CancellationToken ct)
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
            Error = null,
            PendingDownload = null
        });

        try
        {
            WorkspaceDownloadResult result = await _workspacePersistenceService.DownloadAsync(_client, _currentWorkspace.Value, ct);
            if (!result.Success || result.Receipt is null)
            {
                Publish(State with
                {
                    IsBusy = false,
                    Error = result.Error,
                    PendingDownload = null
                });
                return;
            }

            Publish(State with
            {
                IsBusy = false,
                Error = null,
                Notice = $"Download prepared: {result.Receipt.FileName} ({result.Receipt.DocumentLength} bytes).",
                PendingDownload = result.Receipt,
                PendingDownloadVersion = State.PendingDownloadVersion + 1
            });
        }
        catch (Exception ex)
        {
            Publish(State with
            {
                IsBusy = false,
                Error = ex.Message,
                PendingDownload = null
            });
        }
    }
}
