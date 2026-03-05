using System;
using System.Linq;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Presentation;

[TestClass]
public class DesktopDialogFactoryTests
{
    [TestMethod]
    public void CreateCommandDialog_uses_current_preferences_and_workspace_context()
    {
        DesktopDialogFactory factory = new();
        DesktopPreferenceState preferences = DesktopPreferenceState.Default with
        {
            UiScalePercent = 125,
            Theme = "neo",
            Language = "de-de",
            CompactMode = true
        };

        DesktopDialogState dialog = factory.CreateCommandDialog(
            "global_settings",
            profile: null,
            preferences,
            activeSectionJson: null,
            currentWorkspace: new CharacterWorkspaceId("ws-42"));

        Assert.AreEqual("dialog.global_settings", dialog.Id);
        Assert.AreEqual("125", DesktopDialogFieldValueParser.GetValue(dialog, "globalUiScale"));
        Assert.AreEqual("neo", DesktopDialogFieldValueParser.GetValue(dialog, "globalTheme"));
        Assert.AreEqual("de-de", DesktopDialogFieldValueParser.GetValue(dialog, "globalLanguage"));
        Assert.AreEqual("true", DesktopDialogFieldValueParser.GetValue(dialog, "globalCompactMode"));
    }

    [TestMethod]
    public void CreateMetadataDialog_prefills_profile_name_alias_and_notes()
    {
        DesktopDialogFactory factory = new();
        CharacterProfileSection profile = CreateProfile("Apex", "Predator");
        DesktopPreferenceState preferences = DesktopPreferenceState.Default with
        {
            CharacterNotes = "Stealth loadout"
        };

        DesktopDialogState dialog = factory.CreateMetadataDialog(profile, preferences);

        Assert.AreEqual("dialog.workspace.metadata", dialog.Id);
        Assert.AreEqual("Apex", DesktopDialogFieldValueParser.GetValue(dialog, "metadataName"));
        Assert.AreEqual("Predator", DesktopDialogFieldValueParser.GetValue(dialog, "metadataAlias"));
        Assert.AreEqual("Stealth loadout", DesktopDialogFieldValueParser.GetValue(dialog, "metadataNotes"));
    }

    [TestMethod]
    public void CreateUiControlDialog_open_notes_uses_character_notes_preference()
    {
        DesktopDialogFactory factory = new();
        DesktopPreferenceState preferences = DesktopPreferenceState.Default with
        {
            CharacterNotes = "From notes panel"
        };

        DesktopDialogState dialog = factory.CreateUiControlDialog("open_notes", preferences);

        Assert.AreEqual("dialog.ui.open_notes", dialog.Id);
        Assert.AreEqual("From notes panel", DesktopDialogFieldValueParser.GetValue(dialog, "uiNotesEditor"));
    }

    [TestMethod]
    public void CreateUiControlDialog_mutating_controls_use_explicit_action_ids()
    {
        DesktopDialogFactory factory = new();

        DesktopDialogState gearAddDialog = factory.CreateUiControlDialog("gear_add", DesktopPreferenceState.Default);
        DesktopDialogState gearEditDialog = factory.CreateUiControlDialog("gear_edit", DesktopPreferenceState.Default);
        DesktopDialogState gearDeleteDialog = factory.CreateUiControlDialog("gear_delete", DesktopPreferenceState.Default);

        Assert.IsNotNull(gearAddDialog.Actions.SingleOrDefault(action => string.Equals(action.Id, "add", StringComparison.Ordinal)));
        Assert.IsNotNull(gearEditDialog.Actions.SingleOrDefault(action => string.Equals(action.Id, "apply", StringComparison.Ordinal)));
        Assert.IsNotNull(gearDeleteDialog.Actions.SingleOrDefault(action => string.Equals(action.Id, "delete", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void DialogFieldValueParser_normalizes_and_parses_checkbox_values()
    {
        DesktopDialogField checkboxField = new(
            "globalCompactMode",
            "Compact Mode",
            "false",
            "false",
            InputType: "checkbox");

        string normalized = DesktopDialogFieldValueParser.Normalize(checkboxField, "on");
        DesktopDialogState dialog = new(
            "dialog.test",
            "Test",
            null,
            [checkboxField with { Value = normalized }],
            [new DesktopDialogAction("close", "Close", true)]);

        Assert.AreEqual("true", normalized);
        Assert.IsTrue(DesktopDialogFieldValueParser.ParseBool(dialog, "globalCompactMode", false));
    }

    [TestMethod]
    public void CreateCommandDialog_open_character_uses_import_template()
    {
        DesktopDialogFactory factory = new();

        DesktopDialogState dialog = factory.CreateCommandDialog(
            "open_character",
            profile: null,
            DesktopPreferenceState.Default,
            activeSectionJson: null,
            currentWorkspace: null);

        Assert.AreEqual("dialog.open_character", dialog.Id);
        Assert.IsNotNull(dialog.Fields.SingleOrDefault(field => string.Equals(field.Id, "openCharacterXml", StringComparison.Ordinal)));
        Assert.AreEqual(RulesetDefaults.Sr5, DesktopDialogFieldValueParser.GetValue(dialog, "importRulesetId"));
        Assert.IsNotNull(dialog.Actions.SingleOrDefault(action => string.Equals(action.Id, "import", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateCommandDialog_hero_lab_importer_uses_xml_compatibility_fields()
    {
        DesktopDialogFactory factory = new();

        DesktopDialogState dialog = factory.CreateCommandDialog(
            "hero_lab_importer",
            profile: null,
            DesktopPreferenceState.Default,
            activeSectionJson: null,
            currentWorkspace: null);

        Assert.AreEqual("dialog.hero_lab_importer", dialog.Id);
        Assert.IsNotNull(dialog.Fields.SingleOrDefault(field => string.Equals(field.Id, "heroLabXml", StringComparison.Ordinal)));
        Assert.AreEqual(RulesetDefaults.Sr5, DesktopDialogFieldValueParser.GetValue(dialog, "importRulesetId"));
        Assert.IsNotNull(dialog.Actions.SingleOrDefault(action => string.Equals(action.Id, "import", StringComparison.Ordinal)));
    }

    private static CharacterProfileSection CreateProfile(string name, string alias)
    {
        return new CharacterProfileSection(
            Name: name,
            Alias: alias,
            PlayerName: string.Empty,
            Metatype: "Human",
            Metavariant: string.Empty,
            Sex: string.Empty,
            Age: string.Empty,
            Height: string.Empty,
            Weight: string.Empty,
            Hair: string.Empty,
            Eyes: string.Empty,
            Skin: string.Empty,
            Concept: string.Empty,
            Description: string.Empty,
            Background: string.Empty,
            CreatedVersion: string.Empty,
            AppVersion: string.Empty,
            BuildMethod: "Priority",
            GameplayOption: string.Empty,
            Created: true,
            Adept: false,
            Magician: false,
            Technomancer: false,
            AI: false,
            MainMugshotIndex: 0,
            MugshotCount: 0);
    }
}
