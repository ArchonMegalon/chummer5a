using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Shell;

public sealed class ShellPresenter : IShellPresenter
{
    private static readonly string[] MenuOrder = ["file", "edit", "special", "tools", "windows", "help"];
    private readonly IChummerClient _runtimeClient;
    private readonly IShellBootstrapDataProvider _bootstrapDataProvider;

    public ShellPresenter(IChummerClient client, IShellBootstrapDataProvider? bootstrapDataProvider = null)
    {
        _runtimeClient = client;
        _bootstrapDataProvider = bootstrapDataProvider ?? new ShellBootstrapDataProvider(client);
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
            ShellBootstrapData bootstrap = await _bootstrapDataProvider.GetAsync(ct);
            ShellWorkspaceState[] openWorkspaces = MapWorkspaces(bootstrap.Workspaces);
            string preferredRulesetId = RulesetDefaults.Normalize(State.PreferredRulesetId);
            CharacterWorkspaceId? activeWorkspaceId = ResolveActiveWorkspaceId(
                requestedActiveWorkspaceId: null,
                openWorkspaces);
            string activeRulesetId = ResolveRulesetForActiveWorkspace(activeWorkspaceId, openWorkspaces, preferredRulesetId);
            if (!string.Equals(RulesetDefaults.Normalize(bootstrap.RulesetId), activeRulesetId, StringComparison.Ordinal))
            {
                bootstrap = await _bootstrapDataProvider.GetAsync(activeRulesetId, ct);
            }

            IReadOnlyList<AppCommandDefinition> commands = bootstrap.Commands;
            IReadOnlyList<NavigationTabDefinition> tabs = bootstrap.NavigationTabs;

            AppCommandDefinition[] menuRoots = BuildMenuRoots(commands);

            Publish(State with
            {
                IsBusy = false,
                Error = null,
                Notice = openWorkspaces.Length == 0 ? "Shell initialized." : $"Restored {openWorkspaces.Length} workspace(s).",
                ActiveRulesetId = activeRulesetId,
                PreferredRulesetId = preferredRulesetId,
                ActiveWorkspaceId = activeWorkspaceId,
                OpenWorkspaces = openWorkspaces,
                Commands = commands,
                MenuRoots = menuRoots,
                NavigationTabs = tabs,
                ActiveTabId = ResolveActiveTabId(tabs, currentActiveTabId: State.ActiveTabId),
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

    public async Task SetPreferredRulesetAsync(string rulesetId, CancellationToken ct)
    {
        string preferredRulesetId = RulesetDefaults.Normalize(rulesetId);
        string activeRulesetId = State.ActiveWorkspaceId is null
            ? preferredRulesetId
            : State.ActiveRulesetId;
        bool activeRulesetChanged = !string.Equals(State.ActiveRulesetId, activeRulesetId, StringComparison.Ordinal);
        bool requiresCatalogRefresh = activeRulesetChanged
            || State.Commands.Count == 0
            || State.NavigationTabs.Count == 0;

        IReadOnlyList<AppCommandDefinition> commands = State.Commands;
        IReadOnlyList<NavigationTabDefinition> tabs = State.NavigationTabs;
        if (requiresCatalogRefresh)
        {
            Task<IReadOnlyList<AppCommandDefinition>> commandsTask = _runtimeClient.GetCommandsAsync(activeRulesetId, ct);
            Task<IReadOnlyList<NavigationTabDefinition>> tabsTask = _runtimeClient.GetNavigationTabsAsync(activeRulesetId, ct);
            await Task.WhenAll(commandsTask, tabsTask);
            commands = commandsTask.Result;
            tabs = tabsTask.Result;
        }

        Publish(State with
        {
            Error = null,
            PreferredRulesetId = preferredRulesetId,
            ActiveRulesetId = activeRulesetId,
            Commands = commands,
            MenuRoots = BuildMenuRoots(commands),
            NavigationTabs = tabs,
            ActiveTabId = ResolveActiveTabId(tabs, State.ActiveTabId),
            OpenMenuId = null,
            Notice = $"Preferred ruleset set to '{preferredRulesetId}'."
        });
    }

    public async Task SyncWorkspaceContextAsync(CharacterWorkspaceId? activeWorkspaceId, CancellationToken ct)
    {
        IReadOnlyList<WorkspaceListItem> workspaces = await _runtimeClient.ListWorkspacesAsync(ct);
        ShellWorkspaceState[] openWorkspaces = MapWorkspaces(workspaces);
        string preferredRulesetId = RulesetDefaults.Normalize(State.PreferredRulesetId);
        CharacterWorkspaceId? resolvedActiveWorkspace = ResolveActiveWorkspaceId(activeWorkspaceId, openWorkspaces);
        string activeRulesetId = ResolveRulesetForActiveWorkspace(resolvedActiveWorkspace, openWorkspaces, preferredRulesetId);
        bool rulesetChanged = !string.Equals(State.ActiveRulesetId, activeRulesetId, StringComparison.Ordinal);

        IReadOnlyList<AppCommandDefinition> commands = State.Commands;
        IReadOnlyList<NavigationTabDefinition> tabs = State.NavigationTabs;
        if (rulesetChanged || commands.Count == 0 || tabs.Count == 0)
        {
            Task<IReadOnlyList<AppCommandDefinition>> commandsTask = _runtimeClient.GetCommandsAsync(activeRulesetId, ct);
            Task<IReadOnlyList<NavigationTabDefinition>> tabsTask = _runtimeClient.GetNavigationTabsAsync(activeRulesetId, ct);
            await Task.WhenAll(commandsTask, tabsTask);
            commands = commandsTask.Result;
            tabs = tabsTask.Result;
        }

        Publish(State with
        {
            ActiveRulesetId = activeRulesetId,
            PreferredRulesetId = preferredRulesetId,
            ActiveWorkspaceId = resolvedActiveWorkspace,
            OpenWorkspaces = openWorkspaces,
            Commands = commands,
            MenuRoots = BuildMenuRoots(commands),
            NavigationTabs = tabs,
            ActiveTabId = ResolveActiveTabId(tabs, State.ActiveTabId)
        });
    }

    private bool IsCommandEnabled(AppCommandDefinition command)
    {
        return command.EnabledByDefault
            && (!command.RequiresOpenCharacter || State.ActiveWorkspaceId is not null);
    }

    private static ShellWorkspaceState[] MapWorkspaces(IReadOnlyList<WorkspaceListItem> workspaces)
    {
        return workspaces
            .Select(workspace => new ShellWorkspaceState(
                Id: workspace.Id,
                Name: string.IsNullOrWhiteSpace(workspace.Summary.Name) ? "(Unnamed Character)" : workspace.Summary.Name,
                Alias: workspace.Summary.Alias ?? string.Empty,
                LastOpenedUtc: workspace.LastUpdatedUtc,
                RulesetId: RulesetDefaults.Normalize(workspace.RulesetId)))
            .OrderByDescending(workspace => workspace.LastOpenedUtc)
            .ToArray();
    }

    private static AppCommandDefinition[] BuildMenuRoots(IReadOnlyList<AppCommandDefinition> commands)
    {
        return commands
            .Where(command => string.Equals(command.Group, "menu", StringComparison.Ordinal))
            .OrderBy(command => MenuSortIndex(command.Id))
            .ThenBy(command => command.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static CharacterWorkspaceId? ResolveActiveWorkspaceId(
        CharacterWorkspaceId? requestedActiveWorkspaceId,
        IReadOnlyList<ShellWorkspaceState> openWorkspaces)
    {
        if (requestedActiveWorkspaceId is null)
            return openWorkspaces.FirstOrDefault()?.Id;

        bool exists = openWorkspaces.Any(workspace => WorkspaceIdsEqual(workspace.Id, requestedActiveWorkspaceId.Value));
        return exists
            ? requestedActiveWorkspaceId
            : openWorkspaces.FirstOrDefault()?.Id;
    }

    private static string ResolveRulesetForActiveWorkspace(
        CharacterWorkspaceId? activeWorkspaceId,
        IReadOnlyList<ShellWorkspaceState> openWorkspaces,
        string preferredRulesetId)
    {
        if (activeWorkspaceId is null)
            return RulesetDefaults.Normalize(preferredRulesetId);

        ShellWorkspaceState? workspace = openWorkspaces.FirstOrDefault(candidate => WorkspaceIdsEqual(candidate.Id, activeWorkspaceId.Value));
        return workspace is null
            ? RulesetDefaults.Normalize(preferredRulesetId)
            : RulesetDefaults.Normalize(workspace.RulesetId);
    }

    private static string? ResolveActiveTabId(IReadOnlyList<NavigationTabDefinition> tabs, string? currentActiveTabId)
    {
        if (!string.IsNullOrWhiteSpace(currentActiveTabId)
            && tabs.Any(tab => tab.EnabledByDefault && string.Equals(tab.Id, currentActiveTabId, StringComparison.Ordinal)))
        {
            return currentActiveTabId;
        }

        return tabs.FirstOrDefault(tab => tab.EnabledByDefault)?.Id;
    }

    private static int MenuSortIndex(string id)
    {
        int index = Array.IndexOf(MenuOrder, id);
        return index < 0 ? int.MaxValue : index;
    }

    private static bool WorkspaceIdsEqual(CharacterWorkspaceId left, CharacterWorkspaceId right)
    {
        return string.Equals(left.Value, right.Value, StringComparison.Ordinal);
    }

    private void Publish(ShellState nextState)
    {
        State = nextState;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
