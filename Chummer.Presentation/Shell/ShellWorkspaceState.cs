using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Shell;

public sealed record ShellWorkspaceState(
    CharacterWorkspaceId Id,
    string Name,
    string Alias,
    DateTimeOffset LastOpenedUtc,
    string RulesetId = RulesetDefaults.Sr5,
    bool HasSavedWorkspace = false);
