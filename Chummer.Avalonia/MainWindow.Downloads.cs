using System.IO;
using Avalonia.Platform.Storage;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;

namespace Chummer.Avalonia;

public partial class MainWindow
{
    private void DispatchPendingDownload(CharacterOverviewState state)
    {
        WorkspaceDownloadReceipt? pendingDownload = state.PendingDownload;
        if (pendingDownload is null || state.PendingDownloadVersion <= _lastDownloadVersionHandled)
            return;

        long pendingVersion = state.PendingDownloadVersion;
        _lastDownloadVersionHandled = pendingVersion;
        _ = RunUiActionAsync(
            () => HandlePendingDownloadAsync(pendingDownload, pendingVersion),
            "pending download");
    }

    private async Task HandlePendingDownloadAsync(WorkspaceDownloadReceipt pendingDownload, long pendingVersion)
    {
        if (pendingVersion < _lastDownloadVersionHandled)
            return;

        if (!StorageProvider.CanSave)
        {
            _sectionHost.SetNotice("Notice: download requested but file save is unavailable on this platform.");
            return;
        }

        IReadOnlyList<FilePickerFileType> fileTypes =
        [
            new FilePickerFileType("Chummer Character Files")
            {
                Patterns = ["*.chum5", "*.xml"],
                MimeTypes = ["application/xml"]
            }
        ];

        IStorageFile? targetFile = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Character As",
            SuggestedFileName = pendingDownload.FileName,
            FileTypeChoices = fileTypes,
            ShowOverwritePrompt = true
        });

        if (targetFile is null)
        {
            _sectionHost.SetNotice("Notice: download canceled.");
            return;
        }

        byte[] payloadBytes = Convert.FromBase64String(pendingDownload.ContentBase64);
        await using Stream output = await targetFile.OpenWriteAsync();
        if (output.CanSeek)
            output.SetLength(0);

        await output.WriteAsync(payloadBytes, CancellationToken.None);
        await output.FlushAsync(CancellationToken.None);
        _sectionHost.SetNotice($"Notice: downloaded {pendingDownload.FileName} to {targetFile.Name}.");
    }
}
