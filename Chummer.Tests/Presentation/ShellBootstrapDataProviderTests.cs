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
    public async Task GetWorkspacesAsync_reads_saved_preferred_ruleset_when_seeding_workspace_cache()
    {
        var client = new BootstrapClientStub
        {
            Preferences = new ShellPreferences("sr6")
        };
        var provider = new ShellBootstrapDataProvider(client);

        await provider.GetWorkspacesAsync(CancellationToken.None);
        ShellBootstrapData bootstrap = await provider.GetAsync("sr6", CancellationToken.None);

        Assert.AreEqual(1, client.GetShellPreferencesCalls);
        Assert.AreEqual(1, client.GetShellSessionCalls);
        Assert.AreEqual("sr6", bootstrap.PreferredRulesetId);
    }

    [TestMethod]
    public async Task GetAsync_includes_active_workspace_from_bootstrap_snapshot()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var client = new BootstrapClientStub
        {
            Workspaces =
            [
                CreateWorkspace("ws-old", now.AddMinutes(-25), RulesetDefaults.Sr5),
                CreateWorkspace("ws-new", now.AddMinutes(-5), "sr6")
            ],
            Preferences = new ShellPreferences(RulesetDefaults.Sr5),
            Session = new ShellSessionState("ws-old")
        };
        var provider = new ShellBootstrapDataProvider(client);

        ShellBootstrapData bootstrap = await provider.GetAsync(CancellationToken.None);

        Assert.AreEqual("ws-old", bootstrap.ActiveWorkspaceId?.Value);
        Assert.AreEqual(RulesetDefaults.Sr5, bootstrap.ActiveRulesetId);
    }

    [TestMethod]
    public async Task GetAsync_does_not_infer_active_workspace_from_workspace_order_when_session_is_empty()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var client = new BootstrapClientStub
        {
            Workspaces =
            [
                CreateWorkspace("ws-old", now.AddMinutes(-25), RulesetDefaults.Sr5),
                CreateWorkspace("ws-new", now.AddMinutes(-5), "sr6")
            ],
            Preferences = new ShellPreferences(RulesetDefaults.Sr5),
            Session = ShellSessionState.Default
        };
        var provider = new ShellBootstrapDataProvider(client);

        ShellBootstrapData bootstrap = await provider.GetAsync(CancellationToken.None);

        Assert.IsNull(bootstrap.ActiveWorkspaceId);
        Assert.AreEqual(RulesetDefaults.Sr5, bootstrap.ActiveRulesetId);
    }

    [TestMethod]
    public async Task GetAsync_includes_active_tab_from_bootstrap_snapshot()
    {
        var client = new BootstrapClientStub
        {
            Session = new ShellSessionState(ActiveTabId: "tab-rules")
        };
        var provider = new ShellBootstrapDataProvider(client);

        ShellBootstrapData bootstrap = await provider.GetAsync(CancellationToken.None);

        Assert.AreEqual("tab-rules", bootstrap.ActiveTabId);
    }

    [TestMethod]
    public async Task GetAsync_includes_workspace_tab_map_from_bootstrap_snapshot()
    {
        var client = new BootstrapClientStub
        {
            Session = new ShellSessionState(
                ActiveTabsByWorkspace: new Dictionary<string, string>
                {
                    ["ws-a"] = "tab-info",
                    ["ws-b"] = "tab-rules"
                })
        };
        var provider = new ShellBootstrapDataProvider(client);

        ShellBootstrapData bootstrap = await provider.GetAsync(CancellationToken.None);

        Assert.IsNotNull(bootstrap.ActiveTabsByWorkspace);
        Assert.AreEqual("tab-info", bootstrap.ActiveTabsByWorkspace!["ws-a"]);
        Assert.AreEqual("tab-rules", bootstrap.ActiveTabsByWorkspace["ws-b"]);
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
        Assert.AreEqual(2, client.ListWorkspacesCalls);

        string[] expectedRulesets = ["sr5", "sr6"];
        CollectionAssert.AreEquivalent(
            expectedRulesets,
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
        public int GetShellPreferencesCalls { get; private set; }
        public int GetShellSessionCalls { get; private set; }
        public List<string> CommandRulesets { get; } = new();
        public ShellPreferences Preferences { get; set; } = ShellPreferences.Default;
        public ShellSessionState Session { get; set; } = ShellSessionState.Default;
        public IReadOnlyList<WorkspaceListItem> Workspaces { get; set; } = Array.Empty<WorkspaceListItem>();

        public Task<ShellPreferences> GetShellPreferencesAsync(CancellationToken ct)
        {
            GetShellPreferencesCalls++;
            return Task.FromResult(Preferences);
        }

        public Task SaveShellPreferencesAsync(ShellPreferences preferences, CancellationToken ct)
        {
            Preferences = new ShellPreferences(
                PreferredRulesetId: RulesetDefaults.Normalize(preferences.PreferredRulesetId));
            return Task.CompletedTask;
        }

        public Task<ShellSessionState> GetShellSessionAsync(CancellationToken ct)
        {
            GetShellSessionCalls++;
            return Task.FromResult(Session);
        }

        public Task SaveShellSessionAsync(ShellSessionState session, CancellationToken ct)
        {
            Session = new ShellSessionState(
                ActiveWorkspaceId: NormalizeWorkspaceId(session.ActiveWorkspaceId),
                ActiveTabId: NormalizeTabId(session.ActiveTabId),
                ActiveTabsByWorkspace: NormalizeWorkspaceTabMap(session.ActiveTabsByWorkspace));
            return Task.CompletedTask;
        }

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
            return Task.FromResult(Workspaces);
        }

        public async Task<ShellBootstrapSnapshot> GetShellBootstrapAsync(string? rulesetId, CancellationToken ct)
        {
            IReadOnlyList<WorkspaceListItem> workspaces = await ListWorkspacesAsync(ct);
            CharacterWorkspaceId? activeWorkspaceId = ResolveActiveWorkspaceId(workspaces, Session.ActiveWorkspaceId);
            string preferredRulesetId = RulesetDefaults.Normalize(Preferences.PreferredRulesetId);
            string activeRulesetId = activeWorkspaceId is null
                ? preferredRulesetId
                : RulesetDefaults.Normalize(
                    workspaces.First(workspace => string.Equals(workspace.Id.Value, activeWorkspaceId.Value.Value, StringComparison.Ordinal)).RulesetId);
            string effectiveRulesetId = string.IsNullOrWhiteSpace(rulesetId)
                ? activeRulesetId
                : RulesetDefaults.Normalize(rulesetId);
            IReadOnlyList<AppCommandDefinition> commands = await GetCommandsAsync(effectiveRulesetId, ct);
            IReadOnlyList<NavigationTabDefinition> tabs = await GetNavigationTabsAsync(effectiveRulesetId, ct);
            return new ShellBootstrapSnapshot(
                effectiveRulesetId,
                commands,
                tabs,
                workspaces,
                PreferredRulesetId: preferredRulesetId,
                ActiveRulesetId: activeRulesetId,
                ActiveWorkspaceId: activeWorkspaceId,
                ActiveTabId: NormalizeTabId(Session.ActiveTabId),
                ActiveTabsByWorkspace: NormalizeWorkspaceTabMap(Session.ActiveTabsByWorkspace));
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

        private static string? NormalizeWorkspaceId(string? workspaceId)
        {
            return string.IsNullOrWhiteSpace(workspaceId)
                ? null
                : workspaceId.Trim();
        }

        private static string? NormalizeTabId(string? tabId)
        {
            return string.IsNullOrWhiteSpace(tabId)
                ? null
                : tabId.Trim();
        }

        private static Dictionary<string, string>? NormalizeWorkspaceTabMap(IReadOnlyDictionary<string, string>? rawMap)
        {
            if (rawMap is null || rawMap.Count == 0)
            {
                return null;
            }

            Dictionary<string, string> normalized = new(StringComparer.Ordinal);
            foreach ((string workspaceId, string tabId) in rawMap)
            {
                string? normalizedWorkspaceId = string.IsNullOrWhiteSpace(workspaceId)
                    ? null
                    : workspaceId.Trim();
                string? normalizedTabId = NormalizeTabId(tabId);
                if (normalizedWorkspaceId is null || normalizedTabId is null)
                {
                    continue;
                }

                normalized[normalizedWorkspaceId] = normalizedTabId;
            }

            return normalized.Count == 0
                ? null
                : normalized;
        }

        private static CharacterWorkspaceId? ResolveActiveWorkspaceId(
            IReadOnlyList<WorkspaceListItem> workspaces,
            string? preferredWorkspaceId)
        {
            if (string.IsNullOrWhiteSpace(preferredWorkspaceId))
                return null;

            WorkspaceListItem? matchingWorkspace = workspaces.FirstOrDefault(workspace =>
                string.Equals(workspace.Id.Value, preferredWorkspaceId, StringComparison.Ordinal));
            return matchingWorkspace?.Id;
        }
    }

    private static WorkspaceListItem CreateWorkspace(
        string id,
        DateTimeOffset lastUpdatedUtc,
        string rulesetId)
    {
        return new WorkspaceListItem(
            Id: new CharacterWorkspaceId(id),
            Summary: new CharacterFileSummary(
                Name: id,
                Alias: string.Empty,
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
}
