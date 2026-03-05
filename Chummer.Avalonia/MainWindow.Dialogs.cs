using Chummer.Contracts.Presentation;
using Chummer.Presentation.Overview;

namespace Chummer.Avalonia;

public partial class MainWindow
{
    private void SyncDialogWindow(CharacterOverviewState state)
    {
        if (state.ActiveDialog is null)
        {
            if (_dialogWindow is not null)
            {
                _dialogWindow.CloseFromPresenter();
                _dialogWindow = null;
            }

            return;
        }

        if (_dialogWindow is null)
        {
            _dialogWindow = new DesktopDialogWindow(_adapter);
            _dialogWindow.Closed += DialogWindow_OnClosed;
        }

        _dialogWindow.BindDialog(state.ActiveDialog);
        if (!_dialogWindow.IsVisible)
        {
            _dialogWindow.Show(this);
        }
    }

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
            _statusText.Text = $"State: error - {operationName} failed: {ex.Message}";
            _noticeText.Text = $"Notice: {operationName} failed.";
            _serviceStateText.Text = "Service: error";
            _timeStateText.Text = $"Time: {DateTimeOffset.UtcNow:u}";
        }
    }

    private Task SyncShellWorkspaceContextAsync()
    {
        var state = _adapter.State;
        var activeWorkspaceId = state.Session.ActiveWorkspaceId ?? state.WorkspaceId;
        return _shellPresenter.SyncWorkspaceContextAsync(activeWorkspaceId, CancellationToken.None);
    }
}
