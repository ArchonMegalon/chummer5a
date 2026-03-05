#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Linq;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Presentation;

[TestClass]
public sealed class ShellSurfaceResolverTests
{
    [TestMethod]
    public void Resolve_projects_shell_catalog_collections_without_reinterpretation()
    {
        var fileMenu = new AppCommandDefinition("file", "menu.file", "menu", false, true, "sr6");
        var saveCommand = new AppCommandDefinition("save_character", "command.save", "file", true, true, "sr6");
        var profileTab = new NavigationTabDefinition("tab-info", "Info", "profile", "character", true, true, "sr6");
        var shellWorkspaceId = new Chummer.Contracts.Workspaces.CharacterWorkspaceId("ws-shell");
        var shellState = ShellState.Empty with
        {
            ActiveRulesetId = "sr6",
            PreferredRulesetId = RulesetDefaults.Sr5,
            ActiveWorkspaceId = shellWorkspaceId,
            OpenWorkspaces =
            [
                new ShellWorkspaceState(
                    Id: shellWorkspaceId,
                    Name: "Shell Runner",
                    Alias: "SHL",
                    LastOpenedUtc: DateTimeOffset.UtcNow,
                    RulesetId: "sr6")
            ],
            Commands = [fileMenu, saveCommand],
            MenuRoots = [fileMenu],
            NavigationTabs = [profileTab],
            ActiveTabId = profileTab.Id,
            LastCommandId = saveCommand.Id
        };

        var workspaceAction = new WorkspaceSurfaceActionDefinition(
            Id: "action.profile",
            Label: "Profile",
            TabId: profileTab.Id,
            Kind: WorkspaceSurfaceActionKind.Section,
            TargetId: "profile",
            RequiresOpenCharacter: false,
            EnabledByDefault: true,
            RulesetId: "sr6");
        var uiControl = new DesktopUiControlDefinition(
            Id: "ui.refresh",
            Label: "Refresh",
            TabId: profileTab.Id,
            RequiresOpenCharacter: false,
            EnabledByDefault: true,
            RulesetId: "sr6");
        var catalogResolver = new StubShellCatalogResolver([workspaceAction], [uiControl]);
        var availability = new StubAvailabilityEvaluator(
            commandEnabled: true,
            tabEnabled: true,
            actionEnabled: true,
            controlEnabled: true);
        var resolver = new ShellSurfaceResolver(catalogResolver, availability);

        CharacterOverviewState overviewState = CharacterOverviewState.Empty with
        {
            Session = new WorkspaceSessionState(
                ActiveWorkspaceId: new Chummer.Contracts.Workspaces.CharacterWorkspaceId("ws-overview"),
                OpenWorkspaces:
                [
                    new OpenWorkspaceState(
                        Id: shellWorkspaceId,
                        Name: "Shell Runner",
                        Alias: "SHL",
                        LastOpenedUtc: DateTimeOffset.UtcNow,
                        RulesetId: "sr6",
                        HasSavedWorkspace: true),
                    new OpenWorkspaceState(
                        Id: new Chummer.Contracts.Workspaces.CharacterWorkspaceId("ws-overview"),
                        Name: "Overview Runner",
                        Alias: "OVR",
                        LastOpenedUtc: DateTimeOffset.UtcNow,
                        RulesetId: "sr6")
                ],
                RecentWorkspaceIds: []),
            OpenWorkspaces =
            [
                new OpenWorkspaceState(
                    Id: shellWorkspaceId,
                    Name: "Shell Runner",
                    Alias: "SHL",
                    LastOpenedUtc: DateTimeOffset.UtcNow,
                    RulesetId: "sr6",
                    HasSavedWorkspace: true),
                new OpenWorkspaceState(
                    Id: new Chummer.Contracts.Workspaces.CharacterWorkspaceId("ws-overview"),
                    Name: "Overview Runner",
                    Alias: "OVR",
                    LastOpenedUtc: DateTimeOffset.UtcNow,
                    RulesetId: "sr6")
            ],
            ActiveTabId = "tab-overview",
            WorkspaceId = new Chummer.Contracts.Workspaces.CharacterWorkspaceId("ws-overview")
        };

        ShellSurfaceState surface = resolver.Resolve(overviewState, shellState);

        CollectionAssert.AreEqual(shellState.Commands.ToArray(), surface.Commands.ToArray());
        CollectionAssert.AreEqual(shellState.MenuRoots.ToArray(), surface.MenuRoots.ToArray());
        CollectionAssert.AreEqual(shellState.NavigationTabs.ToArray(), surface.NavigationTabs.ToArray());
        Assert.AreEqual("sr6", surface.ActiveRulesetId);
        Assert.AreEqual(profileTab.Id, surface.ActiveTabId);
        Assert.AreEqual(saveCommand.Id, surface.LastCommandId);
        Assert.AreEqual("sr5", surface.PreferredRulesetId);
        Assert.AreEqual("ws-shell", surface.ActiveWorkspaceId?.Value);
        Assert.HasCount(1, surface.OpenWorkspaces);
        Assert.AreEqual("ws-shell", surface.OpenWorkspaces[0].Id.Value);
        Assert.IsTrue(surface.OpenWorkspaces[0].HasSavedWorkspace);
        Assert.AreEqual(profileTab.Id, catalogResolver.LastWorkspaceActionTabId);
        Assert.AreEqual("sr6", catalogResolver.LastWorkspaceActionRulesetId);
        Assert.AreEqual(profileTab.Id, catalogResolver.LastUiControlTabId);
        Assert.AreEqual("sr6", catalogResolver.LastUiControlRulesetId);
        Assert.HasCount(1, surface.WorkspaceActions);
        Assert.HasCount(1, surface.DesktopUiControls);
    }

