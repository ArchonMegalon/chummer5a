using Chummer.Avalonia.Controls;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;

namespace Chummer.Avalonia;

internal static class MainWindowShellFrameProjector
{
    public static MainWindowShellFrame Project(
        CharacterOverviewState state,
        ShellSurfaceState shellSurface,
        ICommandAvailabilityEvaluator commandAvailabilityEvaluator)
    {
        ActiveWorkspaceContext workspaceContext = ResolveActiveWorkspaceContext(shellSurface);
        IReadOnlyDictionary<string, WorkspaceSurfaceActionDefinition> workspaceActionsById = BuildWorkspaceActionLookup(shellSurface.WorkspaceActions);
        CommandPaletteItem[] commands = ProjectCommands(state, shellSurface, commandAvailabilityEvaluator);

        return new MainWindowShellFrame(
            HeaderState: new MainWindowHeaderState(
                ToolStrip: new ToolStripState(
                    BuildToolStripStatusText(state, shellSurface, workspaceContext)),
                MenuBar: new MenuBarState(
                    OpenMenuId: shellSurface.OpenMenuId,
                    KnownMenuIds: shellSurface.MenuRoots.Select(menu => menu.Id).ToArray(),
                    IsBusy: state.IsBusy)),
            ChromeState: new MainWindowChromeState(
                WorkspaceStrip: new WorkspaceStripState(
                    $"Workspace: {(workspaceContext.ActiveWorkspaceId?.Value ?? "none")} (open: {workspaceContext.OpenWorkspaceCount}, {workspaceContext.ActiveWorkspaceSaveStatus})"),
                SummaryHeader: new SummaryHeaderState(
                    Name: state.Profile?.Name,
                    Alias: state.Profile?.Alias,
                    Karma: state.Progress?.Karma.ToString(),
                    Skills: state.Skills?.Count.ToString(),
                    RuntimeSummary: ShellStatusTextFormatter.BuildActiveRuntimeSummary(shellSurface.ActiveRuntime),
                    CanInspectRuntime: shellSurface.ActiveRuntime is not null,
                    CampaignMemory: ProjectCampaignMemory(state, shellSurface)),
                StatusStrip: new StatusStripState(
                    CharacterState: $"Character: {(workspaceContext.ActiveWorkspaceId is null ? "none" : "loaded")}",
                    ServiceState: $"Service: {(shellSurface.Error is null ? "online" : "error")}",
                    TimeState: $"Time: {DateTimeOffset.UtcNow:u}",
                    ComplianceState: ShellStatusTextFormatter.BuildComplianceState(shellSurface, state.Preferences))),
            SectionHostState: new SectionHostState(
                Notice: $"Notice: {(shellSurface.Notice ?? "Ready.")}",
                PreviewJson: state.ActiveSectionJson ?? string.Empty,
                Rows: state.ActiveSectionRows
                    .Select(row => new SectionRowDisplayItem(row.Path, row.Value))
                    .ToArray()),
            CommandDialogPaneState: ProjectCommandDialogState(state, commands, shellSurface.LastCommandId),
            NavigatorPaneState: new NavigatorPaneState(
                OpenWorkspaces: ProjectOpenWorkspaces(state, shellSurface),
                SelectedWorkspaceId: shellSurface.ActiveWorkspaceId?.Value,
                NavigationTabs: ProjectNavigationTabs(state, shellSurface, commandAvailabilityEvaluator),
                ActiveTabId: shellSurface.ActiveTabId,
                SectionActions: ProjectSectionActions(shellSurface),
                ActiveActionId: state.ActiveActionId,
                WorkflowSurfaces: ProjectWorkflowSurfaces(shellSurface)),
            WorkspaceActionsById: workspaceActionsById);
    }

    private static string BuildToolStripStatusText(
        CharacterOverviewState state,
        ShellSurfaceState shellSurface,
        ActiveWorkspaceContext workspaceContext)
    {
        if (shellSurface.Error is not null)
        {
            return $"State: error - {shellSurface.Error}";
        }

        return $"State: {(state.IsBusy ? "busy" : "ready")}, workspace={(workspaceContext.ActiveWorkspaceId?.Value ?? "none")}, open={workspaceContext.OpenWorkspaceCount}, saved={state.HasSavedWorkspace}, last-command={(shellSurface.LastCommandId ?? "none")}";
    }

    private static ActiveWorkspaceContext ResolveActiveWorkspaceContext(ShellSurfaceState shellSurface)
    {
        int openWorkspaceCount = shellSurface.OpenWorkspaces.Count;
        CharacterWorkspaceId? activeWorkspaceId = shellSurface.ActiveWorkspaceId;
        OpenWorkspaceState? activeWorkspace = shellSurface.OpenWorkspaces
            .FirstOrDefault(workspace => string.Equals(workspace.Id.Value, activeWorkspaceId?.Value, StringComparison.Ordinal));
        string activeWorkspaceSaveStatus = activeWorkspace is null
            ? "n/a"
            : activeWorkspace.HasSavedWorkspace ? "saved" : "unsaved";
        return new ActiveWorkspaceContext(activeWorkspaceId, openWorkspaceCount, activeWorkspaceSaveStatus);
    }

