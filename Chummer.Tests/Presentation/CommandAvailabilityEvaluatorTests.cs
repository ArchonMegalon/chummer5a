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
        ICommandAvailabilityEvaluator evaluator = new DefaultCommandAvailabilityEvaluator();
        AppCommandDefinition command = new("save_character", "Save", "file", true, true);

        bool withoutWorkspace = evaluator.IsCommandEnabled(command, CharacterOverviewState.Empty);
        bool withWorkspace = evaluator.IsCommandEnabled(
            command,
            CharacterOverviewState.Empty with { WorkspaceId = new CharacterWorkspaceId("ws-1") });

        Assert.IsFalse(withoutWorkspace);
        Assert.IsTrue(withWorkspace);
    }

    [TestMethod]
    public void IsNavigationTabEnabled_honors_enabled_flag()
    {
        ICommandAvailabilityEvaluator evaluator = new DefaultCommandAvailabilityEvaluator();
        NavigationTabDefinition tab = new("tab-skills", "Skills", "skills", "character", true, false);

        bool enabled = evaluator.IsNavigationTabEnabled(
            tab,
            CharacterOverviewState.Empty with { WorkspaceId = new CharacterWorkspaceId("ws-1") });

        Assert.IsFalse(enabled);
    }

    [TestMethod]
    public void IsWorkspaceActionEnabled_requires_open_workspace_when_flagged()
    {
        ICommandAvailabilityEvaluator evaluator = new DefaultCommandAvailabilityEvaluator();
        WorkspaceSurfaceActionDefinition action = new(
            Id: "tab-info.summary",
            Label: "Summary",
            TabId: "tab-info",
            Kind: WorkspaceSurfaceActionKind.Summary,
            TargetId: "summary",
            RequiresOpenCharacter: true,
            EnabledByDefault: true);

        bool withoutWorkspace = evaluator.IsWorkspaceActionEnabled(action, CharacterOverviewState.Empty);
        bool withWorkspace = evaluator.IsWorkspaceActionEnabled(
            action,
            CharacterOverviewState.Empty with { WorkspaceId = new CharacterWorkspaceId("ws-1") });

        Assert.IsFalse(withoutWorkspace);
        Assert.IsTrue(withWorkspace);
    }

    [TestMethod]
    public void IsUiControlEnabled_requires_open_workspace_when_flagged()
    {
        ICommandAvailabilityEvaluator evaluator = new DefaultCommandAvailabilityEvaluator();
        DesktopUiControlDefinition control = new("gear_add", "Add Gear", "tab-gear", true, true);

        bool withoutWorkspace = evaluator.IsUiControlEnabled(control, CharacterOverviewState.Empty);
        bool withWorkspace = evaluator.IsUiControlEnabled(
            control,
            CharacterOverviewState.Empty with { WorkspaceId = new CharacterWorkspaceId("ws-1") });

        Assert.IsFalse(withoutWorkspace);
        Assert.IsTrue(withWorkspace);
    }
}
