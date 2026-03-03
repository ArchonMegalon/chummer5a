using Chummer.Contracts.Characters;

namespace Chummer.Contracts.Workspaces;

public sealed record WorkspaceImportResult(
    CharacterWorkspaceId Id,
    CharacterFileSummary Summary);
