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

            Task<IReadOnlyList<WorkspaceListItem>> workspacesTask = _client.ListWorkspacesAsync(ct);
            Task<ShellPreferences> preferencesTask = _client.GetShellPreferencesAsync(ct);
            Task<ShellSessionState> sessionTask = _client.GetShellSessionAsync(ct);
            await Task.WhenAll(workspacesTask, preferencesTask, sessionTask);
            IReadOnlyList<WorkspaceListItem> workspaces = workspacesTask.Result;
            ShellPreferences preferences = preferencesTask.Result;
            ShellSessionState session = sessionTask.Result;
            string preferredRulesetId = RulesetDefaults.Normalize(preferences.PreferredRulesetId);
            CharacterWorkspaceId? activeWorkspaceId = ResolveActiveWorkspaceId(workspaces, session.ActiveWorkspaceId);
            IReadOnlyDictionary<string, string>? activeTabsByWorkspace = NormalizeWorkspaceTabMap(session.ActiveTabsByWorkspace);
            _cachedWorkspaces = new CachedWorkspaceData(
                Workspaces: workspaces,
                PreferredRulesetId: preferredRulesetId,
                ActiveRulesetId: ResolveRulesetForWorkspace(activeWorkspaceId, workspaces, preferredRulesetId),
                ActiveWorkspaceId: activeWorkspaceId,
                ActiveTabId: NormalizeTabId(session.ActiveTabId),
                ActiveTabsByWorkspace: activeTabsByWorkspace,
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
            CharacterWorkspaceId? activeWorkspaceId = ResolveActiveWorkspaceId(
                snapshot.Workspaces,
                snapshot.ActiveWorkspaceId?.Value);
            string activeRulesetId = activeWorkspaceId is null
                ? RulesetDefaults.Normalize(snapshot.ActiveRulesetId)
                : ResolveRulesetForWorkspace(
                    activeWorkspaceId,
                    snapshot.Workspaces,
                    snapshot.PreferredRulesetId);
            _cachedWorkspaces = new CachedWorkspaceData(
                Workspaces: snapshot.Workspaces,
                PreferredRulesetId: RulesetDefaults.Normalize(snapshot.PreferredRulesetId),
                ActiveRulesetId: activeRulesetId,
                ActiveWorkspaceId: activeWorkspaceId,
                ActiveTabId: NormalizeTabId(snapshot.ActiveTabId),
                ActiveTabsByWorkspace: NormalizeWorkspaceTabMap(snapshot.ActiveTabsByWorkspace),
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
                ActiveRulesetId: activeRulesetId,
                ActiveWorkspaceId: activeWorkspaceId,
                ActiveTabId: NormalizeTabId(snapshot.ActiveTabId),
                ActiveTabsByWorkspace: NormalizeWorkspaceTabMap(snapshot.ActiveTabsByWorkspace));
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
                ActiveRulesetId: cachedWorkspaces.ActiveRulesetId,
                ActiveWorkspaceId: cachedWorkspaces.ActiveWorkspaceId,
                ActiveTabId: cachedWorkspaces.ActiveTabId,
                ActiveTabsByWorkspace: cachedWorkspaces.ActiveTabsByWorkspace);
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
                ActiveRulesetId: cachedWorkspaces.ActiveRulesetId,
                ActiveWorkspaceId: cachedWorkspaces.ActiveWorkspaceId,
                ActiveTabId: cachedWorkspaces.ActiveTabId,
                ActiveTabsByWorkspace: cachedWorkspaces.ActiveTabsByWorkspace);
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

    private static CharacterWorkspaceId? ResolveActiveWorkspaceId(
        IReadOnlyList<WorkspaceListItem> workspaces,
        string? preferredActiveWorkspaceId)
    {
        if (string.IsNullOrWhiteSpace(preferredActiveWorkspaceId))
            return null;

        WorkspaceListItem? matchingWorkspace = workspaces.FirstOrDefault(workspace =>
            string.Equals(workspace.Id.Value, preferredActiveWorkspaceId, StringComparison.Ordinal));
        return matchingWorkspace?.Id;
    }

    private static string ResolveRulesetForWorkspace(
        CharacterWorkspaceId? activeWorkspaceId,
        IReadOnlyList<WorkspaceListItem> workspaces,
        string preferredRulesetId)
    {
        if (activeWorkspaceId is null)
        {
            return RulesetDefaults.Normalize(preferredRulesetId);
        }

        WorkspaceListItem? matchingWorkspace = workspaces.FirstOrDefault(workspace =>
            string.Equals(workspace.Id.Value, activeWorkspaceId.Value.Value, StringComparison.Ordinal));
        return matchingWorkspace is null
            ? RulesetDefaults.Normalize(preferredRulesetId)
            : RulesetDefaults.Normalize(matchingWorkspace.RulesetId);
    }

    private static string? NormalizeTabId(string? tabId)
    {
        return string.IsNullOrWhiteSpace(tabId)
            ? null
            : tabId.Trim();
    }

    private static IReadOnlyDictionary<string, string>? NormalizeWorkspaceTabMap(IReadOnlyDictionary<string, string>? rawMap)
    {
        if (rawMap is null || rawMap.Count == 0)
        {
            return null;
        }

        Dictionary<string, string> normalized = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> entry in rawMap)
        {
            string? workspaceId = string.IsNullOrWhiteSpace(entry.Key)
                ? null
                : entry.Key.Trim();
            string? tabId = NormalizeTabId(entry.Value);
            if (workspaceId is null || tabId is null)
            {
                continue;
            }

            normalized[workspaceId] = tabId;
        }

        return normalized.Count == 0
            ? null
            : normalized;
    }

    private sealed record CachedCatalogData(
        IReadOnlyList<AppCommandDefinition> Commands,
        IReadOnlyList<NavigationTabDefinition> NavigationTabs,
        DateTimeOffset CachedAtUtc);

    private sealed record CachedWorkspaceData(
        IReadOnlyList<WorkspaceListItem> Workspaces,
        string PreferredRulesetId,
        string ActiveRulesetId,
        CharacterWorkspaceId? ActiveWorkspaceId,
        string? ActiveTabId,
        IReadOnlyDictionary<string, string>? ActiveTabsByWorkspace,
        DateTimeOffset CachedAtUtc);
}
