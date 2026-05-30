#nullable enable annotations

using System;
using Chummer.Avalonia;
using Chummer.Avalonia.Controls;
using Chummer.Contracts.Content;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Presentation;

[TestClass]
public sealed class AvaloniaCampaignMemoryProjectorTests
{
    [TestMethod]
    public void ProjectCampaignMemory_prefers_portability_receipt_for_consequence_and_return_action()
    {
        DateTimeOffset now = new(2026, 5, 5, 12, 0, 0, TimeSpan.Zero);
        CharacterWorkspaceId workspaceId = new("ws-portable");
        WorkspacePortabilityReceipt receipt = new(
            FormatId: WorkspacePortabilityFormatIds.PortableDossierV1,
            CompatibilityState: WorkspacePortabilityCompatibilityStates.CompatibleWithWarnings,
            ContextSummary: "Context",
            ReceiptSummary: "Portable export is ready.",
            ProvenanceSummary: "Provenance",
            PayloadSha256: "abcdef1234567890",
            NextSafeAction: "Open inspect-only first before merge or replace on the receiving surface.",
            SupportedExchangeModes:
            [
                WorkspacePortabilityExchangeModes.InspectOnly
            ],
            Notes:
            [
                new WorkspacePortabilityNote(
                    Code: "section-coverage",
                    Severity: WorkspacePortabilityNoteSeverities.Warning,
                    Summary: "Portable package is missing contacts; inspect before governed replace.")
            ]);
        CharacterOverviewState state = CharacterOverviewState.Empty with
        {
            LatestPortabilityActivity = new WorkspacePortabilityActivity("Last portable export", receipt)
        };
        OpenWorkspaceState workspace = new(
            workspaceId,
            "Circuit Breaker",
            "CB",
            now.AddHours(-3),
            RulesetDefaults.Sr5,
            HasSavedWorkspace: true);
        ShellSurfaceState shellSurface = CreateShellSurface(workspace, workspaceId, activeRuntime: CreateRuntime());

        CampaignMemoryState projection = MainWindowShellFrameProjector.ProjectCampaignMemory(state, shellSurface, now);

        Assert.AreEqual(
            "Last portable export: Portable package is missing contacts; inspect before governed replace.",
            projection.ConsequenceSummary);
        Assert.AreEqual(
            "Open inspect-only first before merge or replace on the receiving surface.",
            projection.ReturnActionSummary);
        Assert.AreEqual(
            "Last opened 3 hours ago with a governed runtime attached.",
            projection.StaleStateSummary);
    }

    [TestMethod]
    public void ProjectCampaignMemory_marks_unsaved_workspace_as_stale_and_falls_back_to_workflow_route()
    {
        DateTimeOffset now = new(2026, 5, 5, 12, 0, 0, TimeSpan.Zero);
        CharacterWorkspaceId workspaceId = new("ws-stale");
        OpenWorkspaceState workspace = new(
            workspaceId,
            "Nightjar",
            "NJ",
            now.AddDays(-9),
            RulesetDefaults.Sr5,
            HasSavedWorkspace: false);
        ShellSurfaceState shellSurface = CreateShellSurface(
            workspace,
            workspaceId,
            activeRuntime: null,
            notice: "Campaign recap imported.",
            workflowLabel: "Resume Career Workbench");

        CampaignMemoryState projection = MainWindowShellFrameProjector.ProjectCampaignMemory(
            CharacterOverviewState.Empty,
            shellSurface,
            now);

        Assert.AreEqual("Campaign recap imported.", projection.ConsequenceSummary);
        Assert.AreEqual(
            "Unsaved workspace changes are still local to this device; save before you trust the next-session return path.",
            projection.StaleStateSummary);
        Assert.AreEqual(
            "Resume with 'Resume Career Workbench' on the active workbench route.",
            projection.ReturnActionSummary);
    }

    private static ShellSurfaceState CreateShellSurface(
        OpenWorkspaceState workspace,
        CharacterWorkspaceId activeWorkspaceId,
        ActiveRuntimeStatusProjection? activeRuntime,
        string? notice = null,
        string? workflowLabel = null)
    {
        WorkflowSurfaceActionBinding[] workflowSurfaces = string.IsNullOrWhiteSpace(workflowLabel)
            ? []
            :
            [
                new WorkflowSurfaceActionBinding(
                    SurfaceId: "surface.resume",
                    WorkflowId: WorkflowDefinitionIds.CareerWorkbench,
                    Label: workflowLabel,
                    ActionId: "resume-career",
                    RegionId: ShellRegionIds.SectionPane,
                    LayoutToken: WorkflowLayoutTokens.CareerWorkbench)
            ];

        return new ShellSurfaceState(
            Commands: [],
            MenuRoots: [],
            NavigationTabs: [],
            WorkspaceActions: [],
            ActiveWorkflowSurfaceActions: workflowSurfaces,
            OpenWorkspaces: [workspace],
            ActiveRulesetId: workspace.RulesetId,
            PreferredRulesetId: workspace.RulesetId,
            ActiveWorkspaceId: activeWorkspaceId,
            ActiveTabId: "tab-info",
            LastCommandId: null,
            WorkflowDefinitions: [],
            WorkflowSurfaces: [],
            ActiveRuntime: activeRuntime)
        {
            Notice = notice
        };
    }

    private static ActiveRuntimeStatusProjection CreateRuntime()
    {
        return new ActiveRuntimeStatusProjection(
            ProfileId: "runtime.sr5",
            Title: "SR5 Runtime",
            RulesetId: RulesetDefaults.Sr5,
            RuntimeFingerprint: "sha256:sr5-runtime",
            InstallState: "ready",
            InstalledTargetKind: RuntimeInspectorTargetKinds.Workspace,
            InstalledTargetId: "ws-portable",
            RulePackCount: 1,
            ProviderBindingCount: 1,
            WarningCount: 0);
    }
}
