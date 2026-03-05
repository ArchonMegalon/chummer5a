using System.IO;
using Avalonia.Platform.Storage;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;

namespace Chummer.Avalonia;

public partial class MainWindow
{
    private async Task HandlePendingDownloadAsync(PendingDownloadDispatchRequest request)
    {
        if (request.Version < _lastDownloadVersionHandled)
            return;

        if (!StorageProvider.CanSave)
        {
            _sectionHost.SetNotice("Notice: download requested but file save is unavailable on this platform.");
            return;
        }

        IReadOnlyList<FilePickerFileType> fileTypes =
            request.Download.Format == WorkspaceDocumentFormat.Json
                ? [
                    new FilePickerFileType("JSON Files")
                    {
                        Patterns = ["*.json"],
                        MimeTypes = ["application/json"]
                    }
                ]
                : [
                    new FilePickerFileType("Chummer Character Files")
                    {
                        Patterns = ["*.chum5", "*.xml"],
                        MimeTypes = ["application/xml"]
                    }
                ];

        string pickerTitle = request.Download.Format == WorkspaceDocumentFormat.Json
            ? "Download Export Bundle"
            : "Save Character As";

        IStorageFile? targetFile = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = pickerTitle,
            SuggestedFileName = request.Download.FileName,
            FileTypeChoices = fileTypes,
            ShowOverwritePrompt = true
        });

        if (targetFile is null)
        {
            _sectionHost.SetNotice("Notice: download canceled.");
            return;
        }

        byte[] payloadBytes = Convert.FromBase64String(request.Download.ContentBase64);
        await using Stream output = await targetFile.OpenWriteAsync();
        if (output.CanSeek)
            output.SetLength(0);

        await output.WriteAsync(payloadBytes, CancellationToken.None);
        await output.FlushAsync(CancellationToken.None);
        _sectionHost.SetNotice($"Notice: downloaded {request.Download.FileName} to {targetFile.Name}.");
    }
}
