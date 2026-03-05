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
        CommandDialogPaneState commandDialogState = BuildCommandDialogState(state, shellFrame);

        ApplyShellFrame(shellFrame, commandDialogState);

        SyncDialogWindow(state);
        DispatchPendingDownload(state);
    }

    private void ApplyShellFrame(
        MainWindowShellFrame shellFrame,
        CommandDialogPaneState commandDialogState)
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

        _commandDialogPane.SetState(commandDialogState);
        _navigatorPane.SetState(shellFrame.NavigatorPaneState);
        _sectionHost.SetSectionPreview(shellFrame.SectionPreviewJson, shellFrame.SectionRows);
    }

    private static CommandDialogPaneState BuildCommandDialogState(
        CharacterOverviewState state,
        MainWindowShellFrame shellFrame)
    {
        if (state.ActiveDialog is null)
        {
            return new CommandDialogPaneState(
                Commands: shellFrame.Commands,
                SelectedCommandId: shellFrame.LastCommandId,
                DialogTitle: null,
                DialogMessage: null,
                Fields: Array.Empty<DialogFieldDisplayItem>(),
                Actions: Array.Empty<DialogActionDisplayItem>());
        }

        DialogFieldDisplayItem[] fields = state.ActiveDialog.Fields
            .Select(field => new DialogFieldDisplayItem(field.Id, field.Label, field.Value))
            .ToArray();
        DialogActionDisplayItem[] actions = state.ActiveDialog.Actions
            .Select(action => new DialogActionDisplayItem(action.Id, action.Label, action.IsPrimary))
            .ToArray();
        return new CommandDialogPaneState(
            Commands: shellFrame.Commands,
            SelectedCommandId: shellFrame.LastCommandId,
            DialogTitle: state.ActiveDialog.Title,
            DialogMessage: state.ActiveDialog.Message,
            Fields: fields,
            Actions: actions);
    }
}
