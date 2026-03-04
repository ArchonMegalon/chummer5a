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
    Chum5Xml = 0
}

public sealed record WorkspaceDocument(
    string Content,
    WorkspaceDocumentFormat Format = WorkspaceDocumentFormat.Chum5Xml,
    string RulesetId = RulesetDefaults.Sr5);

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
    string RulesetId = RulesetDefaults.Sr5);

public sealed record WorkspaceDownloadReceipt(
    CharacterWorkspaceId Id,
    WorkspaceDocumentFormat Format,
    string ContentBase64,
    string FileName,
    int DocumentLength,
    string RulesetId = RulesetDefaults.Sr5);

public sealed record WorkspaceListItem(
    CharacterWorkspaceId Id,
    CharacterFileSummary Summary,
    DateTimeOffset LastUpdatedUtc,
    string RulesetId = RulesetDefaults.Sr5);

public sealed record UpdateWorkspaceMetadata(
    string? Name,
    string? Alias,
    string? Notes);

public sealed record CommandResult<T>(
    bool Success,
    T? Value,
    string? Error)
    where T : class;
