using System.Text;
using Chummer.Contracts.Characters;

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
    WorkspaceDocumentFormat Format = WorkspaceDocumentFormat.Chum5Xml);

public sealed record WorkspaceImportDocument(
    string Content,
    WorkspaceDocumentFormat Format = WorkspaceDocumentFormat.Chum5Xml)
{
    public static WorkspaceImportDocument FromUtf8Bytes(
        byte[] contentBytes,
        WorkspaceDocumentFormat format = WorkspaceDocumentFormat.Chum5Xml)
    {
        string content = Encoding.UTF8.GetString(contentBytes);
        return new WorkspaceImportDocument(content, format);
    }
}

public sealed record WorkspaceSaveReceipt(
    CharacterWorkspaceId Id,
    int DocumentLength);

public sealed record WorkspaceListItem(
    CharacterWorkspaceId Id,
    CharacterFileSummary Summary,
    DateTimeOffset LastUpdatedUtc);

public sealed record UpdateWorkspaceMetadata(
    string? Name,
    string? Alias,
    string? Notes);

public sealed record CommandResult<T>(
    bool Success,
    T? Value,
    string? Error)
    where T : class;
