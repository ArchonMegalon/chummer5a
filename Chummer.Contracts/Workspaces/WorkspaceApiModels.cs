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

public sealed record WorkspaceListItemResponse(
    string Id,
    CharacterFileSummary Summary,
    DateTimeOffset LastUpdatedUtc);

public sealed record WorkspaceListResponse(
    int Count,
    IReadOnlyList<WorkspaceListItemResponse> Workspaces);

public sealed record WorkspaceMetadataResponse(
    CharacterProfileSection Profile);

public sealed record WorkspaceSaveResponse(
    string Id,
    int DocumentLength);

public sealed record WorkspaceDownloadResponse(
    string Id,
    string Format,
    string ContentBase64,
    string FileName,
    int DocumentLength);
