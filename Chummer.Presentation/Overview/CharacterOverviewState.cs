using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public sealed record CharacterOverviewState(
    bool IsBusy,
    string? Error,
    CharacterWorkspaceId? WorkspaceId,
    CharacterProfileSection? Profile,
    CharacterProgressSection? Progress,
    CharacterSkillsSection? Skills,
    CharacterRulesSection? Rules,
    CharacterBuildSection? Build,
    CharacterMovementSection? Movement,
    CharacterAwakeningSection? Awakening,
    bool HasSavedWorkspace)
{
    public static CharacterOverviewState Empty { get; } = new(
        IsBusy: false,
        Error: null,
        WorkspaceId: null,
        Profile: null,
        Progress: null,
        Skills: null,
        Rules: null,
        Build: null,
        Movement: null,
        Awakening: null,
        HasSavedWorkspace: false);
}
