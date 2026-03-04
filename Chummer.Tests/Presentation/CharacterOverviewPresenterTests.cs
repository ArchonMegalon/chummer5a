using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.Json.Nodes;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation;
using Chummer.Presentation.Overview;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Presentation;

[TestClass]
public class CharacterOverviewPresenterTests
{
    [TestMethod]
    public async Task InitializeAsync_loads_command_catalog()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.InitializeAsync(CancellationToken.None);

        Assert.IsFalse(presenter.State.IsBusy);
        Assert.IsNull(presenter.State.Error);
        Assert.IsTrue(presenter.State.Commands.Count > 0);
        Assert.AreEqual("new_character", presenter.State.Commands[0].Id);
        Assert.IsTrue(presenter.State.NavigationTabs.Count > 0);
        Assert.AreEqual("tab-info", presenter.State.NavigationTabs[0].Id);
    }

    [TestMethod]
    public async Task InitializeAsync_restores_open_workspaces_from_service()
    {
        var client = new FakeChummerClient();
        client.SeedWorkspace("ws-legacy-1", "Legacy One", "L1", DateTimeOffset.UtcNow.AddMinutes(-10));
        client.SeedWorkspace("ws-legacy-2", "Legacy Two", "L2", DateTimeOffset.UtcNow.AddMinutes(-1));
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.InitializeAsync(CancellationToken.None);

        Assert.AreEqual(2, presenter.State.OpenWorkspaces.Count);
        Assert.AreEqual("ws-legacy-2", presenter.State.OpenWorkspaces[0].Id.Value);
        Assert.AreEqual("ws-legacy-1", presenter.State.OpenWorkspaces[1].Id.Value);
        StringAssert.Contains(presenter.State.Notice ?? string.Empty, "Restored 2 workspace(s)");
    }

    [TestMethod]
    public async Task LoadAsync_populates_profile_progress_and_skills()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);

        Assert.IsFalse(presenter.State.IsBusy);
        Assert.IsNull(presenter.State.Error);
        Assert.IsNotNull(presenter.State.Profile);
        Assert.IsNotNull(presenter.State.Progress);
        Assert.IsNotNull(presenter.State.Skills);
        Assert.IsNotNull(presenter.State.Rules);
        Assert.IsNotNull(presenter.State.Build);
        Assert.IsNotNull(presenter.State.Movement);
        Assert.IsNotNull(presenter.State.Awakening);
        Assert.AreEqual("ws-1", presenter.State.WorkspaceId?.Value);
        Assert.AreEqual("BLUE", presenter.State.Profile.Alias);
    }

    [TestMethod]
    public async Task ImportAsync_loads_workspace_and_sections()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.ImportAsync(
            new WorkspaceImportDocument("<character><name>Imported</name></character>", WorkspaceDocumentFormat.Chum5Xml),
            CancellationToken.None);

        Assert.IsFalse(presenter.State.IsBusy);
        Assert.IsNull(presenter.State.Error);
        Assert.AreEqual("ws-1", presenter.State.WorkspaceId?.Value);
        Assert.IsNotNull(presenter.State.Profile);
        Assert.IsNotNull(presenter.State.Progress);
        Assert.IsNotNull(presenter.State.Skills);
        Assert.IsNotNull(presenter.State.Rules);
        Assert.IsNotNull(presenter.State.Build);
        Assert.IsNotNull(presenter.State.Movement);
        Assert.IsNotNull(presenter.State.Awakening);
    }

    [TestMethod]
    public async Task LoadAsync_tracks_open_workspaces_for_multi_document_shell_state()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        await presenter.LoadAsync(new CharacterWorkspaceId("ws-2"), CancellationToken.None);

        Assert.AreEqual("ws-2", presenter.State.WorkspaceId?.Value);
        Assert.AreEqual(2, presenter.State.OpenWorkspaces.Count);
        CollectionAssert.AreEquivalent(
            new[] { "ws-1", "ws-2" },
            presenter.State.OpenWorkspaces.Select(workspace => workspace.Id.Value).ToArray());
    }

    [TestMethod]
    public async Task SwitchWorkspaceAsync_restores_workspace_specific_tab_and_section_context()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.InitializeAsync(CancellationToken.None);
        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        await presenter.SelectTabAsync("tab-info", CancellationToken.None);

        await presenter.LoadAsync(new CharacterWorkspaceId("ws-2"), CancellationToken.None);
        await presenter.SelectTabAsync("tab-gear", CancellationToken.None);

        await presenter.SwitchWorkspaceAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        Assert.AreEqual("ws-1", presenter.State.WorkspaceId?.Value);
        Assert.AreEqual("tab-info", presenter.State.ActiveTabId);
        Assert.AreEqual("profile", presenter.State.ActiveSectionId);

        await presenter.SwitchWorkspaceAsync(new CharacterWorkspaceId("ws-2"), CancellationToken.None);
        Assert.AreEqual("ws-2", presenter.State.WorkspaceId?.Value);
        Assert.AreEqual("tab-gear", presenter.State.ActiveTabId);
        Assert.AreEqual("gear", presenter.State.ActiveSectionId);
    }

    [TestMethod]
    public async Task CloseWorkspaceAsync_closes_active_workspace_and_switches_to_recent_workspace()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        await presenter.LoadAsync(new CharacterWorkspaceId("ws-2"), CancellationToken.None);
        await presenter.CloseWorkspaceAsync(new CharacterWorkspaceId("ws-2"), CancellationToken.None);

        Assert.AreEqual("ws-1", presenter.State.WorkspaceId?.Value);
        Assert.AreEqual("ws-1", presenter.State.Session.ActiveWorkspaceId?.Value);
        Assert.AreEqual(1, presenter.State.Session.OpenWorkspaces.Count);
        Assert.AreEqual("ws-1", presenter.State.Session.OpenWorkspaces[0].Id.Value);
    }

    [TestMethod]
    public async Task ExecuteCommandAsync_close_window_switches_to_previous_workspace()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        await presenter.LoadAsync(new CharacterWorkspaceId("ws-2"), CancellationToken.None);
        await presenter.ExecuteCommandAsync("close_window", CancellationToken.None);

        Assert.AreEqual("ws-1", presenter.State.WorkspaceId?.Value);
        Assert.AreEqual(1, presenter.State.OpenWorkspaces.Count);
        Assert.AreEqual("ws-1", presenter.State.OpenWorkspaces[0].Id.Value);
        Assert.IsTrue((presenter.State.Notice ?? string.Empty).Contains("Closed active workspace.", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task UpdateMetadataAsync_requires_loaded_workspace()
    {
        var presenter = new CharacterOverviewPresenter(new FakeChummerClient());

        await presenter.UpdateMetadataAsync(new UpdateWorkspaceMetadata("Name", "Alias", "Notes"), CancellationToken.None);

        Assert.AreEqual("No workspace loaded.", presenter.State.Error);
    }

    [TestMethod]
    public async Task UpdateMetadataAsync_updates_profile_when_client_succeeds()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        await presenter.UpdateMetadataAsync(new UpdateWorkspaceMetadata("Updated", "Alias", "Notes"), CancellationToken.None);

        Assert.IsNull(presenter.State.Error);
        Assert.AreEqual("Updated", presenter.State.Profile?.Name);
    }

    [TestMethod]
    public async Task SaveAsync_requires_loaded_workspace()
    {
        var presenter = new CharacterOverviewPresenter(new FakeChummerClient());

        await presenter.SaveAsync(CancellationToken.None);

        Assert.AreEqual("No workspace loaded.", presenter.State.Error);
    }

    [TestMethod]
    public async Task SaveAsync_marks_workspace_as_saved_after_workspace_load()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        await presenter.UpdateMetadataAsync(new UpdateWorkspaceMetadata("Updated", "Alias", "Notes"), CancellationToken.None);
        await presenter.SaveAsync(CancellationToken.None);

        Assert.IsNull(presenter.State.Error);
        Assert.IsTrue(presenter.State.HasSavedWorkspace);
    }

    [TestMethod]
    public async Task ExecuteCommandAsync_save_character_marks_workspace_as_saved()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        await presenter.ExecuteCommandAsync("save_character", CancellationToken.None);

        Assert.AreEqual("save_character", presenter.State.LastCommandId);
        Assert.IsTrue(presenter.State.HasSavedWorkspace);
        Assert.IsNull(presenter.State.Error);
    }

    [TestMethod]
    public async Task Save_character_as_command_prepares_download_without_marking_workspace_saved()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        await presenter.UpdateMetadataAsync(new UpdateWorkspaceMetadata("Updated", "Alias", "Notes"), CancellationToken.None);
        await presenter.ExecuteCommandAsync("save_character_as", CancellationToken.None);

        Assert.AreEqual("save_character_as", presenter.State.LastCommandId);
        Assert.AreEqual(1, client.DownloadCalls);
        Assert.IsFalse(presenter.State.HasSavedWorkspace);
        Assert.IsNull(presenter.State.Error);
        StringAssert.Contains(presenter.State.Notice ?? string.Empty, "Download prepared:");
        Assert.IsNotNull(presenter.State.PendingDownload);
        Assert.AreEqual(1L, presenter.State.PendingDownloadVersion);
        Assert.AreEqual("ws-1.chum5", presenter.State.PendingDownload?.FileName);
    }

    [TestMethod]
    public async Task SaveAsync_clears_pending_download_after_save_as()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        await presenter.ExecuteCommandAsync("save_character_as", CancellationToken.None);

        Assert.IsNotNull(presenter.State.PendingDownload);

        await presenter.SaveAsync(CancellationToken.None);

        Assert.IsNull(presenter.State.PendingDownload);
        Assert.IsNull(presenter.State.Error);
    }

    [TestMethod]
    public async Task Save_status_is_tracked_per_workspace_when_switching()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        await presenter.SaveAsync(CancellationToken.None);
        await presenter.LoadAsync(new CharacterWorkspaceId("ws-2"), CancellationToken.None);

        OpenWorkspaceState ws1AfterSecondLoad = presenter.State.OpenWorkspaces
            .First(workspace => string.Equals(workspace.Id.Value, "ws-1", StringComparison.Ordinal));
        Assert.IsTrue(ws1AfterSecondLoad.HasSavedWorkspace);
        Assert.IsFalse(presenter.State.HasSavedWorkspace);

        await presenter.SwitchWorkspaceAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);

        Assert.AreEqual("ws-1", presenter.State.WorkspaceId?.Value);
        Assert.IsTrue(presenter.State.HasSavedWorkspace);
        OpenWorkspaceState active = presenter.State.OpenWorkspaces
            .First(workspace => string.Equals(workspace.Id.Value, "ws-1", StringComparison.Ordinal));
        Assert.IsTrue(active.HasSavedWorkspace);
    }

    [TestMethod]
    public async Task ExecuteCommandAsync_unknown_command_sets_error()
    {
        var presenter = new CharacterOverviewPresenter(new FakeChummerClient());

        await presenter.ExecuteCommandAsync("nope", CancellationToken.None);

        Assert.AreEqual("nope", presenter.State.LastCommandId);
        StringAssert.Contains(presenter.State.Error ?? string.Empty, "not implemented");
    }

    [TestMethod]
    public async Task ExecuteCommandAsync_global_settings_opens_dialog()
    {
        var presenter = new CharacterOverviewPresenter(new FakeChummerClient());

        await presenter.ExecuteCommandAsync("global_settings", CancellationToken.None);

        Assert.AreEqual("global_settings", presenter.State.LastCommandId);
        Assert.IsNotNull(presenter.State.ActiveDialog);
        Assert.AreEqual("dialog.global_settings", presenter.State.ActiveDialog?.Id);
    }

    [TestMethod]
    public async Task ExecuteCommandAsync_open_character_opens_import_dialog()
    {
        var presenter = new CharacterOverviewPresenter(new FakeChummerClient());

        await presenter.ExecuteCommandAsync("open_character", CancellationToken.None);

        Assert.AreEqual("open_character", presenter.State.LastCommandId);
        Assert.IsNotNull(presenter.State.ActiveDialog);
        Assert.AreEqual("dialog.open_character", presenter.State.ActiveDialog?.Id);
    }

    [TestMethod]
    public async Task ExecuteDialogActionAsync_import_imports_workspace_from_open_character_dialog()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.ExecuteCommandAsync("open_character", CancellationToken.None);
        await presenter.UpdateDialogFieldAsync("openCharacterXml", "<character><name>Dialog Import</name></character>", CancellationToken.None);
        await presenter.ExecuteDialogActionAsync("import", CancellationToken.None);

        Assert.IsNotNull(client.LastImportedDocument);
        StringAssert.Contains(client.LastImportedDocument!.Content, "Dialog Import");
        Assert.AreEqual("ws-1", presenter.State.WorkspaceId?.Value);
        Assert.IsNull(presenter.State.ActiveDialog);
        Assert.AreEqual("Character imported.", presenter.State.Notice);
    }

    [TestMethod]
    public async Task HandleUiControlAsync_create_entry_opens_dialog()
    {
        var presenter = new CharacterOverviewPresenter(new FakeChummerClient());

        await presenter.HandleUiControlAsync("create_entry", CancellationToken.None);

        Assert.IsNotNull(presenter.State.ActiveDialog);
        Assert.AreEqual("dialog.ui.create_entry", presenter.State.ActiveDialog?.Id);
    }

    [TestMethod]
    public async Task HandleUiControlAsync_all_catalog_controls_are_non_generic()
    {
        var presenter = new CharacterOverviewPresenter(new FakeChummerClient());
        string[] controlIds = DesktopUiControlCatalog.All
            .Select(control => control.Id)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        foreach (string controlId in controlIds)
        {
            await presenter.HandleUiControlAsync(controlId, CancellationToken.None);
            Assert.AreNotEqual("dialog.ui.generic", presenter.State.ActiveDialog?.Id, $"Control '{controlId}' fell back to generic dialog.");
        }
    }

    [TestMethod]
    public async Task ExecuteCommandAsync_all_catalog_commands_are_handled()
    {
        AppCommandDefinition[] commands = AppCommandCatalog.All
            .Where(command => !string.Equals(command.Group, "menu", StringComparison.Ordinal))
            .ToArray();

        foreach (AppCommandDefinition command in commands)
        {
            var presenter = new CharacterOverviewPresenter(new FakeChummerClient());
            await presenter.InitializeAsync(CancellationToken.None);
            if (command.RequiresOpenCharacter)
            {
                await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
            }

            await presenter.ExecuteCommandAsync(command.Id, CancellationToken.None);

            string error = presenter.State.Error ?? string.Empty;
            Assert.IsFalse(
                error.Contains("not implemented", StringComparison.OrdinalIgnoreCase),
                $"Command '{command.Id}' fell through to not-implemented: {error}");
        }
    }

    [TestMethod]
    public async Task ExecuteCommandAsync_dialog_commands_use_non_generic_dialog_templates()
    {
        string[] dialogCommands =
        [
            "new_window",
            "wiki",
            "discord",
            "revision_history",
            "dumpshock",
            "print_setup",
            "print_multiple",
            "print_character",
            "dice_roller",
            "global_settings",
            "character_settings",
            "translator",
            "xml_editor",
            "master_index",
            "character_roster",
            "data_exporter",
            "export_character",
            "report_bug",
            "about",
            "hero_lab_importer",
            "update"
        ];

        foreach (string commandId in dialogCommands)
        {
            var presenter = new CharacterOverviewPresenter(new FakeChummerClient());
            await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
            await presenter.ExecuteCommandAsync(commandId, CancellationToken.None);

            Assert.IsNotNull(presenter.State.ActiveDialog, $"Command '{commandId}' did not open a dialog.");
            Assert.AreNotEqual("dialog.generic", presenter.State.ActiveDialog?.Id, $"Command '{commandId}' fell back to generic dialog template.");
        }
    }

    [TestMethod]
    public async Task ExecuteWorkspaceActionAsync_summary_sets_active_summary_payload()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.InitializeAsync(CancellationToken.None);
        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        WorkspaceSurfaceActionDefinition action = WorkspaceSurfaceActionCatalog.All
            .First(item => string.Equals(item.Id, "tab-info.summary", StringComparison.Ordinal));

        await presenter.ExecuteWorkspaceActionAsync(action, CancellationToken.None);

        Assert.AreEqual("summary", presenter.State.ActiveSectionId);
        Assert.AreEqual("tab-info.summary", presenter.State.ActiveActionId);
        StringAssert.Contains(presenter.State.ActiveSectionJson ?? string.Empty, "\"Name\": \"Troy Simmons\"");
        Assert.IsTrue(presenter.State.ActiveSectionRows.Count > 0);
    }

    [TestMethod]
    public async Task ExecuteWorkspaceActionAsync_metadata_applies_profile_updates_from_dialog()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);
        WorkspaceSurfaceActionDefinition action = WorkspaceSurfaceActionCatalog.All
            .First(item => string.Equals(item.Id, "tab-info.metadata", StringComparison.Ordinal));

        await presenter.InitializeAsync(CancellationToken.None);
        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        await presenter.ExecuteWorkspaceActionAsync(action, CancellationToken.None);
        await presenter.UpdateDialogFieldAsync("metadataName", "Dialog Updated", CancellationToken.None);
        await presenter.UpdateDialogFieldAsync("metadataAlias", "Dialog Alias", CancellationToken.None);
        await presenter.ExecuteDialogActionAsync("apply_metadata", CancellationToken.None);

        Assert.IsNull(presenter.State.ActiveDialog);
        Assert.AreEqual("Dialog Updated", presenter.State.Profile?.Name);
        Assert.AreEqual("Dialog Alias", presenter.State.Profile?.Alias);
    }

    [TestMethod]
    public async Task ExecuteWorkspaceActionAsync_metadata_blank_notes_are_treated_as_no_change()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);
        WorkspaceSurfaceActionDefinition action = WorkspaceSurfaceActionCatalog.All
            .First(item => string.Equals(item.Id, "tab-info.metadata", StringComparison.Ordinal));

        await presenter.InitializeAsync(CancellationToken.None);
        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        await presenter.UpdateMetadataAsync(new UpdateWorkspaceMetadata(null, null, "Existing Notes"), CancellationToken.None);
        await presenter.ExecuteWorkspaceActionAsync(action, CancellationToken.None);
        await presenter.UpdateDialogFieldAsync("metadataNotes", string.Empty, CancellationToken.None);
        await presenter.ExecuteDialogActionAsync("apply_metadata", CancellationToken.None);

        Assert.IsNotNull(client.LastUpdateMetadata);
        Assert.IsNull(client.LastUpdateMetadata!.Notes);
        Assert.AreEqual("Existing Notes", presenter.State.Preferences.CharacterNotes);
    }

    [TestMethod]
    public async Task UpdateMetadataAsync_updates_preference_notes_when_notes_are_provided()
    {
        var presenter = new CharacterOverviewPresenter(new FakeChummerClient());

        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        await presenter.UpdateMetadataAsync(new UpdateWorkspaceMetadata(null, null, "Desk Notes"), CancellationToken.None);

        Assert.AreEqual("Desk Notes", presenter.State.Preferences.CharacterNotes);
    }

    [TestMethod]
    public async Task ExecuteDialogActionAsync_roll_updates_dice_dialog_result_field()
    {
        var presenter = new CharacterOverviewPresenter(new FakeChummerClient());

        await presenter.ExecuteCommandAsync("dice_roller", CancellationToken.None);
        await presenter.UpdateDialogFieldAsync("diceExpression", "3d6+2", CancellationToken.None);
        await presenter.ExecuteDialogActionAsync("roll", CancellationToken.None);

        Assert.IsNotNull(presenter.State.ActiveDialog);
        Assert.IsNotNull(presenter.State.ActiveDialog?.Fields.FirstOrDefault(field => string.Equals(field.Id, "diceResult", StringComparison.Ordinal)));
        StringAssert.Contains(presenter.State.Notice ?? string.Empty, "3d6+2");
    }

    [TestMethod]
    public async Task ExecuteDialogActionAsync_save_global_settings_updates_preferences()
    {
        var presenter = new CharacterOverviewPresenter(new FakeChummerClient());

        await presenter.ExecuteCommandAsync("global_settings", CancellationToken.None);
        await presenter.UpdateDialogFieldAsync("globalUiScale", "125", CancellationToken.None);
        await presenter.UpdateDialogFieldAsync("globalTheme", "dark-steel", CancellationToken.None);
        await presenter.UpdateDialogFieldAsync("globalLanguage", "de-de", CancellationToken.None);
        await presenter.UpdateDialogFieldAsync("globalCompactMode", "true", CancellationToken.None);
        await presenter.ExecuteDialogActionAsync("save", CancellationToken.None);

        Assert.AreEqual(125, presenter.State.Preferences.UiScalePercent);
        Assert.AreEqual("dark-steel", presenter.State.Preferences.Theme);
        Assert.AreEqual("de-de", presenter.State.Preferences.Language);
        Assert.IsTrue(presenter.State.Preferences.CompactMode);
        Assert.IsNull(presenter.State.ActiveDialog);
    }

    [TestMethod]
    public async Task SelectTabAsync_requires_loaded_workspace()
    {
        var presenter = new CharacterOverviewPresenter(new FakeChummerClient());

        await presenter.SelectTabAsync("tab-info", CancellationToken.None);

        Assert.AreEqual("No workspace loaded.", presenter.State.Error);
    }

    [TestMethod]
    public async Task SelectTabAsync_loads_active_section_preview_after_workspace_load()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.InitializeAsync(CancellationToken.None);
        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        await presenter.SelectTabAsync("tab-info", CancellationToken.None);

        Assert.AreEqual("tab-info", presenter.State.ActiveTabId);
        Assert.AreEqual("profile", presenter.State.ActiveSectionId);
        StringAssert.Contains(presenter.State.ActiveSectionJson ?? string.Empty, "\"sectionId\": \"profile\"");
        Assert.IsTrue(presenter.State.ActiveSectionRows.Count > 0);
    }

    private sealed class FakeChummerClient : IChummerClient
    {
        private string _name = "Troy Simmons";
        private string _alias = "BLUE";
        private readonly Dictionary<string, WorkspaceListItem> _workspaces = new(StringComparer.Ordinal);
        private int _clock;
        public int DownloadCalls { get; private set; }
        public UpdateWorkspaceMetadata? LastUpdateMetadata { get; private set; }
        public WorkspaceImportDocument? LastImportedDocument { get; private set; }
        private static readonly IReadOnlyList<AppCommandDefinition> Commands =
        [
            new("new_character", "command.new_character", "file", false, true),
            new("save_character", "command.save_character", "file", true, true)
        ];
        private static readonly IReadOnlyList<NavigationTabDefinition> Tabs =
        [
            new("tab-info", "Info", "profile", "character", true, true),
            new("tab-gear", "Gear", "gear", "character", true, true)
        ];

        public void SeedWorkspace(string workspaceId, string name, string alias, DateTimeOffset? lastUpdatedUtc = null)
        {
            CharacterFileSummary summary = new(
                Name: name,
                Alias: alias,
                Metatype: "Human",
                BuildMethod: "Priority",
                CreatedVersion: "1.0",
                AppVersion: "1.0",
                Karma: 0m,
                Nuyen: 0m,
                Created: true);
            DateTimeOffset timestamp = lastUpdatedUtc ?? DateTimeOffset.UtcNow.AddMinutes(++_clock);
            _workspaces[workspaceId] = new WorkspaceListItem(new CharacterWorkspaceId(workspaceId), summary, timestamp);
        }

        public Task<WorkspaceImportResult> ImportAsync(WorkspaceImportDocument document, CancellationToken ct)
        {
            LastImportedDocument = document;
            SeedWorkspace("ws-1", "Imported", _alias);
            WorkspaceImportResult result = new(
                new CharacterWorkspaceId("ws-1"),
                new CharacterFileSummary(
                    Name: "Imported",
                    Alias: _alias,
                    Metatype: "Ork",
                    BuildMethod: "SumtoTen",
                    CreatedVersion: "1.0",
                    AppVersion: "1.0",
                    Karma: 0m,
                    Nuyen: 0m,
                    Created: true));

            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<WorkspaceListItem>> ListWorkspacesAsync(CancellationToken ct)
        {
            IReadOnlyList<WorkspaceListItem> workspaces = _workspaces.Values
                .OrderByDescending(workspace => workspace.LastUpdatedUtc)
                .ToArray();
            return Task.FromResult(workspaces);
        }

        public Task<bool> CloseWorkspaceAsync(CharacterWorkspaceId id, CancellationToken ct)
        {
            bool removed = _workspaces.Remove(id.Value);
            return Task.FromResult(removed);
        }

        public Task<IReadOnlyList<AppCommandDefinition>> GetCommandsAsync(CancellationToken ct)
        {
            return Task.FromResult(Commands);
        }

        public Task<IReadOnlyList<NavigationTabDefinition>> GetNavigationTabsAsync(CancellationToken ct)
        {
            return Task.FromResult(Tabs);
        }

        public Task<JsonNode> GetSectionAsync(CharacterWorkspaceId id, string sectionId, CancellationToken ct)
        {
            JsonObject section = new()
            {
                ["workspaceId"] = id.Value,
                ["sectionId"] = sectionId
            };

            return Task.FromResult<JsonNode>(section);
        }

        public Task<CharacterFileSummary> GetSummaryAsync(CharacterWorkspaceId id, CancellationToken ct)
        {
            return Task.FromResult(new CharacterFileSummary(
                Name: _name,
                Alias: _alias,
                Metatype: "Ork",
                BuildMethod: "SumtoTen",
                CreatedVersion: "1.0",
                AppVersion: "1.0",
                Karma: 12m,
                Nuyen: 5000m,
                Created: true));
        }

        public Task<CharacterValidationResult> ValidateAsync(CharacterWorkspaceId id, CancellationToken ct)
        {
            return Task.FromResult(new CharacterValidationResult(
                IsValid: true,
                Issues: []));
        }

        public Task<CharacterProfileSection> GetProfileAsync(CharacterWorkspaceId id, CancellationToken ct)
        {
            SeedWorkspace(id.Value, _name, _alias);
            CharacterProfileSection profile = new(
                Name: _name,
                Alias: _alias,
                PlayerName: string.Empty,
                Metatype: "Ork",
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
                BuildMethod: "SumtoTen",
                GameplayOption: string.Empty,
                Created: true,
                Adept: false,
                Magician: false,
                Technomancer: false,
                AI: false,
                MainMugshotIndex: 0,
                MugshotCount: 0);

            return Task.FromResult(profile);
        }

        public Task<CharacterProgressSection> GetProgressAsync(CharacterWorkspaceId id, CancellationToken ct)
        {
            CharacterProgressSection progress = new(
                Karma: 12m,
                Nuyen: 5000m,
                StartingNuyen: 0m,
                StreetCred: 1,
                Notoriety: 0,
                PublicAwareness: 0,
                BurntStreetCred: 0,
                BuildKarma: 0,
                TotalAttributes: 0,
                TotalSpecial: 0,
                PhysicalCmFilled: 0,
                StunCmFilled: 0,
                TotalEssence: 6m,
                InitiateGrade: 0,
                SubmersionGrade: 0,
                MagEnabled: false,
                ResEnabled: false,
                DepEnabled: false);

            return Task.FromResult(progress);
        }

        public Task<CharacterSkillsSection> GetSkillsAsync(CharacterWorkspaceId id, CancellationToken ct)
        {
            CharacterSkillsSection skills = new(
                Count: 1,
                KnowledgeCount: 0,
                Skills:
                [
                    new CharacterSkillSummary(
                        Guid: "1",
                        Suid: string.Empty,
                        Category: "Combat",
                        IsKnowledge: false,
                        BaseValue: 6,
                        KarmaValue: 0,
                        Specializations: ["Semi-Automatics"])
                ]);

            return Task.FromResult(skills);
        }

        public Task<CharacterRulesSection> GetRulesAsync(CharacterWorkspaceId id, CancellationToken ct)
        {
            CharacterRulesSection rules = new(
                GameEdition: "SR5",
                Settings: "default.xml",
                GameplayOption: "Standard",
                GameplayOptionQualityLimit: 25,
                MaxNuyen: 10,
                MaxKarma: 25,
                ContactMultiplier: 3,
                BannedWareGrades: ["Betaware"]);

            return Task.FromResult(rules);
        }

        public Task<CharacterBuildSection> GetBuildAsync(CharacterWorkspaceId id, CancellationToken ct)
        {
            CharacterBuildSection build = new(
                BuildMethod: "SumtoTen",
                PriorityMetatype: "C,2",
                PriorityAttributes: "E,0",
                PrioritySpecial: "A,4",
                PrioritySkills: "B,3",
                PriorityResources: "D,1",
                PriorityTalent: "Mundane",
                SumToTen: 10,
                Special: 1,
                TotalSpecial: 4,
                TotalAttributes: 20,
                ContactPoints: 15,
                ContactPointsUsed: 8);

            return Task.FromResult(build);
        }

        public Task<CharacterMovementSection> GetMovementAsync(CharacterWorkspaceId id, CancellationToken ct)
        {
            CharacterMovementSection movement = new(
                Walk: "2/1/0",
                Run: "4/0/0",
                Sprint: "2/1/0",
                WalkAlt: "2/1/0",
                RunAlt: "4/0/0",
                SprintAlt: "2/1/0",
                PhysicalCmFilled: 0,
                StunCmFilled: 0);

            return Task.FromResult(movement);
        }

        public Task<CharacterAwakeningSection> GetAwakeningAsync(CharacterWorkspaceId id, CancellationToken ct)
        {
            CharacterAwakeningSection awakening = new(
                MagEnabled: false,
                ResEnabled: false,
                DepEnabled: false,
                Adept: false,
                Magician: false,
                Technomancer: false,
                AI: false,
                InitiateGrade: 0,
                SubmersionGrade: 0,
                Tradition: string.Empty,
                TraditionName: string.Empty,
                TraditionDrain: string.Empty,
                SpiritCombat: string.Empty,
                SpiritDetection: string.Empty,
                SpiritHealth: string.Empty,
                SpiritIllusion: string.Empty,
                SpiritManipulation: string.Empty,
                Stream: string.Empty,
                StreamDrain: string.Empty,
                CurrentCounterspellingDice: 0,
                SpellLimit: 0,
                CfpLimit: 0,
                AiNormalProgramLimit: 0,
                AiAdvancedProgramLimit: 0);

            return Task.FromResult(awakening);
        }

        public Task<CommandResult<CharacterProfileSection>> UpdateMetadataAsync(CharacterWorkspaceId id, UpdateWorkspaceMetadata command, CancellationToken ct)
        {
            LastUpdateMetadata = command;
            _name = command.Name ?? _name;
            _alias = command.Alias ?? _alias;
            SeedWorkspace(id.Value, _name, _alias);

            CharacterProfileSection updated = new(
                Name: _name,
                Alias: _alias,
                PlayerName: string.Empty,
                Metatype: "Ork",
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
                BuildMethod: "SumtoTen",
                GameplayOption: string.Empty,
                Created: true,
                Adept: false,
                Magician: false,
                Technomancer: false,
                AI: false,
                MainMugshotIndex: 0,
                MugshotCount: 0);

            return Task.FromResult(new CommandResult<CharacterProfileSection>(
                Success: true,
                Value: updated,
                Error: null));
        }

        public Task<CommandResult<WorkspaceSaveReceipt>> SaveAsync(CharacterWorkspaceId id, CancellationToken ct)
        {
            SeedWorkspace(id.Value, _name, _alias);
            return Task.FromResult(new CommandResult<WorkspaceSaveReceipt>(
                Success: true,
                Value: new WorkspaceSaveReceipt(
                    Id: id,
                    DocumentLength: 64),
                Error: null));
        }

        public Task<CommandResult<WorkspaceDownloadReceipt>> DownloadAsync(CharacterWorkspaceId id, CancellationToken ct)
        {
            DownloadCalls++;
            SeedWorkspace(id.Value, _name, _alias);
            return Task.FromResult(new CommandResult<WorkspaceDownloadReceipt>(
                Success: true,
                Value: new WorkspaceDownloadReceipt(
                    Id: id,
                    Format: WorkspaceDocumentFormat.Chum5Xml,
                    ContentBase64: Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("<character><name>Download</name></character>")),
                    FileName: $"{id.Value}.chum5",
                    DocumentLength: 41),
                Error: null));
        }
    }
}
