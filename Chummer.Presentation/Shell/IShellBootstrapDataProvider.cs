using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Shell;

public sealed record ShellBootstrapData(
    IReadOnlyList<AppCommandDefinition> Commands,
    IReadOnlyList<NavigationTabDefinition> NavigationTabs,
    IReadOnlyList<WorkspaceListItem> Workspaces);

public interface IShellBootstrapDataProvider
{
    async Task<IReadOnlyList<WorkspaceListItem>> GetWorkspacesAsync(CancellationToken ct)
    {
        ShellBootstrapData bootstrap = await GetAsync(ct);
        return bootstrap.Workspaces;
    }

    Task<ShellBootstrapData> GetAsync(CancellationToken ct);

    Task<ShellBootstrapData> GetAsync(string? rulesetId, CancellationToken ct)
    {
        return GetAsync(ct);
    }
}
