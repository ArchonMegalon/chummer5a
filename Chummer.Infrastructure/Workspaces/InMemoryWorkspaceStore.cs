using System.Collections.Concurrent;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Workspaces;

namespace Chummer.Infrastructure.Workspaces;

public sealed class InMemoryWorkspaceStore : IWorkspaceStore
{
    private readonly ConcurrentDictionary<string, WorkspaceEntry> _documents = new(StringComparer.Ordinal);

    public CharacterWorkspaceId Create(WorkspaceDocument document)
    {
        string key = Guid.NewGuid().ToString("N");
        _documents[key] = new WorkspaceEntry(document, DateTimeOffset.UtcNow);
        return new CharacterWorkspaceId(key);
    }

    public IReadOnlyList<WorkspaceStoreEntry> List()
    {
        return _documents
            .OrderByDescending(pair => pair.Value.LastUpdatedUtc)
            .Select(pair => new WorkspaceStoreEntry(
                Id: new CharacterWorkspaceId(pair.Key),
                LastUpdatedUtc: pair.Value.LastUpdatedUtc))
            .ToArray();
    }

    public bool TryGet(CharacterWorkspaceId id, out WorkspaceDocument document)
    {
        if (_documents.TryGetValue(id.Value, out WorkspaceEntry? entry))
        {
            document = entry.Document;
            return true;
        }

        document = null!;
        return false;
    }

    public void Save(CharacterWorkspaceId id, WorkspaceDocument document)
    {
        _documents[id.Value] = new WorkspaceEntry(document, DateTimeOffset.UtcNow);
    }

    public bool Delete(CharacterWorkspaceId id)
    {
        return _documents.TryRemove(id.Value, out _);
    }

    private sealed record WorkspaceEntry(WorkspaceDocument Document, DateTimeOffset LastUpdatedUtc);
}
