using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;

namespace Chummer.Contracts.Presentation;

public static class ShellBootstrapDefaults
{
    public const int MaxWorkspaces = 25;
}

public sealed record ShellPreferences(
    string PreferredRulesetId)
{
    public static ShellPreferences Default { get; } = new(RulesetDefaults.Sr5);
}

public sealed record ShellSessionState(
    string? ActiveWorkspaceId = null,
    string? ActiveTabId = null,
    IReadOnlyDictionary<string, string>? ActiveTabsByWorkspace = null)
{
    public static ShellSessionState Default { get; } = new();
}

public sealed record ShellBootstrapResponse(
    string RulesetId,
    IReadOnlyList<AppCommandDefinition> Commands,
    IReadOnlyList<NavigationTabDefinition> NavigationTabs,
    IReadOnlyList<WorkspaceListItemResponse> Workspaces,
    string PreferredRulesetId = RulesetDefaults.Sr5,
    string ActiveRulesetId = RulesetDefaults.Sr5,
    string? ActiveWorkspaceId = null,
    string? ActiveTabId = null,
    IReadOnlyDictionary<string, string>? ActiveTabsByWorkspace = null);

public sealed record ShellBootstrapSnapshot(
    string RulesetId,
    IReadOnlyList<AppCommandDefinition> Commands,
    IReadOnlyList<NavigationTabDefinition> NavigationTabs,
    IReadOnlyList<WorkspaceListItem> Workspaces,
    string PreferredRulesetId = RulesetDefaults.Sr5,
    string ActiveRulesetId = RulesetDefaults.Sr5,
    CharacterWorkspaceId? ActiveWorkspaceId = null,
    string? ActiveTabId = null,
    IReadOnlyDictionary<string, string>? ActiveTabsByWorkspace = null);
