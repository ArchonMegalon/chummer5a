using System.Text.Json.Nodes;

namespace Chummer.Application.Tools;

public interface ISettingsStore
{
    JsonObject Load(string scope);

    void Save(string scope, JsonObject settings);
}
