#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation;
using Chummer.Presentation.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Presentation;

[TestClass]
public class ShellPresenterTests
{
    [TestMethod]
    public async Task InitializeAsync_loads_shell_contract_and_restores_workspaces()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var client = new ShellClientStub
        {
            Workspaces =
            [
                CreateWorkspace("ws-old", "Old Character", "OLD", now.AddMinutes(-25)),
                CreateWorkspace("ws-new", "New Character", "NEW", now.AddMinutes(-5))
            ]
        };
        var presenter = new ShellPresenter(client);

        await presenter.InitializeAsync(CancellationToken.None);

        Assert.IsFalse(presenter.State.IsBusy);
        Assert.IsNull(presenter.State.Error);
        Assert.AreEqual(2, presenter.State.OpenWorkspaces.Count);
        Assert.AreEqual("ws-new", presenter.State.ActiveWorkspaceId?.Value);
        Assert.AreEqual("ws-new", presenter.State.OpenWorkspaces[0].Id.Value);
        Assert.AreEqual("file", presenter.State.MenuRoots[0].Id);
        Assert.AreEqual("tab-info", presenter.State.ActiveTabId);
        StringAssert.Contains(presenter.State.Notice ?? string.Empty, "Restored 2 workspace(s).");
    }

    [TestMethod]
    public async Task InitializeAsync_uses_active_workspace_ruleset_for_shell_contract()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var client = new ShellClientStub
        {
            Workspaces =
            [
                CreateWorkspace("ws-sr5", "SR5 Character", "SR5", now.AddMinutes(-25), RulesetDefaults.Sr5),
                CreateWorkspace("ws-sr6", "SR6 Character", "SR6", now.AddMinutes(-5), "sr6")
            ]
        };
        var presenter = new ShellPresenter(client);

        await presenter.InitializeAsync(CancellationToken.None);

        Assert.AreEqual("sr6", presenter.State.ActiveRulesetId);
        CollectionAssert.Contains(client.RequestedCommandRulesets, "sr6");
        CollectionAssert.Contains(client.RequestedNavigationRulesets, "sr6");
    }

    [TestMethod]
    public async Task InitializeAsync_requests_catalogs_only_for_active_ruleset()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var client = new ShellClientStub
        {
            Workspaces =
            [
                CreateWorkspace("ws-sr5", "SR5 Character", "SR5", now.AddMinutes(-25), RulesetDefaults.Sr5),
                CreateWorkspace("ws-sr6", "SR6 Character", "SR6", now.AddMinutes(-5), "sr6")
            ]
        };
        var presenter = new ShellPresenter(client);

        await presenter.InitializeAsync(CancellationToken.None);

        string?[] expectedSr6Rulesets = ["sr6"];
        CollectionAssert.AreEqual(expectedSr6Rulesets, client.RequestedCommandRulesets);
        CollectionAssert.AreEqual(expectedSr6Rulesets, client.RequestedNavigationRulesets);
    }

    [TestMethod]
    public async Task SyncWorkspaceContextAsync_switches_ruleset_when_active_workspace_changes()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var client = new ShellClientStub
        {
            Workspaces =
            [
                CreateWorkspace("ws-sr5", "SR5 Character", "SR5", now.AddMinutes(-5), RulesetDefaults.Sr5),
                CreateWorkspace("ws-sr6", "SR6 Character", "SR6", now.AddMinutes(-25), "sr6")
            ]
        };
        var presenter = new ShellPresenter(client);

        await presenter.InitializeAsync(CancellationToken.None);

        client.Workspaces =
        [
            CreateWorkspace("ws-sr6", "SR6 Character", "SR6", now.AddMinutes(-1), "sr6"),
            CreateWorkspace("ws-sr5", "SR5 Character", "SR5", now.AddMinutes(-20), RulesetDefaults.Sr5)
        ];
        await presenter.SyncWorkspaceContextAsync(new CharacterWorkspaceId("ws-sr6"), CancellationToken.None);

        Assert.AreEqual("ws-sr6", presenter.State.ActiveWorkspaceId?.Value);
        Assert.AreEqual("sr6", presenter.State.ActiveRulesetId);
        CollectionAssert.Contains(client.RequestedCommandRulesets, "sr6");
        CollectionAssert.Contains(client.RequestedNavigationRulesets, "sr6");
    }

    [TestMethod]
    public async Task ToggleMenuAsync_toggles_open_and_closed_state()
    {
        var presenter = new ShellPresenter(new ShellClientStub());
        await presenter.InitializeAsync(CancellationToken.None);

        await presenter.ToggleMenuAsync("file", CancellationToken.None);
        Assert.AreEqual("file", presenter.State.OpenMenuId);

        await presenter.ToggleMenuAsync("file", CancellationToken.None);
        Assert.IsNull(presenter.State.OpenMenuId);
    }

    [TestMethod]
    public async Task SelectTabAsync_rejects_disabled_tabs()
    {
        var client = new ShellClientStub
        {
            NavigationTabs =
            [
                new NavigationTabDefinition("tab-enabled", "Enabled", "profile", "character", true, true),
                new NavigationTabDefinition("tab-disabled", "Disabled", "profile", "character", true, false)
            ]
        };
        var presenter = new ShellPresenter(client);
        await presenter.InitializeAsync(CancellationToken.None);

        await presenter.SelectTabAsync("tab-disabled", CancellationToken.None);

        Assert.AreEqual("Tab 'tab-disabled' is disabled.", presenter.State.Error);
        Assert.AreEqual("tab-enabled", presenter.State.ActiveTabId);
    }

    [TestMethod]
    public async Task ExecuteCommandAsync_requires_workspace_for_workspace_scoped_commands()
    {
        var presenter = new ShellPresenter(new ShellClientStub());
        await presenter.InitializeAsync(CancellationToken.None);

        await presenter.ExecuteCommandAsync("save_character", CancellationToken.None);

        Assert.AreEqual("Command 'save_character' is disabled in the current shell state.", presenter.State.Error);
    }

    [TestMethod]
    public async Task ExecuteCommandAsync_menu_command_updates_open_menu_and_last_command()
    {
        var presenter = new ShellPresenter(new ShellClientStub());
        await presenter.InitializeAsync(CancellationToken.None);

        await presenter.ExecuteCommandAsync("file", CancellationToken.None);

        Assert.AreEqual("file", presenter.State.OpenMenuId);
        Assert.AreEqual("file", presenter.State.LastCommandId);
        Assert.IsNull(presenter.State.Error);
    }

    [TestMethod]
    public async Task SetPreferredRulesetAsync_updates_active_ruleset_when_no_workspace_is_open()
    {
        var client = new ShellClientStub
        {
            Workspaces = Array.Empty<WorkspaceListItem>()
        };
        var presenter = new ShellPresenter(client);
        await presenter.InitializeAsync(CancellationToken.None);

        await presenter.SetPreferredRulesetAsync("sr6", CancellationToken.None);

        Assert.AreEqual("sr6", presenter.State.PreferredRulesetId);
        Assert.AreEqual("sr6", presenter.State.ActiveRulesetId);
        CollectionAssert.Contains(client.RequestedBootstrapRulesets, "sr6");
        CollectionAssert.Contains(client.RequestedCommandRulesets, "sr6");
        CollectionAssert.Contains(client.RequestedNavigationRulesets, "sr6");
        Assert.AreEqual("sr6", client.Preferences.PreferredRulesetId);
    }

    [TestMethod]
    public async Task SetPreferredRulesetAsync_does_not_override_active_workspace_ruleset()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var client = new ShellClientStub
        {
            Workspaces =
            [
                CreateWorkspace("ws-sr5", "SR5 Character", "SR5", now, RulesetDefaults.Sr5)
            ]
        };
        var presenter = new ShellPresenter(client);
        await presenter.InitializeAsync(CancellationToken.None);

        await presenter.SetPreferredRulesetAsync("sr6", CancellationToken.None);

        Assert.AreEqual("sr6", presenter.State.PreferredRulesetId);
        Assert.AreEqual("sr5", presenter.State.ActiveRulesetId);
        string[] expectedSr5BootstrapRulesets = ["sr5"];
        string?[] expectedSr5Rulesets = ["sr5"];
        CollectionAssert.AreEqual(expectedSr5BootstrapRulesets, client.RequestedBootstrapRulesets);
        CollectionAssert.AreEqual(expectedSr5Rulesets, client.RequestedCommandRulesets);
        CollectionAssert.AreEqual(expectedSr5Rulesets, client.RequestedNavigationRulesets);
        Assert.AreEqual("sr6", client.Preferences.PreferredRulesetId);
    }

    [TestMethod]
    public async Task InitializeAsync_uses_saved_preferred_ruleset_when_no_workspace_is_open()
    {
        var client = new ShellClientStub
        {
            Workspaces = Array.Empty<WorkspaceListItem>(),
            Preferences = new ShellUserPreferences("sr6")
        };
        var presenter = new ShellPresenter(client);

        await presenter.InitializeAsync(CancellationToken.None);

        Assert.AreEqual("sr6", presenter.State.PreferredRulesetId);
        Assert.AreEqual("sr6", presenter.State.ActiveRulesetId);
        string[] expectedSr6BootstrapRulesets = ["sr6"];
        CollectionAssert.AreEqual(expectedSr6BootstrapRulesets, client.RequestedBootstrapRulesets);
    }

    [TestMethod]
    public async Task SetPreferredRulesetAsync_persists_preference_via_runtime_client()
    {
        var client = new ShellClientStub
        {
            Workspaces = Array.Empty<WorkspaceListItem>()
        };
        var presenter = new ShellPresenter(client);
        await presenter.InitializeAsync(CancellationToken.None);

        await presenter.SetPreferredRulesetAsync("sr6", CancellationToken.None);

        Assert.AreEqual(1, client.SavedPreferences.Count);
        Assert.AreEqual("sr6", client.SavedPreferences[0].PreferredRulesetId);
    }

    private static WorkspaceListItem CreateWorkspace(
        string id,
        string name,
        string alias,
        DateTimeOffset lastUpdatedUtc,
        string rulesetId = RulesetDefaults.Sr5)
    {
        return new WorkspaceListItem(
            Id: new CharacterWorkspaceId(id),
            Summary: new CharacterFileSummary(
                Name: name,
                Alias: alias,
                Metatype: "Human",
                BuildMethod: "Priority",
                CreatedVersion: "5",
                AppVersion: "5",
                Karma: 0m,
                Nuyen: 0m,
                Created: true),
            LastUpdatedUtc: lastUpdatedUtc,
            RulesetId: rulesetId);
    }

    private sealed class ShellClientStub : IChummerClient
    {
        public IReadOnlyList<AppCommandDefinition> Commands { get; set; } = AppCommandCatalog.All;

        public IReadOnlyList<NavigationTabDefinition> NavigationTabs { get; set; } = NavigationTabCatalog.All;

        public IReadOnlyList<WorkspaceListItem> Workspaces { get; set; } = Array.Empty<WorkspaceListItem>();

        public ShellUserPreferences Preferences { get; set; } = ShellUserPreferences.Default;

        public List<ShellUserPreferences> SavedPreferences { get; } = new();

        public List<string?> RequestedCommandRulesets { get; } = new();

        public List<string?> RequestedNavigationRulesets { get; } = new();

        public List<string?> RequestedBootstrapRulesets { get; } = new();

        public Task<IReadOnlyList<AppCommandDefinition>> GetCommandsAsync(string? rulesetId, CancellationToken ct)
        {
            RequestedCommandRulesets.Add(rulesetId);
            return Task.FromResult(Commands);
        }

        public Task<IReadOnlyList<NavigationTabDefinition>> GetNavigationTabsAsync(string? rulesetId, CancellationToken ct)
        {
            RequestedNavigationRulesets.Add(rulesetId);
            return Task.FromResult(NavigationTabs);
        }

        public Task<IReadOnlyList<WorkspaceListItem>> ListWorkspacesAsync(CancellationToken ct) => Task.FromResult(Workspaces);

        public Task<ShellUserPreferences> GetShellPreferencesAsync(CancellationToken ct)
            => Task.FromResult(Preferences);

        public Task SaveShellPreferencesAsync(ShellUserPreferences preferences, CancellationToken ct)
        {
            Preferences = new ShellUserPreferences(RulesetDefaults.Normalize(preferences.PreferredRulesetId));
            SavedPreferences.Add(Preferences);
            return Task.CompletedTask;
        }

        public async Task<ShellBootstrapSnapshot> GetShellBootstrapAsync(string? rulesetId, CancellationToken ct)
        {
            string effectiveRulesetId = string.IsNullOrWhiteSpace(rulesetId)
                ? RulesetDefaults.Normalize(
                    Workspaces
                        .OrderByDescending(workspace => workspace.LastUpdatedUtc)
                        .FirstOrDefault()
                        ?.RulesetId
                    ?? Preferences.PreferredRulesetId)
                : RulesetDefaults.Normalize(rulesetId);
            RequestedBootstrapRulesets.Add(effectiveRulesetId);
            IReadOnlyList<AppCommandDefinition> commands = await GetCommandsAsync(effectiveRulesetId, ct);
            IReadOnlyList<NavigationTabDefinition> tabs = await GetNavigationTabsAsync(effectiveRulesetId, ct);
            IReadOnlyList<WorkspaceListItem> workspaces = await ListWorkspacesAsync(ct);
            string preferredRulesetId = RulesetDefaults.Normalize(Preferences.PreferredRulesetId);
            string activeRulesetId = RulesetDefaults.Normalize(
                workspaces
                    .OrderByDescending(workspace => workspace.LastUpdatedUtc)
                    .FirstOrDefault()
                    ?.RulesetId
                ?? preferredRulesetId);
            return new ShellBootstrapSnapshot(
                RulesetId: effectiveRulesetId,
                Commands: commands,
                NavigationTabs: tabs,
                Workspaces: workspaces,
                PreferredRulesetId: preferredRulesetId,
                ActiveRulesetId: activeRulesetId);
        }

        public Task<WorkspaceImportResult> ImportAsync(WorkspaceImportDocument document, CancellationToken ct) => throw new NotImplementedException();

        public Task<bool> CloseWorkspaceAsync(CharacterWorkspaceId id, CancellationToken ct) => throw new NotImplementedException();

        public Task<JsonNode> GetSectionAsync(CharacterWorkspaceId id, string sectionId, CancellationToken ct) => throw new NotImplementedException();

        public Task<CharacterFileSummary> GetSummaryAsync(CharacterWorkspaceId id, CancellationToken ct) => throw new NotImplementedException();

        public Task<CharacterValidationResult> ValidateAsync(CharacterWorkspaceId id, CancellationToken ct) => throw new NotImplementedException();

        public Task<CharacterProfileSection> GetProfileAsync(CharacterWorkspaceId id, CancellationToken ct) => throw new NotImplementedException();

        public Task<CharacterProgressSection> GetProgressAsync(CharacterWorkspaceId id, CancellationToken ct) => throw new NotImplementedException();

        public Task<CharacterSkillsSection> GetSkillsAsync(CharacterWorkspaceId id, CancellationToken ct) => throw new NotImplementedException();

        public Task<CharacterRulesSection> GetRulesAsync(CharacterWorkspaceId id, CancellationToken ct) => throw new NotImplementedException();

        public Task<CharacterBuildSection> GetBuildAsync(CharacterWorkspaceId id, CancellationToken ct) => throw new NotImplementedException();

        public Task<CharacterMovementSection> GetMovementAsync(CharacterWorkspaceId id, CancellationToken ct) => throw new NotImplementedException();

        public Task<CharacterAwakeningSection> GetAwakeningAsync(CharacterWorkspaceId id, CancellationToken ct) => throw new NotImplementedException();

        public Task<CommandResult<CharacterProfileSection>> UpdateMetadataAsync(CharacterWorkspaceId id, UpdateWorkspaceMetadata command, CancellationToken ct) => throw new NotImplementedException();

        public Task<CommandResult<WorkspaceSaveReceipt>> SaveAsync(CharacterWorkspaceId id, CancellationToken ct) => throw new NotImplementedException();

        public Task<CommandResult<WorkspaceDownloadReceipt>> DownloadAsync(CharacterWorkspaceId id, CancellationToken ct) => throw new NotImplementedException();
    }
}
