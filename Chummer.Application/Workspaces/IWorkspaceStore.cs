using Chummer.Contracts.Workspaces;

namespace Chummer.Application.Workspaces;

public interface IWorkspaceStore
{
    CharacterWorkspaceId Create(WorkspaceDocument document);

    IReadOnlyList<CharacterWorkspaceId> ListIds();

    bool TryGet(CharacterWorkspaceId id, out WorkspaceDocument document);

    void Save(CharacterWorkspaceId id, WorkspaceDocument document);

    bool Delete(CharacterWorkspaceId id);
}
