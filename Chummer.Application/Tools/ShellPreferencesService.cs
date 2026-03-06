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

    public ShellPreferences Load()
    {
        ShellPreferences stored = _store.Load();
        return new ShellPreferences(
            PreferredRulesetId: RulesetDefaults.NormalizeOptional(stored.PreferredRulesetId) ?? string.Empty);
    }

    public void Save(ShellPreferences preferences)
    {
        ShellPreferences normalized = new(
            PreferredRulesetId: RulesetDefaults.NormalizeOptional(preferences.PreferredRulesetId) ?? string.Empty);
        _store.Save(normalized);
    }
}
