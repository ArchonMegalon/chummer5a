using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;

namespace Chummer.Application.Workspaces;

public interface IWorkspaceService
{
    WorkspaceImportResult Import(WorkspaceImportDocument document);

    IReadOnlyList<WorkspaceListItem> List();

    bool Close(CharacterWorkspaceId id);

    object? GetSection(CharacterWorkspaceId id, string sectionId);

    CharacterFileSummary? GetSummary(CharacterWorkspaceId id);

    CharacterValidationResult? Validate(CharacterWorkspaceId id);

    CharacterProfileSection? GetProfile(CharacterWorkspaceId id);

    CharacterProgressSection? GetProgress(CharacterWorkspaceId id);

    CharacterSkillsSection? GetSkills(CharacterWorkspaceId id);

    CharacterRulesSection? GetRules(CharacterWorkspaceId id);

    CharacterBuildSection? GetBuild(CharacterWorkspaceId id);

    CharacterMovementSection? GetMovement(CharacterWorkspaceId id);

    CharacterAwakeningSection? GetAwakening(CharacterWorkspaceId id);

    CommandResult<CharacterProfileSection> UpdateMetadata(CharacterWorkspaceId id, UpdateWorkspaceMetadata command);

    CommandResult<WorkspaceSaveReceipt> Save(CharacterWorkspaceId id);

    CommandResult<WorkspaceDownloadReceipt> Download(CharacterWorkspaceId id);
}
