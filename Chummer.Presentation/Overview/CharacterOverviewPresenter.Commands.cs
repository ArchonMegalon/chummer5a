using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public sealed partial class CharacterOverviewPresenter
{
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
            DownloadAsync: DownloadAsync,
            LoadAsync: LoadAsync,
            CreateResetState: CreateWorkspaceResetState,
            CloseAllAsync: CloseAllWorkspacesAsync,
            CloseWorkspaceAsync: CloseWorkspaceAsync);

        await _commandDispatcher.DispatchAsync(commandId, context, ct);
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

}
