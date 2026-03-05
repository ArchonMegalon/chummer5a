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

    public ShellPreferences Load()
    {
        var settings = _settingsStore.Load(GlobalSettingsScope);
        string preferredRulesetId = settings[PreferredRulesetIdKey]?.GetValue<string>() ?? string.Empty;
        return new ShellPreferences(preferredRulesetId);
    }

    public void Save(ShellPreferences preferences)
    {
        var settings = _settingsStore.Load(GlobalSettingsScope);
        settings[PreferredRulesetIdKey] = preferences.PreferredRulesetId;
        _settingsStore.Save(GlobalSettingsScope, settings);
    }
}
