using Chummer.Application.Tools;
using Chummer.Contracts.Presentation;

namespace Chummer.Infrastructure.Files;

public sealed class SettingsShellPreferencesStore : IShellPreferencesStore
{
    private const string GlobalSettingsScope = "global";
    private const string PreferredRulesetIdKey = "preferredRulesetId";
    private const string ActiveWorkspaceIdKey = "activeWorkspaceId";
    private readonly ISettingsStore _settingsStore;

    public SettingsShellPreferencesStore(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    public ShellUserPreferences Load()
    {
        var settings = _settingsStore.Load(GlobalSettingsScope);
        string preferredRulesetId = settings[PreferredRulesetIdKey]?.GetValue<string>() ?? string.Empty;
        string? activeWorkspaceId = settings[ActiveWorkspaceIdKey]?.GetValue<string>();
        return new ShellUserPreferences(preferredRulesetId, activeWorkspaceId);
    }

    public void Save(ShellUserPreferences preferences)
    {
        var settings = _settingsStore.Load(GlobalSettingsScope);
        settings[PreferredRulesetIdKey] = preferences.PreferredRulesetId;
        if (string.IsNullOrWhiteSpace(preferences.ActiveWorkspaceId))
        {
            settings.Remove(ActiveWorkspaceIdKey);
        }
        else
        {
            settings[ActiveWorkspaceIdKey] = preferences.ActiveWorkspaceId;
        }
        _settingsStore.Save(GlobalSettingsScope, settings);
    }
}
