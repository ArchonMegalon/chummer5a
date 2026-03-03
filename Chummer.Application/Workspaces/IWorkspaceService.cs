using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;

namespace Chummer.Application.Workspaces;

public interface IWorkspaceService
{
    WorkspaceImportResult Import(string xml);

    CharacterProfileSection? GetProfile(CharacterWorkspaceId id);

    CharacterProgressSection? GetProgress(CharacterWorkspaceId id);

    CharacterSkillsSection? GetSkills(CharacterWorkspaceId id);

    CommandResult<CharacterProfileSection> UpdateMetadata(CharacterWorkspaceId id, UpdateWorkspaceMetadata command);

    CommandResult<string> Save(CharacterWorkspaceId id);
}
