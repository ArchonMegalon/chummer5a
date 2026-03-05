using Avalonia.Threading;
using Chummer.Avalonia.Controls;
using Chummer.Presentation.Overview;

namespace Chummer.Avalonia;

public partial class MainWindow
{
    private void ShellPresenter_OnStateChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(RefreshState);
    }

    private void RefreshState()
    {
        CharacterOverviewState state = _adapter.State;
        MainWindowShellFrame shellFrame = MainWindowShellFrameProjector.Project(
            state,
            _shellSurfaceResolver.Resolve(state, _shellPresenter.State),
            _commandAvailabilityEvaluator);

        ApplyShellFrame(shellFrame);
        ApplyPostRefreshEffects(state);
    }

    private void ApplyShellFrame(MainWindowShellFrame shellFrame)
    {
        _workspaceActionsById = shellFrame.WorkspaceActionsById;

        ApplyHeaderState(shellFrame.HeaderState);
        ApplyChromeState(shellFrame.ChromeState);

        _commandDialogPane.SetState(shellFrame.CommandDialogPaneState);
        _navigatorPane.SetState(shellFrame.NavigatorPaneState);
        _sectionHost.SetState(shellFrame.SectionHostState);
    }

    private void ApplyHeaderState(MainWindowHeaderState headerState)
    {
        _toolStrip.SetState(headerState.ToolStrip);
        _menuBar.SetState(headerState.MenuBar);
    }

    private void ApplyChromeState(MainWindowChromeState chromeState)
    {
        _workspaceStrip.SetState(chromeState.WorkspaceStrip);
        _summaryHeader.SetState(chromeState.SummaryHeader);
        _statusStrip.SetState(chromeState.StatusStrip);
    }

    private void ApplyPostRefreshEffects(CharacterOverviewState state)
    {
        _dialogWindow = MainWindowDialogWindowCoordinator.Sync(
            owner: this,
            currentWindow: _dialogWindow,
            activeDialog: state.ActiveDialog,
            adapter: _adapter,
            onClosed: DialogWindow_OnClosed);

        PendingDownloadDispatchRequest? pendingDownloadRequest = PendingDownloadDispatchCoordinator.TryCreate(
            state,
            _lastDownloadVersionHandled);
        if (pendingDownloadRequest is null)
        {
            return;
        }

        _lastDownloadVersionHandled = pendingDownloadRequest.Version;
        _ = RunUiActionAsync(
            () => HandlePendingDownloadAsync(pendingDownloadRequest),
            "pending download");
    }
}
