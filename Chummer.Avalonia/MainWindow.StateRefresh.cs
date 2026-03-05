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
        RefreshDialogState(state);

        SyncDialogWindow(state);
        DispatchPendingDownload(state);
    }

    private void ApplyShellFrame(MainWindowShellFrame shellFrame)
    {
        _workspaceActionsById = shellFrame.WorkspaceActionsById;

        _toolStrip.SetStatusText(shellFrame.ToolStripStatusText);
        _sectionHost.SetNotice(shellFrame.NoticeText);
        _workspaceStrip.SetWorkspaceText(shellFrame.WorkspaceStripText);

        _summaryHeader.SetValues(
            name: shellFrame.SummaryName,
            alias: shellFrame.SummaryAlias,
            karma: shellFrame.SummaryKarma,
            skills: shellFrame.SummarySkills);

        _statusStrip.SetValues(
            characterState: shellFrame.CharacterStatusText,
            serviceState: shellFrame.ServiceStatusText,
            timeState: shellFrame.TimeStatusText,
            complianceState: shellFrame.ComplianceStatusText);

        _menuBar.SetMenuState(
            openMenuId: shellFrame.OpenMenuId,
            knownMenuIds: shellFrame.KnownMenuIds,
            isBusy: shellFrame.IsBusy);

        _commandDialogPane.SetCommands(shellFrame.Commands, shellFrame.LastCommandId);
        _navigatorPane.SetState(shellFrame.NavigatorPaneState);
        _sectionHost.SetSectionPreview(shellFrame.SectionPreviewJson, shellFrame.SectionRows);
    }

    private void RefreshDialogState(CharacterOverviewState state)
    {
        if (state.ActiveDialog is null)
        {
            _commandDialogPane.SetDialog(
                title: null,
                message: null,
                fields: Array.Empty<DialogFieldDisplayItem>(),
                actions: Array.Empty<DialogActionDisplayItem>());
            return;
        }

        DialogFieldDisplayItem[] fields = state.ActiveDialog.Fields
            .Select(field => new DialogFieldDisplayItem(field.Id, field.Label, field.Value))
            .ToArray();
        DialogActionDisplayItem[] actions = state.ActiveDialog.Actions
            .Select(action => new DialogActionDisplayItem(action.Id, action.Label, action.IsPrimary))
            .ToArray();
        _commandDialogPane.SetDialog(
            title: state.ActiveDialog.Title,
            message: state.ActiveDialog.Message,
            fields: fields,
            actions: actions);
    }
}
