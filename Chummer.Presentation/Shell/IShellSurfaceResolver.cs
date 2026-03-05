using Chummer.Contracts.Presentation;
using Chummer.Presentation.Overview;

namespace Chummer.Presentation.Shell;

public sealed record ShellSurfaceState(
    IReadOnlyList<WorkspaceSurfaceActionDefinition> WorkspaceActions,
    IReadOnlyList<DesktopUiControlDefinition> DesktopUiControls)
{
    public static ShellSurfaceState Empty { get; } = new([], []);
}

public interface IShellSurfaceResolver
{
    ShellSurfaceState Resolve(CharacterOverviewState overviewState, ShellState shellState);
}
