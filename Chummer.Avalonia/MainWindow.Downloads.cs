using Chummer.Presentation.Overview;

namespace Chummer.Avalonia;

public partial class MainWindow
{
    private async Task HandlePendingDownloadAsync(PendingDownloadDispatchRequest request)
    {
        if (!_transientStateCoordinator.ShouldHandleDownload(request))
            return;

        DesktopDownloadSaveResult saveResult = await MainWindowDesktopFileCoordinator.SaveDownloadAsync(
            StorageProvider,
            request,
            CancellationToken.None);
        if (saveResult.Outcome == DesktopFileOperationOutcome.Unavailable)
        {
            MainWindowFeedbackCoordinator.ShowDownloadUnavailable(_controls.SectionHost);
            return;
        }

        if (saveResult.Outcome == DesktopFileOperationOutcome.Cancelled)
        {
            MainWindowFeedbackCoordinator.ShowDownloadCancelled(_controls.SectionHost);
            return;
        }

        MainWindowFeedbackCoordinator.ShowDownloadCompleted(
            _controls.SectionHost,
            saveResult.Notice,
            request.Download.FileName);
    }
}
