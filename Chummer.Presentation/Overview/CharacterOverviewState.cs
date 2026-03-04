using Chummer.Contracts.Characters;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public sealed record CharacterOverviewState(
    bool IsBusy,
    string? Error,
    CharacterWorkspaceId? WorkspaceId,
    IReadOnlyList<OpenWorkspaceState> OpenWorkspaces,
    CharacterProfileSection? Profile,
    CharacterProgressSection? Progress,
    CharacterSkillsSection? Skills,
    CharacterRulesSection? Rules,
    CharacterBuildSection? Build,
    CharacterMovementSection? Movement,
    CharacterAwakeningSection? Awakening,
    string? ActiveTabId,
    string? ActiveActionId,
    string? ActiveSectionId,
    string? ActiveSectionJson,
    IReadOnlyList<SectionRowState> ActiveSectionRows,
    string? LastCommandId,
    string? Notice,
    DesktopDialogState? ActiveDialog,
    DesktopPreferenceState Preferences,
    IReadOnlyList<AppCommandDefinition> Commands,
    IReadOnlyList<NavigationTabDefinition> NavigationTabs,
    bool HasSavedWorkspace)
{
    public static CharacterOverviewState Empty { get; } = new(
        IsBusy: false,
        Error: null,
        WorkspaceId: null,
        OpenWorkspaces: [],
        Profile: null,
        Progress: null,
        Skills: null,
        Rules: null,
        Build: null,
        Movement: null,
        Awakening: null,
        ActiveTabId: null,
        ActiveActionId: null,
        ActiveSectionId: null,
        ActiveSectionJson: null,
        ActiveSectionRows: [],
        LastCommandId: null,
        Notice: null,
        ActiveDialog: null,
        Preferences: DesktopPreferenceState.Default,
        Commands: [],
        NavigationTabs: [],
        HasSavedWorkspace: false);
}
