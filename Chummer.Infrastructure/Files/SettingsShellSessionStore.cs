using Chummer.Application.Tools;
using Chummer.Contracts.Presentation;
using System.Text.Json.Nodes;

namespace Chummer.Infrastructure.Files;

public sealed class SettingsShellSessionStore : IShellSessionStore
{
    private const string GlobalSettingsScope = "global";
    private const string ActiveWorkspaceIdKey = "activeWorkspaceId";
    private const string ActiveTabIdKey = "activeTabId";
    private const string ActiveTabsByWorkspaceKey = "activeTabsByWorkspace";
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
        return new ShellSessionState(
            ActiveWorkspaceId: activeWorkspaceId,
            ActiveTabId: activeTabId,
            ActiveTabsByWorkspace: LoadWorkspaceTabMap(settings));
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

        SaveWorkspaceTabMap(settings, session.ActiveTabsByWorkspace);
        _settingsStore.Save(GlobalSettingsScope, settings);
    }

    private static IReadOnlyDictionary<string, string>? LoadWorkspaceTabMap(JsonObject settings)
    {
        if (settings[ActiveTabsByWorkspaceKey] is not JsonObject tabsByWorkspaceNode)
        {
            return null;
        }

        Dictionary<string, string> map = new(StringComparer.Ordinal);
        foreach ((string key, JsonNode? value) in tabsByWorkspaceNode)
        {
            string? tabId = value?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(tabId))
            {
                map[key] = tabId;
            }
        }

        return map.Count == 0
            ? null
            : map;
    }

    private static void SaveWorkspaceTabMap(JsonObject settings, IReadOnlyDictionary<string, string>? map)
    {
        if (map is null || map.Count == 0)
        {
            settings.Remove(ActiveTabsByWorkspaceKey);
            return;
        }

        JsonObject serialized = [];
        foreach ((string workspaceId, string tabId) in map)
        {
            serialized[workspaceId] = tabId;
        }

        settings[ActiveTabsByWorkspaceKey] = serialized;
    }
}
