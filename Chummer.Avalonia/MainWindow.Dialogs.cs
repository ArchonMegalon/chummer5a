using Chummer.Contracts.Presentation;
using Chummer.Presentation.Overview;

namespace Chummer.Avalonia;

public partial class MainWindow
{
    private void DialogWindow_OnClosed(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, _dialogWindow))
        {
            _dialogWindow = null;
        }
    }

    private async Task RunUiActionAsync(Func<Task> operation, string operationName)
    {
        try
        {
            await operation();
            await SyncShellWorkspaceContextAsync();
        }
        catch (OperationCanceledException)
        {
            // UI-triggered operations are best-effort; canceled actions should not fault the window thread.
        }
        catch (Exception ex)
        {
            _toolStrip.SetStatusText($"State: error - {operationName} failed: {ex.Message}");
            _sectionHost.SetNotice($"Notice: {operationName} failed.");
            _statusStrip.SetServiceAndTime(
                serviceState: "Service: error",
                timeState: $"Time: {DateTimeOffset.UtcNow:u}");
        }
    }

    private Task SyncShellWorkspaceContextAsync()
    {
        var state = _adapter.State;
        var activeWorkspaceId = state.Session.ActiveWorkspaceId ?? state.WorkspaceId;
        return _shellPresenter.SyncWorkspaceContextAsync(activeWorkspaceId, CancellationToken.None);
    }
}
