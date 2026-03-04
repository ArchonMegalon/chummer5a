using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Presentation;

[TestClass]
public class CommandAvailabilityEvaluatorTests
{
    [TestMethod]
    public void IsCommandEnabled_requires_open_workspace_when_flagged()
    {
        AppCommandDefinition command = new("save_character", "Save", "file", true, true);

        bool withoutWorkspace = CommandAvailabilityEvaluator.IsCommandEnabled(command, CharacterOverviewState.Empty);
        bool withWorkspace = CommandAvailabilityEvaluator.IsCommandEnabled(
            command,
            CharacterOverviewState.Empty with { WorkspaceId = new CharacterWorkspaceId("ws-1") });

        Assert.IsFalse(withoutWorkspace);
        Assert.IsTrue(withWorkspace);
    }

    [TestMethod]
    public void IsNavigationTabEnabled_honors_enabled_flag()
    {
        NavigationTabDefinition tab = new("tab-skills", "Skills", "skills", "character", true, false);

        bool enabled = CommandAvailabilityEvaluator.IsNavigationTabEnabled(
            tab,
            CharacterOverviewState.Empty with { WorkspaceId = new CharacterWorkspaceId("ws-1") });

        Assert.IsFalse(enabled);
    }

    [TestMethod]
    public void IsWorkspaceActionEnabled_requires_open_workspace_when_flagged()
    {
        WorkspaceSurfaceActionDefinition action = new(
            Id: "tab-info.summary",
            Label: "Summary",
            TabId: "tab-info",
            Kind: WorkspaceSurfaceActionKind.Summary,
            TargetId: "summary",
            RequiresOpenCharacter: true,
            EnabledByDefault: true);

        bool withoutWorkspace = CommandAvailabilityEvaluator.IsWorkspaceActionEnabled(action, CharacterOverviewState.Empty);
        bool withWorkspace = CommandAvailabilityEvaluator.IsWorkspaceActionEnabled(
            action,
            CharacterOverviewState.Empty with { WorkspaceId = new CharacterWorkspaceId("ws-1") });

        Assert.IsFalse(withoutWorkspace);
        Assert.IsTrue(withWorkspace);
    }

    [TestMethod]
    public void IsUiControlEnabled_requires_open_workspace_when_flagged()
    {
        DesktopUiControlDefinition control = new("gear_add", "Add Gear", "tab-gear", true, true);

        bool withoutWorkspace = CommandAvailabilityEvaluator.IsUiControlEnabled(control, CharacterOverviewState.Empty);
        bool withWorkspace = CommandAvailabilityEvaluator.IsUiControlEnabled(
            control,
            CharacterOverviewState.Empty with { WorkspaceId = new CharacterWorkspaceId("ws-1") });

        Assert.IsFalse(withoutWorkspace);
        Assert.IsTrue(withWorkspace);
    }
}
