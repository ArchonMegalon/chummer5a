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
            WorkspaceOverviewLifecycleResult result = await _workspaceOverviewLifecycleCoordinator.ImportAsync(State, document, ct);
            Publish(result.State);
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
            WorkspaceOverviewLifecycleResult result = await _workspaceOverviewLifecycleCoordinator.LoadAsync(State, id, ct);
            Publish(result.State);
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
        WorkspaceOverviewLifecycleResult result = await _workspaceOverviewLifecycleCoordinator.SwitchAsync(State, id, ct);
        Publish(result.State);
    }

    public async Task CloseWorkspaceAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        WorkspaceOverviewLifecycleResult result = await _workspaceOverviewLifecycleCoordinator.CloseAsync(State, id, ct);
        Publish(result.State);
    }

    private async Task CloseAllWorkspacesAsync(CancellationToken ct, string notice)
    {
        WorkspaceOverviewLifecycleResult result = await _workspaceOverviewLifecycleCoordinator.CloseAllAsync(State, ct, notice);
        Publish(result.State);
    }

    private CharacterOverviewState CreateWorkspaceResetState(string commandId, string notice)
    {
        return _workspaceOverviewLifecycleCoordinator.CreateResetState(State, commandId, notice).State;
    }
}
