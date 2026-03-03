using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation;

public interface IChummerClient
{
    Task<CharacterProfileSection> GetProfileAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CharacterProgressSection> GetProgressAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CharacterSkillsSection> GetSkillsAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CommandResult<CharacterProfileSection>> UpdateMetadataAsync(
        CharacterWorkspaceId id,
        UpdateWorkspaceMetadata command,
        CancellationToken ct);
}
