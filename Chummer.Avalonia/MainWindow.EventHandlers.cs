using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Shell;

namespace Chummer.Avalonia;

public partial class MainWindow
{
    private async void ImportButton_OnClick(object? sender, RoutedEventArgs e)
    {
        string importText = _xmlInputBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(importText))
        {
            _statusText.Text = "State: provide debug XML content before importing.";
            return;
        }

        await RunUiActionAsync(
            () => _adapter.ImportAsync(Encoding.UTF8.GetBytes(importText), CancellationToken.None),
            "import debug XML");
    }

    private async void ImportFileButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!StorageProvider.CanOpen)
        {
            _statusText.Text = "State: file picker unavailable on this platform.";
            return;
        }

        await RunUiActionAsync(async () =>
        {
            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Character File",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Chummer Character Files")
                    {
                        Patterns = ["*.chum5", "*.xml"]
                    }
                ]
            });

            IStorageFile? file = files.FirstOrDefault();
            if (file is null)
                return;

            await using Stream stream = await file.OpenReadAsync();
            using MemoryStream memory = new();
            await stream.CopyToAsync(memory, CancellationToken.None);
            await _adapter.ImportAsync(memory.ToArray(), CancellationToken.None);
        }, "import character file");
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        await RunUiActionAsync(
            async () =>
            {
                await _shellPresenter.InitializeAsync(CancellationToken.None);
                await _adapter.InitializeAsync(CancellationToken.None);
            },
            "initialize desktop shell");
    }

    private async void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await RunUiActionAsync(
            () => _presenter.SaveAsync(CancellationToken.None),
            "save workspace");
    }

    private async void CloseWorkspaceButton_OnClick(object? sender, RoutedEventArgs e)
    {
        CharacterWorkspaceId? activeWorkspaceId = _adapter.State.Session.ActiveWorkspaceId ?? _adapter.State.WorkspaceId;
        if (activeWorkspaceId is null)
        {
            _statusText.Text = "State: no active workspace to close.";
            return;
        }

        await RunUiActionAsync(
            () => _adapter.CloseWorkspaceAsync(activeWorkspaceId.Value, CancellationToken.None),
            "close workspace");
    }

    private async void MenuButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Content is null)
            return;

        string menuId = button.Content.ToString()!.Trim().ToLowerInvariant();
        await RunUiActionAsync(
            () => _shellPresenter.ToggleMenuAsync(menuId, CancellationToken.None),
            $"toggle menu '{menuId}'");
    }

    private void ShellPresenter_OnStateChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(RefreshState);
    }

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
        _suppressCommandSelectionEvent = true;
        _commandsList.SelectedItem = null;
        _suppressCommandSelectionEvent = false;
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
        _suppressSectionActionSelectionEvent = true;
        _sectionActionsList.SelectedItem = null;
        _suppressSectionActionSelectionEvent = false;
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
        _suppressUiControlSelectionEvent = true;
        _uiControlsList.SelectedItem = null;
        _suppressUiControlSelectionEvent = false;
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
        _suppressDialogActionSelectionEvent = true;
        _dialogActionsList.SelectedItem = null;
        _suppressDialogActionSelectionEvent = false;
    }

    private async void Window_OnKeyDown(object? sender, KeyEventArgs e)
    {
        bool commandModifier = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        bool shiftModifier = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        bool altModifier = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        if (!DesktopShortcutCatalog.TryResolveCommandId(
                e.Key.ToString(),
                commandModifier,
                shiftModifier,
                altModifier,
                out string commandId))
        {
            return;
        }

        e.Handled = true;
        await RunUiActionAsync(
            async () =>
            {
                await _shellPresenter.ExecuteCommandAsync(commandId, CancellationToken.None);
                await _adapter.ExecuteCommandAsync(commandId, CancellationToken.None);
            },
            $"execute hotkey command '{commandId}'");
    }
}
