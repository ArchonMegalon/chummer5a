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
    string? LastSavedXml)
{
    public static CharacterOverviewState Empty { get; } = new(
        IsBusy: false,
        Error: null,
        WorkspaceId: null,
        Profile: null,
        Progress: null,
        Skills: null,
        LastSavedXml: null);
}
