#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chummer.Application.Tools;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
using Chummer.Desktop.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public sealed class InProcessChummerClientRulesetPluginTests
{
    [TestMethod]
    public async Task GetCommands_and_tabs_use_ruleset_plugin_definitions_when_registered()
    {
        var pluginCommands = new[]
        {
            new AppCommandDefinition(
                Id: "sr6_custom_command",
                LabelKey: "command.sr6_custom_command",
                Group: "tools",
                RequiresOpenCharacter: false,
                EnabledByDefault: true,
                RulesetId: "sr6")
        };
        var pluginTabs = new[]
        {
            new NavigationTabDefinition(
                Id: "tab-sr6-custom",
                Label: "SR6 Custom",
                SectionId: "profile",
                Group: "character",
                RequiresOpenCharacter: true,
                EnabledByDefault: true,
                RulesetId: "sr6")
        };

        var client = new InProcessChummerClient(
            new NoOpWorkspaceService(),
            new RulesetShellCatalogResolverService(
                new RulesetPluginRegistry([new StubRulesetPlugin("sr6", pluginCommands, pluginTabs)])));

        IReadOnlyList<AppCommandDefinition> commands = await client.GetCommandsAsync("SR6", CancellationToken.None);
        IReadOnlyList<NavigationTabDefinition> tabs = await client.GetNavigationTabsAsync("sr6", CancellationToken.None);

        Assert.HasCount(1, commands);
        Assert.AreEqual("sr6_custom_command", commands[0].Id);
        Assert.HasCount(1, tabs);
        Assert.AreEqual("tab-sr6-custom", tabs[0].Id);
    }

    [TestMethod]
    public async Task GetCommands_and_tabs_fallback_to_catalog_when_plugin_is_missing()
    {
        var client = new InProcessChummerClient(
            new NoOpWorkspaceService(),
            new RulesetShellCatalogResolverService(new RulesetPluginRegistry(Array.Empty<IRulesetPlugin>())));

        IReadOnlyList<AppCommandDefinition> commands = await client.GetCommandsAsync("sr5", CancellationToken.None);
        IReadOnlyList<NavigationTabDefinition> tabs = await client.GetNavigationTabsAsync("sr5", CancellationToken.None);

        Assert.HasCount(AppCommandCatalog.ForRuleset("sr5").Count, commands);
        Assert.HasCount(NavigationTabCatalog.ForRuleset("sr5").Count, tabs);
        Assert.IsTrue(commands.Any(command => string.Equals(command.Id, "file", StringComparison.Ordinal)));
        Assert.IsTrue(tabs.Any(tab => string.Equals(tab.Id, "tab-info", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task GetShellBootstrap_uses_saved_preferred_ruleset_when_no_workspaces_are_open()
    {
        var preferencesStore = new InMemoryShellPreferencesStore();
        preferencesStore.Save(new ShellPreferences("sr6"));
        var client = new InProcessChummerClient(
            new NoOpWorkspaceService(),
            new RulesetShellCatalogResolverService(new RulesetPluginRegistry(Array.Empty<IRulesetPlugin>())),
            new ShellPreferencesService(preferencesStore));

        ShellBootstrapSnapshot snapshot = await client.GetShellBootstrapAsync(rulesetId: null, CancellationToken.None);

        Assert.AreEqual("sr6", snapshot.RulesetId);
    }

    [TestMethod]
    public async Task SaveShellPreferences_persists_preferred_ruleset()
    {
        var preferencesStore = new InMemoryShellPreferencesStore();
        var client = new InProcessChummerClient(
            new NoOpWorkspaceService(),
            new RulesetShellCatalogResolverService(new RulesetPluginRegistry(Array.Empty<IRulesetPlugin>())),
            new ShellPreferencesService(preferencesStore));

        await client.SaveShellPreferencesAsync(new ShellPreferences("sr6"), CancellationToken.None);
        ShellPreferences restored = await client.GetShellPreferencesAsync(CancellationToken.None);

        Assert.AreEqual("sr6", restored.PreferredRulesetId);
    }

    [TestMethod]
    public async Task SaveShellSession_persists_active_workspace()
    {
        var sessionStore = new InMemoryShellSessionStore();
        var client = new InProcessChummerClient(
            new NoOpWorkspaceService(),
            new RulesetShellCatalogResolverService(new RulesetPluginRegistry(Array.Empty<IRulesetPlugin>())),
            shellSessionService: new ShellSessionService(sessionStore));

        await client.SaveShellSessionAsync(new ShellSessionState("ws-sr6"), CancellationToken.None);
        ShellSessionState restored = await client.GetShellSessionAsync(CancellationToken.None);

        Assert.AreEqual("ws-sr6", restored.ActiveWorkspaceId);
    }

    [TestMethod]
    public async Task GetShellBootstrap_restores_saved_active_workspace_when_present()
    {
        var workspaceService = new NoOpWorkspaceService
        {
            Workspaces =
            [
                CreateWorkspace("ws-sr5", DateTimeOffset.UtcNow.AddMinutes(-10), RulesetDefaults.Sr5),
                CreateWorkspace("ws-sr6", DateTimeOffset.UtcNow.AddMinutes(-5), "sr6")
            ]
        };
        var preferencesStore = new InMemoryShellPreferencesStore();
        preferencesStore.Save(new ShellPreferences(RulesetDefaults.Sr5));
        var sessionStore = new InMemoryShellSessionStore();
        sessionStore.Save(new ShellSessionState("ws-sr5"));
        var client = new InProcessChummerClient(
            workspaceService,
            new RulesetShellCatalogResolverService(new RulesetPluginRegistry(Array.Empty<IRulesetPlugin>())),
            new ShellPreferencesService(preferencesStore),
            new ShellSessionService(sessionStore));

        ShellBootstrapSnapshot snapshot = await client.GetShellBootstrapAsync(rulesetId: null, CancellationToken.None);

        Assert.AreEqual("ws-sr5", snapshot.ActiveWorkspaceId?.Value);
        Assert.AreEqual(RulesetDefaults.Sr5, snapshot.ActiveRulesetId);
        Assert.AreEqual(RulesetDefaults.Sr5, snapshot.RulesetId);
    }

    private sealed class StubRulesetPlugin : IRulesetPlugin
    {
        public StubRulesetPlugin(
            string rulesetId,
            IReadOnlyList<AppCommandDefinition> commands,
            IReadOnlyList<NavigationTabDefinition> tabs)
        {
            Id = new RulesetId(rulesetId);
            DisplayName = $"Stub {rulesetId}";
            Serializer = new StubRulesetSerializer(Id);
            ShellDefinitions = new StubRulesetShellDefinitions(commands, tabs);
            Catalogs = new StubRulesetCatalogProvider();
            Rules = new StubRulesetRuleHost();
            Scripts = new StubRulesetScriptHost();
        }

        public RulesetId Id { get; }

        public string DisplayName { get; }

        public IRulesetSerializer Serializer { get; }

        public IRulesetShellDefinitionProvider ShellDefinitions { get; }

        public IRulesetCatalogProvider Catalogs { get; }

        public IRulesetRuleHost Rules { get; }

        public IRulesetScriptHost Scripts { get; }
    }

    private sealed class StubRulesetSerializer : IRulesetSerializer
    {
        public StubRulesetSerializer(RulesetId rulesetId)
        {
            RulesetId = rulesetId;
        }

        public RulesetId RulesetId { get; }

        public int SchemaVersion => 1;

        public WorkspacePayloadEnvelope Wrap(string payloadKind, string payload)
        {
            return new WorkspacePayloadEnvelope(
                RulesetId: RulesetId.ToString(),
                SchemaVersion: SchemaVersion,
                PayloadKind: payloadKind,
                Payload: payload);
        }
    }

    private sealed class StubRulesetShellDefinitions : IRulesetShellDefinitionProvider
    {
        private readonly IReadOnlyList<AppCommandDefinition> _commands;
        private readonly IReadOnlyList<NavigationTabDefinition> _tabs;

        public StubRulesetShellDefinitions(
            IReadOnlyList<AppCommandDefinition> commands,
            IReadOnlyList<NavigationTabDefinition> tabs)
        {
            _commands = commands;
            _tabs = tabs;
        }

        public IReadOnlyList<AppCommandDefinition> GetCommands() => _commands;

        public IReadOnlyList<NavigationTabDefinition> GetNavigationTabs() => _tabs;
    }

    private sealed class StubRulesetCatalogProvider : IRulesetCatalogProvider
    {
        public IReadOnlyList<WorkspaceSurfaceActionDefinition> GetWorkspaceActions() => Array.Empty<WorkspaceSurfaceActionDefinition>();

        public IReadOnlyList<DesktopUiControlDefinition> GetDesktopUiControls() => Array.Empty<DesktopUiControlDefinition>();
    }

    private sealed class StubRulesetRuleHost : IRulesetRuleHost
    {
        public ValueTask<RulesetRuleEvaluationResult> EvaluateAsync(RulesetRuleEvaluationRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new RulesetRuleEvaluationResult(
                Success: true,
                Outputs: request.Inputs,
                Messages: Array.Empty<string>()));
        }
    }

    private sealed class StubRulesetScriptHost : IRulesetScriptHost
    {
        public ValueTask<RulesetScriptExecutionResult> ExecuteAsync(RulesetScriptExecutionRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new RulesetScriptExecutionResult(
                Success: true,
                Error: null,
                Outputs: new Dictionary<string, object?>()));
        }
    }

    private sealed class InMemoryShellPreferencesStore : IShellPreferencesStore
    {
        private ShellPreferences _preferences = ShellPreferences.Default;

        public ShellPreferences Load()
        {
            return _preferences;
        }

        public void Save(ShellPreferences preferences)
        {
            _preferences = preferences;
        }
    }

    private sealed class InMemoryShellSessionStore : IShellSessionStore
    {
        private ShellSessionState _session = ShellSessionState.Default;

        public ShellSessionState Load()
        {
            return _session;
        }

        public void Save(ShellSessionState session)
        {
            _session = session;
        }
    }

    private sealed class NoOpWorkspaceService : IWorkspaceService
    {
        public WorkspaceImportResult Import(WorkspaceImportDocument document) => throw new NotSupportedException();

        public IReadOnlyList<WorkspaceListItem> Workspaces { get; init; } = Array.Empty<WorkspaceListItem>();

        public IReadOnlyList<WorkspaceListItem> List(int? maxCount = null)
        {
            if (maxCount is > 0)
            {
                return Workspaces.Take(maxCount.Value).ToArray();
            }

            return Workspaces;
        }

        public bool Close(CharacterWorkspaceId id) => throw new NotSupportedException();

        public object? GetSection(CharacterWorkspaceId id, string sectionId) => throw new NotSupportedException();

        public CharacterFileSummary? GetSummary(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterValidationResult? Validate(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterProfileSection? GetProfile(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterProgressSection? GetProgress(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterSkillsSection? GetSkills(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterRulesSection? GetRules(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterBuildSection? GetBuild(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterMovementSection? GetMovement(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterAwakeningSection? GetAwakening(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CommandResult<CharacterProfileSection> UpdateMetadata(CharacterWorkspaceId id, UpdateWorkspaceMetadata command) => throw new NotSupportedException();

        public CommandResult<WorkspaceSaveReceipt> Save(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CommandResult<WorkspaceDownloadReceipt> Download(CharacterWorkspaceId id) => throw new NotSupportedException();
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
                Alias: id,
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
