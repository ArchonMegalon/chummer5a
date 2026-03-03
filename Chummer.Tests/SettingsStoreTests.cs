using System;
using System.IO;
using System.Text.Json.Nodes;
using Chummer.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class SettingsStoreTests
{
    [TestMethod]
    public void Load_returns_empty_json_when_scope_file_is_missing()
    {
        string stateDirectory = CreateTempStateDirectory();
        try
        {
            var store = new FileSettingsStore(stateDirectory);

            JsonObject settings = store.Load("global");

            Assert.AreEqual(0, settings.Count);
        }
        finally
        {
            Directory.Delete(stateDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void Save_and_load_roundtrip_preserves_values()
    {
        string stateDirectory = CreateTempStateDirectory();
        try
        {
            var store = new FileSettingsStore(stateDirectory);
            JsonObject expected = new()
            {
                ["uiScale"] = 120,
                ["theme"] = "classic",
                ["compactMode"] = true
            };

            store.Save("global", expected);
            JsonObject actual = store.Load("global");

            Assert.AreEqual(120, actual["uiScale"]?.GetValue<int>());
            Assert.AreEqual("classic", actual["theme"]?.GetValue<string>());
            Assert.AreEqual(true, actual["compactMode"]?.GetValue<bool>());
        }
        finally
        {
            Directory.Delete(stateDirectory, recursive: true);
        }
    }

    private static string CreateTempStateDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "chummer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
