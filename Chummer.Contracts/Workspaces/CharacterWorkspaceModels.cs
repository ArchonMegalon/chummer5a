using System.Text;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Rulesets;

namespace Chummer.Contracts.Workspaces;

public readonly record struct CharacterWorkspaceId(string Value)
{
    public override string ToString() => Value;
}

public enum WorkspaceDocumentFormat
{
    Chum5Xml = 0,
    Json = 1
}

public sealed record WorkspaceDocumentState
{
    public WorkspaceDocumentState(
        string rulesetId,
        int schemaVersion,
        string payloadKind,
        string payload)
    {
        RulesetId = RulesetDefaults.Normalize(rulesetId);
        SchemaVersion = schemaVersion;
        PayloadKind = payloadKind;
        Payload = payload;
    }

    public WorkspaceDocumentState(WorkspacePayloadEnvelope envelope)
        : this(envelope.RulesetId, envelope.SchemaVersion, envelope.PayloadKind, envelope.Payload)
    {
    }

    public string RulesetId { get; init; }

    public int SchemaVersion { get; init; }

    public string PayloadKind { get; init; }

    public string Payload { get; init; }

    public WorkspacePayloadEnvelope ToEnvelope()
    {
        return new WorkspacePayloadEnvelope(
            RulesetId,
            SchemaVersion,
            PayloadKind,
            Payload);
    }
}

public sealed record WorkspaceDocument(
    WorkspaceDocumentState State,
    WorkspaceDocumentFormat Format = WorkspaceDocumentFormat.Chum5Xml)
{
    public WorkspaceDocument(
        WorkspacePayloadEnvelope PayloadEnvelope,
        WorkspaceDocumentFormat Format = WorkspaceDocumentFormat.Chum5Xml)
        : this(new WorkspaceDocumentState(PayloadEnvelope), Format)
    {
    }

    public WorkspaceDocument(
        string Content,
        WorkspaceDocumentFormat Format = WorkspaceDocumentFormat.Chum5Xml,
        string RulesetId = RulesetDefaults.Sr5)
        : this(
            new WorkspaceDocumentState(
                rulesetId: RulesetId,
                schemaVersion: 1,
                payloadKind: "workspace",
                payload: Content),
            Format)
    {
    }

    public WorkspacePayloadEnvelope PayloadEnvelope => State.ToEnvelope();

    public string Content => State.Payload;

    public string RulesetId => State.RulesetId;

    public int SchemaVersion => State.SchemaVersion;

    public string PayloadKind => State.PayloadKind;
}

public sealed record WorkspaceImportDocument(
    string Content,
    WorkspaceDocumentFormat Format = WorkspaceDocumentFormat.Chum5Xml,
    string RulesetId = RulesetDefaults.Sr5)
{
    public static WorkspaceImportDocument FromUtf8Bytes(
        byte[] contentBytes,
        WorkspaceDocumentFormat format = WorkspaceDocumentFormat.Chum5Xml,
        string rulesetId = RulesetDefaults.Sr5)
    {
        string content = Encoding.UTF8.GetString(contentBytes);
        return new WorkspaceImportDocument(content, format, rulesetId);
    }
}

public sealed record WorkspaceSaveReceipt(
    CharacterWorkspaceId Id,
    int DocumentLength,
    string RulesetId);

public sealed record WorkspaceDownloadReceipt(
    CharacterWorkspaceId Id,
    WorkspaceDocumentFormat Format,
    string ContentBase64,
    string FileName,
    int DocumentLength,
    string RulesetId);

public sealed record WorkspaceExportReceipt(
    CharacterWorkspaceId Id,
    WorkspaceDocumentFormat Format,
    string ContentBase64,
    string FileName,
    int DocumentLength,
    string RulesetId);

public sealed record WorkspacePrintReceipt(
    CharacterWorkspaceId Id,
    string ContentBase64,
    string FileName,
    string MimeType,
    int DocumentLength,
    string Title,
    string RulesetId);

public sealed record WorkspaceListItem(
    CharacterWorkspaceId Id,
    CharacterFileSummary Summary,
    DateTimeOffset LastUpdatedUtc,
    string RulesetId,
    bool HasSavedWorkspace = false);

public sealed record UpdateWorkspaceMetadata(
    string? Name,
    string? Alias,
    string? Notes);

public sealed record CommandResult<T>(
    bool Success,
    T? Value,
    string? Error)
    where T : class;
