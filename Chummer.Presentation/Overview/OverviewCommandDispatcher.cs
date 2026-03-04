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
            context.Publish(context.State with
            {
                Error = null,
                Notice = "Use the file import action in this head to open a character document."
            });
            return;
        }

        if (OverviewCommandPolicy.IsDialogCommand(commandId))
        {
            context.Publish(context.State with
            {
                Error = null,
                ActiveDialog = context.DialogFactory.CreateCommandDialog(
                    commandId,
                    context.State.Profile,
                    context.State.Preferences,
                    context.State.ActiveSectionJson,
                    context.CurrentWorkspace)
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
            case "save_character_as":
                await context.SaveAsync(ct);
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
}
