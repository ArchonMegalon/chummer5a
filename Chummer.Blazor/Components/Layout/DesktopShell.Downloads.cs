using Chummer.Contracts.Workspaces;
using Microsoft.JSInterop;

namespace Chummer.Blazor.Components.Layout;

public partial class DesktopShell
{
    private async Task DispatchPendingDownloadAsync()
    {
        WorkspaceDownloadReceipt? pendingDownload = State.PendingDownload;
        if (pendingDownload is null || State.PendingDownloadVersion <= _lastDownloadVersionHandled)
            return;

        string mimeType = pendingDownload.Format == WorkspaceDocumentFormat.Chum5Xml
            ? "application/xml"
            : "application/octet-stream";

        try
        {
            await JsRuntime.InvokeVoidAsync(
                "chummerDownloads.downloadBase64",
                pendingDownload.FileName,
                pendingDownload.ContentBase64,
                mimeType);
            _lastDownloadVersionHandled = State.PendingDownloadVersion;
        }
        catch (JSException ex)
        {
            ImportError = $"Download failed: {ex.Message}";
        }
    }
}
