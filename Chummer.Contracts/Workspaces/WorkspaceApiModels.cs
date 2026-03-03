using Chummer.Contracts.Characters;

namespace Chummer.Contracts.Workspaces;

public sealed record WorkspaceImportRequest(
    string? ContentBase64,
    string? Format,
    string? Xml);

public sealed record WorkspaceImportResult(
    CharacterWorkspaceId Id,
    CharacterFileSummary Summary);

public sealed record WorkspaceImportResponse(
    string Id,
    CharacterFileSummary Summary);

public sealed record WorkspaceMetadataResponse(
    CharacterProfileSection Profile);

public sealed record WorkspaceSaveResponse(
    string Id,
    int DocumentLength);
