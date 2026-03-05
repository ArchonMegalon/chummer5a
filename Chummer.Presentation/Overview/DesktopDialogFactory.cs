using Chummer.Contracts.Characters;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public sealed class DesktopDialogFactory : IDesktopDialogFactory
{
    public DesktopDialogState CreateMetadataDialog(
        CharacterProfileSection? profile,
        DesktopPreferenceState preferences)
    {
        return new DesktopDialogState(
            Id: "dialog.workspace.metadata",
            Title: "Edit Metadata",
            Message: "Apply character metadata changes to the active workspace.",
            Fields:
            [
                new DesktopDialogField("metadataName", "Name", profile?.Name ?? string.Empty, "Character Name"),
                new DesktopDialogField("metadataAlias", "Alias", profile?.Alias ?? string.Empty, "Street Name"),
                new DesktopDialogField("metadataNotes", "Notes", preferences.CharacterNotes, "Notes", true)
            ],
            Actions:
            [
                new DesktopDialogAction("apply_metadata", "Apply", true),
                new DesktopDialogAction("cancel", "Cancel")
            ]);
    }

    public DesktopDialogState CreateCommandDialog(
        string commandId,
        CharacterProfileSection? profile,
        DesktopPreferenceState preferences,
        string? activeSectionJson,
        CharacterWorkspaceId? currentWorkspace)
    {
        string name = profile?.Name ?? "(none)";
        string alias = profile?.Alias ?? string.Empty;
        string workspace = currentWorkspace?.Value ?? "(none)";

        return commandId switch
        {
            "open_character" => CreateOpenCharacterDialog(
                "dialog.open_character",
                "Open Character",
                "Paste Chummer XML to import into a workspace."),
            "open_for_printing" => CreateOpenCharacterDialog(
                "dialog.open_for_printing",
                "Open for Printing",
                "Paste Chummer XML to stage print workflows."),
            "open_for_export" => CreateOpenCharacterDialog(
                "dialog.open_for_export",
                "Open for Export",
                "Paste Chummer XML to stage export workflows."),
            "print_setup" => new DesktopDialogState(
                "dialog.print_setup",
                "Print Setup",
                "Printer setup is delegated to host/browser print capabilities.",
                [
                    new DesktopDialogField("printLandscape", "Landscape", "false", "false", InputType: "checkbox"),
                    new DesktopDialogField("printBackground", "Print background graphics", "true", "true", InputType: "checkbox")
                ],
                [
                    new DesktopDialogAction("ok", "OK", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "dice_roller" => new DesktopDialogState(
                "dialog.dice_roller",
                "Dice Roller",
                "Enter an expression and execute roll from this dialog.",
                [new DesktopDialogField("diceExpression", "Expression", "12d6", "12d6")],
                [
                    new DesktopDialogAction("roll", "Roll", true),
                    new DesktopDialogAction("close", "Close")
                ]),
            "global_settings" => new DesktopDialogState(
                "dialog.global_settings",
                "Global Settings",
                null,
                [
                    new DesktopDialogField("globalUiScale", "UI Scale (%)", preferences.UiScalePercent.ToString(), "100", InputType: "number"),
                    new DesktopDialogField("globalTheme", "Theme", preferences.Theme, "classic"),
                    new DesktopDialogField("globalLanguage", "Language", preferences.Language, "en-us"),
                    new DesktopDialogField("globalCompactMode", "Compact Mode", preferences.CompactMode ? "true" : "false", "false", InputType: "checkbox")
                ],
                [
                    new DesktopDialogAction("save", "Save", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "character_settings" => new DesktopDialogState(
                "dialog.character_settings",
                "Character Settings",
                null,
                [
                    new DesktopDialogField("characterPriority", "Priority System", preferences.CharacterPriority, "SumToTen"),
                    new DesktopDialogField("characterKarmaNuyen", "Karma/Nuyen Ratio", preferences.KarmaNuyenRatio.ToString(), "2", InputType: "number"),
                    new DesktopDialogField("characterHouseRulesEnabled", "Enable House Rules", preferences.HouseRulesEnabled ? "true" : "false", "false", InputType: "checkbox"),
                    new DesktopDialogField("characterNotes", "Character Notes", preferences.CharacterNotes, "notes", true)
                ],
                [
                    new DesktopDialogAction("save", "Save", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "translator" => new DesktopDialogState(
                "dialog.translator",
                "Translator",
                "Language catalog preview.",
                [
                    new DesktopDialogField("translatorSearch", "Language Search", string.Empty, "filter languages"),
                    new DesktopDialogField("lang1", "English", "en-us", "en-us", IsReadOnly: true),
                    new DesktopDialogField("lang2", "Deutsch", "de-de", "de-de", IsReadOnly: true),
                    new DesktopDialogField("lang3", "Francais", "fr-fr", "fr-fr", IsReadOnly: true)
                ],
                [new DesktopDialogAction("close", "Close", true)]),
            "xml_editor" => new DesktopDialogState(
                "dialog.xml_editor",
                "XML Editor",
                "Edit/import flow in this head is file-first; this is a debug preview.",
                [new DesktopDialogField("xmlEditorDialog", "XML", activeSectionJson ?? "<character />", "<character />", true)],
                [
                    new DesktopDialogAction("apply", "Apply", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "master_index" => new DesktopDialogState(
                "dialog.master_index",
                "Master Index",
                "Catalog data is served by the API and surfaced here in desktop parity mode.",
                [new DesktopDialogField("root", "Data Root", "/app/data", "/app/data", IsReadOnly: true)],
                [new DesktopDialogAction("close", "Close", true)]),
            "character_roster" => new DesktopDialogState(
                "dialog.character_roster",
                "Character Roster",
                "Roster persistence is managed by the shared API store.",
                [
                    new DesktopDialogField("name", "Name", name, name),
                    new DesktopDialogField("alias", "Alias", alias, alias),
                    new DesktopDialogField("workspace", "Workspace", workspace, workspace, IsReadOnly: true)
                ],
                [new DesktopDialogAction("close", "Close", true)]),
            "data_exporter" => new DesktopDialogState(
                "dialog.data_exporter",
                "Data Exporter",
                "Export pipeline is routed through API tool endpoints.",
                [new DesktopDialogField("dataExportPreview", "Export Preview", $"Workspace: {workspace}", "{}", true, true)],
                [
                    new DesktopDialogAction("download", "Download", true),
                    new DesktopDialogAction("close", "Close")
                ]),
            "export_character" => new DesktopDialogState(
                "dialog.export_character",
                "Export Character",
                "Export selected character bundle.",
                [new DesktopDialogField("dataExportPreview", "Export Preview", $"Workspace: {workspace}", "{}", true, true)],
                [
                    new DesktopDialogAction("download", "Download", true),
                    new DesktopDialogAction("close", "Close")
                ]),
            "report_bug" => new DesktopDialogState(
                "dialog.report_bug",
                "Report Bug",
                "Open the issue form in your browser: https://github.com/chummer5a/chummer5a/issues/new/choose",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "about" => new DesktopDialogState(
                "dialog.about",
                "About Chummer",
                "Dual-head preview over shared presenter/API behavior path.",
                [
                    new DesktopDialogField("runtime", "Runtime", "net10.0", "net10.0", IsReadOnly: true),
                    new DesktopDialogField("workspace", "Workspace", workspace, workspace, IsReadOnly: true)
                ],
                [new DesktopDialogAction("close", "Close", true)]),
            "hero_lab_importer" => new DesktopDialogState(
                "dialog.hero_lab_importer",
                "Hero Lab Importer",
                "Paste Hero Lab XML payload to import using compatibility mode.",
                [
                    new DesktopDialogField("heroLabSource", "Input File", ".por/.xml", ".por/.xml"),
                    CreateRulesetField(),
                    new DesktopDialogField(
                        "heroLabXml",
                        "Hero Lab XML",
                        "<character><name>Hero Lab Import</name></character>",
                        "<character><name>Hero Lab Import</name></character>",
                        IsMultiline: true)
                ],
                [
                    new DesktopDialogAction("import", "Import", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "new_window" => new DesktopDialogState(
                "dialog.new_window",
                "New Window",
                "Open a second shell instance from your platform runtime.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "close_window" => new DesktopDialogState(
                "dialog.close_window",
                "Close Window",
                "Close-window action is host/platform specific.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "wiki" => new DesktopDialogState(
                "dialog.wiki",
                "Wiki",
                "https://github.com/chummer5a/chummer5a/wiki",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "discord" => new DesktopDialogState(
                "dialog.discord",
                "Discord",
                "https://discord.gg/EV44Mya",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "revision_history" => new DesktopDialogState(
                "dialog.revision_history",
                "Revision History",
                "https://github.com/chummer5a/chummer5a/releases",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "dumpshock" => new DesktopDialogState(
                "dialog.dumpshock",
                "Dumpshock Thread",
                "https://forums.dumpshock.com/index.php?showtopic=37464",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "print_character" => new DesktopDialogState(
                "dialog.print_character",
                "Print Character",
                "Print preview is rendered by host/browser print facilities.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "print_multiple" => new DesktopDialogState(
                "dialog.print_multiple",
                "Print Multiple",
                "Batch print is available through roster and print endpoints.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "update" => new DesktopDialogState(
                "dialog.update",
                "Check for Updates",
                "Update channel status can be checked from the service layer.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            _ => new DesktopDialogState(
                "dialog.generic",
                commandId,
                $"Command '{commandId}' is recognized but has no dedicated dialog template yet.",
                [],
                [new DesktopDialogAction("close", "Close", true)])
        };
    }

    private static DesktopDialogState CreateOpenCharacterDialog(
        string id,
        string title,
        string message)
    {
        const string defaultXml = "<character><name>Imported Runner</name></character>";

        return new DesktopDialogState(
            Id: id,
            Title: title,
            Message: message,
            Fields:
            [
                CreateRulesetField(),
                new DesktopDialogField(
                    Id: "openCharacterXml",
                    Label: "Character XML",
                    Value: defaultXml,
                    Placeholder: defaultXml,
                    IsMultiline: true)
            ],
            Actions:
            [
                new DesktopDialogAction("import", "Import", true),
                new DesktopDialogAction("cancel", "Cancel")
            ]);
    }

    private static DesktopDialogField CreateRulesetField()
    {
        return new DesktopDialogField(
            Id: "importRulesetId",
            Label: "Ruleset",
            Value: RulesetDefaults.Sr5,
            Placeholder: RulesetDefaults.Sr5);
    }

    public DesktopDialogState CreateUiControlDialog(
        string controlId,
        DesktopPreferenceState preferences)
    {
        return controlId switch
        {
            "create_entry" => new DesktopDialogState(
                "dialog.ui.create_entry",
                "Add Entry",
                null,
                [new DesktopDialogField("uiCreateEntryName", "Entry Name", string.Empty, "New entry")],
                [
                    new DesktopDialogAction("add", "Add", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "edit_entry" => new DesktopDialogState(
                "dialog.ui.edit_entry",
                "Edit Entry",
                null,
                [new DesktopDialogField("uiEditEntryName", "Entry Name", "Current Entry", "Current Entry")],
                [
                    new DesktopDialogAction("apply", "Apply", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "delete_entry" => new DesktopDialogState(
                "dialog.ui.delete_entry",
                "Delete Entry",
                "Delete selected entry?",
                [],
                [
                    new DesktopDialogAction("delete", "Delete", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "open_notes" => new DesktopDialogState(
                "dialog.ui.open_notes",
                "Notes",
                null,
                [new DesktopDialogField("uiNotesEditor", "Notes", preferences.CharacterNotes, "notes", true)],
                [
                    new DesktopDialogAction("save", "Save", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "move_up" => new DesktopDialogState(
                "dialog.ui.move_up",
                "Move Up",
                "Moved selection up.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "move_down" => new DesktopDialogState(
                "dialog.ui.move_down",
                "Move Down",
                "Moved selection down.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "toggle_free_paid" => new DesktopDialogState(
                "dialog.ui.toggle_free_paid",
                "Free/Paid",
                "Toggled free/paid state for selected item.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "show_source" => new DesktopDialogState(
                "dialog.ui.show_source",
                "Source",
                "Source book and page metadata is shown here.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "gear_add" => new DesktopDialogState(
                "dialog.ui.gear_add",
                "Add Gear",
                null,
                [new DesktopDialogField("uiGearName", "Gear Name", string.Empty, "Ares Predator")],
                [
                    new DesktopDialogAction("add", "Add", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "gear_edit" => new DesktopDialogState(
                "dialog.ui.gear_edit",
                "Edit Gear",
                null,
                [new DesktopDialogField("uiGearEditName", "Gear Name", "Selected Gear", "Selected Gear")],
                [
                    new DesktopDialogAction("apply", "Apply", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "gear_delete" => new DesktopDialogState(
                "dialog.ui.gear_delete",
                "Delete Gear",
                "Deleted selected gear item.",
                [],
                [
                    new DesktopDialogAction("delete", "Delete", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "gear_mount" => new DesktopDialogState(
                "dialog.ui.gear_mount",
                "Mount Gear",
                "Mounted selected gear on compatible host.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "gear_source" => new DesktopDialogState(
                "dialog.ui.gear_source",
                "Gear Source",
                "Gear source references are displayed here.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "magic_add" => new DesktopDialogState(
                "dialog.ui.magic_add",
                "Add Spell/Power",
                null,
                [new DesktopDialogField("uiMagicName", "Name", string.Empty, "Spell or Power")],
                [
                    new DesktopDialogAction("add", "Add", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "magic_delete" => new DesktopDialogState(
                "dialog.ui.magic_delete",
                "Delete Spell/Power",
                "Removed selected spell/power.",
                [],
                [
                    new DesktopDialogAction("delete", "Delete", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "magic_bind" => new DesktopDialogState(
                "dialog.ui.magic_bind",
                "Bind/Link",
                "Bind/link workflow started for selected magical item.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "magic_source" => new DesktopDialogState(
                "dialog.ui.magic_source",
                "Magic Source",
                "Magical source references are displayed here.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "skill_add" => new DesktopDialogState(
                "dialog.ui.skill_add",
                "Add Skill",
                null,
                [new DesktopDialogField("uiSkillName", "Skill", string.Empty, "Perception")],
                [
                    new DesktopDialogAction("add", "Add", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "skill_specialize" => new DesktopDialogState(
                "dialog.ui.skill_specialize",
                "Specialize Skill",
                null,
                [new DesktopDialogField("uiSkillSpec", "Specialization", string.Empty, "Visual")],
                [
                    new DesktopDialogAction("apply", "Apply", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "skill_remove" => new DesktopDialogState(
                "dialog.ui.skill_remove",
                "Remove Skill",
                "Removed selected skill.",
                [],
                [
                    new DesktopDialogAction("delete", "Delete", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "skill_group" => new DesktopDialogState(
                "dialog.ui.skill_group",
                "Skill Group",
                "Opened skill group assignment.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "combat_add_weapon" => new DesktopDialogState(
                "dialog.ui.combat_add_weapon",
                "Add Weapon",
                null,
                [new DesktopDialogField("uiWeaponName", "Weapon", string.Empty, "Colt M23")],
                [
                    new DesktopDialogAction("add", "Add", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "combat_add_armor" => new DesktopDialogState(
                "dialog.ui.combat_add_armor",
                "Add Armor",
                null,
                [new DesktopDialogField("uiArmorName", "Armor", string.Empty, "Armor Jacket")],
                [
                    new DesktopDialogAction("add", "Add", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "combat_reload" => new DesktopDialogState(
                "dialog.ui.combat_reload",
                "Reload Weapon",
                "Reloaded selected weapon.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "combat_damage_track" => new DesktopDialogState(
                "dialog.ui.combat_damage_track",
                "Damage Track",
                "Applied one damage track step.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "contact_add" => new DesktopDialogState(
                "dialog.ui.contact_add",
                "Add Contact",
                null,
                [new DesktopDialogField("uiContactName", "Name", string.Empty, "Contact Name")],
                [
                    new DesktopDialogAction("add", "Add", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "contact_edit" => new DesktopDialogState(
                "dialog.ui.contact_edit",
                "Edit Contact",
                null,
                [new DesktopDialogField("uiContactEditName", "Name", "Selected Contact", "Selected Contact")],
                [
                    new DesktopDialogAction("apply", "Apply", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "contact_remove" => new DesktopDialogState(
                "dialog.ui.contact_remove",
                "Remove Contact",
                "Removed selected contact.",
                [],
                [
                    new DesktopDialogAction("delete", "Delete", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "contact_connection" => new DesktopDialogState(
                "dialog.ui.contact_connection",
                "Connection / Loyalty",
                null,
                [
                    new DesktopDialogField("uiContactConnection", "Connection", "3", "3", InputType: "number"),
                    new DesktopDialogField("uiContactLoyalty", "Loyalty", "3", "3", InputType: "number")
                ],
                [
                    new DesktopDialogAction("apply", "Apply", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            _ => new DesktopDialogState(
                "dialog.ui.generic",
                "Desktop Control",
                $"Desktop control '{controlId}' triggered.",
                [],
                [new DesktopDialogAction("close", "Close", true)])
        };
    }
}
