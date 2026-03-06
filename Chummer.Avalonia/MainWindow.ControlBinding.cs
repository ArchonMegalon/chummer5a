using Chummer.Avalonia.Controls;

namespace Chummer.Avalonia;

internal static class MainWindowControlBinder
{
    public static MainWindowControls Bind(
        ToolStripControl toolStrip,
        WorkspaceStripControl workspaceStrip,
        SummaryHeaderControl summaryHeader,
        ShellMenuBarControl menuBar,
        NavigatorPaneControl navigatorPane,
        SectionHostControl sectionHost,
        CommandDialogPaneControl commandDialogPane,
        StatusStripControl statusStrip,
        EventHandler onImportFileRequested,
        EventHandler onImportRawRequested,
        EventHandler onSaveRequested,
        EventHandler onCloseWorkspaceRequested,
        EventHandler<string> onMenuSelected,
        EventHandler<string> onWorkspaceSelected,
        EventHandler<string> onNavigationTabSelected,
        EventHandler<string> onSectionActionSelected,
        EventHandler<string> onUiControlSelected,
        EventHandler<string> onCommandSelected,
        EventHandler<string> onDialogActionSelected)
    {
        toolStrip.ImportFileRequested += onImportFileRequested;
        toolStrip.ImportRawRequested += onImportRawRequested;
        toolStrip.SaveRequested += onSaveRequested;
        toolStrip.CloseWorkspaceRequested += onCloseWorkspaceRequested;
        menuBar.MenuSelected += onMenuSelected;
        navigatorPane.WorkspaceSelected += onWorkspaceSelected;
        navigatorPane.NavigationTabSelected += onNavigationTabSelected;
        navigatorPane.SectionActionSelected += onSectionActionSelected;
        navigatorPane.UiControlSelected += onUiControlSelected;
        commandDialogPane.CommandSelected += onCommandSelected;
        commandDialogPane.DialogActionSelected += onDialogActionSelected;

        return new MainWindowControls(
            toolStrip,
            workspaceStrip,
            summaryHeader,
            menuBar,
            navigatorPane,
            sectionHost,
            commandDialogPane,
            statusStrip);
    }
}

internal sealed record MainWindowControls(
    ToolStripControl ToolStrip,
    WorkspaceStripControl WorkspaceStrip,
    SummaryHeaderControl SummaryHeader,
    ShellMenuBarControl MenuBar,
    NavigatorPaneControl NavigatorPane,
    SectionHostControl SectionHost,
    CommandDialogPaneControl CommandDialogPane,
    StatusStripControl StatusStrip)
{
    public string SectionHostInputText => SectionHost.XmlInputText;

    public void ApplyShellFrame(MainWindowShellFrame shellFrame)
    {
        ToolStrip.SetState(shellFrame.HeaderState.ToolStrip);
        MenuBar.SetState(shellFrame.HeaderState.MenuBar);
        WorkspaceStrip.SetState(shellFrame.ChromeState.WorkspaceStrip);
        SummaryHeader.SetState(shellFrame.ChromeState.SummaryHeader);
        StatusStrip.SetState(shellFrame.ChromeState.StatusStrip);
        CommandDialogPane.SetState(shellFrame.CommandDialogPaneState);
        NavigatorPane.SetState(shellFrame.NavigatorPaneState);
        SectionHost.SetState(shellFrame.SectionHostState);
    }
}
