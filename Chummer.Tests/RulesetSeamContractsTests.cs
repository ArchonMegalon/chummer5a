#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chummer.Application.Characters;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
using Chummer.Infrastructure.Xml;
using Chummer.Rulesets.Sr4;
using Chummer.Rulesets.Sr5;
using Chummer.Rulesets.Sr6;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class RulesetSeamContractsTests
{
    [TestMethod]
    public void Workspace_models_keep_native_format_defaults_but_require_explicit_ruleset_inputs()
    {
        WorkspaceDocument workspaceDocument = new("<character />", RulesetId: RulesetDefaults.Sr5);
        WorkspaceImportDocument importDocument = new("<character />", RulesetId: RulesetDefaults.Sr5);
        WorkspaceSaveReceipt saveReceipt = new(new CharacterWorkspaceId("ws-1"), DocumentLength: 128, RulesetId: RulesetDefaults.Sr5);
        WorkspaceDownloadReceipt downloadReceipt = new(
            new CharacterWorkspaceId("ws-1"),
            WorkspaceDocumentFormat.NativeXml,
            ContentBase64: "PGNoYXJhY3RlciAvPg==",
            FileName: "ws-1.chum5",
            DocumentLength: 128,
            RulesetId: RulesetDefaults.Sr5);
        WorkspaceListItem listItem = new(
            new CharacterWorkspaceId("ws-1"),
            Summary: new Chummer.Contracts.Characters.CharacterFileSummary(
                Name: "Test",
                Alias: "Alias",
                Metatype: "Human",
                BuildMethod: "Priority",
                CreatedVersion: "1.0",
                AppVersion: "1.0",
                Karma: 0m,
                Nuyen: 0m,
                Created: true),
            LastUpdatedUtc: DateTimeOffset.UtcNow,
            RulesetId: RulesetDefaults.Sr5);
        WorkspacePayloadEnvelope envelope = new(RulesetDefaults.Sr5, SchemaVersion: 1, PayloadKind: "workspace", Payload: "{}");

        Assert.AreEqual(RulesetDefaults.Sr5, workspaceDocument.State.RulesetId);
        Assert.AreEqual(WorkspaceDocumentFormat.NativeXml, workspaceDocument.Format);
        Assert.AreEqual(1, workspaceDocument.State.SchemaVersion);
        Assert.AreEqual("workspace", workspaceDocument.State.PayloadKind);
        Assert.AreEqual("<character />", workspaceDocument.State.Payload);
        Assert.AreEqual(RulesetDefaults.Sr5, workspaceDocument.PayloadEnvelope.RulesetId);
        Assert.AreEqual(RulesetDefaults.Sr5, importDocument.RulesetId);
        Assert.AreEqual(WorkspaceDocumentFormat.NativeXml, importDocument.Format);
        Assert.AreEqual(RulesetDefaults.Sr5, saveReceipt.RulesetId);
        Assert.AreEqual(RulesetDefaults.Sr5, downloadReceipt.RulesetId);
        Assert.AreEqual(RulesetDefaults.Sr5, listItem.RulesetId);
        Assert.AreEqual(RulesetDefaults.Sr5, envelope.RulesetId);
    }

    [TestMethod]
    public void Presentation_catalogs_support_ruleset_filtering_without_changing_sr5_defaults()
    {
        IReadOnlyList<AppCommandDefinition> sr5Commands = AppCommandCatalog.ForRuleset(null);
        IReadOnlyList<NavigationTabDefinition> sr5Tabs = NavigationTabCatalog.ForRuleset(RulesetDefaults.Sr5);
        IReadOnlyList<WorkspaceSurfaceActionDefinition> sr5Actions = WorkspaceSurfaceActionCatalog.ForRuleset(string.Empty);
        IReadOnlyList<DesktopUiControlDefinition> sr5Controls = DesktopUiControlCatalog.ForRuleset("SR5");

        Assert.IsGreaterThan(0, sr5Commands.Count);
        Assert.IsGreaterThan(0, sr5Tabs.Count);
        Assert.IsGreaterThan(0, sr5Actions.Count);
        Assert.IsGreaterThan(0, sr5Controls.Count);

        Assert.IsFalse(AppCommandCatalog.ForRuleset("sr6").Any());
        Assert.IsFalse(AppCommandCatalog.ForRuleset("sr4").Any());
        Assert.IsFalse(NavigationTabCatalog.ForRuleset("sr6").Any());
        Assert.IsFalse(NavigationTabCatalog.ForRuleset("sr4").Any());
        Assert.IsFalse(WorkspaceSurfaceActionCatalog.ForRuleset("sr6").Any());
        Assert.IsFalse(WorkspaceSurfaceActionCatalog.ForRuleset("sr4").Any());
        Assert.IsFalse(DesktopUiControlCatalog.ForRuleset("sr6").Any());
        Assert.IsFalse(DesktopUiControlCatalog.ForRuleset("sr4").Any());
        Assert.IsFalse(WorkspaceSurfaceActionCatalog.ForTab("tab-info", "sr6").Any());
        Assert.IsFalse(WorkspaceSurfaceActionCatalog.ForTab("tab-info", "sr4").Any());
        Assert.IsFalse(DesktopUiControlCatalog.ForTab("tab-info", "sr6").Any());
        Assert.IsFalse(DesktopUiControlCatalog.ForTab("tab-info", "sr4").Any());
    }

    [TestMethod]
    public void Ruleset_plugin_contracts_are_declared_for_serializer_shell_catalog_rule_and_script_hosts()
    {
        Assert.IsTrue(typeof(IRulesetPlugin).IsInterface);
        Assert.IsTrue(typeof(IRulesetPluginRegistry).IsInterface);
        Assert.IsTrue(typeof(IRulesetSelectionPolicy).IsInterface);
        Assert.IsTrue(typeof(IRulesetShellCatalogResolver).IsInterface);
        Assert.IsTrue(typeof(IRulesetSerializer).IsInterface);
        Assert.IsTrue(typeof(IRulesetShellDefinitionProvider).IsInterface);
        Assert.IsTrue(typeof(IRulesetCatalogProvider).IsInterface);
        Assert.IsTrue(typeof(IRulesetRuleHost).IsInterface);
        Assert.IsTrue(typeof(IRulesetScriptHost).IsInterface);
    }

    [TestMethod]
    public void Ruleset_defaults_expose_sr4_sr5_and_sr6_ids()
    {
        Assert.AreEqual(string.Empty, RulesetId.Default.NormalizedValue);
        Assert.AreEqual("sr4", new RulesetId(RulesetDefaults.Sr4).NormalizedValue);
        Assert.AreEqual("sr5", new RulesetId(RulesetDefaults.Sr5).NormalizedValue);
        Assert.AreEqual("sr6", new RulesetId(RulesetDefaults.Sr6).NormalizedValue);
    }

    [TestMethod]
    public void Ruleset_defaults_only_expose_explicit_normalization_helpers()
    {
        Assert.IsNull(typeof(RulesetDefaults).GetMethod("Normalize", [typeof(string)]));
        Assert.IsNotNull(typeof(RulesetDefaults).GetMethod(nameof(RulesetDefaults.NormalizeOptional)));
        Assert.IsNotNull(typeof(RulesetDefaults).GetMethod(nameof(RulesetDefaults.NormalizeRequired)));
        Assert.IsNotNull(typeof(RulesetDefaults).GetMethod(nameof(RulesetDefaults.NormalizeOrDefault)));
        Assert.IsNull(RulesetDefaults.NormalizeOptional(" "));
        Assert.AreEqual(RulesetDefaults.Sr4, RulesetDefaults.NormalizeRequired(" SR4 "));
        Assert.AreEqual(RulesetDefaults.Sr6, RulesetDefaults.NormalizeOrDefault(null, RulesetDefaults.Sr6));
    }

    [TestMethod]
    public void Ruleset_workspace_codecs_require_explicit_ruleset_id_for_wrap_import()
    {
        IRulesetWorkspaceCodec[] codecs =
        [
            new Sr4WorkspaceCodec(),
            new Sr5WorkspaceCodec(
                new XmlCharacterFileQueries(new CharacterFileService()),
                new XmlCharacterSectionQueries(new CharacterSectionService()),
                new XmlCharacterMetadataCommands(new CharacterFileService())),
            new Sr6WorkspaceCodec()
        ];

        foreach (IRulesetWorkspaceCodec codec in codecs)
        {
            Assert.ThrowsExactly<ArgumentException>(() => codec.WrapImport(
                string.Empty,
                new WorkspaceImportDocument("<character />", string.Empty)));
        }
    }

    [TestMethod]
    public void Sr4_ruleset_registration_is_opt_in()
    {
        ServiceCollection services = new();
        services.AddSr4Ruleset();

        using ServiceProvider provider = services.BuildServiceProvider();
        IRulesetPlugin[] plugins = provider.GetServices<IRulesetPlugin>().ToArray();
        IRulesetWorkspaceCodec[] codecs = provider.GetServices<IRulesetWorkspaceCodec>().ToArray();

        Assert.IsTrue(plugins.Any(plugin => string.Equals(plugin.Id.NormalizedValue, RulesetDefaults.Sr4, StringComparison.Ordinal)));
        Assert.IsTrue(codecs.Any(codec => string.Equals(codec.RulesetId, RulesetDefaults.Sr4, StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Sr5_plugin_adapters_expose_existing_shell_catalogs_without_behavior_change()
    {
        Sr5RulesetPlugin plugin = new();

        Assert.AreEqual(RulesetDefaults.Sr5, plugin.Id.NormalizedValue);
        Assert.AreEqual("Shadowrun 5", plugin.DisplayName);
        Assert.AreEqual(RulesetDefaults.Sr5, plugin.Serializer.RulesetId.NormalizedValue);
        Assert.AreEqual(1, plugin.Serializer.SchemaVersion);

        WorkspacePayloadEnvelope envelope = plugin.Serializer.Wrap("workspace", "{}");
        Assert.AreEqual(RulesetDefaults.Sr5, envelope.RulesetId);
        Assert.AreEqual("workspace", envelope.PayloadKind);
        Assert.AreEqual("{}", envelope.Payload);

        Assert.IsGreaterThan(0, plugin.ShellDefinitions.GetCommands().Count);
        Assert.IsGreaterThan(0, plugin.ShellDefinitions.GetNavigationTabs().Count);
        Assert.IsGreaterThan(0, plugin.Catalogs.GetWorkspaceActions().Count);
        Assert.IsGreaterThan(0, plugin.Catalogs.GetDesktopUiControls().Count);

        RulesetRuleEvaluationResult ruleResult = await plugin.Rules.EvaluateAsync(
            new RulesetRuleEvaluationRequest(
                RuleId: "sr5.noop",
                Inputs: new Dictionary<string, object?> { ["karma"] = 12 }),
            CancellationToken.None);
        Assert.IsTrue(ruleResult.Success);
        Assert.IsTrue(ruleResult.Outputs.ContainsKey("karma"));

        RulesetScriptExecutionResult scriptResult = await plugin.Scripts.ExecuteAsync(
            new RulesetScriptExecutionRequest(
                ScriptId: "sr5.noop",
                ScriptSource: "-- noop",
                Inputs: new Dictionary<string, object?> { ["nuyen"] = 5000 }),
            CancellationToken.None);
        Assert.IsTrue(scriptResult.Success);
        Assert.AreEqual("noop", scriptResult.Outputs["mode"]);
    }

    [TestMethod]
    public async Task Sr6_plugin_skeleton_exposes_independent_catalogs_and_codec_contracts()
    {
        Sr6RulesetPlugin plugin = new();
        Sr6WorkspaceCodec codec = new();

        Assert.AreEqual(RulesetDefaults.Sr6, plugin.Id.NormalizedValue);
        Assert.AreEqual("Shadowrun 6", plugin.DisplayName);
        Assert.AreEqual(RulesetDefaults.Sr6, plugin.Serializer.RulesetId.NormalizedValue);
        Assert.AreEqual(Sr6WorkspaceCodec.SchemaVersion, plugin.Serializer.SchemaVersion);
        Assert.AreEqual(RulesetDefaults.Sr6, codec.RulesetId);
        Assert.AreEqual(Sr6WorkspaceCodec.Sr6PayloadKind, codec.PayloadKind);

        WorkspacePayloadEnvelope wrapped = codec.WrapImport(
            RulesetDefaults.Sr6,
            new WorkspaceImportDocument("<character><name>Switchback</name><alias>Ghost</alias></character>", RulesetDefaults.Sr6));
        CharacterFileSummary summary = codec.ParseSummary(wrapped);

        Assert.AreEqual(RulesetDefaults.Sr6, wrapped.RulesetId);
        Assert.AreEqual("Switchback", summary.Name);
        Assert.AreEqual("Ghost", summary.Alias);
        Assert.IsGreaterThan(0, plugin.ShellDefinitions.GetCommands().Count);
        Assert.IsGreaterThan(0, plugin.ShellDefinitions.GetNavigationTabs().Count);
        Assert.IsGreaterThan(0, plugin.Catalogs.GetWorkspaceActions().Count);
        Assert.IsGreaterThan(0, plugin.Catalogs.GetDesktopUiControls().Count);

        WorkspaceDownloadReceipt download = codec.BuildDownload(
            new CharacterWorkspaceId("ws-sr6"),
            wrapped,
            WorkspaceDocumentFormat.NativeXml);
        Assert.AreEqual("ws-sr6.chum6", download.FileName);

        RulesetRuleEvaluationResult ruleResult = await plugin.Rules.EvaluateAsync(
            new RulesetRuleEvaluationRequest(
                RuleId: "sr6.noop",
                Inputs: new Dictionary<string, object?> { ["edge"] = 2 }),
            CancellationToken.None);
        Assert.IsTrue(ruleResult.Success);
        Assert.IsTrue(ruleResult.Outputs.ContainsKey("edge"));

        RulesetScriptExecutionResult scriptResult = await plugin.Scripts.ExecuteAsync(
            new RulesetScriptExecutionRequest(
                ScriptId: "sr6.noop",
                ScriptSource: "// noop",
                Inputs: new Dictionary<string, object?> { ["essence"] = 5.8m }),
            CancellationToken.None);
        Assert.IsTrue(scriptResult.Success);
        Assert.AreEqual(RulesetDefaults.Sr6, scriptResult.Outputs["rulesetId"]);
    }

    [TestMethod]
    public async Task Sr4_plugin_scaffold_exposes_independent_catalogs_and_codec_contracts()
    {
        Sr4RulesetPlugin plugin = new();
        Sr4WorkspaceCodec codec = new();

        Assert.AreEqual(RulesetDefaults.Sr4, plugin.Id.NormalizedValue);
        Assert.AreEqual("Shadowrun 4", plugin.DisplayName);
        Assert.AreEqual(RulesetDefaults.Sr4, plugin.Serializer.RulesetId.NormalizedValue);
        Assert.AreEqual(Sr4WorkspaceCodec.SchemaVersion, plugin.Serializer.SchemaVersion);
        Assert.AreEqual(RulesetDefaults.Sr4, codec.RulesetId);
        Assert.AreEqual(Sr4WorkspaceCodec.Sr4PayloadKind, codec.PayloadKind);

        WorkspacePayloadEnvelope wrapped = codec.WrapImport(
            RulesetDefaults.Sr4,
            new WorkspaceImportDocument("<character><name>Ghost</name><alias>Switchback</alias></character>", RulesetDefaults.Sr4));
        CharacterFileSummary summary = codec.ParseSummary(wrapped);

        Assert.AreEqual(RulesetDefaults.Sr4, wrapped.RulesetId);
        Assert.AreEqual("Ghost", summary.Name);
        Assert.AreEqual("Switchback", summary.Alias);
        Assert.IsGreaterThan(0, plugin.ShellDefinitions.GetCommands().Count);
        Assert.IsGreaterThan(0, plugin.ShellDefinitions.GetNavigationTabs().Count);
        Assert.IsGreaterThan(0, plugin.Catalogs.GetWorkspaceActions().Count);
        Assert.IsGreaterThan(0, plugin.Catalogs.GetDesktopUiControls().Count);

        WorkspaceDownloadReceipt download = codec.BuildDownload(
            new CharacterWorkspaceId("ws-sr4"),
            wrapped,
            WorkspaceDocumentFormat.NativeXml);
        Assert.AreEqual("ws-sr4.chum4", download.FileName);

        RulesetRuleEvaluationResult ruleResult = await plugin.Rules.EvaluateAsync(
            new RulesetRuleEvaluationRequest(
                RuleId: "sr4.noop",
                Inputs: new Dictionary<string, object?> { ["essence"] = 5.5m }),
            CancellationToken.None);
        Assert.IsTrue(ruleResult.Success);
        Assert.IsTrue(ruleResult.Outputs.ContainsKey("essence"));

        RulesetScriptExecutionResult scriptResult = await plugin.Scripts.ExecuteAsync(
            new RulesetScriptExecutionRequest(
                ScriptId: "sr4.noop",
                ScriptSource: "-- noop",
                Inputs: new Dictionary<string, object?> { ["karma"] = 9 }),
            CancellationToken.None);
        Assert.IsTrue(scriptResult.Success);
        Assert.AreEqual(RulesetDefaults.Sr4, scriptResult.Outputs["rulesetId"]);
    }
}
