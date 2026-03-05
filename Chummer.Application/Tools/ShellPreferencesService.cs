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
        return new ShellUserPreferences(RulesetDefaults.Normalize(stored.PreferredRulesetId));
    }

    public void Save(ShellUserPreferences preferences)
    {
        ShellUserPreferences normalized = new(RulesetDefaults.Normalize(preferences.PreferredRulesetId));
        _store.Save(normalized);
    }
}
