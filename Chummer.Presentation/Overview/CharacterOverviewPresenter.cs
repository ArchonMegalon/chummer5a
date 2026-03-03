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

    public async Task LoadAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            Task<CharacterProfileSection> profileTask = _client.GetProfileAsync(id, ct);
            Task<CharacterProgressSection> progressTask = _client.GetProgressAsync(id, ct);
            Task<CharacterSkillsSection> skillsTask = _client.GetSkillsAsync(id, ct);

            await Task.WhenAll(profileTask, progressTask, skillsTask);

            _currentWorkspace = id;
            Publish(new CharacterOverviewState(
                IsBusy: false,
                Error: null,
                Profile: profileTask.Result,
                Progress: progressTask.Result,
                Skills: skillsTask.Result));
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

    private void Publish(CharacterOverviewState state)
    {
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
