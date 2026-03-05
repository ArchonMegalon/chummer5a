using System.Text.Json;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;

namespace Chummer.Infrastructure.Workspaces;

public sealed class FileWorkspaceStore : IWorkspaceStore
{
    private const int CurrentWorkspaceSchemaVersion = 1;
    private const string WorkspacePayloadKind = "workspace";
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

    public IReadOnlyList<WorkspaceStoreEntry> List()
    {
        return Directory.EnumerateFiles(_workspaceDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(path => new
            {
                FileName = Path.GetFileNameWithoutExtension(path),
                LastUpdatedUtc = new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.FileName))
            .OrderByDescending(item => item.LastUpdatedUtc)
            .Select(item => new WorkspaceStoreEntry(
                Id: new CharacterWorkspaceId(item.FileName),
                LastUpdatedUtc: item.LastUpdatedUtc))
            .Where(entry => TryGetPath(entry.Id) is not null)
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

        string? content = ResolveContent(record);

        if (string.IsNullOrWhiteSpace(content))
        {
            document = null!;
            return false;
        }

        WorkspaceDocumentFormat format = ParseFormat(record.Format);
        string rulesetId = ResolveRulesetId(record);
        WorkspacePayloadEnvelope envelope = ResolveEnvelope(record, content, rulesetId);
        document = new WorkspaceDocument(
            Content: content,
            Format: format,
            RulesetId: rulesetId,
            PayloadEnvelope: envelope);
        return true;
    }

    public void Save(CharacterWorkspaceId id, WorkspaceDocument document)
    {
        string? path = TryGetPath(id);
        if (path is null)
            throw new InvalidOperationException("Workspace id contains unsupported characters.");

        WorkspacePayloadEnvelope envelope = ResolveEnvelope(document);
        PersistedWorkspaceRecord record = new(
            Content: envelope.Payload,
            Format: document.Format.ToString(),
            RulesetId: envelope.RulesetId)
        {
            Envelope = envelope
        };
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

    private static string? ResolveContent(PersistedWorkspaceRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.Envelope?.Payload))
        {
            return record.Envelope.Payload;
        }

        if (!string.IsNullOrWhiteSpace(record.Content))
        {
            return record.Content;
        }

        return record.Xml;
    }

    private static string ResolveRulesetId(PersistedWorkspaceRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.Envelope?.RulesetId))
        {
            return RulesetDefaults.Normalize(record.Envelope.RulesetId);
        }

        return RulesetDefaults.Normalize(record.RulesetId);
    }

    private static WorkspacePayloadEnvelope ResolveEnvelope(
        WorkspaceDocument document)
    {
        WorkspacePayloadEnvelope? existingEnvelope = document.PayloadEnvelope;
        string normalizedRulesetId = RulesetDefaults.Normalize(
            existingEnvelope?.RulesetId ?? document.RulesetId);
        int schemaVersion = existingEnvelope?.SchemaVersion is > 0
            ? existingEnvelope.SchemaVersion
            : CurrentWorkspaceSchemaVersion;
        string payloadKind = string.IsNullOrWhiteSpace(existingEnvelope?.PayloadKind)
            ? WorkspacePayloadKind
            : existingEnvelope.PayloadKind;
        string payload = existingEnvelope?.Payload ?? document.Content;
        return new WorkspacePayloadEnvelope(
            RulesetId: normalizedRulesetId,
            SchemaVersion: schemaVersion,
            PayloadKind: payloadKind,
            Payload: payload);
    }

    private static WorkspacePayloadEnvelope ResolveEnvelope(
        PersistedWorkspaceRecord record,
        string content,
        string fallbackRulesetId)
    {
        WorkspacePayloadEnvelope? envelope = record.Envelope;
        string normalizedRulesetId = RulesetDefaults.Normalize(envelope?.RulesetId ?? fallbackRulesetId);
        int schemaVersion = envelope?.SchemaVersion is > 0
            ? envelope.SchemaVersion
            : CurrentWorkspaceSchemaVersion;
        string payloadKind = string.IsNullOrWhiteSpace(envelope?.PayloadKind)
            ? WorkspacePayloadKind
            : envelope.PayloadKind;
        string payload = envelope?.Payload ?? content;
        return new WorkspacePayloadEnvelope(
            RulesetId: normalizedRulesetId,
            SchemaVersion: schemaVersion,
            PayloadKind: payloadKind,
            Payload: payload);
    }

    private sealed record PersistedWorkspaceRecord(
        string Content,
        string Format,
        string RulesetId = RulesetDefaults.Sr5)
    {
        public WorkspacePayloadEnvelope? Envelope { get; init; }

        // Backward compatibility for legacy persisted payloads.
        public string? Xml { get; init; }
    }
}
