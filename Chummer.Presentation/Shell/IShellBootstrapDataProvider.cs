using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Shell;

public sealed record ShellBootstrapData(
    IReadOnlyList<AppCommandDefinition> Commands,
    IReadOnlyList<NavigationTabDefinition> NavigationTabs,
    IReadOnlyList<WorkspaceListItem> Workspaces);

public interface IShellBootstrapDataProvider
{
    Task<ShellBootstrapData> GetAsync(CancellationToken ct);
}
