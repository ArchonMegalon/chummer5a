using Chummer.Avalonia.Controls;
using Chummer.Presentation.Overview;

namespace Chummer.Avalonia;

public partial class MainWindow
{
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
        _transientStateCoordinator.ApplyShellFrame(shellFrame);
        _controls.ApplyShellFrame(shellFrame);
    }

    private void ApplyPostRefreshEffects(CharacterOverviewState state)
    {
        PendingDownloadDispatchRequest? pendingDownloadRequest = _transientStateCoordinator.ApplyPostRefresh(
            this,
            state,
            _adapter,
            DialogWindow_OnClosed);
        if (pendingDownloadRequest is null)
        {
            return;
        }

        _ = RunUiActionAsync(
            () => HandlePendingDownloadAsync(pendingDownloadRequest),
            "pending download");
    }
}
