using Chummer.Contracts.Presentation;
using Chummer.Presentation.Overview;

namespace Chummer.Avalonia;

internal sealed class MainWindowTransientStateCoordinator
{
    private IReadOnlyDictionary<string, WorkspaceSurfaceActionDefinition> _workspaceActionsById
        = new Dictionary<string, WorkspaceSurfaceActionDefinition>(StringComparer.Ordinal);
    private DesktopDialogWindow? _dialogWindow;
    private long _lastHandledDownloadVersion;

    public void ApplyShellFrame(MainWindowShellFrame shellFrame)
    {
        _workspaceActionsById = shellFrame.WorkspaceActionsById;
    }

    public PendingDownloadDispatchRequest? ApplyPostRefresh(
        MainWindow owner,
        CharacterOverviewState state,
        CharacterOverviewViewModelAdapter adapter,
        EventHandler onDialogClosed)
    {
        MainWindowPostRefreshResult postRefresh = MainWindowPostRefreshCoordinator.Apply(
            owner: owner,
            currentDialogWindow: _dialogWindow,
            state: state,
            adapter: adapter,
            lastHandledDownloadVersion: _lastHandledDownloadVersion,
            onDialogClosed: onDialogClosed);
        _dialogWindow = postRefresh.DialogWindow;

        PendingDownloadDispatchRequest? pendingDownloadRequest = postRefresh.PendingDownloadRequest;
        if (pendingDownloadRequest is null)
        {
            return null;
        }

        _lastHandledDownloadVersion = postRefresh.LastHandledDownloadVersion;
        return pendingDownloadRequest;
    }

    public bool ShouldHandleDownload(PendingDownloadDispatchRequest request)
    {
        return request.Version >= _lastHandledDownloadVersion;
    }

    public bool TryResolveWorkspaceAction(string actionId, out WorkspaceSurfaceActionDefinition? action)
    {
        return _workspaceActionsById.TryGetValue(actionId, out action);
    }

    public void ClearDialogWindow(object? sender)
    {
        if (ReferenceEquals(sender, _dialogWindow))
        {
            _dialogWindow = null;
        }
    }

    public DesktopDialogWindow? DetachDialogWindow()
    {
        DesktopDialogWindow? dialogWindow = _dialogWindow;
        _dialogWindow = null;
        return dialogWindow;
    }
}
