using Chummer.Contracts.Api;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
using System.Text.RegularExpressions;

namespace Chummer.Presentation.Overview;

public sealed class DialogCoordinator : IDialogCoordinator
{
    private static readonly Regex DiceExpressionRegex = new(@"^\s*(\d+)d(\d+)([+-]\d+)?\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task CoordinateAsync(string actionId, DialogCoordinationContext context, CancellationToken ct)
    {
        DesktopDialogState? dialog = context.State.ActiveDialog;
        if (dialog is null)
            return;

        if (string.IsNullOrWhiteSpace(actionId))
        {
            context.Publish(context.State with { Error = "Dialog action id is required." });
            return;
        }

        switch (actionId)
        {
            case "cancel":
            case "close":
                context.Publish(context.State with
                {
                    ActiveDialog = null,
                    Error = null
                });
                return;
            default:
                break;
        }

        if (string.Equals(dialog.Id, "dialog.workspace.metadata", StringComparison.Ordinal) && string.Equals(actionId, "apply_metadata", StringComparison.Ordinal))
        {
            await ApplyMetadataDialogAsync(dialog, context, ct);
            return;
        }

        if (string.Equals(dialog.Id, "dialog.open_character", StringComparison.Ordinal)
            && string.Equals(actionId, "import", StringComparison.Ordinal))
        {
            await ImportCharacterDialogAsync(dialog, context, ct);
            return;
        }

        if (string.Equals(dialog.Id, "dialog.open_for_printing", StringComparison.Ordinal)
            && string.Equals(actionId, "import", StringComparison.Ordinal))
        {
            await ImportCharacterDialogAsync(
                dialog,
                context,
                ct,
                successNotice: "Character imported for printing.",
                afterImportAsync: context.PrintAsync);
            return;
        }

        if (string.Equals(dialog.Id, "dialog.open_for_export", StringComparison.Ordinal)
            && string.Equals(actionId, "import", StringComparison.Ordinal))
        {
            await ImportCharacterDialogAsync(
                dialog,
                context,
                ct,
                successNotice: "Character imported for export.",
                afterImportAsync: context.ExportAsync);
            return;
        }

        if (string.Equals(dialog.Id, "dialog.hero_lab_importer", StringComparison.Ordinal)
            && string.Equals(actionId, "import", StringComparison.Ordinal))
        {
            await ImportCharacterDialogAsync(
                dialog,
                context,
                ct,
                fieldId: "heroLabXml",
                requiredError: "Hero Lab XML is required.",
                successNotice: "Hero Lab XML imported.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.dice_roller", StringComparison.Ordinal) && string.Equals(actionId, "roll", StringComparison.Ordinal))
        {
            RollDice(dialog, context);
            return;
        }

        if (string.Equals(dialog.Id, "dialog.global_settings", StringComparison.Ordinal) && string.Equals(actionId, "save", StringComparison.Ordinal))
        {
            ApplyGlobalSettings(dialog, context);
            return;
        }

        if (string.Equals(dialog.Id, "dialog.switch_ruleset", StringComparison.Ordinal) && string.Equals(actionId, "apply_ruleset", StringComparison.Ordinal))
        {
            await ApplyPreferredRulesetAsync(dialog, context, ct);
            return;
        }

        if (string.Equals(dialog.Id, "dialog.character_settings", StringComparison.Ordinal) && string.Equals(actionId, "save", StringComparison.Ordinal))
        {
            ApplyCharacterSettings(dialog, context);
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.open_notes", StringComparison.Ordinal) && string.Equals(actionId, "save", StringComparison.Ordinal))
        {
            string notes = DesktopDialogFieldValueParser.GetValue(dialog, "uiNotesEditor") ?? string.Empty;
            PublishDialogNotice(context, "Notes saved.", context.State.Preferences with
            {
                CharacterNotes = notes
            });
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.create_entry", StringComparison.Ordinal) && string.Equals(actionId, "add", StringComparison.Ordinal))
        {
            string entryName = ReadDialogValue(dialog, "uiCreateEntryName", "New entry");
            PublishDialogNotice(context, $"Entry '{entryName}' added.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.edit_entry", StringComparison.Ordinal) && string.Equals(actionId, "apply", StringComparison.Ordinal))
        {
            string entryName = ReadDialogValue(dialog, "uiEditEntryName", "Current Entry");
            PublishDialogNotice(context, $"Entry renamed to '{entryName}'.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.delete_entry", StringComparison.Ordinal) && string.Equals(actionId, "delete", StringComparison.Ordinal))
        {
            PublishDialogNotice(context, "Entry deleted.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.gear_add", StringComparison.Ordinal) && string.Equals(actionId, "add", StringComparison.Ordinal))
        {
            string gearName = ReadDialogValue(dialog, "uiGearName", "Ares Predator");
            PublishDialogNotice(context, $"Gear '{gearName}' added.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.gear_edit", StringComparison.Ordinal) && string.Equals(actionId, "apply", StringComparison.Ordinal))
        {
            string gearName = ReadDialogValue(dialog, "uiGearEditName", "Selected Gear");
            PublishDialogNotice(context, $"Gear renamed to '{gearName}'.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.gear_delete", StringComparison.Ordinal) && string.Equals(actionId, "delete", StringComparison.Ordinal))
        {
            PublishDialogNotice(context, "Gear deleted.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.magic_add", StringComparison.Ordinal) && string.Equals(actionId, "add", StringComparison.Ordinal))
        {
            string magicName = ReadDialogValue(dialog, "uiMagicName", "Spell or Power");
            PublishDialogNotice(context, $"Spell/power '{magicName}' added.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.magic_delete", StringComparison.Ordinal) && string.Equals(actionId, "delete", StringComparison.Ordinal))
        {
            PublishDialogNotice(context, "Spell/power deleted.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.skill_add", StringComparison.Ordinal) && string.Equals(actionId, "add", StringComparison.Ordinal))
        {
            string skillName = ReadDialogValue(dialog, "uiSkillName", "Perception");
            PublishDialogNotice(context, $"Skill '{skillName}' added.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.skill_specialize", StringComparison.Ordinal) && string.Equals(actionId, "apply", StringComparison.Ordinal))
        {
            string specialization = ReadDialogValue(dialog, "uiSkillSpec", "Visual");
            PublishDialogNotice(context, $"Skill specialization set to '{specialization}'.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.skill_remove", StringComparison.Ordinal) && string.Equals(actionId, "delete", StringComparison.Ordinal))
        {
            PublishDialogNotice(context, "Skill removed.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.combat_add_weapon", StringComparison.Ordinal) && string.Equals(actionId, "add", StringComparison.Ordinal))
        {
            string weaponName = ReadDialogValue(dialog, "uiWeaponName", "Colt M23");
            PublishDialogNotice(context, $"Weapon '{weaponName}' added.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.combat_add_armor", StringComparison.Ordinal) && string.Equals(actionId, "add", StringComparison.Ordinal))
        {
            string armorName = ReadDialogValue(dialog, "uiArmorName", "Armor Jacket");
            PublishDialogNotice(context, $"Armor '{armorName}' added.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.contact_add", StringComparison.Ordinal) && string.Equals(actionId, "add", StringComparison.Ordinal))
        {
            string contactName = ReadDialogValue(dialog, "uiContactName", "Contact Name");
            PublishDialogNotice(context, $"Contact '{contactName}' added.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.contact_edit", StringComparison.Ordinal) && string.Equals(actionId, "apply", StringComparison.Ordinal))
        {
            string contactName = ReadDialogValue(dialog, "uiContactEditName", "Selected Contact");
            PublishDialogNotice(context, $"Contact renamed to '{contactName}'.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.contact_remove", StringComparison.Ordinal) && string.Equals(actionId, "delete", StringComparison.Ordinal))
        {
            PublishDialogNotice(context, "Contact removed.");
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.contact_connection", StringComparison.Ordinal) && string.Equals(actionId, "apply", StringComparison.Ordinal))
        {
            string connection = DesktopDialogFieldValueParser.GetValue(dialog, "uiContactConnection") ?? "0";
            string loyalty = DesktopDialogFieldValueParser.GetValue(dialog, "uiContactLoyalty") ?? "0";
            PublishDialogNotice(context, $"Contact connection/loyalty applied ({connection}/{loyalty}).");
            return;
        }

        if ((string.Equals(dialog.Id, "dialog.data_exporter", StringComparison.Ordinal)
            || string.Equals(dialog.Id, "dialog.export_character", StringComparison.Ordinal))
            && string.Equals(actionId, "download", StringComparison.Ordinal))
        {
            if (context.ExportAsync is null)
            {
                PublishDialogNotice(context, "Export bundle prepared for download.");
                return;
            }

            await context.ExportAsync(ct);
            return;
        }

        if (string.Equals(dialog.Id, "dialog.print_character", StringComparison.Ordinal)
            && string.Equals(actionId, "print", StringComparison.Ordinal))
        {
            if (context.PrintAsync is null)
            {
                PublishDialogNotice(context, "Print preview prepared.");
                return;
            }

            await context.PrintAsync(ct);
            return;
        }

        context.Publish(context.State with
        {
            ActiveDialog = null,
            Error = null,
            Notice = $"{dialog.Title}: action '{actionId}' executed."
        });
    }

    private static async Task ApplyPreferredRulesetAsync(
        DesktopDialogState dialog,
        DialogCoordinationContext context,
        CancellationToken ct)
    {
        string? rulesetId = ReadRequiredRuleset(dialog, context, "preferredRulesetId", "Preferred ruleset is required.");
        if (rulesetId is null)
            return;

        if (context.SetPreferredRulesetAsync is null)
        {
            PublishDialogNotice(context, $"Preferred ruleset set to '{rulesetId}'.");
            return;
        }

        await context.SetPreferredRulesetAsync(rulesetId, ct);

        CharacterOverviewState stateAfterUpdate = context.GetState();
        if (stateAfterUpdate.Error is null)
        {
            context.Publish(stateAfterUpdate with
            {
                ActiveDialog = null,
                Error = null,
                Notice = $"Preferred ruleset set to '{rulesetId}'."
            });
        }
    }

    private static async Task ImportCharacterDialogAsync(
        DesktopDialogState dialog,
        DialogCoordinationContext context,
        CancellationToken ct,
        string fieldId = "openCharacterXml",
        string rulesetFieldId = "importRulesetId",
        string requiredError = "Character XML is required.",
        string successNotice = "Character imported.",
        Func<CancellationToken, Task>? afterImportAsync = null)
    {
        string xml = DesktopDialogFieldValueParser.GetValue(dialog, fieldId) ?? string.Empty;
        string? rulesetId = ReadRequiredRuleset(dialog, context, rulesetFieldId, "Ruleset is required.");
        if (string.IsNullOrWhiteSpace(xml))
        {
            context.Publish(context.State with
            {
                Error = requiredError,
                Notice = null
            });
            return;
        }

        if (rulesetId is null)
            return;

        await context.ImportAsync(new WorkspaceImportDocument(xml, rulesetId, WorkspaceDocumentFormat.NativeXml), ct);

        CharacterOverviewState stateAfterImport = context.GetState();
        if (stateAfterImport.Error is not null)
        {
            return;
        }

        if (afterImportAsync is not null)
        {
            await afterImportAsync(ct);
            CharacterOverviewState stateAfterFollowUp = context.GetState();
            if (stateAfterFollowUp.Error is null && stateAfterFollowUp.ActiveDialog is not null)
            {
                context.Publish(stateAfterFollowUp with
                {
                    ActiveDialog = null,
                    Error = null
                });
            }

            return;
        }

        context.Publish(stateAfterImport with
        {
            ActiveDialog = null,
            Error = null,
            Notice = successNotice
        });
    }

    private static string? ReadRequiredRuleset(
        DesktopDialogState dialog,
        DialogCoordinationContext context,
        string fieldId,
        string requiredError)
    {
        string? rulesetId = RulesetDefaults.NormalizeOptional(DesktopDialogFieldValueParser.GetValue(dialog, fieldId));
        if (!string.IsNullOrWhiteSpace(rulesetId))
            return rulesetId;

        context.Publish(context.State with
        {
            Error = requiredError,
            Notice = null
        });
        return null;
    }

    private static void ApplyGlobalSettings(DesktopDialogState dialog, DialogCoordinationContext context)
    {
        int uiScalePercent = DesktopDialogFieldValueParser.ParseInt(dialog, "globalUiScale", context.State.Preferences.UiScalePercent);
        string theme = DesktopDialogFieldValueParser.GetValue(dialog, "globalTheme") ?? context.State.Preferences.Theme;
        string requestedLanguage = DesktopDialogFieldValueParser.GetValue(dialog, "globalLanguage") ?? context.State.Preferences.Language;
        string language = TranslatorLanguageCatalog.NormalizeOrFallback(requestedLanguage);
        bool compactMode = DesktopDialogFieldValueParser.ParseBool(dialog, "globalCompactMode", context.State.Preferences.CompactMode);
        bool languageChanged = !string.Equals(language, context.State.Preferences.Language, StringComparison.OrdinalIgnoreCase);
        bool fallbackApplied = !string.Equals(language, requestedLanguage, StringComparison.OrdinalIgnoreCase);

        string notice = "Global settings updated.";
        if (languageChanged)
        {
            notice = "Global settings updated. Restart the desktop client to apply the language change.";
        }
        if (fallbackApplied)
        {
            notice = $"Global settings updated. Unsupported language fell back to {TranslatorLanguageCatalog.FallbackCode}. Restart the desktop client to apply the language change.";
        }

        context.Publish(context.State with
        {
            ActiveDialog = null,
            Error = null,
            Preferences = context.State.Preferences with
            {
                UiScalePercent = uiScalePercent,
                Theme = theme,
                Language = language,
                CompactMode = compactMode
            },
            Notice = notice
        });
    }

    private static void ApplyCharacterSettings(DesktopDialogState dialog, DialogCoordinationContext context)
    {
        string priority = DesktopDialogFieldValueParser.GetValue(dialog, "characterPriority") ?? context.State.Preferences.CharacterPriority;
        int karmaNuyenRatio = DesktopDialogFieldValueParser.ParseInt(dialog, "characterKarmaNuyen", context.State.Preferences.KarmaNuyenRatio);
        bool houseRules = DesktopDialogFieldValueParser.ParseBool(dialog, "characterHouseRulesEnabled", context.State.Preferences.HouseRulesEnabled);
        string notes = DesktopDialogFieldValueParser.GetValue(dialog, "characterNotes") ?? context.State.Preferences.CharacterNotes;

        context.Publish(context.State with
        {
            ActiveDialog = null,
            Error = null,
            Build = context.State.Build is null ? null : context.State.Build with { BuildMethod = priority },
            Preferences = context.State.Preferences with
            {
                CharacterPriority = priority,
                KarmaNuyenRatio = karmaNuyenRatio,
                HouseRulesEnabled = houseRules,
                CharacterNotes = notes
            },
            Notice = "Character settings updated."
        });
    }

    private static async Task ApplyMetadataDialogAsync(DesktopDialogState dialog, DialogCoordinationContext context, CancellationToken ct)
    {
        string? name = DesktopDialogFieldValueParser.GetValue(dialog, "metadataName");
        string? alias = DesktopDialogFieldValueParser.GetValue(dialog, "metadataAlias");
        string? notes = DesktopDialogFieldValueParser.GetValue(dialog, "metadataNotes");
        string? normalizedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes;

        await context.UpdateMetadataAsync(new UpdateWorkspaceMetadata(
            Name: string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
            Alias: string.IsNullOrWhiteSpace(alias) ? null : alias.Trim(),
            Notes: normalizedNotes), ct);

        CharacterOverviewState stateAfterUpdate = context.GetState();
        if (stateAfterUpdate.Error is null)
        {
            context.Publish(stateAfterUpdate with
            {
                ActiveDialog = null,
                Error = null,
                Notice = "Metadata updated."
            });
        }
    }

    private static void RollDice(DesktopDialogState dialog, DialogCoordinationContext context)
    {
        string expression = DesktopDialogFieldValueParser.GetValue(dialog, "diceExpression") ?? "1d6";
        if (!TryRollExpression(expression, out int total, out int hits, out string error))
        {
            context.Publish(context.State with { Error = error });
            return;
        }

        string summary = $"{expression} => total {total}, hits {hits}";
        List<DesktopDialogField> fields = dialog.Fields
            .Where(field => !string.Equals(field.Id, "diceResult", StringComparison.Ordinal))
            .ToList();
        fields.Add(new DesktopDialogField(
            Id: "diceResult",
            Label: "Last Result",
            Value: summary,
            Placeholder: summary,
            IsMultiline: false,
            IsReadOnly: true));

        context.Publish(context.State with
        {
            Error = null,
            Notice = summary,
            ActiveDialog = dialog with
            {
                Message = "Expression rolled using Shadowrun-style d6 hits.",
                Fields = fields
            }
        });
    }

    private static void PublishDialogNotice(
        DialogCoordinationContext context,
        string notice,
        DesktopPreferenceState? preferences = null)
    {
        context.Publish(context.State with
        {
            ActiveDialog = null,
            Error = null,
            Preferences = preferences ?? context.State.Preferences,
            Notice = notice
        });
    }

    private static string ReadDialogValue(
        DesktopDialogState dialog,
        string fieldId,
        string fallback)
    {
        string? value = DesktopDialogFieldValueParser.GetValue(dialog, fieldId);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static bool TryRollExpression(string expression, out int total, out int hits, out string error)
    {
        total = 0;
        hits = 0;
        error = string.Empty;

        Match match = DiceExpressionRegex.Match(expression);
        if (!match.Success)
        {
            error = "Dice expression must match NdM with optional +K modifier (example: 12d6+2).";
            return false;
        }

        int count = int.Parse(match.Groups[1].Value);
        int sides = int.Parse(match.Groups[2].Value);
        int modifier = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

        if (count < 1 || count > 100 || sides < 2 || sides > 100)
        {
            error = "Dice expression is outside supported limits.";
            return false;
        }

        for (int index = 0; index < count; index++)
        {
            int value = Random.Shared.Next(1, sides + 1);
            total += value;
            if (sides == 6 && value >= 5)
            {
                hits++;
            }
        }

        total += modifier;
        if (sides != 6)
        {
            hits = 0;
        }

        return true;
    }
}
