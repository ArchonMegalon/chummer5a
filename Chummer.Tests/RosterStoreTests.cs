using System;
using System.IO;
using Chummer.Contracts.Api;
using Chummer.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class RosterStoreTests
{
    [TestMethod]
    public void Upsert_adds_new_entry_and_avoids_duplicate_name_alias_pairs()
    {
        string stateDirectory = CreateTempStateDirectory();
        try
        {
            var store = new FileRosterStore(stateDirectory);
            RosterEntry entry = new("BLUE", "Troy", "Ork", DateTimeOffset.UtcNow.ToString("O"));

            IReadOnlyList<RosterEntry> first = store.Upsert(entry);
            IReadOnlyList<RosterEntry> second = store.Upsert(entry);

            Assert.AreEqual(1, first.Count);
            Assert.AreEqual(1, second.Count);
        }
        finally
        {
            Directory.Delete(stateDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void Upsert_enforces_maximum_of_fifty_entries()
    {
        string stateDirectory = CreateTempStateDirectory();
        try
        {
            var store = new FileRosterStore(stateDirectory);

            for (int i = 0; i < 55; i++)
            {
                store.Upsert(new RosterEntry($"Name-{i}", $"Alias-{i}", "Human", DateTimeOffset.UtcNow.ToString("O")));
            }

            IReadOnlyList<RosterEntry> entries = store.Load();
            Assert.AreEqual(50, entries.Count);
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
