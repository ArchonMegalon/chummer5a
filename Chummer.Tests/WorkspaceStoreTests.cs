using System;
using System.IO;
using Chummer.Application.Workspaces;
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
            IWorkspaceStore store = new FileWorkspaceStore(stateDirectory);
            WorkspaceDocument expected = new("<character><name>Neo</name></character>");

            CharacterWorkspaceId id = store.Create(expected);
            bool found = store.TryGet(id, out WorkspaceDocument actual);

            Assert.IsTrue(found);
            Assert.AreEqual(expected.Content, actual.Content);
            Assert.AreEqual(expected.Format, actual.Format);
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
                IWorkspaceStore store = new FileWorkspaceStore(stateDirectory);
                id = store.Create(new WorkspaceDocument("<character><alias>BLUE</alias></character>"));
            }

            {
                IWorkspaceStore store = new FileWorkspaceStore(stateDirectory);
                bool found = store.TryGet(id, out WorkspaceDocument loaded);
                Assert.IsTrue(found);
                StringAssert.Contains(loaded.Content, "BLUE");
            }
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
            IWorkspaceStore store = new FileWorkspaceStore(stateDirectory);
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
            IWorkspaceStore store = new FileWorkspaceStore(stateDirectory);
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

    private static string CreateTempStateDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "chummer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
