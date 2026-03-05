using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Shell;

namespace Chummer.Avalonia;

public partial class MainWindow
{
    private async void ToolStrip_OnImportRawRequested(object? sender, EventArgs e)
    {
        string importText = _sectionHost.XmlInputText;
        if (string.IsNullOrWhiteSpace(importText))
        {
            _toolStrip.SetStatusText("State: provide debug XML content before importing.");
            return;
        }

        await RunUiActionAsync(
            () => _adapter.ImportAsync(Encoding.UTF8.GetBytes(importText), CancellationToken.None),
            "import debug XML");
    }

    private async void ToolStrip_OnImportFileRequested(object? sender, EventArgs e)
    {
        if (!StorageProvider.CanOpen)
        {
            _toolStrip.SetStatusText("State: file picker unavailable on this platform.");
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

    private async void ToolStrip_OnSaveRequested(object? sender, EventArgs e)
    {
        await RunUiActionAsync(
            () => _presenter.SaveAsync(CancellationToken.None),
            "save workspace");
    }

    private async void ToolStrip_OnCloseWorkspaceRequested(object? sender, EventArgs e)
    {
        CharacterWorkspaceId? activeWorkspaceId = _adapter.State.Session.ActiveWorkspaceId ?? _adapter.State.WorkspaceId;
        if (activeWorkspaceId is null)
        {
            _toolStrip.SetStatusText("State: no active workspace to close.");
            return;
        }

        await RunUiActionAsync(
            () => _adapter.CloseWorkspaceAsync(activeWorkspaceId.Value, CancellationToken.None),
            "close workspace");
    }

    private async void MenuBar_OnMenuSelected(object? sender, string menuId)
    {
        await RunUiActionAsync(
            () => _shellPresenter.ToggleMenuAsync(menuId, CancellationToken.None),
            $"toggle menu '{menuId}'");
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
