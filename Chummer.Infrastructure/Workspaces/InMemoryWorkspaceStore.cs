using System.Collections.Concurrent;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Workspaces;

namespace Chummer.Infrastructure.Workspaces;

public sealed class InMemoryWorkspaceStore : IWorkspaceStore
{
    private readonly ConcurrentDictionary<string, WorkspaceDocument> _documents = new(StringComparer.Ordinal);

    public CharacterWorkspaceId Create(WorkspaceDocument document)
    {
        string key = Guid.NewGuid().ToString("N");
        _documents[key] = document;
        return new CharacterWorkspaceId(key);
    }

    public bool TryGet(CharacterWorkspaceId id, out WorkspaceDocument document)
    {
        return _documents.TryGetValue(id.Value, out document!);
    }

    public void Save(CharacterWorkspaceId id, WorkspaceDocument document)
    {
        _documents[id.Value] = document;
    }
}
