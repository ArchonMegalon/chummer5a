using Chummer.Contracts.Characters;

namespace Chummer.Contracts.Workspaces;

public readonly record struct CharacterWorkspaceId(string Value)
{
    public override string ToString() => Value;
}

public sealed record WorkspaceImportDocument(
    string Xml);

public sealed record WorkspaceDocument(
    string Xml);

public sealed record UpdateWorkspaceMetadata(
    string? Name,
    string? Alias,
    string? Notes);

public sealed record CommandResult<T>(
    bool Success,
    T? Value,
    string? Error)
    where T : class;
