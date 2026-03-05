using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;

namespace Chummer.Avalonia;

public partial class MainWindow
{
    private async void CommandDialogPane_OnCommandSelected(object? sender, string commandId)
    {
        await RunUiActionAsync(
            async () =>
            {
                await _shellPresenter.ExecuteCommandAsync(commandId, CancellationToken.None);
                await _adapter.ExecuteCommandAsync(commandId, CancellationToken.None);
            },
            $"execute command '{commandId}'");
    }

    private async void NavigatorPane_OnWorkspaceSelected(object? sender, string workspaceId)
    {
        await RunUiActionAsync(
            () => _adapter.SwitchWorkspaceAsync(new CharacterWorkspaceId(workspaceId), CancellationToken.None),
            $"switch workspace '{workspaceId}'");
    }

    private async void NavigatorPane_OnNavigationTabSelected(object? sender, string tabId)
    {
        await RunUiActionAsync(
            async () =>
            {
                await _shellPresenter.SelectTabAsync(tabId, CancellationToken.None);
                if (!string.Equals(_shellPresenter.State.ActiveTabId, tabId, StringComparison.Ordinal))
                    return;

                await _adapter.SelectTabAsync(tabId, CancellationToken.None);
            },
            $"select tab '{tabId}'");
    }

    private async void NavigatorPane_OnSectionActionSelected(object? sender, string actionId)
    {
        if (!_workspaceActionsById.TryGetValue(actionId, out WorkspaceSurfaceActionDefinition? action))
            return;

        await RunUiActionAsync(
            () => _adapter.ExecuteWorkspaceActionAsync(action, CancellationToken.None),
            $"execute workspace action '{actionId}'");
    }

    private async void NavigatorPane_OnUiControlSelected(object? sender, string controlId)
    {
        await RunUiActionAsync(
            () => _adapter.HandleUiControlAsync(controlId, CancellationToken.None),
            $"handle desktop control '{controlId}'");
    }

    private async void CommandDialogPane_OnDialogActionSelected(object? sender, string actionId)
    {
        await RunUiActionAsync(
            () => _adapter.ExecuteDialogActionAsync(actionId, CancellationToken.None),
            $"execute dialog action '{actionId}'");
    }
}
