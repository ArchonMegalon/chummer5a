using Avalonia.Controls;
using Chummer.Contracts.Workspaces;

namespace Chummer.Avalonia;

public partial class MainWindow
{
    private async void CommandsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressCommandSelectionEvent)
            return;

        if (_commandsList.SelectedItem is not CommandListItem command || !command.Enabled)
            return;

        await RunUiActionAsync(
            async () =>
            {
                await _shellPresenter.ExecuteCommandAsync(command.Id, CancellationToken.None);
                await _adapter.ExecuteCommandAsync(command.Id, CancellationToken.None);
            },
            $"execute command '{command.Id}'");
        ClearSelection(_commandsList, ref _suppressCommandSelectionEvent);
    }

    private async void OpenWorkspacesList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressWorkspaceSelectionEvent)
            return;

        if (_openWorkspacesList.SelectedItem is not WorkspaceListItem workspace || !workspace.Enabled)
            return;

        await RunUiActionAsync(
            () => _adapter.SwitchWorkspaceAsync(new CharacterWorkspaceId(workspace.Id), CancellationToken.None),
            $"switch workspace '{workspace.Id}'");
    }

    private async void NavigationTabsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressTabSelectionEvent)
            return;

        if (_navigationTabsList.SelectedItem is not TabListItem tab || !tab.Enabled)
            return;

        await RunUiActionAsync(
            async () =>
            {
                await _shellPresenter.SelectTabAsync(tab.Id, CancellationToken.None);
                if (!string.Equals(_shellPresenter.State.ActiveTabId, tab.Id, StringComparison.Ordinal))
                    return;

                await _adapter.SelectTabAsync(tab.Id, CancellationToken.None);
            },
            $"select tab '{tab.Id}'");
    }

    private async void SectionActionsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSectionActionSelectionEvent)
            return;

        if (_sectionActionsList.SelectedItem is not SectionActionListItem action)
            return;

        await RunUiActionAsync(
            () => _adapter.ExecuteWorkspaceActionAsync(action.Action, CancellationToken.None),
            $"execute workspace action '{action.Id}'");
        ClearSelection(_sectionActionsList, ref _suppressSectionActionSelectionEvent);
    }

    private async void UiControlsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressUiControlSelectionEvent)
            return;

        if (_uiControlsList.SelectedItem is not UiControlListItem control)
            return;

        await RunUiActionAsync(
            () => _adapter.HandleUiControlAsync(control.Id, CancellationToken.None),
            $"handle desktop control '{control.Id}'");
        ClearSelection(_uiControlsList, ref _suppressUiControlSelectionEvent);
    }

    private async void DialogActionsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressDialogActionSelectionEvent)
            return;

        if (_dialogActionsList.SelectedItem is not DialogActionListItem action)
            return;

        await RunUiActionAsync(
            () => _adapter.ExecuteDialogActionAsync(action.Id, CancellationToken.None),
            $"execute dialog action '{action.Id}'");
        ClearSelection(_dialogActionsList, ref _suppressDialogActionSelectionEvent);
    }

    private static void ClearSelection(ListBox listBox, ref bool suppressSelectionEvent)
    {
        suppressSelectionEvent = true;
        listBox.SelectedItem = null;
        suppressSelectionEvent = false;
    }
}
