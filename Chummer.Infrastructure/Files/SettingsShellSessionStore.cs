using Chummer.Application.Tools;
using Chummer.Contracts.Presentation;

namespace Chummer.Infrastructure.Files;

public sealed class SettingsShellSessionStore : IShellSessionStore
{
    private const string GlobalSettingsScope = "global";
    private const string ActiveWorkspaceIdKey = "activeWorkspaceId";
    private const string ActiveTabIdKey = "activeTabId";
    private readonly ISettingsStore _settingsStore;

    public SettingsShellSessionStore(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    public ShellSessionState Load()
    {
        var settings = _settingsStore.Load(GlobalSettingsScope);
        string? activeWorkspaceId = settings[ActiveWorkspaceIdKey]?.GetValue<string>();
        string? activeTabId = settings[ActiveTabIdKey]?.GetValue<string>();
        return new ShellSessionState(activeWorkspaceId, activeTabId);
    }

    public void Save(ShellSessionState session)
    {
        var settings = _settingsStore.Load(GlobalSettingsScope);
        if (string.IsNullOrWhiteSpace(session.ActiveWorkspaceId))
        {
            settings.Remove(ActiveWorkspaceIdKey);
        }
        else
        {
            settings[ActiveWorkspaceIdKey] = session.ActiveWorkspaceId;
        }

        if (string.IsNullOrWhiteSpace(session.ActiveTabId))
        {
            settings.Remove(ActiveTabIdKey);
        }
        else
        {
            settings[ActiveTabIdKey] = session.ActiveTabId;
        }
        _settingsStore.Save(GlobalSettingsScope, settings);
    }
}
