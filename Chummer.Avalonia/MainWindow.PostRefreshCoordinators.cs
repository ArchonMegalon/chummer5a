using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;

namespace Chummer.Avalonia;

internal static class MainWindowPostRefreshCoordinator
{
    public static MainWindowPostRefreshResult Apply(
        MainWindow owner,
        DesktopDialogWindow? currentDialogWindow,
        CharacterOverviewState state,
        CharacterOverviewViewModelAdapter adapter,
        long lastHandledDownloadVersion,
        EventHandler onDialogClosed)
    {
        DesktopDialogWindow? dialogWindow = SyncDialogWindow(
            owner,
            currentDialogWindow,
            state.ActiveDialog,
            adapter,
            onDialogClosed);

        PendingDownloadDispatchRequest? pendingDownloadRequest = TryCreatePendingDownload(
            state,
            lastHandledDownloadVersion);
        long nextHandledDownloadVersion = pendingDownloadRequest?.Version ?? lastHandledDownloadVersion;

        return new MainWindowPostRefreshResult(
            dialogWindow,
            pendingDownloadRequest,
            nextHandledDownloadVersion);
    }

    private static DesktopDialogWindow? SyncDialogWindow(
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

    private static PendingDownloadDispatchRequest? TryCreatePendingDownload(
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

internal sealed record MainWindowPostRefreshResult(
    DesktopDialogWindow? DialogWindow,
    PendingDownloadDispatchRequest? PendingDownloadRequest,
    long LastHandledDownloadVersion);
