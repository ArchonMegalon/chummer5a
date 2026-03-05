using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public sealed class RulesetShellCatalogResolverTests
{
    [TestMethod]
    public void ResolveCommands_prefers_ruleset_plugin_when_available()
    {
        IReadOnlyList<AppCommandDefinition> commands = RulesetShellCatalogResolver.ResolveCommands(
            "sr6",
            [new StubRulesetPlugin(
                rulesetId: "sr6",
                commands:
                [
                    new AppCommandDefinition(
                        Id: "sr6-only",
                        LabelKey: "command.sr6-only",
                        Group: "tools",
                        RequiresOpenCharacter: false,
                        EnabledByDefault: true,
                        RulesetId: "sr6")
                ],
                tabs: [])]);

        Assert.AreEqual(1, commands.Count);
        Assert.AreEqual("sr6-only", commands[0].Id);
    }

    [TestMethod]
    public void ResolveNavigationTabs_falls_back_to_catalog_without_matching_plugin()
    {
        IReadOnlyList<NavigationTabDefinition> tabs = RulesetShellCatalogResolver.ResolveNavigationTabs(
            "sr5",
            [new StubRulesetPlugin("sr6", commands: [], tabs: [])]);

        Assert.AreEqual(NavigationTabCatalog.ForRuleset("sr5").Count, tabs.Count);
        Assert.IsTrue(tabs.Any(tab => string.Equals(tab.Id, "tab-info", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ResolveCommands_uses_last_matching_plugin_when_multiple_are_registered()
    {
        IReadOnlyList<AppCommandDefinition> commands = RulesetShellCatalogResolver.ResolveCommands(
            "sr6",
            [
                new StubRulesetPlugin(
                    "sr6",
                    commands:
                    [
                        new AppCommandDefinition("first", "command.first", "tools", false, true, "sr6")
                    ],
                    tabs: []),
                new StubRulesetPlugin(
                    "sr6",
                    commands:
                    [
                        new AppCommandDefinition("second", "command.second", "tools", false, true, "sr6")
                    ],
                    tabs: [])
            ]);

        Assert.AreEqual(1, commands.Count);
        Assert.AreEqual("second", commands[0].Id);
    }

    [TestMethod]
    public void ResolveWorkspaceActionsForTab_prefers_ruleset_plugin_catalogs()
    {
        IReadOnlyList<WorkspaceSurfaceActionDefinition> actions = RulesetShellCatalogResolver.ResolveWorkspaceActionsForTab(
            tabId: "tab-sr6",
            rulesetId: "sr6",
            plugins:
            [
                new StubRulesetPlugin(
                    rulesetId: "sr6",
                    commands: [],
                    tabs: [],
                    actions:
                    [
                        new WorkspaceSurfaceActionDefinition(
                            Id: "tab-sr6.summary",
                            Label: "SR6 Summary",
                            TabId: "tab-sr6",
                            Kind: WorkspaceSurfaceActionKind.Summary,
                            TargetId: "summary",
                            RequiresOpenCharacter: true,
                            EnabledByDefault: true,
                            RulesetId: "sr6")
                    ],
                    controls: [])
            ]);

        Assert.AreEqual(1, actions.Count);
        Assert.AreEqual("tab-sr6.summary", actions[0].Id);
        Assert.AreEqual("sr6", actions[0].RulesetId);
    }

    [TestMethod]
    public void ResolveDesktopUiControlsForTab_falls_back_to_catalog_without_matching_plugin()
    {
        IReadOnlyList<DesktopUiControlDefinition> controls = RulesetShellCatalogResolver.ResolveDesktopUiControlsForTab(
            tabId: "tab-info",
            rulesetId: "sr5",
            plugins: [new StubRulesetPlugin("sr6", commands: [], tabs: [])]);

        Assert.AreEqual(DesktopUiControlCatalog.ForTab("tab-info", "sr5").Count, controls.Count);
        Assert.IsTrue(controls.Any(control => string.Equals(control.TabId, "tab-info", StringComparison.Ordinal)));
    }

    private sealed class StubRulesetPlugin : IRulesetPlugin
    {
        public StubRulesetPlugin(
            string rulesetId,
            IReadOnlyList<AppCommandDefinition> commands,
            IReadOnlyList<NavigationTabDefinition> tabs,
            IReadOnlyList<WorkspaceSurfaceActionDefinition>? actions = null,
            IReadOnlyList<DesktopUiControlDefinition>? controls = null)
        {
            Id = new RulesetId(rulesetId);
            DisplayName = rulesetId;
            Serializer = new StubSerializer(Id);
            ShellDefinitions = new StubShellDefinitions(commands, tabs);
            Catalogs = new StubCatalogs(actions, controls);
            Rules = new StubRules();
            Scripts = new StubScripts();
        }

        public RulesetId Id { get; }

        public string DisplayName { get; }

        public IRulesetSerializer Serializer { get; }

        public IRulesetShellDefinitionProvider ShellDefinitions { get; }

        public IRulesetCatalogProvider Catalogs { get; }

        public IRulesetRuleHost Rules { get; }

        public IRulesetScriptHost Scripts { get; }
    }

    private sealed class StubSerializer : IRulesetSerializer
    {
        public StubSerializer(RulesetId id)
        {
            RulesetId = id;
        }

        public RulesetId RulesetId { get; }

        public int SchemaVersion => 1;

        public WorkspacePayloadEnvelope Wrap(string payloadKind, string payload)
        {
            return new WorkspacePayloadEnvelope(RulesetId.ToString(), SchemaVersion, payloadKind, payload);
        }
    }

    private sealed class StubShellDefinitions : IRulesetShellDefinitionProvider
    {
        private readonly IReadOnlyList<AppCommandDefinition> _commands;
        private readonly IReadOnlyList<NavigationTabDefinition> _tabs;

        public StubShellDefinitions(
            IReadOnlyList<AppCommandDefinition> commands,
            IReadOnlyList<NavigationTabDefinition> tabs)
        {
            _commands = commands;
            _tabs = tabs;
        }

        public IReadOnlyList<AppCommandDefinition> GetCommands() => _commands;

        public IReadOnlyList<NavigationTabDefinition> GetNavigationTabs() => _tabs;
    }

    private sealed class StubCatalogs : IRulesetCatalogProvider
    {
        private readonly IReadOnlyList<WorkspaceSurfaceActionDefinition> _actions;
        private readonly IReadOnlyList<DesktopUiControlDefinition> _controls;

        public StubCatalogs(
            IReadOnlyList<WorkspaceSurfaceActionDefinition>? actions = null,
            IReadOnlyList<DesktopUiControlDefinition>? controls = null)
        {
            _actions = actions ?? Array.Empty<WorkspaceSurfaceActionDefinition>();
            _controls = controls ?? Array.Empty<DesktopUiControlDefinition>();
        }

        public IReadOnlyList<WorkspaceSurfaceActionDefinition> GetWorkspaceActions() => _actions;

        public IReadOnlyList<DesktopUiControlDefinition> GetDesktopUiControls() => _controls;
    }

    private sealed class StubRules : IRulesetRuleHost
    {
        public ValueTask<RulesetRuleEvaluationResult> EvaluateAsync(RulesetRuleEvaluationRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new RulesetRuleEvaluationResult(true, request.Inputs, Array.Empty<string>()));
        }
    }

    private sealed class StubScripts : IRulesetScriptHost
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
}