    [TestMethod]
    public void Resolve_does_not_rehydrate_shell_session_facts_from_overview_state_when_shell_state_is_empty()
    {
        var resolver = new ShellSurfaceResolver(
            new StubShellCatalogResolver([], []),
            new StubAvailabilityEvaluator(
                commandEnabled: true,
                tabEnabled: true,
                actionEnabled: true,
                controlEnabled: true));

        CharacterOverviewState overviewState = CharacterOverviewState.Empty with
        {
            Session = new WorkspaceSessionState(
                ActiveWorkspaceId: new Chummer.Contracts.Workspaces.CharacterWorkspaceId("ws-overview"),
                OpenWorkspaces:
                [
                    new OpenWorkspaceState(
                        Id: new Chummer.Contracts.Workspaces.CharacterWorkspaceId("ws-overview"),
                        Name: "Overview Runner",
                        Alias: "OVR",
                        LastOpenedUtc: DateTimeOffset.UtcNow,
                        RulesetId: "sr6",
                        HasSavedWorkspace: true)
                ],
                RecentWorkspaceIds: []),
            WorkspaceId = new Chummer.Contracts.Workspaces.CharacterWorkspaceId("ws-overview"),
            ActiveTabId = "tab-overview"
        };

        ShellSurfaceState surface = resolver.Resolve(overviewState, ShellState.Empty);

        Assert.IsNull(surface.ActiveWorkspaceId);
        Assert.IsNull(surface.ActiveTabId);
        Assert.IsEmpty(surface.OpenWorkspaces);
    }

    [TestMethod]
    public void Resolve_uses_preferred_ruleset_when_active_is_missing_and_filters_surface_entries()
    {
        var profileTab = new NavigationTabDefinition("tab-info", "Info", "profile", "character", true, true, "sr6");
        var shellState = ShellState.Empty with
        {
            ActiveRulesetId = string.Empty,
            PreferredRulesetId = "sr6",
            NavigationTabs = [profileTab],
            ActiveTabId = profileTab.Id,
            OpenMenuId = "file",
            Notice = "surface-notice",
            Error = "surface-error"
        };

        var allowedAction = new WorkspaceSurfaceActionDefinition(
            Id: "action.allowed",
            Label: "Allowed",
            TabId: profileTab.Id,
            Kind: WorkspaceSurfaceActionKind.Section,
            TargetId: "profile",
            RequiresOpenCharacter: false,
            EnabledByDefault: true,
            RulesetId: "sr6");
        var blockedAction = allowedAction with { Id = "action.blocked", Label = "Blocked" };
        var allowedControl = new DesktopUiControlDefinition(
            Id: "ui.allowed",
            Label: "Allowed",
            TabId: profileTab.Id,
            RequiresOpenCharacter: false,
            EnabledByDefault: true,
            RulesetId: "sr6");
        var blockedControl = allowedControl with { Id = "ui.blocked", Label = "Blocked" };
        var catalogResolver = new StubShellCatalogResolver(
            [allowedAction, blockedAction],
            [allowedControl, blockedControl]);
        var availability = new StubAvailabilityEvaluator(
            commandEnabled: true,
            tabEnabled: true,
            actionEnabled: true,
            controlEnabled: true,
            blockedActionIds: ["action.blocked"],
            blockedControlIds: ["ui.blocked"]);
        var resolver = new ShellSurfaceResolver(catalogResolver, availability);

        CharacterOverviewState overviewState = CharacterOverviewState.Empty with
        {
            LastCommandId = "overview-command"
        };
        ShellSurfaceState surface = resolver.Resolve(overviewState, shellState);

        Assert.AreEqual("sr6", surface.ActiveRulesetId);
        Assert.AreEqual("sr6", surface.PreferredRulesetId);
        Assert.AreEqual(profileTab.Id, surface.ActiveTabId);
        Assert.AreEqual("file", surface.OpenMenuId);
        Assert.AreEqual("surface-notice", surface.Notice);
        Assert.AreEqual("surface-error", surface.Error);
        Assert.AreEqual("overview-command", surface.LastCommandId);
        Assert.AreEqual(profileTab.Id, catalogResolver.LastWorkspaceActionTabId);
        Assert.AreEqual("sr6", catalogResolver.LastWorkspaceActionRulesetId);
        Assert.AreEqual(profileTab.Id, catalogResolver.LastUiControlTabId);
        Assert.AreEqual("sr6", catalogResolver.LastUiControlRulesetId);
        Assert.HasCount(1, surface.WorkspaceActions);
        Assert.AreEqual("action.allowed", surface.WorkspaceActions[0].Id);
        Assert.HasCount(1, surface.DesktopUiControls);
        Assert.AreEqual("ui.allowed", surface.DesktopUiControls[0].Id);
    }

