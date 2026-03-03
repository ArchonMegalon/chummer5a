using System.Text.Json;
using Chummer.Application.Tools;
using Chummer.Contracts.Api;

namespace Chummer.Infrastructure.Files;

public sealed class FileRosterStore : IRosterStore
{
    private readonly string _path;

    public FileRosterStore(string? stateDirectory = null)
    {
        string directory = stateDirectory ?? Path.Combine(Path.GetTempPath(), "chummer-state");
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "roster.json");
    }

    public IReadOnlyList<RosterEntry> Load()
    {
        if (!File.Exists(_path))
            return Array.Empty<RosterEntry>();

        List<RosterEntry>? entries = JsonSerializer.Deserialize<List<RosterEntry>>(File.ReadAllText(_path));
        return entries ?? [];
    }

    public IReadOnlyList<RosterEntry> Upsert(RosterEntry entry)
    {
        IReadOnlyList<RosterEntry> existing = Load();

        List<RosterEntry> merged = [entry];
        foreach (RosterEntry current in existing)
        {
            if (string.Equals(current.Name, entry.Name, StringComparison.Ordinal)
                && string.Equals(current.Alias, entry.Alias, StringComparison.Ordinal))
            {
                continue;
            }

            merged.Add(current);
        }

        if (merged.Count > 50)
            merged = merged.Take(50).ToList();

        File.WriteAllText(_path, JsonSerializer.Serialize(merged));
        return merged;
    }
}
