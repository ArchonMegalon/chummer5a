using System.Text.Json;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Workspaces;

namespace Chummer.Infrastructure.Workspaces;

public sealed class FileWorkspaceStore : IWorkspaceStore
{
    private readonly string _workspaceDirectory;

    public FileWorkspaceStore(string? stateDirectory = null)
    {
        string directory = stateDirectory ?? Path.Combine(Path.GetTempPath(), "chummer-state");
        _workspaceDirectory = Path.Combine(directory, "workspaces");
        Directory.CreateDirectory(_workspaceDirectory);
    }

    public CharacterWorkspaceId Create(WorkspaceDocument document)
    {
        string id = Guid.NewGuid().ToString("N");
        CharacterWorkspaceId workspaceId = new(id);
        Save(workspaceId, document);
        return workspaceId;
    }

    public bool TryGet(CharacterWorkspaceId id, out WorkspaceDocument document)
    {
        string? path = TryGetPath(id);
        if (path is null || !File.Exists(path))
        {
            document = null!;
            return false;
        }

        PersistedWorkspaceRecord? record = JsonSerializer.Deserialize<PersistedWorkspaceRecord>(File.ReadAllText(path));
        if (record is null || string.IsNullOrWhiteSpace(record.Xml))
        {
            document = null!;
            return false;
        }

        document = new WorkspaceDocument(record.Xml);
        return true;
    }

    public void Save(CharacterWorkspaceId id, WorkspaceDocument document)
    {
        string? path = TryGetPath(id);
        if (path is null)
            throw new InvalidOperationException("Workspace id contains unsupported characters.");

        PersistedWorkspaceRecord record = new(document.Xml);
        File.WriteAllText(path, JsonSerializer.Serialize(record));
    }

    private string? TryGetPath(CharacterWorkspaceId id)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
            return null;

        foreach (char character in id.Value)
        {
            if (!(char.IsLetterOrDigit(character) || character is '-' or '_'))
                return null;
        }

        return Path.Combine(_workspaceDirectory, $"{id.Value}.json");
    }

    private sealed record PersistedWorkspaceRecord(string Xml);
}
