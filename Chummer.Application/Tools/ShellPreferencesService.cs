using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;

namespace Chummer.Application.Tools;

public sealed class ShellPreferencesService : IShellPreferencesService
{
    private readonly IShellPreferencesStore _store;

    public ShellPreferencesService(IShellPreferencesStore store)
    {
        _store = store;
    }

    public ShellUserPreferences Load()
    {
        ShellUserPreferences stored = _store.Load();
        return new ShellUserPreferences(
            PreferredRulesetId: RulesetDefaults.Normalize(stored.PreferredRulesetId),
            ActiveWorkspaceId: NormalizeWorkspaceId(stored.ActiveWorkspaceId));
    }

    public void Save(ShellUserPreferences preferences)
    {
        ShellUserPreferences normalized = new(
            PreferredRulesetId: RulesetDefaults.Normalize(preferences.PreferredRulesetId),
            ActiveWorkspaceId: NormalizeWorkspaceId(preferences.ActiveWorkspaceId));
        _store.Save(normalized);
    }

    private static string? NormalizeWorkspaceId(string? workspaceId)
    {
        return string.IsNullOrWhiteSpace(workspaceId)
            ? null
            : workspaceId.Trim();
    }
}