    private static CommandPaletteItem[] ProjectCommands(
        CharacterOverviewState state,
        ShellSurfaceState shellSurface,
        ICommandAvailabilityEvaluator commandAvailabilityEvaluator)
    {
        IEnumerable<AppCommandDefinition> visibleCommands = shellSurface.Commands
            .Where(command => !string.Equals(command.Group, "menu", StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(shellSurface.OpenMenuId))
        {
            visibleCommands = visibleCommands.Where(command => string.Equals(command.Group, shellSurface.OpenMenuId, StringComparison.Ordinal));
        }

        return visibleCommands
            .Select(command => new CommandPaletteItem(
                command.Id,
                command.Group,
                commandAvailabilityEvaluator.IsCommandEnabled(command, state)))
            .ToArray();
    }

    private static NavigatorWorkspaceItem[] ProjectOpenWorkspaces(CharacterOverviewState state, ShellSurfaceState shellSurface)
    {
        return shellSurface.OpenWorkspaces
            .Select(workspace => new NavigatorWorkspaceItem(
                workspace.Id.Value,
                workspace.Name,
                workspace.Alias,
                workspace.HasSavedWorkspace,
                Enabled: !state.IsBusy))
            .ToArray();
    }

    private static NavigatorTabItem[] ProjectNavigationTabs(
        CharacterOverviewState state,
        ShellSurfaceState shellSurface,
        ICommandAvailabilityEvaluator commandAvailabilityEvaluator)
    {
        return shellSurface.NavigationTabs
            .Select(tab => new NavigatorTabItem(
                tab.Id,
                tab.Label,
                tab.SectionId,
                tab.Group,
                commandAvailabilityEvaluator.IsNavigationTabEnabled(tab, state)))
            .ToArray();
    }

    private static NavigatorSectionActionItem[] ProjectSectionActions(ShellSurfaceState shellSurface)
    {
        return shellSurface.WorkspaceActions
            .Select(action => new NavigatorSectionActionItem(
                action.Id,
                action.Label,
                action.Kind))
            .ToArray();
    }

    private static NavigatorWorkflowSurfaceItem[] ProjectWorkflowSurfaces(ShellSurfaceState shellSurface)
    {
        return shellSurface.ActiveWorkflowSurfaceActions
            .Select(surface => new NavigatorWorkflowSurfaceItem(
                surface.SurfaceId,
                surface.WorkflowId,
                surface.Label,
                surface.ActionId))
            .ToArray();
    }

    private static CommandDialogPaneState ProjectCommandDialogState(
        CharacterOverviewState state,
        CommandPaletteItem[] commands,
        string? lastCommandId)
    {
        if (state.ActiveDialog is null)
        {
            return new CommandDialogPaneState(
                Commands: commands,
                SelectedCommandId: lastCommandId,
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
            Commands: commands,
            SelectedCommandId: lastCommandId,
            DialogTitle: state.ActiveDialog.Title,
            DialogMessage: state.ActiveDialog.Message,
            Fields: fields,
            Actions: actions);
    }

    private static IReadOnlyDictionary<string, WorkspaceSurfaceActionDefinition> BuildWorkspaceActionLookup(
        IReadOnlyList<WorkspaceSurfaceActionDefinition> workspaceActions)
    {
        var lookup = new Dictionary<string, WorkspaceSurfaceActionDefinition>(StringComparer.Ordinal);
        foreach (WorkspaceSurfaceActionDefinition action in workspaceActions)
        {
            lookup[action.Id] = action;
        }

        return lookup;
    }

    internal static CampaignMemoryState ProjectCampaignMemory(
        CharacterOverviewState state,
        ShellSurfaceState shellSurface)
    {
        return ProjectCampaignMemory(state, shellSurface, DateTimeOffset.UtcNow);
    }

    internal static CampaignMemoryState ProjectCampaignMemory(
        CharacterOverviewState state,
        ShellSurfaceState shellSurface,
        DateTimeOffset nowUtc)
    {
        OpenWorkspaceState? activeWorkspace = shellSurface.OpenWorkspaces
            .FirstOrDefault(workspace => string.Equals(workspace.Id.Value, shellSurface.ActiveWorkspaceId?.Value, StringComparison.Ordinal));

        string consequenceSummary = BuildConsequenceSummary(state, shellSurface, activeWorkspace);
        string staleStateSummary = BuildStaleStateSummary(shellSurface, activeWorkspace, nowUtc);
        string returnActionSummary = BuildReturnActionSummary(state, shellSurface);
        return new CampaignMemoryState(consequenceSummary, staleStateSummary, returnActionSummary);
    }

    private static string BuildConsequenceSummary(
        CharacterOverviewState state,
        ShellSurfaceState shellSurface,
        OpenWorkspaceState? activeWorkspace)
    {
        if (state.LatestPortabilityActivity is { } activity)
        {
            WorkspacePortabilityNote? primaryNote = activity.Receipt.Notes
                .FirstOrDefault(note =>
                    string.Equals(note.Severity, WorkspacePortabilityNoteSeverities.Warning, StringComparison.Ordinal)
                    || string.Equals(note.Severity, WorkspacePortabilityNoteSeverities.Error, StringComparison.Ordinal));
            string summary = primaryNote?.Summary ?? activity.Receipt.ReceiptSummary;
            return $"{activity.Title}: {summary}";
        }

        if (!string.IsNullOrWhiteSpace(shellSurface.Notice))
        {
            return shellSurface.Notice;
        }

        if (activeWorkspace is not null)
        {
            return $"No governed campaign consequence receipt is pinned for {activeWorkspace.Name} yet.";
        }

        return "No restored workspace is carrying campaign consequence memory yet.";
    }

    private static string BuildStaleStateSummary(
        ShellSurfaceState shellSurface,
        OpenWorkspaceState? activeWorkspace,
        DateTimeOffset nowUtc)
    {
        if (activeWorkspace is null)
        {
            return "No active workspace is restored. Open a roster entry or import a dossier before the next session.";
        }

        if (!activeWorkspace.HasSavedWorkspace)
        {
            return "Unsaved workspace changes are still local to this device; save before you trust the next-session return path.";
        }

        if (shellSurface.ActiveRuntime is null)
        {
            return "Workspace restored without a governed runtime pin; inspect runtime before you resume.";
        }

        TimeSpan age = nowUtc - activeWorkspace.LastOpenedUtc;
        if (age >= TimeSpan.FromDays(7))
        {
            return $"Last opened {FormatAge(age)} ago; treat session context as stale until you review recap and rules.";
        }

        if (age >= TimeSpan.FromHours(24))
        {
            return $"Last opened {FormatAge(age)} ago; review recap and open tabs before continuing.";
        }

        return $"Last opened {FormatAge(age)} ago with a governed runtime attached.";
    }

    private static string BuildReturnActionSummary(
        CharacterOverviewState state,
        ShellSurfaceState shellSurface)
    {
        if (state.LatestPortabilityActivity is { } activity
            && !string.IsNullOrWhiteSpace(activity.Receipt.NextSafeAction))
        {
            return activity.Receipt.NextSafeAction;
        }

        WorkflowSurfaceActionBinding? workflowSurface = shellSurface.ActiveWorkflowSurfaceActions.FirstOrDefault();
        if (workflowSurface is not null)
        {
            return $"Resume with '{workflowSurface.Label}' on the active workbench route.";
        }

        WorkspaceSurfaceActionDefinition? workspaceAction = shellSurface.WorkspaceActions.FirstOrDefault();
        if (workspaceAction is not null)
        {
            return $"Resume with '{workspaceAction.Label}' from the active tab.";
        }

        if (!string.IsNullOrWhiteSpace(shellSurface.ActiveTabId))
        {
            return $"Reopen '{shellSurface.ActiveTabId}' and review the workspace before play.";
        }

        return "Reopen the workspace and review profile, rules, and recap before play.";
    }

    private static string FormatAge(TimeSpan age)
    {
        if (age < TimeSpan.FromMinutes(1))
        {
            return "under a minute";
        }

        if (age < TimeSpan.FromHours(1))
        {
            int minutes = Math.Max(1, (int)Math.Floor(age.TotalMinutes));
            return $"{minutes} minute{(minutes == 1 ? string.Empty : "s")}";
        }

        if (age < TimeSpan.FromDays(1))
        {
            int hours = Math.Max(1, (int)Math.Floor(age.TotalHours));
            return $"{hours} hour{(hours == 1 ? string.Empty : "s")}";
        }

        int days = Math.Max(1, (int)Math.Floor(age.TotalDays));
        return $"{days} day{(days == 1 ? string.Empty : "s")}";
    }

    private sealed record ActiveWorkspaceContext(
        CharacterWorkspaceId? ActiveWorkspaceId,
        int OpenWorkspaceCount,
        string ActiveWorkspaceSaveStatus);
}

internal sealed record MainWindowShellFrame(
    MainWindowHeaderState HeaderState,
    MainWindowChromeState ChromeState,
    SectionHostState SectionHostState,
    CommandDialogPaneState CommandDialogPaneState,
    NavigatorPaneState NavigatorPaneState,
    IReadOnlyDictionary<string, WorkspaceSurfaceActionDefinition> WorkspaceActionsById);

internal sealed record MainWindowHeaderState(
    ToolStripState ToolStrip,
    MenuBarState MenuBar);

internal sealed record MainWindowChromeState(
    WorkspaceStripState WorkspaceStrip,
    SummaryHeaderState SummaryHeader,
    StatusStripState StatusStrip);
