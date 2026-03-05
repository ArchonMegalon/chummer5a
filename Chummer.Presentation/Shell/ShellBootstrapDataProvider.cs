using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;
using System.Diagnostics.CodeAnalysis;

namespace Chummer.Presentation.Shell;

public sealed class ShellBootstrapDataProvider : IShellBootstrapDataProvider
{
    private static readonly TimeSpan BootstrapCacheWindow = TimeSpan.FromSeconds(10);
    private readonly IChummerClient _client;
    private readonly SemaphoreSlim _sync = new(1, 1);
    private ShellBootstrapData? _cached;
    private DateTimeOffset _cachedAtUtc;

    public ShellBootstrapDataProvider(IChummerClient client)
    {
        _client = client;
    }

    public async Task<ShellBootstrapData> GetAsync(CancellationToken ct)
    {
        if (TryGetCached(out ShellBootstrapData? cached))
        {
            return cached;
        }

        await _sync.WaitAsync(ct);
        try
        {
            if (TryGetCached(out cached))
            {
                return cached;
            }

            Task<IReadOnlyList<AppCommandDefinition>> commandsTask = _client.GetCommandsAsync(ct);
            Task<IReadOnlyList<NavigationTabDefinition>> tabsTask = _client.GetNavigationTabsAsync(ct);
            Task<IReadOnlyList<WorkspaceListItem>> workspacesTask = _client.ListWorkspacesAsync(ct);
            await Task.WhenAll(commandsTask, tabsTask, workspacesTask);

            ShellBootstrapData loaded = new(
                Commands: commandsTask.Result,
                NavigationTabs: tabsTask.Result,
                Workspaces: workspacesTask.Result);
            _cached = loaded;
            _cachedAtUtc = DateTimeOffset.UtcNow;
            return loaded;
        }
        finally
        {
            _sync.Release();
        }
    }

    private bool TryGetCached([NotNullWhen(true)] out ShellBootstrapData? cached)
    {
        if (_cached is not null && DateTimeOffset.UtcNow - _cachedAtUtc <= BootstrapCacheWindow)
        {
            cached = _cached;
            return true;
        }

        cached = null;
        return false;
    }
}
