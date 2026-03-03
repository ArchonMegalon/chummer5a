using System.Collections.Concurrent;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Workspaces;

namespace Chummer.Infrastructure.Workspaces;

public sealed class InMemoryWorkspaceStore : IWorkspaceStore
{
    private readonly ConcurrentDictionary<string, string> _documents = new(StringComparer.Ordinal);

    public CharacterWorkspaceId Create(string xml)
    {
        string key = Guid.NewGuid().ToString("N");
        _documents[key] = xml;
        return new CharacterWorkspaceId(key);
    }

    public bool TryGet(CharacterWorkspaceId id, out string xml)
    {
        return _documents.TryGetValue(id.Value, out xml!);
    }

    public void Save(CharacterWorkspaceId id, string xml)
    {
        _documents[id.Value] = xml;
    }
}
