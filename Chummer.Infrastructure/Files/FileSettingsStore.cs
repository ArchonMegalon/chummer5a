using System.Text.Json;
using System.Text.Json.Nodes;
using Chummer.Application.Tools;

namespace Chummer.Infrastructure.Files;

public sealed class FileSettingsStore : ISettingsStore
{
    private readonly string _stateDirectory;

    public FileSettingsStore(string? stateDirectory = null)
    {
        _stateDirectory = stateDirectory ?? Path.Combine(Path.GetTempPath(), "chummer-state");
        Directory.CreateDirectory(_stateDirectory);
    }

    public JsonObject Load(string scope)
    {
        string path = GetPath(scope);
        if (!File.Exists(path))
            return new JsonObject();

        string text = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(text))
            return new JsonObject();

        try
        {
            JsonNode? parsed = JsonNode.Parse(text);
            if (parsed is JsonObject json)
                return json;
        }
        catch
        {
            // fall through and return empty object when persisted settings are invalid.
        }

        return new JsonObject();
    }

    public void Save(string scope, JsonObject settings)
    {
        string path = GetPath(scope);
        string json = settings.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = false
        });
        File.WriteAllText(path, json);
    }

    private string GetPath(string scope)
    {
        return Path.Combine(_stateDirectory, $"{scope}-settings.json");
    }
}
