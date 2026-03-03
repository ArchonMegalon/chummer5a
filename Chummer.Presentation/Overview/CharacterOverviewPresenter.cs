using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public sealed class CharacterOverviewPresenter : ICharacterOverviewPresenter
{
    private readonly IChummerClient _client;
    private CharacterWorkspaceId? _currentWorkspace;

    public CharacterOverviewPresenter(IChummerClient client)
    {
        _client = client;
    }

    public CharacterOverviewState State { get; private set; } = CharacterOverviewState.Empty;

    public event EventHandler? StateChanged;

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
            CommandResult<CharacterProfileSection> result = await _client.UpdateMetadataAsync(_currentWorkspace.Value, command, ct);
            if (!result.Success || result.Value is null)
            {
                Publish(State with
                {
                    IsBusy = false,
                    Error = result.Error ?? "Metadata update failed."
                });
                return;
            }

            Publish(State with
            {
                IsBusy = false,
                Error = null,
                WorkspaceId = _currentWorkspace,
                Profile = result.Value
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
            CommandResult<WorkspaceDocument> result = await _client.SaveAsync(_currentWorkspace.Value, ct);
            if (!result.Success || result.Value is null)
            {
                Publish(State with
                {
                    IsBusy = false,
                    Error = result.Error ?? "Save failed."
                });
                return;
            }

            Publish(State with
            {
                IsBusy = false,
                Error = null,
                WorkspaceId = _currentWorkspace,
                HasSavedWorkspace = true
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

    private async Task LoadWorkspaceAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        Task<CharacterProfileSection> profileTask = _client.GetProfileAsync(id, ct);
        Task<CharacterProgressSection> progressTask = _client.GetProgressAsync(id, ct);
        Task<CharacterSkillsSection> skillsTask = _client.GetSkillsAsync(id, ct);
        Task<CharacterRulesSection> rulesTask = _client.GetRulesAsync(id, ct);
        Task<CharacterBuildSection> buildTask = _client.GetBuildAsync(id, ct);
        Task<CharacterMovementSection> movementTask = _client.GetMovementAsync(id, ct);
        Task<CharacterAwakeningSection> awakeningTask = _client.GetAwakeningAsync(id, ct);

        await Task.WhenAll(profileTask, progressTask, skillsTask, rulesTask, buildTask, movementTask, awakeningTask);

        _currentWorkspace = id;
        Publish(new CharacterOverviewState(
            IsBusy: false,
            Error: null,
            WorkspaceId: id,
            Profile: profileTask.Result,
            Progress: progressTask.Result,
            Skills: skillsTask.Result,
            Rules: rulesTask.Result,
            Build: buildTask.Result,
            Movement: movementTask.Result,
            Awakening: awakeningTask.Result,
            HasSavedWorkspace: State.HasSavedWorkspace));
    }

    private void Publish(CharacterOverviewState state)
    {
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
