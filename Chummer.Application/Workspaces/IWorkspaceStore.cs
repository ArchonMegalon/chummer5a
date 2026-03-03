using Chummer.Contracts.Workspaces;

namespace Chummer.Application.Workspaces;

public interface IWorkspaceStore
{
    CharacterWorkspaceId Create(WorkspaceDocument document);

    bool TryGet(CharacterWorkspaceId id, out WorkspaceDocument document);

    void Save(CharacterWorkspaceId id, WorkspaceDocument document);
}