    private sealed class StubShellCatalogResolver : IRulesetShellCatalogResolver
    {
        private readonly IReadOnlyList<WorkspaceSurfaceActionDefinition> _workspaceActions;
        private readonly IReadOnlyList<DesktopUiControlDefinition> _uiControls;

        public StubShellCatalogResolver(
            IReadOnlyList<WorkspaceSurfaceActionDefinition> workspaceActions,
            IReadOnlyList<DesktopUiControlDefinition> uiControls)
        {
            _workspaceActions = workspaceActions;
            _uiControls = uiControls;
        }

        public string? LastWorkspaceActionTabId { get; private set; }
        public string? LastWorkspaceActionRulesetId { get; private set; }
        public string? LastUiControlTabId { get; private set; }
        public string? LastUiControlRulesetId { get; private set; }

        public IReadOnlyList<AppCommandDefinition> ResolveCommands(string? rulesetId) => [];

        public IReadOnlyList<NavigationTabDefinition> ResolveNavigationTabs(string? rulesetId) => [];

        public IReadOnlyList<WorkspaceSurfaceActionDefinition> ResolveWorkspaceActionsForTab(string? tabId, string? rulesetId)
        {
            LastWorkspaceActionTabId = tabId;
            LastWorkspaceActionRulesetId = rulesetId;
            return _workspaceActions;
        }

        public IReadOnlyList<DesktopUiControlDefinition> ResolveDesktopUiControlsForTab(string? tabId, string? rulesetId)
        {
            LastUiControlTabId = tabId;
            LastUiControlRulesetId = rulesetId;
            return _uiControls;
        }
    }

    private sealed class StubAvailabilityEvaluator : ICommandAvailabilityEvaluator
    {
        private readonly bool _commandEnabled;
        private readonly bool _tabEnabled;
        private readonly bool _actionEnabled;
        private readonly bool _controlEnabled;
        private readonly HashSet<string> _blockedActionIds;
        private readonly HashSet<string> _blockedControlIds;

        public StubAvailabilityEvaluator(
            bool commandEnabled,
            bool tabEnabled,
            bool actionEnabled,
            bool controlEnabled,
            IReadOnlyList<string>? blockedActionIds = null,
            IReadOnlyList<string>? blockedControlIds = null)
        {
            _commandEnabled = commandEnabled;
            _tabEnabled = tabEnabled;
            _actionEnabled = actionEnabled;
            _controlEnabled = controlEnabled;
            _blockedActionIds = (blockedActionIds ?? [])
                .ToHashSet(StringComparer.Ordinal);
            _blockedControlIds = (blockedControlIds ?? [])
                .ToHashSet(StringComparer.Ordinal);
        }

        public bool IsCommandEnabled(AppCommandDefinition command, CharacterOverviewState state) => _commandEnabled;

        public bool IsNavigationTabEnabled(NavigationTabDefinition tab, CharacterOverviewState state) => _tabEnabled;

        public bool IsWorkspaceActionEnabled(WorkspaceSurfaceActionDefinition action, CharacterOverviewState state)
        {
            return _actionEnabled && !_blockedActionIds.Contains(action.Id);
        }

        public bool IsUiControlEnabled(DesktopUiControlDefinition control, CharacterOverviewState state)
        {
            return _controlEnabled && !_blockedControlIds.Contains(control.Id);
        }
    }
}
