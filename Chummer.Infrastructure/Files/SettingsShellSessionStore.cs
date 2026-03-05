using Chummer.Application.Tools;
using Chummer.Contracts.Presentation;

namespace Chummer.Infrastructure.Files;

public sealed class SettingsShellSessionStore : IShellSessionStore
{
    private const string GlobalSettingsScope = "global";
    private const string ActiveWorkspaceIdKey = "activeWorkspaceId";
    private readonly ISettingsStore _settingsStore;

    public SettingsShellSessionStore(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    public ShellSessionState Load()
    {
        var settings = _settingsStore.Load(GlobalSettingsScope);
        string? activeWorkspaceId = settings[ActiveWorkspaceIdKey]?.GetValue<string>();
        return new ShellSessionState(activeWorkspaceId);
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
        _settingsStore.Save(GlobalSettingsScope, settings);
    }
}
