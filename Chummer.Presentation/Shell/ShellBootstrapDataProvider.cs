using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
using System.Diagnostics.CodeAnalysis;

namespace Chummer.Presentation.Shell;

public sealed class ShellBootstrapDataProvider : IShellBootstrapDataProvider
{
    private static readonly TimeSpan BootstrapCacheWindow = TimeSpan.FromSeconds(10);
    private readonly IChummerClient _client;
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly Dictionary<string, CachedCatalogData> _cachedCatalogsByRuleset = new(StringComparer.Ordinal);
    private CachedWorkspaceData? _cachedWorkspaces;

    public ShellBootstrapDataProvider(IChummerClient client)
    {
        _client = client;
    }

    public async Task<ShellBootstrapData> GetAsync(CancellationToken ct)
    {
        return await GetAsync(rulesetId: null, ct);
    }

    public async Task<ShellBootstrapData> GetAsync(string? rulesetId, CancellationToken ct)
    {
        string effectiveRulesetId = RulesetDefaults.Normalize(rulesetId);
        if (TryGetCachedCatalog(effectiveRulesetId, out CachedCatalogData? cachedCatalog)
            && TryGetCachedWorkspaces(out CachedWorkspaceData? cachedWorkspaces))
        {
            return new ShellBootstrapData(cachedCatalog.Commands, cachedCatalog.NavigationTabs, cachedWorkspaces.Workspaces);
        }

        await _sync.WaitAsync(ct);
        try
        {
            if (TryGetCachedCatalog(effectiveRulesetId, out cachedCatalog)
                && TryGetCachedWorkspaces(out cachedWorkspaces))
            {
                return new ShellBootstrapData(cachedCatalog.Commands, cachedCatalog.NavigationTabs, cachedWorkspaces.Workspaces);
            }

            Task<IReadOnlyList<AppCommandDefinition>>? commandsTask = null;
            Task<IReadOnlyList<NavigationTabDefinition>>? tabsTask = null;
            Task<IReadOnlyList<WorkspaceListItem>>? workspacesTask = null;
            List<Task> pendingTasks = [];

            if (!TryGetCachedCatalog(effectiveRulesetId, out cachedCatalog))
            {
                commandsTask = _client.GetCommandsAsync(effectiveRulesetId, ct);
                tabsTask = _client.GetNavigationTabsAsync(effectiveRulesetId, ct);
                pendingTasks.Add(commandsTask);
                pendingTasks.Add(tabsTask);
            }

            if (!TryGetCachedWorkspaces(out cachedWorkspaces))
            {
                workspacesTask = _client.ListWorkspacesAsync(ct);
                pendingTasks.Add(workspacesTask);
            }

            if (pendingTasks.Count > 0)
            {
                await Task.WhenAll(pendingTasks);
            }

            if (commandsTask is not null && tabsTask is not null)
            {
                cachedCatalog = new CachedCatalogData(commandsTask.Result, tabsTask.Result, DateTimeOffset.UtcNow);
                _cachedCatalogsByRuleset[effectiveRulesetId] = cachedCatalog;
            }

            if (workspacesTask is not null)
            {
                cachedWorkspaces = new CachedWorkspaceData(workspacesTask.Result, DateTimeOffset.UtcNow);
                _cachedWorkspaces = cachedWorkspaces;
            }

            if (cachedCatalog is null || cachedWorkspaces is null)
            {
                throw new InvalidOperationException("Shell bootstrap cache could not be resolved.");
            }

            return new ShellBootstrapData(cachedCatalog.Commands, cachedCatalog.NavigationTabs, cachedWorkspaces.Workspaces);
        }
        finally
        {
            _sync.Release();
        }
    }

    private bool TryGetCachedCatalog(string rulesetId, [NotNullWhen(true)] out CachedCatalogData? cached)
    {
        if (_cachedCatalogsByRuleset.TryGetValue(rulesetId, out CachedCatalogData? cachedEntry)
            && DateTimeOffset.UtcNow - cachedEntry.CachedAtUtc <= BootstrapCacheWindow)
        {
            cached = cachedEntry;
            return true;
        }

        cached = null;
        return false;
    }

    private bool TryGetCachedWorkspaces([NotNullWhen(true)] out CachedWorkspaceData? cached)
    {
        if (_cachedWorkspaces is not null && DateTimeOffset.UtcNow - _cachedWorkspaces.CachedAtUtc <= BootstrapCacheWindow)
        {
            cached = _cachedWorkspaces;
            return true;
        }

        cached = null;
        return false;
    }

    private sealed record CachedCatalogData(
        IReadOnlyList<AppCommandDefinition> Commands,
        IReadOnlyList<NavigationTabDefinition> NavigationTabs,
        DateTimeOffset CachedAtUtc);

    private sealed record CachedWorkspaceData(
        IReadOnlyList<WorkspaceListItem> Workspaces,
        DateTimeOffset CachedAtUtc);
}
