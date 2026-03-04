using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Presentation;
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

    private static WorkspaceListItem CreateWorkspace(
        string id,
        string name,
        string alias,
        DateTimeOffset lastUpdatedUtc)
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
            LastUpdatedUtc: lastUpdatedUtc);
    }

    private sealed class ShellClientStub : IChummerClient
    {
        public IReadOnlyList<AppCommandDefinition> Commands { get; set; } = AppCommandCatalog.All;

        public IReadOnlyList<NavigationTabDefinition> NavigationTabs { get; set; } = NavigationTabCatalog.All;

        public IReadOnlyList<WorkspaceListItem> Workspaces { get; set; } = Array.Empty<WorkspaceListItem>();

        public Task<IReadOnlyList<AppCommandDefinition>> GetCommandsAsync(CancellationToken ct) => Task.FromResult(Commands);

        public Task<IReadOnlyList<NavigationTabDefinition>> GetNavigationTabsAsync(CancellationToken ct) => Task.FromResult(NavigationTabs);

        public Task<IReadOnlyList<WorkspaceListItem>> ListWorkspacesAsync(CancellationToken ct) => Task.FromResult(Workspaces);

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
