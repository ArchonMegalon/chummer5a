using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Shell;

public sealed record ShellState(
    bool IsBusy,
    string? Error,
    string? Notice,
    string ActiveRulesetId,
    CharacterWorkspaceId? ActiveWorkspaceId,
    IReadOnlyList<ShellWorkspaceState> OpenWorkspaces,
    IReadOnlyList<AppCommandDefinition> Commands,
    IReadOnlyList<AppCommandDefinition> MenuRoots,
    IReadOnlyList<NavigationTabDefinition> NavigationTabs,
    string? ActiveTabId,
    string? OpenMenuId,
    string? LastCommandId)
{
    public static ShellState Empty { get; } = new(
        IsBusy: false,
        Error: null,
        Notice: null,
        ActiveRulesetId: RulesetDefaults.Sr5,
        ActiveWorkspaceId: null,
        OpenWorkspaces: [],
        Commands: [],
        MenuRoots: [],
        NavigationTabs: [],
        ActiveTabId: null,
        OpenMenuId: null,
        LastCommandId: null);
}
