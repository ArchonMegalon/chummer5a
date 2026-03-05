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

    public async Task<IReadOnlyList<WorkspaceListItem>> GetWorkspacesAsync(CancellationToken ct)
    {
        if (TryGetCachedWorkspaces(out CachedWorkspaceData? cached))
        {
            return cached.Workspaces;
        }

        await _sync.WaitAsync(ct);
        try
        {
            if (TryGetCachedWorkspaces(out cached))
            {
                return cached.Workspaces;
            }

            IReadOnlyList<WorkspaceListItem> workspaces = await _client.ListWorkspacesAsync(ct);
            _cachedWorkspaces = new CachedWorkspaceData(workspaces, DateTimeOffset.UtcNow);
            return workspaces;
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<ShellBootstrapData> GetAsync(string? rulesetId, CancellationToken ct)
    {
        string effectiveRulesetId = RulesetDefaults.Normalize(rulesetId);
        IReadOnlyList<WorkspaceListItem> workspaces = await GetWorkspacesAsync(ct);
        if (TryGetCachedCatalog(effectiveRulesetId, out CachedCatalogData? cachedCatalog))
        {
            return new ShellBootstrapData(cachedCatalog.Commands, cachedCatalog.NavigationTabs, workspaces);
        }

        await _sync.WaitAsync(ct);
        try
        {
            if (TryGetCachedCatalog(effectiveRulesetId, out cachedCatalog))
            {
                return new ShellBootstrapData(cachedCatalog.Commands, cachedCatalog.NavigationTabs, workspaces);
            }

            Task<IReadOnlyList<AppCommandDefinition>> commandsTask = _client.GetCommandsAsync(effectiveRulesetId, ct);
            Task<IReadOnlyList<NavigationTabDefinition>> tabsTask = _client.GetNavigationTabsAsync(effectiveRulesetId, ct);
            await Task.WhenAll(commandsTask, tabsTask);
            cachedCatalog = new CachedCatalogData(commandsTask.Result, tabsTask.Result, DateTimeOffset.UtcNow);
            _cachedCatalogsByRuleset[effectiveRulesetId] = cachedCatalog;

            if (cachedCatalog is null)
            {
                throw new InvalidOperationException("Shell bootstrap cache could not be resolved.");
            }

            return new ShellBootstrapData(cachedCatalog.Commands, cachedCatalog.NavigationTabs, workspaces);
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
