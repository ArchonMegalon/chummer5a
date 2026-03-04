using System;
using System.Collections.Generic;
using System.Linq;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Presentation;

[TestClass]
public class WorkspaceSessionPresenterTests
{
    [TestMethod]
    public void Restore_sets_active_workspace_and_recent_order()
    {
        WorkspaceSessionPresenter presenter = new();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        WorkspaceListItem[] workspaces =
        [
            CreateWorkspace("ws-old", "Old", "O", now.AddMinutes(-20)),
            CreateWorkspace("ws-new", "New", "N", now.AddMinutes(-5))
        ];

        WorkspaceSessionState state = presenter.Restore(workspaces);

        Assert.AreEqual("ws-new", state.ActiveWorkspaceId?.Value);
        CollectionAssert.AreEqual(
            new[] { "ws-new", "ws-old" },
            state.OpenWorkspaces.Select(workspace => workspace.Id.Value).ToArray());
        CollectionAssert.AreEqual(
            new[] { "ws-new", "ws-old" },
            state.RecentWorkspaceIds.Select(id => id.Value).ToArray());
    }

    [TestMethod]
    public void Switch_updates_active_workspace_and_recent_order()
    {
        WorkspaceSessionPresenter presenter = new();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        presenter.Restore(
        [
            CreateWorkspace("ws-1", "One", "A", now.AddMinutes(-10)),
            CreateWorkspace("ws-2", "Two", "B", now.AddMinutes(-5))
        ]);

        WorkspaceSessionState switched = presenter.Switch(new CharacterWorkspaceId("ws-1"));

        Assert.AreEqual("ws-1", switched.ActiveWorkspaceId?.Value);
        CollectionAssert.AreEqual(
            new[] { "ws-1", "ws-2" },
            switched.RecentWorkspaceIds.Select(id => id.Value).ToArray());
    }

    [TestMethod]
    public void Open_activates_workspace_and_upserts_profile_label()
    {
        WorkspaceSessionPresenter presenter = new();
        CharacterWorkspaceId workspaceId = new("ws-open");
        CharacterProfileSection profile = CreateProfile("Opened Character", "OPEN");

        WorkspaceSessionState opened = presenter.Open(workspaceId, profile);

        Assert.AreEqual("ws-open", opened.ActiveWorkspaceId?.Value);
        Assert.AreEqual(1, opened.OpenWorkspaces.Count);
        Assert.AreEqual("Opened Character", opened.OpenWorkspaces[0].Name);
        Assert.AreEqual("OPEN", opened.OpenWorkspaces[0].Alias);
        CollectionAssert.AreEqual(
            new[] { "ws-open" },
            opened.RecentWorkspaceIds.Select(id => id.Value).ToArray());
    }

    [TestMethod]
    public void Close_active_workspace_selects_most_recent_remaining_workspace()
    {
        WorkspaceSessionPresenter presenter = new();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        presenter.Restore(
        [
            CreateWorkspace("ws-1", "One", "A", now.AddMinutes(-15)),
            CreateWorkspace("ws-2", "Two", "B", now.AddMinutes(-10)),
            CreateWorkspace("ws-3", "Three", "C", now.AddMinutes(-5))
        ]);

        presenter.Switch(new CharacterWorkspaceId("ws-1"));
        WorkspaceSessionState closed = presenter.Close(new CharacterWorkspaceId("ws-1"));

        Assert.AreEqual("ws-3", closed.ActiveWorkspaceId?.Value);
        CollectionAssert.AreEquivalent(
            new[] { "ws-2", "ws-3" },
            closed.OpenWorkspaces.Select(workspace => workspace.Id.Value).ToArray());
    }

    [TestMethod]
    public void CloseAll_clears_open_workspaces_and_active_workspace()
    {
        WorkspaceSessionPresenter presenter = new();
        presenter.Open(new CharacterWorkspaceId("ws-1"), CreateProfile("One", "A"));
        presenter.Open(new CharacterWorkspaceId("ws-2"), CreateProfile("Two", "B"));

        WorkspaceSessionState cleared = presenter.CloseAll();

        Assert.IsNull(cleared.ActiveWorkspaceId);
        Assert.AreEqual(0, cleared.OpenWorkspaces.Count);
        CollectionAssert.AreEqual(
            new[] { "ws-2", "ws-1" },
            cleared.RecentWorkspaceIds.Select(id => id.Value).ToArray());
    }

    private static WorkspaceListItem CreateWorkspace(
        string id,
        string name,
        string alias,
        DateTimeOffset lastUpdatedUtc)
    {
        return new WorkspaceListItem(
            Id: new CharacterWorkspaceId(id),
            Summary: new CharacterFileSummary(
                Name: name,
                Alias: alias,
                Metatype: "Human",
                BuildMethod: "Priority",
                CreatedVersion: "5",
                AppVersion: "5",
                Karma: 0m,
                Nuyen: 0m,
                Created: true),
            LastUpdatedUtc: lastUpdatedUtc);
    }

    private static CharacterProfileSection CreateProfile(string name, string alias)
    {
        return new CharacterProfileSection(
            Name: name,
            Alias: alias,
            PlayerName: string.Empty,
            Metatype: "Human",
            Metavariant: string.Empty,
            Sex: string.Empty,
            Age: string.Empty,
            Height: string.Empty,
            Weight: string.Empty,
            Hair: string.Empty,
            Eyes: string.Empty,
            Skin: string.Empty,
            Concept: string.Empty,
            Description: string.Empty,
            Background: string.Empty,
            CreatedVersion: string.Empty,
            AppVersion: string.Empty,
            BuildMethod: "Priority",
            GameplayOption: string.Empty,
            Created: true,
            Adept: false,
            Magician: false,
            Technomancer: false,
            AI: false,
            MainMugshotIndex: 0,
            MugshotCount: 0);
    }
}
