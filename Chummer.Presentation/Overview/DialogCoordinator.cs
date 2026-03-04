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

        if (string.Equals(dialog.Id, "dialog.character_settings", StringComparison.Ordinal) && string.Equals(actionId, "save", StringComparison.Ordinal))
        {
            ApplyCharacterSettings(dialog, context);
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.open_notes", StringComparison.Ordinal) && string.Equals(actionId, "save", StringComparison.Ordinal))
        {
            string notes = DesktopDialogFieldValueParser.GetValue(dialog, "uiNotesEditor") ?? string.Empty;
            context.Publish(context.State with
            {
                ActiveDialog = null,
                Error = null,
                Preferences = context.State.Preferences with
                {
                    CharacterNotes = notes
                },
                Notice = "Notes saved."
            });
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.contact_connection", StringComparison.Ordinal) && string.Equals(actionId, "apply", StringComparison.Ordinal))
        {
            string connection = DesktopDialogFieldValueParser.GetValue(dialog, "uiContactConnection") ?? "0";
            string loyalty = DesktopDialogFieldValueParser.GetValue(dialog, "uiContactLoyalty") ?? "0";
            context.Publish(context.State with
            {
                ActiveDialog = null,
                Error = null,
                Notice = $"Contact connection/loyalty applied ({connection}/{loyalty})."
            });
            return;
        }

        if ((string.Equals(dialog.Id, "dialog.data_exporter", StringComparison.Ordinal)
            || string.Equals(dialog.Id, "dialog.export_character", StringComparison.Ordinal))
            && string.Equals(actionId, "download", StringComparison.Ordinal))
        {
            context.Publish(context.State with
            {
                ActiveDialog = null,
                Error = null,
                Notice = "Export bundle prepared for download."
            });
            return;
        }

        context.Publish(context.State with
        {
            ActiveDialog = null,
            Error = null,
            Notice = $"{dialog.Title}: action '{actionId}' executed."
        });
    }

    private static void ApplyGlobalSettings(DesktopDialogState dialog, DialogCoordinationContext context)
    {
        int uiScalePercent = DesktopDialogFieldValueParser.ParseInt(dialog, "globalUiScale", context.State.Preferences.UiScalePercent);
        string theme = DesktopDialogFieldValueParser.GetValue(dialog, "globalTheme") ?? context.State.Preferences.Theme;
        string language = DesktopDialogFieldValueParser.GetValue(dialog, "globalLanguage") ?? context.State.Preferences.Language;
        bool compactMode = DesktopDialogFieldValueParser.ParseBool(dialog, "globalCompactMode", context.State.Preferences.CompactMode);

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
            Notice = "Global settings updated."
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
