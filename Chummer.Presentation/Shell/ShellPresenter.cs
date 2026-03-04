using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Shell;

public sealed class ShellPresenter : IShellPresenter
{
    private static readonly string[] MenuOrder = ["file", "edit", "special", "tools", "windows", "help"];
    private readonly IChummerClient _client;

    public ShellPresenter(IChummerClient client)
    {
        _client = client;
    }

    public ShellState State { get; private set; } = ShellState.Empty;

    public event EventHandler? StateChanged;

    public async Task InitializeAsync(CancellationToken ct)
    {
        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            Task<IReadOnlyList<AppCommandDefinition>> commandsTask = _client.GetCommandsAsync(ct);
            Task<IReadOnlyList<NavigationTabDefinition>> tabsTask = _client.GetNavigationTabsAsync(ct);
            Task<IReadOnlyList<WorkspaceListItem>> workspacesTask = _client.ListWorkspacesAsync(ct);
            await Task.WhenAll(commandsTask, tabsTask, workspacesTask);

            IReadOnlyList<AppCommandDefinition> commands = commandsTask.Result;
            IReadOnlyList<NavigationTabDefinition> tabs = tabsTask.Result;

            ShellWorkspaceState[] openWorkspaces = workspacesTask.Result
                .Select(workspace => new ShellWorkspaceState(
                    Id: workspace.Id,
                    Name: string.IsNullOrWhiteSpace(workspace.Summary.Name) ? "(Unnamed Character)" : workspace.Summary.Name,
                    Alias: workspace.Summary.Alias ?? string.Empty,
                    LastOpenedUtc: workspace.LastUpdatedUtc))
                .OrderByDescending(workspace => workspace.LastOpenedUtc)
                .ToArray();

            AppCommandDefinition[] menuRoots = commands
                .Where(command => string.Equals(command.Group, "menu", StringComparison.Ordinal))
                .OrderBy(command => MenuSortIndex(command.Id))
                .ThenBy(command => command.Id, StringComparer.Ordinal)
                .ToArray();

            Publish(State with
            {
                IsBusy = false,
                Error = null,
                Notice = openWorkspaces.Length == 0 ? "Shell initialized." : $"Restored {openWorkspaces.Length} workspace(s).",
                ActiveWorkspaceId = openWorkspaces.Length == 0 ? null : openWorkspaces[0].Id,
                OpenWorkspaces = openWorkspaces,
                Commands = commands,
                MenuRoots = menuRoots,
                NavigationTabs = tabs,
                ActiveTabId = tabs.FirstOrDefault(tab => tab.EnabledByDefault)?.Id,
                OpenMenuId = null
            });
        }
        catch (Exception ex)
        {
            Publish(State with
            {
                IsBusy = false,
                Error = ex.Message
            });
        }
    }

    public Task ExecuteCommandAsync(string commandId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            Publish(State with { Error = "Command id is required." });
            return Task.CompletedTask;
        }

        AppCommandDefinition? command = State.Commands
            .FirstOrDefault(candidate => string.Equals(candidate.Id, commandId, StringComparison.Ordinal));
        if (command is null)
        {
            Publish(State with { Error = $"Unknown command '{commandId}'." });
            return Task.CompletedTask;
        }

        if (!IsCommandEnabled(command))
        {
            Publish(State with { Error = $"Command '{commandId}' is disabled in the current shell state." });
            return Task.CompletedTask;
        }

        if (string.Equals(command.Group, "menu", StringComparison.Ordinal))
        {
            string? nextOpenMenu = string.Equals(State.OpenMenuId, command.Id, StringComparison.Ordinal)
                ? null
                : command.Id;
            Publish(State with
            {
                Error = null,
                LastCommandId = command.Id,
                OpenMenuId = nextOpenMenu,
                Notice = nextOpenMenu is null
                    ? $"Menu '{command.Id}' closed."
                    : $"Menu '{command.Id}' opened."
            });
            return Task.CompletedTask;
        }

        Publish(State with
        {
            Error = null,
            LastCommandId = command.Id,
            OpenMenuId = null,
            Notice = $"Command '{command.Id}' dispatched through shared shell contract."
        });

        return Task.CompletedTask;
    }

    public Task SelectTabAsync(string tabId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tabId))
        {
            Publish(State with { Error = "Tab id is required." });
            return Task.CompletedTask;
        }

        NavigationTabDefinition? tab = State.NavigationTabs
            .FirstOrDefault(candidate => string.Equals(candidate.Id, tabId, StringComparison.Ordinal));
        if (tab is null)
        {
            Publish(State with { Error = $"Unknown tab '{tabId}'." });
            return Task.CompletedTask;
        }

        if (!tab.EnabledByDefault)
        {
            Publish(State with { Error = $"Tab '{tabId}' is disabled." });
            return Task.CompletedTask;
        }

        Publish(State with
        {
            Error = null,
            ActiveTabId = tab.Id,
            OpenMenuId = null,
            Notice = $"Selected tab '{tab.Id}'."
        });

        return Task.CompletedTask;
    }

    public Task ToggleMenuAsync(string menuId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(menuId))
        {
            Publish(State with { Error = "Menu id is required." });
            return Task.CompletedTask;
        }

        bool knownMenu = State.MenuRoots.Any(menu => string.Equals(menu.Id, menuId, StringComparison.Ordinal));
        if (!knownMenu)
        {
            Publish(State with { Error = $"Unknown menu '{menuId}'." });
            return Task.CompletedTask;
        }

        string? nextOpenMenu = string.Equals(State.OpenMenuId, menuId, StringComparison.Ordinal)
            ? null
            : menuId;
        Publish(State with
        {
            Error = null,
            OpenMenuId = nextOpenMenu,
            Notice = nextOpenMenu is null
                ? $"Menu '{menuId}' closed."
                : $"Menu '{menuId}' opened."
        });

        return Task.CompletedTask;
    }

    private bool IsCommandEnabled(AppCommandDefinition command)
    {
        return command.EnabledByDefault
            && (!command.RequiresOpenCharacter || State.ActiveWorkspaceId is not null);
    }

    private static int MenuSortIndex(string id)
    {
        int index = Array.IndexOf(MenuOrder, id);
        return index < 0 ? int.MaxValue : index;
    }

    private void Publish(ShellState nextState)
    {
        State = nextState;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
