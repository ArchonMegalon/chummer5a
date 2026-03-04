using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public interface IWorkspacePersistenceService
{
    Task<WorkspaceMetadataUpdateResult> UpdateMetadataAsync(
        IChummerClient client,
        CharacterWorkspaceId workspaceId,
        UpdateWorkspaceMetadata command,
        DesktopPreferenceState preferences,
        CancellationToken ct);

    Task<WorkspaceSaveResult> SaveAsync(
        IChummerClient client,
        CharacterWorkspaceId workspaceId,
        CancellationToken ct);
}

public sealed record WorkspaceMetadataUpdateResult(
    bool Success,
    CharacterProfileSection? Profile,
    DesktopPreferenceState Preferences,
    string? Error);

public sealed record WorkspaceSaveResult(
    bool Success,
    string? Error);
