using Chummer.Contracts.Workspaces;

namespace Chummer.Application.Workspaces;

public interface IWorkspaceStore
{
    CharacterWorkspaceId Create(string xml);

    bool TryGet(CharacterWorkspaceId id, out string xml);

    void Save(CharacterWorkspaceId id, string xml);
}
