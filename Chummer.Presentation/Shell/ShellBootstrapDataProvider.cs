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
            _cachedWorkspaces = new CachedWorkspaceData(
                Workspaces: workspaces,
                PreferredRulesetId: RulesetDefaults.Sr5,
                ActiveRulesetId: RulesetDefaults.Normalize(workspaces.FirstOrDefault()?.RulesetId),
                CachedAtUtc: DateTimeOffset.UtcNow);
            return workspaces;
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<ShellBootstrapData> GetAsync(string? rulesetId, CancellationToken ct)
    {
        string? requestedRulesetId = string.IsNullOrWhiteSpace(rulesetId)
            ? null
            : RulesetDefaults.Normalize(rulesetId);

        if (TryGetCachedBootstrap(requestedRulesetId, out ShellBootstrapData? cachedBootstrap))
        {
            return cachedBootstrap;
        }

        await _sync.WaitAsync(ct);
        try
        {
            if (TryGetCachedBootstrap(requestedRulesetId, out cachedBootstrap))
            {
                return cachedBootstrap;
            }

            ShellBootstrapSnapshot snapshot = await _client.GetShellBootstrapAsync(requestedRulesetId, ct);
            string resolvedRulesetId = RulesetDefaults.Normalize(snapshot.RulesetId);
            DateTimeOffset cachedAtUtc = DateTimeOffset.UtcNow;
            _cachedWorkspaces = new CachedWorkspaceData(
                Workspaces: snapshot.Workspaces,
                PreferredRulesetId: RulesetDefaults.Normalize(snapshot.PreferredRulesetId),
                ActiveRulesetId: RulesetDefaults.Normalize(snapshot.ActiveRulesetId),
                CachedAtUtc: cachedAtUtc);
            var cachedCatalog = new CachedCatalogData(snapshot.Commands, snapshot.NavigationTabs, cachedAtUtc);
            _cachedCatalogsByRuleset[resolvedRulesetId] = cachedCatalog;
            if (!string.IsNullOrWhiteSpace(requestedRulesetId)
                && !string.Equals(requestedRulesetId, resolvedRulesetId, StringComparison.OrdinalIgnoreCase))
            {
                _cachedCatalogsByRuleset[requestedRulesetId] = cachedCatalog;
            }

            return new ShellBootstrapData(
                RulesetId: resolvedRulesetId,
                Commands: snapshot.Commands,
                NavigationTabs: snapshot.NavigationTabs,
                Workspaces: snapshot.Workspaces,
                PreferredRulesetId: RulesetDefaults.Normalize(snapshot.PreferredRulesetId),
                ActiveRulesetId: RulesetDefaults.Normalize(snapshot.ActiveRulesetId));
        }
        finally
        {
            _sync.Release();
        }
    }

    private bool TryGetCachedBootstrap(string? requestedRulesetId, [NotNullWhen(true)] out ShellBootstrapData? cachedBootstrap)
    {
        string effectiveRulesetId = RulesetDefaults.Normalize(requestedRulesetId);
        if (!TryGetCachedWorkspaces(out CachedWorkspaceData? cachedWorkspaces))
        {
            cachedBootstrap = null;
            return false;
        }

        if (TryGetCachedCatalog(effectiveRulesetId, out CachedCatalogData? cachedCatalog))
        {
            cachedBootstrap = new ShellBootstrapData(
                RulesetId: effectiveRulesetId,
                Commands: cachedCatalog.Commands,
                NavigationTabs: cachedCatalog.NavigationTabs,
                Workspaces: cachedWorkspaces.Workspaces,
                PreferredRulesetId: cachedWorkspaces.PreferredRulesetId,
                ActiveRulesetId: cachedWorkspaces.ActiveRulesetId);
            return true;
        }

        if (!string.IsNullOrWhiteSpace(requestedRulesetId))
        {
            cachedBootstrap = null;
            return false;
        }

        string activeWorkspaceRulesetId = RulesetDefaults.Normalize(cachedWorkspaces.ActiveRulesetId);
        if (TryGetCachedCatalog(activeWorkspaceRulesetId, out cachedCatalog))
        {
            cachedBootstrap = new ShellBootstrapData(
                RulesetId: activeWorkspaceRulesetId,
                Commands: cachedCatalog.Commands,
                NavigationTabs: cachedCatalog.NavigationTabs,
                Workspaces: cachedWorkspaces.Workspaces,
                PreferredRulesetId: cachedWorkspaces.PreferredRulesetId,
                ActiveRulesetId: cachedWorkspaces.ActiveRulesetId);
            return true;
        }

        cachedBootstrap = null;
        return false;
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
        string PreferredRulesetId,
        string ActiveRulesetId,
        DateTimeOffset CachedAtUtc);
}
