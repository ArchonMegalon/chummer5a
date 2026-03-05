using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Presentation;

[TestClass]
public class ShellBootstrapDataProviderTests
{
    [TestMethod]
    public async Task GetAsync_reuses_cached_payload_within_bootstrap_window()
    {
        var client = new BootstrapClientStub();
        var provider = new ShellBootstrapDataProvider(client);

        await provider.GetAsync(CancellationToken.None);
        await provider.GetAsync(CancellationToken.None);

        Assert.AreEqual(1, client.GetCommandsCalls);
        Assert.AreEqual(1, client.GetNavigationTabsCalls);
        Assert.AreEqual(1, client.ListWorkspacesCalls);
    }

    [TestMethod]
    public async Task GetWorkspacesAsync_caches_workspace_payload_without_catalog_requests()
    {
        var client = new BootstrapClientStub();
        var provider = new ShellBootstrapDataProvider(client);

        await provider.GetWorkspacesAsync(CancellationToken.None);
        await provider.GetWorkspacesAsync(CancellationToken.None);

        Assert.AreEqual(0, client.GetCommandsCalls);
        Assert.AreEqual(0, client.GetNavigationTabsCalls);
        Assert.AreEqual(1, client.ListWorkspacesCalls);
    }

    [TestMethod]
    public async Task Shared_provider_avoids_duplicate_startup_fetches_between_shell_and_overview()
    {
        var client = new BootstrapClientStub();
        var provider = new ShellBootstrapDataProvider(client);
        var shellPresenter = new ShellPresenter(client, provider);
        var overviewPresenter = new CharacterOverviewPresenter(client, bootstrapDataProvider: provider);

        await shellPresenter.InitializeAsync(CancellationToken.None);
        await overviewPresenter.InitializeAsync(CancellationToken.None);

        Assert.AreEqual(1, client.GetCommandsCalls);
        Assert.AreEqual(1, client.GetNavigationTabsCalls);
        Assert.AreEqual(1, client.ListWorkspacesCalls);
    }

    [TestMethod]
    public async Task GetAsync_caches_workspaces_across_rulesets_and_scopes_catalog_requests()
    {
        var client = new BootstrapClientStub();
        var provider = new ShellBootstrapDataProvider(client);

        await provider.GetAsync("sr5", CancellationToken.None);
        await provider.GetAsync("sr6", CancellationToken.None);
        await provider.GetAsync("sr6", CancellationToken.None);

        Assert.AreEqual(2, client.GetCommandsCalls);
        Assert.AreEqual(2, client.GetNavigationTabsCalls);
        Assert.AreEqual(1, client.ListWorkspacesCalls);

        CollectionAssert.AreEquivalent(
            new[] { "sr5", "sr6" },
            client.CommandRulesets
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray());
    }

    private sealed class BootstrapClientStub : IChummerClient
    {
        public int GetCommandsCalls { get; private set; }
        public int GetNavigationTabsCalls { get; private set; }
        public int ListWorkspacesCalls { get; private set; }
        public List<string> CommandRulesets { get; } = new();

        public Task<IReadOnlyList<AppCommandDefinition>> GetCommandsAsync(string? rulesetId, CancellationToken ct)
        {
            GetCommandsCalls++;
            CommandRulesets.Add(rulesetId ?? "sr5");
            return Task.FromResult<IReadOnlyList<AppCommandDefinition>>(AppCommandCatalog.All);
        }

        public Task<IReadOnlyList<NavigationTabDefinition>> GetNavigationTabsAsync(string? rulesetId, CancellationToken ct)
        {
            GetNavigationTabsCalls++;
            return Task.FromResult<IReadOnlyList<NavigationTabDefinition>>(NavigationTabCatalog.All);
        }

        public Task<IReadOnlyList<WorkspaceListItem>> ListWorkspacesAsync(CancellationToken ct)
        {
            ListWorkspacesCalls++;
            return Task.FromResult<IReadOnlyList<WorkspaceListItem>>([]);
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
