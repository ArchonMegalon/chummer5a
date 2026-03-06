using System.Text.Json;
using System.Text.Json.Serialization;
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
        WorkspaceDocumentState state = ResolveState(record, content, rulesetId);
        document = new WorkspaceDocument(state, format);
        return true;
    }

    public void Save(CharacterWorkspaceId id, WorkspaceDocument document)
    {
        string? path = TryGetPath(id);
        if (path is null)
            throw new InvalidOperationException("Workspace id contains unsupported characters.");

        PersistedWorkspaceRecord record = new(document.Format.ToString())
        {
            Envelope = NormalizeEnvelope(document.State)
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

        return WorkspaceDocumentFormat.NativeXml;
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
        return RulesetDefaults.NormalizeOptional(record.Envelope?.RulesetId)
            ?? RulesetDefaults.NormalizeOptional(record.RulesetId)
            ?? DetectRulesetId(record.Envelope?.PayloadKind, record.Envelope?.Payload ?? record.Content ?? record.Xml)
            ?? string.Empty;
    }

    private static WorkspaceDocumentState ResolveState(
        PersistedWorkspaceRecord record,
        string content,
        string fallbackRulesetId)
    {
        WorkspacePayloadEnvelope? envelope = record.Envelope;
        string normalizedRulesetId = RulesetDefaults.NormalizeOptional(envelope?.RulesetId)
            ?? RulesetDefaults.NormalizeOptional(fallbackRulesetId)
            ?? DetectRulesetId(envelope?.PayloadKind, envelope?.Payload ?? content)
            ?? string.Empty;
        int schemaVersion = envelope?.SchemaVersion is > 0
            ? envelope.SchemaVersion
            : CurrentWorkspaceSchemaVersion;
        string payloadKind = string.IsNullOrWhiteSpace(envelope?.PayloadKind)
            ? WorkspacePayloadKind
            : envelope.PayloadKind;
        string payload = envelope?.Payload ?? content;
        return new WorkspaceDocumentState(
            rulesetId: normalizedRulesetId,
            schemaVersion: schemaVersion,
            payloadKind: payloadKind,
            payload: payload);
    }

    private static WorkspacePayloadEnvelope NormalizeEnvelope(WorkspaceDocumentState state)
    {
        int schemaVersion = state.SchemaVersion > 0
            ? state.SchemaVersion
            : CurrentWorkspaceSchemaVersion;
        string payloadKind = string.IsNullOrWhiteSpace(state.PayloadKind)
            ? WorkspacePayloadKind
            : state.PayloadKind;
        return new WorkspacePayloadEnvelope(
            RulesetId: state.RulesetId,
            SchemaVersion: schemaVersion,
            PayloadKind: payloadKind,
            Payload: state.Payload);
    }

    private static string? DetectRulesetId(string? payloadKind, string? payload)
    {
        string? normalizedPayloadKind = RulesetDefaults.NormalizeOptional(payloadKind);
        if (normalizedPayloadKind is not null)
        {
            if (normalizedPayloadKind.StartsWith($"{RulesetDefaults.Sr5}/", StringComparison.Ordinal))
            {
                return RulesetDefaults.Sr5;
            }

            if (normalizedPayloadKind.StartsWith($"{RulesetDefaults.Sr6}/", StringComparison.Ordinal))
            {
                return RulesetDefaults.Sr6;
            }
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        if (payload.IndexOf(">SR6<", StringComparison.OrdinalIgnoreCase) >= 0
            || payload.IndexOf(">Shadowrun 6<", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return RulesetDefaults.Sr6;
        }

        if (payload.IndexOf(">SR5<", StringComparison.OrdinalIgnoreCase) >= 0
            || payload.IndexOf(">Shadowrun 5<", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return RulesetDefaults.Sr5;
        }

        return null;
    }

    private sealed record PersistedWorkspaceRecord(string Format)
    {
        public WorkspacePayloadEnvelope? Envelope { get; init; }

        // Backward compatibility for older persisted payloads.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Content { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RulesetId { get; init; }

        // Backward compatibility for legacy persisted payloads.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Xml { get; init; }
    }
}
