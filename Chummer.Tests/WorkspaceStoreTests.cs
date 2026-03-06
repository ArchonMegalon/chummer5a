using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
using Chummer.Infrastructure.Workspaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class WorkspaceStoreTests
{
    [TestMethod]
    public void File_workspace_store_create_and_get_roundtrip()
    {
        string stateDirectory = CreateTempStateDirectory();
        try
        {
            FileWorkspaceStore store = new(stateDirectory);
            WorkspaceDocument expected = new("<character><name>Neo</name></character>");

            CharacterWorkspaceId id = store.Create(expected);
            bool found = store.TryGet(id, out WorkspaceDocument actual);

            Assert.IsTrue(found);
            Assert.AreEqual(expected.State, actual.State);
            Assert.AreEqual(expected.PayloadEnvelope.Payload, actual.PayloadEnvelope.Payload);
            Assert.AreEqual(expected.Format, actual.Format);
            Assert.AreEqual(RulesetDefaults.Sr5, actual.PayloadEnvelope.RulesetId);
        }
        finally
        {
            Directory.Delete(stateDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void File_workspace_store_persists_across_instances()
    {
        string stateDirectory = CreateTempStateDirectory();
        try
        {
            CharacterWorkspaceId id;
            {
                FileWorkspaceStore store = new(stateDirectory);
                id = store.Create(new WorkspaceDocument("<character><alias>BLUE</alias></character>"));
            }

            {
                FileWorkspaceStore store = new(stateDirectory);
                bool found = store.TryGet(id, out WorkspaceDocument loaded);
                Assert.IsTrue(found);
                StringAssert.Contains(loaded.PayloadEnvelope.Payload, "BLUE");
            }
        }
        finally
        {
            Directory.Delete(stateDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void File_workspace_store_persists_ruleset_id_across_instances()
    {
        string stateDirectory = CreateTempStateDirectory();
        try
        {
            CharacterWorkspaceId id;
            {
                FileWorkspaceStore store = new(stateDirectory);
                id = store.Create(new WorkspaceDocument("<character><name>Ruleset</name></character>", RulesetId: "SR6"));
            }

            {
                FileWorkspaceStore store = new(stateDirectory);
                bool found = store.TryGet(id, out WorkspaceDocument loaded);
                Assert.IsTrue(found);
                Assert.AreEqual("sr6", loaded.PayloadEnvelope.RulesetId);
            }
        }
        finally
        {
            Directory.Delete(stateDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void File_workspace_store_persists_internal_payload_envelope()
    {
        string stateDirectory = CreateTempStateDirectory();
        try
        {
            FileWorkspaceStore store = new(stateDirectory);
            CharacterWorkspaceId id = store.Create(new WorkspaceDocument(
                "<character><name>Envelope</name></character>",
                RulesetId: "SR6"));
            string persistedPath = Path.Combine(stateDirectory, "workspaces", $"{id.Value}.json");

            using JsonDocument json = JsonDocument.Parse(File.ReadAllText(persistedPath));
            JsonElement root = json.RootElement;
            Assert.IsTrue(root.TryGetProperty("Envelope", out JsonElement envelope));
            Assert.IsFalse(root.TryGetProperty("Content", out _));
            Assert.IsFalse(root.TryGetProperty("RulesetId", out _));
            Assert.AreEqual("sr6", envelope.GetProperty("RulesetId").GetString());
            Assert.AreEqual(1, envelope.GetProperty("SchemaVersion").GetInt32());
            Assert.AreEqual("workspace", envelope.GetProperty("PayloadKind").GetString());
            StringAssert.Contains(envelope.GetProperty("Payload").GetString() ?? string.Empty, "<character>");
        }
        finally
        {
            Directory.Delete(stateDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void File_workspace_store_reads_legacy_payload_without_envelope()
    {
        string stateDirectory = CreateTempStateDirectory();
        try
        {
            FileWorkspaceStore store = new(stateDirectory);
            CharacterWorkspaceId id = new("legacypayload");
            string persistedPath = Path.Combine(stateDirectory, "workspaces", $"{id.Value}.json");
            File.WriteAllText(
                persistedPath,
                """
                {
                  "Content": "<character><name>Legacy</name></character>",
                  "Format": "Chum5Xml",
                  "RulesetId": "SR6"
                }
                """);

            bool found = store.TryGet(id, out WorkspaceDocument loaded);

            Assert.IsTrue(found);
            StringAssert.Contains(loaded.PayloadEnvelope.Payload, "Legacy");
            StringAssert.Contains(loaded.State.Payload, "Legacy");
            Assert.AreEqual(WorkspaceDocumentFormat.NativeXml, loaded.Format);
            Assert.AreEqual("sr6", loaded.PayloadEnvelope.RulesetId);
        }
        finally
        {
            Directory.Delete(stateDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void File_workspace_store_rejects_invalid_ids()
    {
        string stateDirectory = CreateTempStateDirectory();
        try
        {
            FileWorkspaceStore store = new(stateDirectory);
            bool found = store.TryGet(new CharacterWorkspaceId("../bad"), out _);
            Assert.IsFalse(found);
        }
        finally
        {
            Directory.Delete(stateDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void File_workspace_store_returns_false_for_corrupt_payload()
    {
        string stateDirectory = CreateTempStateDirectory();
        try
        {
            FileWorkspaceStore store = new(stateDirectory);
            CharacterWorkspaceId id = store.Create(new WorkspaceDocument("<character><name>Neo</name></character>"));
            string persistedPath = Path.Combine(stateDirectory, "workspaces", $"{id.Value}.json");

            File.WriteAllText(persistedPath, "{invalid-json");

            bool found = store.TryGet(id, out _);
            Assert.IsFalse(found);
        }
        finally
        {
            Directory.Delete(stateDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void File_workspace_store_lists_created_workspaces()
    {
        string stateDirectory = CreateTempStateDirectory();
        try
        {
            FileWorkspaceStore store = new(stateDirectory);
            CharacterWorkspaceId first = store.Create(new WorkspaceDocument("<character><name>First</name></character>"));
            CharacterWorkspaceId second = store.Create(new WorkspaceDocument("<character><name>Second</name></character>"));

            IReadOnlyList<WorkspaceStoreEntry> listed = store.List();

            CollectionAssert.AreEquivalent(
                new[] { first.Value, second.Value },
                listed.Select(item => item.Id.Value).ToArray());
        }
        finally
        {
            Directory.Delete(stateDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void File_workspace_store_delete_removes_workspace()
    {
        string stateDirectory = CreateTempStateDirectory();
        try
        {
            FileWorkspaceStore store = new(stateDirectory);
            CharacterWorkspaceId id = store.Create(new WorkspaceDocument("<character><name>DeleteMe</name></character>"));

            bool deleted = store.Delete(id);
            bool found = store.TryGet(id, out _);

            Assert.IsTrue(deleted);
            Assert.IsFalse(found);
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
