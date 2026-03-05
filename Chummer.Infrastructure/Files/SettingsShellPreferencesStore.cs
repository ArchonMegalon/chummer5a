using Chummer.Application.Tools;
using Chummer.Contracts.Presentation;

namespace Chummer.Infrastructure.Files;

public sealed class SettingsShellPreferencesStore : IShellPreferencesStore
{
    private const string GlobalSettingsScope = "global";
    private const string PreferredRulesetIdKey = "preferredRulesetId";
    private readonly ISettingsStore _settingsStore;

    public SettingsShellPreferencesStore(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    public ShellUserPreferences Load()
    {
        string preferredRulesetId = _settingsStore.Load(GlobalSettingsScope)[PreferredRulesetIdKey]?.GetValue<string>() ?? string.Empty;
        return new ShellUserPreferences(preferredRulesetId);
    }

    public void Save(ShellUserPreferences preferences)
    {
        var settings = _settingsStore.Load(GlobalSettingsScope);
        settings[PreferredRulesetIdKey] = preferences.PreferredRulesetId;
        _settingsStore.Save(GlobalSettingsScope, settings);
    }
}
