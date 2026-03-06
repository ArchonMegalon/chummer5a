using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public sealed class OverviewCommandDispatcher : IOverviewCommandDispatcher
{
    public async Task DispatchAsync(string commandId, OverviewCommandExecutionContext context, CancellationToken ct)
    {
        if (OverviewCommandPolicy.IsMenuCommand(commandId))
        {
            context.Publish(context.State with
            {
                Error = null,
                Notice = $"Menu '{commandId}' is handled by the active UI shell."
            });
            return;
        }

        if (OverviewCommandPolicy.IsImportHintCommand(commandId))
        {
            DesktopDialogState dialog = BuildCommandDialog(commandId, context);
            context.Publish(context.State with
            {
                Error = null,
                ActiveDialog = dialog,
                Notice = $"Import flow ready for '{commandId}'."
            });
            return;
        }

        if (OverviewCommandPolicy.IsDialogCommand(commandId))
        {
            DesktopDialogState dialog = BuildCommandDialog(commandId, context);
            context.Publish(context.State with
            {
                Error = null,
                ActiveDialog = dialog
            });
            return;
        }

        if (OverviewCommandPolicy.IsEditorRelayCommand(commandId))
        {
            context.Publish(context.State with
            {
                Error = null,
                Notice = $"Command '{commandId}' dispatched to the active section editor."
            });
            return;
        }

        switch (commandId)
        {
            case "save_character":
                await context.SaveAsync(ct);
                return;
            case "save_character_as":
                await context.DownloadAsync(ct);
                return;
            case "print_character":
                await context.PrintAsync(ct);
                return;
            case "refresh_character":
                if (context.CurrentWorkspace is null)
                {
                    context.Publish(context.State with { Error = "No workspace loaded." });
                    return;
                }

                await context.LoadAsync(context.CurrentWorkspace.Value, ct);
                return;
            case "new_character":
                context.Publish(context.CreateResetState(commandId, "New character workspace initialized."));
                return;
            case "new_critter":
                context.Publish(context.CreateResetState(commandId, "New critter workspace initialized."));
                return;
            case "close_all":
            case "restart":
                await context.CloseAllAsync(ct, "Workspace reset complete.");
                return;
            case "close_window":
                if (context.CurrentWorkspace is null)
                {
                    context.Publish(context.State with
                    {
                        Error = null,
                        Notice = "No open workspace to close."
                    });
                    return;
                }

                await context.CloseWorkspaceAsync(context.CurrentWorkspace.Value, ct);
                return;
            default:
                context.Publish(context.State with
                {
                    Error = $"Command '{commandId}' is not implemented in shared presenter yet."
                });
                return;
        }
    }

    private static DesktopDialogState BuildCommandDialog(string commandId, OverviewCommandExecutionContext context)
    {
        DesktopDialogState dialog = context.DialogFactory.CreateCommandDialog(
            commandId,
            context.State.Profile,
            context.State.Preferences,
            context.State.ActiveSectionJson,
            context.CurrentWorkspace);

        string activeRulesetId = ResolveActiveRulesetId(context);
        DesktopDialogState importSeeded = SetFieldValue(dialog, "importRulesetId", activeRulesetId);
        return SetFieldValue(importSeeded, "preferredRulesetId", activeRulesetId);
    }

    private static string ResolveActiveRulesetId(OverviewCommandExecutionContext context)
    {
        CharacterWorkspaceId? activeWorkspace = context.CurrentWorkspace;
        if (activeWorkspace is null)
            return RulesetDefaults.Sr5;

        OpenWorkspaceState? workspace = context.State.OpenWorkspaces.FirstOrDefault(
            candidate => string.Equals(candidate.Id.Value, activeWorkspace.Value.Value, StringComparison.Ordinal));
        return workspace is null
            ? RulesetDefaults.Sr5
            : RulesetDefaults.Normalize(workspace.RulesetId);
    }

    private static DesktopDialogState SetFieldValue(DesktopDialogState dialog, string fieldId, string value)
    {
        if (dialog.Fields.Count == 0)
            return dialog;

        bool updated = false;
        DesktopDialogField[] fields = dialog.Fields
            .Select(field =>
            {
                if (!string.Equals(field.Id, fieldId, StringComparison.Ordinal))
                    return field;

                updated = true;
                return field with { Value = value };
            })
            .ToArray();

        return updated
            ? dialog with { Fields = fields }
            : dialog;
    }
}
