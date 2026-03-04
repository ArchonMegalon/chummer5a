using System;
using System.Collections.Generic;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class RulesetSeamContractsTests
{
    [TestMethod]
    public void Workspace_models_default_to_sr5_ruleset_and_expose_payload_envelope()
    {
        WorkspaceDocument workspaceDocument = new("<character />");
        WorkspaceImportDocument importDocument = new("<character />");
        WorkspaceSaveReceipt saveReceipt = new(new CharacterWorkspaceId("ws-1"), DocumentLength: 128);
        WorkspaceDownloadReceipt downloadReceipt = new(
            new CharacterWorkspaceId("ws-1"),
            WorkspaceDocumentFormat.Chum5Xml,
            ContentBase64: "PGNoYXJhY3RlciAvPg==",
            FileName: "ws-1.chum5",
            DocumentLength: 128);
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
            LastUpdatedUtc: DateTimeOffset.UtcNow);
        WorkspacePayloadEnvelope envelope = new(RulesetDefaults.Sr5, SchemaVersion: 1, PayloadKind: "workspace", Payload: "{}");

        Assert.AreEqual(RulesetDefaults.Sr5, workspaceDocument.RulesetId);
        Assert.AreEqual(RulesetDefaults.Sr5, importDocument.RulesetId);
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

        Assert.IsTrue(sr5Commands.Count > 0);
        Assert.IsTrue(sr5Tabs.Count > 0);
        Assert.IsTrue(sr5Actions.Count > 0);
        Assert.IsTrue(sr5Controls.Count > 0);

        Assert.AreEqual(0, AppCommandCatalog.ForRuleset("sr6").Count);
        Assert.AreEqual(0, NavigationTabCatalog.ForRuleset("sr6").Count);
        Assert.AreEqual(0, WorkspaceSurfaceActionCatalog.ForRuleset("sr6").Count);
        Assert.AreEqual(0, DesktopUiControlCatalog.ForRuleset("sr6").Count);
        Assert.AreEqual(0, WorkspaceSurfaceActionCatalog.ForTab("tab-info", "sr6").Count);
        Assert.AreEqual(0, DesktopUiControlCatalog.ForTab("tab-info", "sr6").Count);
    }

    [TestMethod]
    public void Ruleset_plugin_contracts_are_declared_for_serializer_shell_catalog_rule_and_script_hosts()
    {
        Assert.IsTrue(typeof(IRulesetPlugin).IsInterface);
        Assert.IsTrue(typeof(IRulesetSerializer).IsInterface);
        Assert.IsTrue(typeof(IRulesetShellDefinitionProvider).IsInterface);
        Assert.IsTrue(typeof(IRulesetCatalogProvider).IsInterface);
        Assert.IsTrue(typeof(IRulesetRuleHost).IsInterface);
        Assert.IsTrue(typeof(IRulesetScriptHost).IsInterface);
    }
}
