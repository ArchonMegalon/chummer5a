#nullable enable annotations

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
        RulesetShellCatalogResolverService resolver = CreateResolver(
            new StubRulesetPlugin(
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
                tabs: []));
        IReadOnlyList<AppCommandDefinition> commands = resolver.ResolveCommands("sr6");

        Assert.HasCount(1, commands);
        Assert.AreEqual("sr6-only", commands[0].Id);
    }

    [TestMethod]
    public void ResolveNavigationTabs_falls_back_to_catalog_without_matching_plugin()
    {
        RulesetShellCatalogResolverService resolver = CreateResolver(new StubRulesetPlugin("sr6", commands: [], tabs: []));
        IReadOnlyList<NavigationTabDefinition> tabs = resolver.ResolveNavigationTabs("sr5");

        Assert.HasCount(NavigationTabCatalog.ForRuleset("sr5").Count, tabs);
        Assert.IsTrue(tabs.Any(tab => string.Equals(tab.Id, "tab-info", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ResolveCommands_uses_last_matching_plugin_when_multiple_are_registered()
    {
        RulesetShellCatalogResolverService resolver = CreateResolver(
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
                tabs: []));
        IReadOnlyList<AppCommandDefinition> commands = resolver.ResolveCommands("sr6");

        Assert.HasCount(1, commands);
        Assert.AreEqual("second", commands[0].Id);
    }

    [TestMethod]
    public void ResolveWorkspaceActionsForTab_prefers_ruleset_plugin_catalogs()
    {
        RulesetShellCatalogResolverService resolver = CreateResolver(
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
                controls: []));
        IReadOnlyList<WorkspaceSurfaceActionDefinition> actions = resolver.ResolveWorkspaceActionsForTab("tab-sr6", "sr6");

        Assert.HasCount(1, actions);
        Assert.AreEqual("tab-sr6.summary", actions[0].Id);
        Assert.AreEqual("sr6", actions[0].RulesetId);
    }

    [TestMethod]
    public void ResolveDesktopUiControlsForTab_falls_back_to_catalog_without_matching_plugin()
    {
        RulesetShellCatalogResolverService resolver = CreateResolver(new StubRulesetPlugin("sr6", commands: [], tabs: []));
        IReadOnlyList<DesktopUiControlDefinition> controls = resolver.ResolveDesktopUiControlsForTab("tab-info", "sr5");

        Assert.HasCount(DesktopUiControlCatalog.ForTab("tab-info", "sr5").Count, controls);
        Assert.IsTrue(controls.Any(control => string.Equals(control.TabId, "tab-info", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void RulesetPluginRegistry_does_not_treat_blank_ruleset_as_sr5_plugin_request()
    {
        RulesetPluginRegistry registry = new([new StubRulesetPlugin("sr5", commands: [], tabs: [])]);

        Assert.IsNull(registry.Resolve(null));
        Assert.IsNull(registry.Resolve(" "));
        Assert.IsNotNull(registry.Resolve("sr5"));
    }

    private static RulesetShellCatalogResolverService CreateResolver(params IRulesetPlugin[] plugins)
    {
        return new RulesetShellCatalogResolverService(new RulesetPluginRegistry(plugins));
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
