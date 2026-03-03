using Chummer.Contracts.Characters;

namespace Chummer.Contracts.Workspaces;

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
    string Xml);
