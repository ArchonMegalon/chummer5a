using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;

namespace Chummer.Avalonia;

internal static class MainWindowDialogWindowCoordinator
{
    public static DesktopDialogWindow? Sync(
        MainWindow owner,
        DesktopDialogWindow? currentWindow,
        DesktopDialogState? activeDialog,
        CharacterOverviewViewModelAdapter adapter,
        EventHandler onClosed)
    {
        if (activeDialog is null)
        {
            if (currentWindow is not null)
            {
                currentWindow.CloseFromPresenter();
            }

            return null;
        }

        DesktopDialogWindow dialogWindow = currentWindow ?? CreateDialogWindow(adapter, onClosed);
        dialogWindow.BindDialog(activeDialog);
        if (!dialogWindow.IsVisible)
        {
            dialogWindow.Show(owner);
        }

        return dialogWindow;
    }

    private static DesktopDialogWindow CreateDialogWindow(
        CharacterOverviewViewModelAdapter adapter,
        EventHandler onClosed)
    {
        DesktopDialogWindow dialogWindow = new(adapter);
        dialogWindow.Closed += onClosed;
        return dialogWindow;
    }
}

internal static class PendingDownloadDispatchCoordinator
{
    public static PendingDownloadDispatchRequest? TryCreate(
        CharacterOverviewState state,
        long lastHandledVersion)
    {
        WorkspaceDownloadReceipt? pendingDownload = state.PendingDownload;
        if (pendingDownload is null || state.PendingDownloadVersion <= lastHandledVersion)
        {
            return null;
        }

        return new PendingDownloadDispatchRequest(
            pendingDownload,
            state.PendingDownloadVersion);
    }
}

internal sealed record PendingDownloadDispatchRequest(
    WorkspaceDownloadReceipt Download,
    long Version);
