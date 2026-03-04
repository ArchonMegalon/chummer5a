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

    public IReadOnlyList<CharacterWorkspaceId> ListIds()
    {
        return Directory.EnumerateFiles(_workspaceDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(path => new
            {
                Path = path,
                LastWriteUtc = File.GetLastWriteTimeUtc(path)
            })
            .OrderByDescending(item => item.LastWriteUtc)
            .Select(item => Path.GetFileNameWithoutExtension(item.Path))
            .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
            .Select(fileName => new CharacterWorkspaceId(fileName))
            .ToArray();
    }

    public bool TryGet(CharacterWorkspaceId id, out WorkspaceDocument document)
    {
        string? path = TryGetPath(id);
        if (path is null || !File.Exists(path))
        {
            document = null!;
            return false;
        }

        PersistedWorkspaceRecord? record;
        try
        {
            record = JsonSerializer.Deserialize<PersistedWorkspaceRecord>(File.ReadAllText(path));
        }
        catch (IOException)
        {
            document = null!;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            document = null!;
            return false;
        }
        catch (JsonException)
        {
            document = null!;
            return false;
        }

        if (record is null)
        {
            document = null!;
            return false;
        }

        string? content = record.Content;
        if (string.IsNullOrWhiteSpace(content))
            content = record.Xml;

        if (string.IsNullOrWhiteSpace(content))
        {
            document = null!;
            return false;
        }

        WorkspaceDocumentFormat format = ParseFormat(record.Format);
        document = new WorkspaceDocument(content, format);
        return true;
    }

    public void Save(CharacterWorkspaceId id, WorkspaceDocument document)
    {
        string? path = TryGetPath(id);
        if (path is null)
            throw new InvalidOperationException("Workspace id contains unsupported characters.");

        PersistedWorkspaceRecord record = new(document.Content, document.Format.ToString());
        string tempPath = $"{path}.tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(record));
        File.Move(tempPath, path, overwrite: true);
    }

    public bool Delete(CharacterWorkspaceId id)
    {
        string? path = TryGetPath(id);
        if (path is null || !File.Exists(path))
            return false;

        try
        {
            File.Delete(path);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
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

    private static WorkspaceDocumentFormat ParseFormat(string? format)
    {
        if (Enum.TryParse(format, ignoreCase: true, out WorkspaceDocumentFormat parsed))
            return parsed;

        return WorkspaceDocumentFormat.Chum5Xml;
    }

    private sealed record PersistedWorkspaceRecord(string Content, string Format)
    {
        // Backward compatibility for legacy persisted payloads.
        public string? Xml { get; init; }
    }
}
