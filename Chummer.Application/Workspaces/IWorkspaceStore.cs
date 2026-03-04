using Chummer.Contracts.Workspaces;

namespace Chummer.Application.Workspaces;

public readonly record struct WorkspaceStoreEntry(
    CharacterWorkspaceId Id,
    DateTimeOffset LastUpdatedUtc);

public interface IWorkspaceStore
{
    CharacterWorkspaceId Create(WorkspaceDocument document);

    IReadOnlyList<WorkspaceStoreEntry> List();

    bool TryGet(CharacterWorkspaceId id, out WorkspaceDocument document);

    void Save(CharacterWorkspaceId id, WorkspaceDocument document);

    bool Delete(CharacterWorkspaceId id);
}
