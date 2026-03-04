using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public interface IWorkspaceSessionPresenter
{
    WorkspaceSessionState State { get; }

    WorkspaceSessionState Restore(IReadOnlyList<WorkspaceListItem> workspaces);

    WorkspaceSessionState Open(CharacterWorkspaceId id, CharacterProfileSection? profile);

    WorkspaceSessionState Switch(CharacterWorkspaceId id);

    WorkspaceSessionState ClearActive();

    WorkspaceSessionState Close(CharacterWorkspaceId id);

    WorkspaceSessionState CloseAll();

    bool Contains(CharacterWorkspaceId id);
}
