using Chummer.Contracts.Characters;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;
using System.Text.Json;

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
            await Task.WhenAll(commandsTask, tabsTask);

            Publish(State with
            {
                IsBusy = false,
                Error = null,
                Commands = commandsTask.Result,
                NavigationTabs = tabsTask.Result
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

        switch (commandId)
        {
            case "save_character":
            case "save_character_as":
                await SaveAsync(ct);
                return;
            case "refresh_character":
                if (_currentWorkspace is null)
                {
                    Publish(State with { Error = "No workspace loaded." });
                    return;
                }

                await LoadAsync(_currentWorkspace.Value, ct);
                return;
            case "new_character":
                _currentWorkspace = null;
                Publish(CharacterOverviewState.Empty with
                {
                    Commands = State.Commands,
                    NavigationTabs = State.NavigationTabs,
                    LastCommandId = commandId
                });
                return;
            default:
                Publish(State with
                {
                    Error = $"Command '{commandId}' is not implemented in shared presenter yet."
                });
                return;
        }
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

        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            var section = await _client.GetSectionAsync(_currentWorkspace.Value, tab.SectionId, ct);
            Publish(State with
            {
                IsBusy = false,
                Error = null,
                ActiveTabId = tab.Id,
                ActiveSectionId = tab.SectionId,
                ActiveSectionJson = section.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
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
            CommandResult<WorkspaceSaveReceipt> result = await _client.SaveAsync(_currentWorkspace.Value, ct);
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
            ActiveTabId: null,
            ActiveSectionId: null,
            ActiveSectionJson: null,
            LastCommandId: State.LastCommandId,
            Commands: State.Commands,
            NavigationTabs: State.NavigationTabs,
            HasSavedWorkspace: false));
    }

    private void Publish(CharacterOverviewState state)
    {
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
