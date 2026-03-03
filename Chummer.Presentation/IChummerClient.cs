using Chummer.Contracts.Characters;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation;

public interface IChummerClient
{
    Task<WorkspaceImportResult> ImportAsync(WorkspaceImportDocument document, CancellationToken ct);

    Task<IReadOnlyList<AppCommandDefinition>> GetCommandsAsync(CancellationToken ct);

    Task<CharacterProfileSection> GetProfileAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CharacterProgressSection> GetProgressAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CharacterSkillsSection> GetSkillsAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CharacterRulesSection> GetRulesAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CharacterBuildSection> GetBuildAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CharacterMovementSection> GetMovementAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CharacterAwakeningSection> GetAwakeningAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CommandResult<CharacterProfileSection>> UpdateMetadataAsync(
        CharacterWorkspaceId id,
        UpdateWorkspaceMetadata command,
        CancellationToken ct);

    Task<CommandResult<WorkspaceSaveReceipt>> SaveAsync(CharacterWorkspaceId id, CancellationToken ct);
}
