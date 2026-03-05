using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Chummer.Avalonia;
using Chummer.Blazor;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Presentation;

[TestClass]
public class DualHeadAcceptanceTests
{
    private static readonly Uri BaseUri = ResolveBaseUri();
    private static readonly string? ApiKey = ResolveApiKey();

    [TestMethod]
    public async Task Avalonia_and_Blazor_overview_flows_show_equivalent_state_after_import()
    {
        string xml = File.ReadAllText(FindTestFilePath("Barrett.chum5"));
        byte[] documentBytes = Encoding.UTF8.GetBytes(xml);

        CharacterOverviewState avaloniaState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.ImportAsync(documentBytes, CancellationToken.None);
            avaloniaState = adapter.State;
        }

        CharacterOverviewState blazorState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            await bridge.ImportAsync(documentBytes, CancellationToken.None);
            blazorState = callbackState.WorkspaceId is null ? bridge.Current : callbackState;
        }

        Assert.IsNotNull(avaloniaState.WorkspaceId);
        Assert.IsNotNull(blazorState.WorkspaceId);
        Assert.AreEqual(avaloniaState.Profile?.Name, blazorState.Profile?.Name);
        Assert.AreEqual(avaloniaState.Profile?.Alias, blazorState.Profile?.Alias);
        Assert.AreEqual(avaloniaState.Progress?.Karma, blazorState.Progress?.Karma);
        Assert.AreEqual(avaloniaState.Skills?.Count, blazorState.Skills?.Count);
        Assert.AreEqual(avaloniaState.Rules?.GameEdition, blazorState.Rules?.GameEdition);
        Assert.AreEqual("Moa", avaloniaState.Profile?.Name);
        Assert.AreEqual("Barrett", avaloniaState.Profile?.Alias);
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_metadata_save_roundtrip_match()
    {
        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        byte[] documentBytes = Encoding.UTF8.GetBytes(xml);
        UpdateWorkspaceMetadata update = new("Updated Name", "Updated Alias", "Updated Notes");

        CharacterOverviewState avaloniaState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.ImportAsync(documentBytes, CancellationToken.None);
            await presenter.UpdateMetadataAsync(update, CancellationToken.None);
            await presenter.SaveAsync(CancellationToken.None);
            avaloniaState = adapter.State;
        }

        CharacterOverviewState blazorState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            await bridge.ImportAsync(documentBytes, CancellationToken.None);
            await presenter.UpdateMetadataAsync(update, CancellationToken.None);
            await presenter.SaveAsync(CancellationToken.None);
            blazorState = callbackState.WorkspaceId is null ? bridge.Current : callbackState;
        }

        Assert.AreEqual("Updated Name", avaloniaState.Profile?.Name);
        Assert.AreEqual("Updated Alias", avaloniaState.Profile?.Alias);
        Assert.AreEqual("Updated Name", blazorState.Profile?.Name);
        Assert.AreEqual("Updated Alias", blazorState.Profile?.Alias);
        Assert.IsTrue(avaloniaState.HasSavedWorkspace);
        Assert.IsTrue(blazorState.HasSavedWorkspace);
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_tab_selection_loads_same_workspace_section()
    {
        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        byte[] documentBytes = Encoding.UTF8.GetBytes(xml);

        CharacterOverviewState avaloniaState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.InitializeAsync(CancellationToken.None);
            await adapter.ImportAsync(documentBytes, CancellationToken.None);
            await adapter.SelectTabAsync("tab-skills", CancellationToken.None);
            avaloniaState = adapter.State;
        }

        CharacterOverviewState blazorState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            await bridge.InitializeAsync(CancellationToken.None);
            await bridge.ImportAsync(documentBytes, CancellationToken.None);
            await bridge.SelectTabAsync("tab-skills", CancellationToken.None);
            blazorState = callbackState.WorkspaceId is null ? bridge.Current : callbackState;
        }

        Assert.AreEqual("tab-skills", avaloniaState.ActiveTabId);
        Assert.AreEqual("tab-skills", blazorState.ActiveTabId);
        Assert.AreEqual("skills", avaloniaState.ActiveSectionId);
        Assert.AreEqual("skills", blazorState.ActiveSectionId);
        Assert.AreEqual(avaloniaState.ActiveSectionJson, blazorState.ActiveSectionJson);
        Assert.AreEqual(avaloniaState.ActiveSectionRows.Count, blazorState.ActiveSectionRows.Count);
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_command_dispatch_save_character_matches()
    {
        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        byte[] documentBytes = Encoding.UTF8.GetBytes(xml);

        CharacterOverviewState avaloniaState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.ImportAsync(documentBytes, CancellationToken.None);
            await adapter.ExecuteCommandAsync("save_character", CancellationToken.None);
            avaloniaState = adapter.State;
        }

        CharacterOverviewState blazorState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            await bridge.ImportAsync(documentBytes, CancellationToken.None);
            await bridge.ExecuteCommandAsync("save_character", CancellationToken.None);
            blazorState = callbackState.WorkspaceId is null ? bridge.Current : callbackState;
        }

        Assert.AreEqual("save_character", avaloniaState.LastCommandId);
        Assert.AreEqual("save_character", blazorState.LastCommandId);
        Assert.IsTrue(avaloniaState.HasSavedWorkspace);
        Assert.IsTrue(blazorState.HasSavedWorkspace);
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_command_dialog_dispatch_matches()
    {
        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        byte[] documentBytes = Encoding.UTF8.GetBytes(xml);

        CharacterOverviewState avaloniaState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.ImportAsync(documentBytes, CancellationToken.None);
            await adapter.ExecuteCommandAsync("global_settings", CancellationToken.None);
            avaloniaState = adapter.State;
        }

        CharacterOverviewState blazorState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            await bridge.ImportAsync(documentBytes, CancellationToken.None);
            await bridge.ExecuteCommandAsync("global_settings", CancellationToken.None);
            blazorState = callbackState.WorkspaceId is null ? bridge.Current : callbackState;
        }

        Assert.AreEqual("global_settings", avaloniaState.LastCommandId);
        Assert.AreEqual("global_settings", blazorState.LastCommandId);
        Assert.IsNotNull(avaloniaState.ActiveDialog);
        Assert.IsNotNull(blazorState.ActiveDialog);
        Assert.AreEqual(avaloniaState.ActiveDialog?.Id, blazorState.ActiveDialog?.Id);
        Assert.AreEqual("Global Settings", avaloniaState.ActiveDialog?.Title);
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_dialog_field_updates_match()
    {
        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        byte[] documentBytes = Encoding.UTF8.GetBytes(xml);

        CharacterOverviewState avaloniaState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.ImportAsync(documentBytes, CancellationToken.None);
            await adapter.ExecuteCommandAsync("global_settings", CancellationToken.None);
            await adapter.UpdateDialogFieldAsync("globalUiScale", "125", CancellationToken.None);
            avaloniaState = adapter.State;
        }

        CharacterOverviewState blazorState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            await bridge.ImportAsync(documentBytes, CancellationToken.None);
            await bridge.ExecuteCommandAsync("global_settings", CancellationToken.None);
            await bridge.UpdateDialogFieldAsync("globalUiScale", "125", CancellationToken.None);
            blazorState = callbackState.WorkspaceId is null ? bridge.Current : callbackState;
        }

        string? avaloniaUiScale = avaloniaState.ActiveDialog?.Fields.FirstOrDefault(field => string.Equals(field.Id, "globalUiScale", StringComparison.Ordinal)).Value;
        string? blazorUiScale = blazorState.ActiveDialog?.Fields.FirstOrDefault(field => string.Equals(field.Id, "globalUiScale", StringComparison.Ordinal)).Value;
        Assert.AreEqual("125", avaloniaUiScale);
        Assert.AreEqual("125", blazorUiScale);
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_global_settings_save_updates_shared_preferences()
    {
        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        byte[] documentBytes = Encoding.UTF8.GetBytes(xml);

        CharacterOverviewState avaloniaState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.ImportAsync(documentBytes, CancellationToken.None);
            await adapter.ExecuteCommandAsync("global_settings", CancellationToken.None);
            await adapter.UpdateDialogFieldAsync("globalUiScale", "120", CancellationToken.None);
            await adapter.UpdateDialogFieldAsync("globalTheme", "steel", CancellationToken.None);
            await adapter.ExecuteDialogActionAsync("save", CancellationToken.None);
            avaloniaState = adapter.State;
        }

        CharacterOverviewState blazorState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            await bridge.ImportAsync(documentBytes, CancellationToken.None);
            await bridge.ExecuteCommandAsync("global_settings", CancellationToken.None);
            await bridge.UpdateDialogFieldAsync("globalUiScale", "120", CancellationToken.None);
            await bridge.UpdateDialogFieldAsync("globalTheme", "steel", CancellationToken.None);
            await bridge.ExecuteDialogActionAsync("save", CancellationToken.None);
            blazorState = callbackState.WorkspaceId is null ? bridge.Current : callbackState;
        }

        Assert.AreEqual(120, avaloniaState.Preferences.UiScalePercent);
        Assert.AreEqual(120, blazorState.Preferences.UiScalePercent);
        Assert.AreEqual("steel", avaloniaState.Preferences.Theme);
        Assert.AreEqual("steel", blazorState.Preferences.Theme);
        Assert.IsNull(avaloniaState.ActiveDialog);
        Assert.IsNull(blazorState.ActiveDialog);
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_dialog_workflow_keeps_shell_regions_in_parity()
    {
        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        byte[] documentBytes = Encoding.UTF8.GetBytes(xml);
        DefaultCommandAvailabilityEvaluator evaluator = new();

        ShellRegionSnapshot avaloniaBeforeDialog;
        ShellRegionSnapshot avaloniaDialogOpen;
        ShellRegionSnapshot avaloniaAfterDialogSave;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.InitializeAsync(CancellationToken.None);
            await adapter.ImportAsync(documentBytes, CancellationToken.None);
            await adapter.SelectTabAsync("tab-info", CancellationToken.None);
            avaloniaBeforeDialog = BuildShellRegionSnapshot(adapter.State, evaluator);

            await adapter.ExecuteCommandAsync("global_settings", CancellationToken.None);
            avaloniaDialogOpen = BuildShellRegionSnapshot(adapter.State, evaluator);

            await adapter.UpdateDialogFieldAsync("globalTheme", "mint", CancellationToken.None);
            await adapter.UpdateDialogFieldAsync("globalUiScale", "130", CancellationToken.None);
            await adapter.ExecuteDialogActionAsync("save", CancellationToken.None);
            avaloniaAfterDialogSave = BuildShellRegionSnapshot(adapter.State, evaluator);
        }

        ShellRegionSnapshot blazorBeforeDialog;
        ShellRegionSnapshot blazorDialogOpen;
        ShellRegionSnapshot blazorAfterDialogSave;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            CharacterOverviewState Snapshot() => callbackState.WorkspaceId is null ? bridge.Current : callbackState;

            await bridge.InitializeAsync(CancellationToken.None);
            await bridge.ImportAsync(documentBytes, CancellationToken.None);
            await bridge.SelectTabAsync("tab-info", CancellationToken.None);
            blazorBeforeDialog = BuildShellRegionSnapshot(Snapshot(), evaluator);

            await bridge.ExecuteCommandAsync("global_settings", CancellationToken.None);
            blazorDialogOpen = BuildShellRegionSnapshot(Snapshot(), evaluator);

            await bridge.UpdateDialogFieldAsync("globalTheme", "mint", CancellationToken.None);
            await bridge.UpdateDialogFieldAsync("globalUiScale", "130", CancellationToken.None);
            await bridge.ExecuteDialogActionAsync("save", CancellationToken.None);
            blazorAfterDialogSave = BuildShellRegionSnapshot(Snapshot(), evaluator);
        }

        AssertShellRegionsEqual(avaloniaBeforeDialog, blazorBeforeDialog, "before-dialog");
        AssertShellRegionsEqual(avaloniaDialogOpen, blazorDialogOpen, "dialog-open");
        AssertShellRegionsEqual(avaloniaAfterDialogSave, blazorAfterDialogSave, "after-dialog-save");

        Assert.IsTrue(avaloniaBeforeDialog.OpenWorkspaceCount >= 1);
        Assert.AreEqual(avaloniaBeforeDialog.OpenWorkspaceCount, avaloniaDialogOpen.OpenWorkspaceCount);
        Assert.AreEqual(avaloniaDialogOpen.OpenWorkspaceCount, avaloniaAfterDialogSave.OpenWorkspaceCount);
        Assert.IsTrue(blazorBeforeDialog.OpenWorkspaceCount >= 1);
        Assert.AreEqual(blazorBeforeDialog.OpenWorkspaceCount, blazorDialogOpen.OpenWorkspaceCount);
        Assert.AreEqual(blazorDialogOpen.OpenWorkspaceCount, blazorAfterDialogSave.OpenWorkspaceCount);

        Assert.AreEqual("dialog.global_settings", avaloniaDialogOpen.DialogId);
        Assert.AreEqual("dialog.global_settings", blazorDialogOpen.DialogId);
        Assert.AreEqual("Global Settings", avaloniaDialogOpen.DialogTitle);
        Assert.AreEqual("Global Settings", blazorDialogOpen.DialogTitle);
        Assert.IsNull(avaloniaAfterDialogSave.DialogId);
        Assert.IsNull(blazorAfterDialogSave.DialogId);
        Assert.AreEqual("mint", avaloniaAfterDialogSave.Theme);
        Assert.AreEqual("mint", blazorAfterDialogSave.Theme);
        Assert.AreEqual(130, avaloniaAfterDialogSave.UiScalePercent);
        Assert.AreEqual(130, blazorAfterDialogSave.UiScalePercent);
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_workspace_action_summary_matches()
    {
        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        byte[] documentBytes = Encoding.UTF8.GetBytes(xml);
        WorkspaceSurfaceActionDefinition action = WorkspaceSurfaceActionCatalog.All
            .First(item => string.Equals(item.Id, "tab-info.summary", StringComparison.Ordinal));

        CharacterOverviewState avaloniaState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.InitializeAsync(CancellationToken.None);
            await adapter.ImportAsync(documentBytes, CancellationToken.None);
            await adapter.ExecuteWorkspaceActionAsync(action, CancellationToken.None);
            avaloniaState = adapter.State;
        }

        CharacterOverviewState blazorState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            await bridge.InitializeAsync(CancellationToken.None);
            await bridge.ImportAsync(documentBytes, CancellationToken.None);
            await bridge.ExecuteWorkspaceActionAsync(action, CancellationToken.None);
            blazorState = callbackState.WorkspaceId is null ? bridge.Current : callbackState;
        }

        Assert.AreEqual("summary", avaloniaState.ActiveSectionId);
        Assert.AreEqual("summary", blazorState.ActiveSectionId);
        Assert.AreEqual("tab-info.summary", avaloniaState.ActiveActionId);
        Assert.AreEqual("tab-info.summary", blazorState.ActiveActionId);

        using JsonDocument avaloniaJson = JsonDocument.Parse(avaloniaState.ActiveSectionJson ?? "{}");
        using JsonDocument blazorJson = JsonDocument.Parse(blazorState.ActiveSectionJson ?? "{}");

        JsonElement avaloniaRoot = avaloniaJson.RootElement;
        JsonElement blazorRoot = blazorJson.RootElement;

        Assert.AreEqual(GetString(avaloniaRoot, "Name"), GetString(blazorRoot, "Name"));
        Assert.AreEqual(GetString(avaloniaRoot, "Alias"), GetString(blazorRoot, "Alias"));
        Assert.AreEqual(GetString(avaloniaRoot, "Metatype"), GetString(blazorRoot, "Metatype"));
        Assert.AreEqual(GetString(avaloniaRoot, "BuildMethod"), GetString(blazorRoot, "BuildMethod"));
        Assert.AreEqual(GetDecimal(avaloniaRoot, "Karma"), GetDecimal(blazorRoot, "Karma"));
        Assert.AreEqual(GetDecimal(avaloniaRoot, "Nuyen"), GetDecimal(blazorRoot, "Nuyen"));
        Assert.AreEqual(avaloniaState.ActiveSectionRows.Count, blazorState.ActiveSectionRows.Count);
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_info_family_workspace_actions_render_matching_sections()
    {
        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        byte[] documentBytes = Encoding.UTF8.GetBytes(xml);
        string[] actionIds =
        [
            "tab-info.profile",
            "tab-info.progress",
            "tab-info.rules",
            "tab-info.build",
            "tab-info.movement",
            "tab-info.awakening"
        ];

        var expectedSections = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["tab-info.profile"] = "profile",
            ["tab-info.progress"] = "progress",
            ["tab-info.rules"] = "rules",
            ["tab-info.build"] = "build",
            ["tab-info.movement"] = "movement",
            ["tab-info.awakening"] = "awakening"
        };

        var avaloniaSnapshots = new Dictionary<string, (string? ActionId, string? SectionId, string? Json, int RowCount)>(StringComparer.Ordinal);
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.InitializeAsync(CancellationToken.None);
            await adapter.ImportAsync(documentBytes, CancellationToken.None);

            foreach (string actionId in actionIds)
            {
                WorkspaceSurfaceActionDefinition action = WorkspaceSurfaceActionCatalog.All
                    .First(item => string.Equals(item.Id, actionId, StringComparison.Ordinal));
                await adapter.ExecuteWorkspaceActionAsync(action, CancellationToken.None);
                CharacterOverviewState state = adapter.State;
                avaloniaSnapshots[actionId] = (state.ActiveActionId, state.ActiveSectionId, state.ActiveSectionJson, state.ActiveSectionRows.Count);
            }
        }

        var blazorSnapshots = new Dictionary<string, (string? ActionId, string? SectionId, string? Json, int RowCount)>(StringComparer.Ordinal);
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            CharacterOverviewState Snapshot() => callbackState.WorkspaceId is null ? bridge.Current : callbackState;

            await bridge.InitializeAsync(CancellationToken.None);
            await bridge.ImportAsync(documentBytes, CancellationToken.None);

            foreach (string actionId in actionIds)
            {
                WorkspaceSurfaceActionDefinition action = WorkspaceSurfaceActionCatalog.All
                    .First(item => string.Equals(item.Id, actionId, StringComparison.Ordinal));
                await bridge.ExecuteWorkspaceActionAsync(action, CancellationToken.None);
                CharacterOverviewState state = Snapshot();
                blazorSnapshots[actionId] = (state.ActiveActionId, state.ActiveSectionId, state.ActiveSectionJson, state.ActiveSectionRows.Count);
            }
        }

        foreach (string actionId in actionIds)
        {
            Assert.IsTrue(avaloniaSnapshots.TryGetValue(actionId, out var avalonia), $"Missing Avalonia snapshot for action '{actionId}'.");
            Assert.IsTrue(blazorSnapshots.TryGetValue(actionId, out var blazor), $"Missing Blazor snapshot for action '{actionId}'.");

            Assert.AreEqual(actionId, avalonia.ActionId);
            Assert.AreEqual(actionId, blazor.ActionId);
            Assert.AreEqual(expectedSections[actionId], avalonia.SectionId);
            Assert.AreEqual(expectedSections[actionId], blazor.SectionId);
            Assert.AreEqual(avalonia.Json, blazor.Json);
            Assert.IsTrue(avalonia.RowCount > 0);
            Assert.IsTrue(blazor.RowCount > 0);
            Assert.AreEqual(avalonia.RowCount, blazor.RowCount);
        }
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_attributes_and_skills_workspace_actions_render_matching_sections()
    {
        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        byte[] documentBytes = Encoding.UTF8.GetBytes(xml);
        string[] actionIds =
        [
            "tab-attributes.attributes",
            "tab-attributes.attributedetails",
            "tab-skills.skills"
        ];

        var expectedSections = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["tab-attributes.attributes"] = "attributes",
            ["tab-attributes.attributedetails"] = "attributedetails",
            ["tab-skills.skills"] = "skills"
        };

        var avaloniaSnapshots = new Dictionary<string, (string? ActionId, string? SectionId, string? Json, int RowCount)>(StringComparer.Ordinal);
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.InitializeAsync(CancellationToken.None);
            await adapter.ImportAsync(documentBytes, CancellationToken.None);

            foreach (string actionId in actionIds)
            {
                WorkspaceSurfaceActionDefinition action = WorkspaceSurfaceActionCatalog.All
                    .First(item => string.Equals(item.Id, actionId, StringComparison.Ordinal));
                await adapter.ExecuteWorkspaceActionAsync(action, CancellationToken.None);
                CharacterOverviewState state = adapter.State;
                avaloniaSnapshots[actionId] = (state.ActiveActionId, state.ActiveSectionId, state.ActiveSectionJson, state.ActiveSectionRows.Count);
            }
        }

        var blazorSnapshots = new Dictionary<string, (string? ActionId, string? SectionId, string? Json, int RowCount)>(StringComparer.Ordinal);
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            CharacterOverviewState Snapshot() => callbackState.WorkspaceId is null ? bridge.Current : callbackState;

            await bridge.InitializeAsync(CancellationToken.None);
            await bridge.ImportAsync(documentBytes, CancellationToken.None);

            foreach (string actionId in actionIds)
            {
                WorkspaceSurfaceActionDefinition action = WorkspaceSurfaceActionCatalog.All
                    .First(item => string.Equals(item.Id, actionId, StringComparison.Ordinal));
                await bridge.ExecuteWorkspaceActionAsync(action, CancellationToken.None);
                CharacterOverviewState state = Snapshot();
                blazorSnapshots[actionId] = (state.ActiveActionId, state.ActiveSectionId, state.ActiveSectionJson, state.ActiveSectionRows.Count);
            }
        }

        foreach (string actionId in actionIds)
        {
            Assert.IsTrue(avaloniaSnapshots.TryGetValue(actionId, out var avalonia), $"Missing Avalonia snapshot for action '{actionId}'.");
            Assert.IsTrue(blazorSnapshots.TryGetValue(actionId, out var blazor), $"Missing Blazor snapshot for action '{actionId}'.");

            Assert.AreEqual(actionId, avalonia.ActionId);
            Assert.AreEqual(actionId, blazor.ActionId);
            Assert.AreEqual(expectedSections[actionId], avalonia.SectionId);
            Assert.AreEqual(expectedSections[actionId], blazor.SectionId);
            Assert.AreEqual(avalonia.Json, blazor.Json);
            Assert.AreEqual(avalonia.RowCount, blazor.RowCount);
        }
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_gear_family_workspace_actions_render_matching_sections()
    {
        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        byte[] documentBytes = Encoding.UTF8.GetBytes(xml);
        string[] actionIds =
        [
            "tab-gear.inventory",
            "tab-gear.gear",
            "tab-gear.weapons",
            "tab-gear.armors",
            "tab-gear.vehicles"
        ];

        var expectedSections = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["tab-gear.inventory"] = "inventory",
            ["tab-gear.gear"] = "gear",
            ["tab-gear.weapons"] = "weapons",
            ["tab-gear.armors"] = "armors",
            ["tab-gear.vehicles"] = "vehicles"
        };

        var avaloniaSnapshots = new Dictionary<string, (string? ActionId, string? SectionId, string? Json, int RowCount)>(StringComparer.Ordinal);
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.InitializeAsync(CancellationToken.None);
            await adapter.ImportAsync(documentBytes, CancellationToken.None);

            foreach (string actionId in actionIds)
            {
                WorkspaceSurfaceActionDefinition action = WorkspaceSurfaceActionCatalog.All
                    .First(item => string.Equals(item.Id, actionId, StringComparison.Ordinal));
                await adapter.ExecuteWorkspaceActionAsync(action, CancellationToken.None);
                CharacterOverviewState state = adapter.State;
                avaloniaSnapshots[actionId] = (state.ActiveActionId, state.ActiveSectionId, state.ActiveSectionJson, state.ActiveSectionRows.Count);
            }
        }

        var blazorSnapshots = new Dictionary<string, (string? ActionId, string? SectionId, string? Json, int RowCount)>(StringComparer.Ordinal);
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            CharacterOverviewState Snapshot() => callbackState.WorkspaceId is null ? bridge.Current : callbackState;

            await bridge.InitializeAsync(CancellationToken.None);
            await bridge.ImportAsync(documentBytes, CancellationToken.None);

            foreach (string actionId in actionIds)
            {
                WorkspaceSurfaceActionDefinition action = WorkspaceSurfaceActionCatalog.All
                    .First(item => string.Equals(item.Id, actionId, StringComparison.Ordinal));
                await bridge.ExecuteWorkspaceActionAsync(action, CancellationToken.None);
                CharacterOverviewState state = Snapshot();
                blazorSnapshots[actionId] = (state.ActiveActionId, state.ActiveSectionId, state.ActiveSectionJson, state.ActiveSectionRows.Count);
            }
        }

        foreach (string actionId in actionIds)
        {
            Assert.IsTrue(avaloniaSnapshots.TryGetValue(actionId, out var avalonia), $"Missing Avalonia snapshot for action '{actionId}'.");
            Assert.IsTrue(blazorSnapshots.TryGetValue(actionId, out var blazor), $"Missing Blazor snapshot for action '{actionId}'.");

            Assert.AreEqual(actionId, avalonia.ActionId);
            Assert.AreEqual(actionId, blazor.ActionId);
            Assert.AreEqual(expectedSections[actionId], avalonia.SectionId);
            Assert.AreEqual(expectedSections[actionId], blazor.SectionId);
            Assert.AreEqual(avalonia.Json, blazor.Json);
            Assert.AreEqual(avalonia.RowCount, blazor.RowCount);
        }
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_magic_family_workspace_actions_render_matching_sections()
    {
        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        byte[] documentBytes = Encoding.UTF8.GetBytes(xml);
        string[] actionIds =
        [
            "tab-magician.spirits",
            "tab-magician.metamagics",
            "tab-adept.powers",
            "tab-technomancer.complexforms",
            "tab-technomancer.aiprograms"
        ];

        var expectedSections = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["tab-magician.spirits"] = "spirits",
            ["tab-magician.metamagics"] = "metamagics",
            ["tab-adept.powers"] = "powers",
            ["tab-technomancer.complexforms"] = "complexforms",
            ["tab-technomancer.aiprograms"] = "aiprograms"
        };

        var avaloniaSnapshots = new Dictionary<string, (string? ActionId, string? SectionId, string? Json, int RowCount)>(StringComparer.Ordinal);
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.InitializeAsync(CancellationToken.None);
            await adapter.ImportAsync(documentBytes, CancellationToken.None);

            foreach (string actionId in actionIds)
            {
                WorkspaceSurfaceActionDefinition action = WorkspaceSurfaceActionCatalog.All
                    .First(item => string.Equals(item.Id, actionId, StringComparison.Ordinal));
                await adapter.ExecuteWorkspaceActionAsync(action, CancellationToken.None);
                CharacterOverviewState state = adapter.State;
                avaloniaSnapshots[actionId] = (state.ActiveActionId, state.ActiveSectionId, state.ActiveSectionJson, state.ActiveSectionRows.Count);
            }
        }

        var blazorSnapshots = new Dictionary<string, (string? ActionId, string? SectionId, string? Json, int RowCount)>(StringComparer.Ordinal);
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            CharacterOverviewState Snapshot() => callbackState.WorkspaceId is null ? bridge.Current : callbackState;

            await bridge.InitializeAsync(CancellationToken.None);
            await bridge.ImportAsync(documentBytes, CancellationToken.None);

            foreach (string actionId in actionIds)
            {
                WorkspaceSurfaceActionDefinition action = WorkspaceSurfaceActionCatalog.All
                    .First(item => string.Equals(item.Id, actionId, StringComparison.Ordinal));
                await bridge.ExecuteWorkspaceActionAsync(action, CancellationToken.None);
                CharacterOverviewState state = Snapshot();
                blazorSnapshots[actionId] = (state.ActiveActionId, state.ActiveSectionId, state.ActiveSectionJson, state.ActiveSectionRows.Count);
            }
        }

        foreach (string actionId in actionIds)
        {
            Assert.IsTrue(avaloniaSnapshots.TryGetValue(actionId, out var avalonia), $"Missing Avalonia snapshot for action '{actionId}'.");
            Assert.IsTrue(blazorSnapshots.TryGetValue(actionId, out var blazor), $"Missing Blazor snapshot for action '{actionId}'.");

            Assert.AreEqual(actionId, avalonia.ActionId);
            Assert.AreEqual(actionId, blazor.ActionId);
            Assert.AreEqual(expectedSections[actionId], avalonia.SectionId);
            Assert.AreEqual(expectedSections[actionId], blazor.SectionId);
            Assert.AreEqual(avalonia.Json, blazor.Json);
            Assert.AreEqual(avalonia.RowCount, blazor.RowCount);
        }
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_support_family_workspace_actions_render_matching_sections()
    {
        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        byte[] documentBytes = Encoding.UTF8.GetBytes(xml);
        string[] actionIds =
        [
            "tab-lifestyle.lifestyles",
            "tab-contacts.contacts",
            "tab-calendar.calendar",
            "tab-improvements.improvements"
        ];

        var expectedSections = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["tab-lifestyle.lifestyles"] = "lifestyles",
            ["tab-contacts.contacts"] = "contacts",
            ["tab-calendar.calendar"] = "calendar",
            ["tab-improvements.improvements"] = "improvements"
        };

        var avaloniaSnapshots = new Dictionary<string, (string? ActionId, string? SectionId, string? Json, int RowCount)>(StringComparer.Ordinal);
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.InitializeAsync(CancellationToken.None);
            await adapter.ImportAsync(documentBytes, CancellationToken.None);

            foreach (string actionId in actionIds)
            {
                WorkspaceSurfaceActionDefinition action = WorkspaceSurfaceActionCatalog.All
                    .First(item => string.Equals(item.Id, actionId, StringComparison.Ordinal));
                await adapter.ExecuteWorkspaceActionAsync(action, CancellationToken.None);
                CharacterOverviewState state = adapter.State;
                avaloniaSnapshots[actionId] = (state.ActiveActionId, state.ActiveSectionId, state.ActiveSectionJson, state.ActiveSectionRows.Count);
            }
        }

        var blazorSnapshots = new Dictionary<string, (string? ActionId, string? SectionId, string? Json, int RowCount)>(StringComparer.Ordinal);
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            CharacterOverviewState Snapshot() => callbackState.WorkspaceId is null ? bridge.Current : callbackState;

            await bridge.InitializeAsync(CancellationToken.None);
            await bridge.ImportAsync(documentBytes, CancellationToken.None);

            foreach (string actionId in actionIds)
            {
                WorkspaceSurfaceActionDefinition action = WorkspaceSurfaceActionCatalog.All
                    .First(item => string.Equals(item.Id, actionId, StringComparison.Ordinal));
                await bridge.ExecuteWorkspaceActionAsync(action, CancellationToken.None);
                CharacterOverviewState state = Snapshot();
                blazorSnapshots[actionId] = (state.ActiveActionId, state.ActiveSectionId, state.ActiveSectionJson, state.ActiveSectionRows.Count);
            }
        }

        foreach (string actionId in actionIds)
        {
            Assert.IsTrue(avaloniaSnapshots.TryGetValue(actionId, out var avalonia), $"Missing Avalonia snapshot for action '{actionId}'.");
            Assert.IsTrue(blazorSnapshots.TryGetValue(actionId, out var blazor), $"Missing Blazor snapshot for action '{actionId}'.");

            Assert.AreEqual(actionId, avalonia.ActionId);
            Assert.AreEqual(actionId, blazor.ActionId);
            Assert.AreEqual(expectedSections[actionId], avalonia.SectionId);
            Assert.AreEqual(expectedSections[actionId], blazor.SectionId);
            Assert.AreEqual(avalonia.Json, blazor.Json);
            Assert.AreEqual(avalonia.RowCount, blazor.RowCount);
        }
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_combat_and_cyberware_workspace_actions_render_matching_sections()
    {
        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        byte[] documentBytes = Encoding.UTF8.GetBytes(xml);
        string[] actionIds =
        [
            "tab-combat.weapons",
            "tab-combat.armors",
            "tab-combat.drugs",
            "tab-armor.armormods",
            "tab-cyberware.cyberwares"
        ];

        var expectedSections = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["tab-combat.weapons"] = "weapons",
            ["tab-combat.armors"] = "armors",
            ["tab-combat.drugs"] = "drugs",
            ["tab-armor.armormods"] = "armormods",
            ["tab-cyberware.cyberwares"] = "cyberwares"
        };

        var avaloniaSnapshots = new Dictionary<string, (string? ActionId, string? SectionId, string? Json, int RowCount)>(StringComparer.Ordinal);
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.InitializeAsync(CancellationToken.None);
            await adapter.ImportAsync(documentBytes, CancellationToken.None);

            foreach (string actionId in actionIds)
            {
                WorkspaceSurfaceActionDefinition action = WorkspaceSurfaceActionCatalog.All
                    .First(item => string.Equals(item.Id, actionId, StringComparison.Ordinal));
                await adapter.ExecuteWorkspaceActionAsync(action, CancellationToken.None);
                CharacterOverviewState state = adapter.State;
                avaloniaSnapshots[actionId] = (state.ActiveActionId, state.ActiveSectionId, state.ActiveSectionJson, state.ActiveSectionRows.Count);
            }
        }

        var blazorSnapshots = new Dictionary<string, (string? ActionId, string? SectionId, string? Json, int RowCount)>(StringComparer.Ordinal);
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            CharacterOverviewState Snapshot() => callbackState.WorkspaceId is null ? bridge.Current : callbackState;

            await bridge.InitializeAsync(CancellationToken.None);
            await bridge.ImportAsync(documentBytes, CancellationToken.None);

            foreach (string actionId in actionIds)
            {
                WorkspaceSurfaceActionDefinition action = WorkspaceSurfaceActionCatalog.All
                    .First(item => string.Equals(item.Id, actionId, StringComparison.Ordinal));
                await bridge.ExecuteWorkspaceActionAsync(action, CancellationToken.None);
                CharacterOverviewState state = Snapshot();
                blazorSnapshots[actionId] = (state.ActiveActionId, state.ActiveSectionId, state.ActiveSectionJson, state.ActiveSectionRows.Count);
            }
        }

        foreach (string actionId in actionIds)
        {
            Assert.IsTrue(avaloniaSnapshots.TryGetValue(actionId, out var avalonia), $"Missing Avalonia snapshot for action '{actionId}'.");
            Assert.IsTrue(blazorSnapshots.TryGetValue(actionId, out var blazor), $"Missing Blazor snapshot for action '{actionId}'.");

            Assert.AreEqual(actionId, avalonia.ActionId);
            Assert.AreEqual(actionId, blazor.ActionId);
            Assert.AreEqual(expectedSections[actionId], avalonia.SectionId);
            Assert.AreEqual(expectedSections[actionId], blazor.SectionId);
            Assert.AreEqual(avalonia.Json, blazor.Json);
            Assert.AreEqual(avalonia.RowCount, blazor.RowCount);
        }
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_shell_surfaces_expose_identical_ids()
    {
        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        byte[] documentBytes = Encoding.UTF8.GetBytes(xml);
        DefaultCommandAvailabilityEvaluator evaluator = new();

        CharacterOverviewState avaloniaState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.InitializeAsync(CancellationToken.None);
            await adapter.ImportAsync(documentBytes, CancellationToken.None);
            await adapter.SelectTabAsync("tab-info", CancellationToken.None);
            avaloniaState = adapter.State;
        }

        CharacterOverviewState blazorState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            await bridge.InitializeAsync(CancellationToken.None);
            await bridge.ImportAsync(documentBytes, CancellationToken.None);
            await bridge.SelectTabAsync("tab-info", CancellationToken.None);
            blazorState = callbackState.WorkspaceId is null ? bridge.Current : callbackState;
        }

        string[] avaloniaCommandIds = avaloniaState.Commands
            .Where(command => evaluator.IsCommandEnabled(command, avaloniaState))
            .Select(command => command.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] blazorCommandIds = blazorState.Commands
            .Where(command => evaluator.IsCommandEnabled(command, blazorState))
            .Select(command => command.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        CollectionAssert.AreEquivalent(avaloniaCommandIds, blazorCommandIds);

        string[] avaloniaTabIds = avaloniaState.NavigationTabs
            .Where(tab => evaluator.IsNavigationTabEnabled(tab, avaloniaState))
            .Select(tab => tab.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] blazorTabIds = blazorState.NavigationTabs
            .Where(tab => evaluator.IsNavigationTabEnabled(tab, blazorState))
            .Select(tab => tab.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        CollectionAssert.AreEquivalent(avaloniaTabIds, blazorTabIds);

        string[] avaloniaActionIds = RulesetShellCatalogResolver.ResolveWorkspaceActionsForTab(
                avaloniaState.ActiveTabId,
                ResolveActiveRulesetId(avaloniaState))
            .Where(action => evaluator.IsWorkspaceActionEnabled(action, avaloniaState))
            .Select(action => action.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] blazorActionIds = RulesetShellCatalogResolver.ResolveWorkspaceActionsForTab(
                blazorState.ActiveTabId,
                ResolveActiveRulesetId(blazorState))
            .Where(action => evaluator.IsWorkspaceActionEnabled(action, blazorState))
            .Select(action => action.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        CollectionAssert.AreEquivalent(avaloniaActionIds, blazorActionIds);

        string[] avaloniaControlIds = RulesetShellCatalogResolver.ResolveDesktopUiControlsForTab(
                avaloniaState.ActiveTabId,
                ResolveActiveRulesetId(avaloniaState))
            .Where(control => evaluator.IsUiControlEnabled(control, avaloniaState))
            .Select(control => control.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] blazorControlIds = RulesetShellCatalogResolver.ResolveDesktopUiControlsForTab(
                blazorState.ActiveTabId,
                ResolveActiveRulesetId(blazorState))
            .Where(control => evaluator.IsUiControlEnabled(control, blazorState))
            .Select(control => control.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        CollectionAssert.AreEquivalent(avaloniaControlIds, blazorControlIds);
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_two_workspace_import_switch_save_flow_matches()
    {
        byte[] firstDocument = Encoding.UTF8.GetBytes(File.ReadAllText(FindTestFilePath("Apex Predator.chum5")));
        byte[] secondDocument = Encoding.UTF8.GetBytes(File.ReadAllText(FindTestFilePath("Barrett.chum5")));
        CharacterWorkspaceId avaloniaFirstWorkspace;
        CharacterWorkspaceId avaloniaSecondWorkspace;
        CharacterOverviewState avaloniaAfterSwitchToFirst;
        CharacterOverviewState avaloniaAfterSwitchToSecond;

        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);

            await adapter.InitializeAsync(CancellationToken.None);
            await adapter.ImportAsync(firstDocument, CancellationToken.None);
            avaloniaFirstWorkspace = adapter.State.WorkspaceId!.Value;
            await adapter.SelectTabAsync("tab-skills", CancellationToken.None);
            await presenter.UpdateMetadataAsync(new UpdateWorkspaceMetadata("Avalonia One", "AV1", "Notes 1"), CancellationToken.None);
            await presenter.SaveAsync(CancellationToken.None);

            await adapter.ImportAsync(secondDocument, CancellationToken.None);
            avaloniaSecondWorkspace = adapter.State.WorkspaceId!.Value;
            await adapter.SelectTabAsync("tab-info", CancellationToken.None);
            await presenter.UpdateMetadataAsync(new UpdateWorkspaceMetadata("Avalonia Two", "AV2", "Notes 2"), CancellationToken.None);
            await presenter.SaveAsync(CancellationToken.None);

            await adapter.SwitchWorkspaceAsync(avaloniaFirstWorkspace, CancellationToken.None);
            avaloniaAfterSwitchToFirst = adapter.State;

            await adapter.SwitchWorkspaceAsync(avaloniaSecondWorkspace, CancellationToken.None);
            avaloniaAfterSwitchToSecond = adapter.State;
        }

        CharacterWorkspaceId blazorFirstWorkspace;
        CharacterWorkspaceId blazorSecondWorkspace;
        CharacterOverviewState blazorAfterSwitchToFirst;
        CharacterOverviewState blazorAfterSwitchToSecond;

        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            CharacterOverviewState Snapshot() => callbackState.WorkspaceId is null ? bridge.Current : callbackState;

            await bridge.InitializeAsync(CancellationToken.None);
            await bridge.ImportAsync(firstDocument, CancellationToken.None);
            blazorFirstWorkspace = Snapshot().WorkspaceId!.Value;
            await bridge.SelectTabAsync("tab-skills", CancellationToken.None);
            await presenter.UpdateMetadataAsync(new UpdateWorkspaceMetadata("Blazor One", "BZ1", "Notes 1"), CancellationToken.None);
            await presenter.SaveAsync(CancellationToken.None);

            await bridge.ImportAsync(secondDocument, CancellationToken.None);
            blazorSecondWorkspace = Snapshot().WorkspaceId!.Value;
            await bridge.SelectTabAsync("tab-info", CancellationToken.None);
            await presenter.UpdateMetadataAsync(new UpdateWorkspaceMetadata("Blazor Two", "BZ2", "Notes 2"), CancellationToken.None);
            await presenter.SaveAsync(CancellationToken.None);

            await bridge.SwitchWorkspaceAsync(blazorFirstWorkspace, CancellationToken.None);
            blazorAfterSwitchToFirst = Snapshot();

            await bridge.SwitchWorkspaceAsync(blazorSecondWorkspace, CancellationToken.None);
            blazorAfterSwitchToSecond = Snapshot();
        }

        Assert.AreNotEqual(avaloniaFirstWorkspace.Value, avaloniaSecondWorkspace.Value);
        Assert.AreNotEqual(blazorFirstWorkspace.Value, blazorSecondWorkspace.Value);

        Assert.IsTrue(avaloniaAfterSwitchToFirst.Session.OpenWorkspaces.Count >= 2);
        Assert.IsTrue(blazorAfterSwitchToFirst.Session.OpenWorkspaces.Count >= 2);
        CollectionAssert.IsSubsetOf(
            new[] { avaloniaFirstWorkspace.Value, avaloniaSecondWorkspace.Value },
            avaloniaAfterSwitchToFirst.Session.OpenWorkspaces.Select(workspace => workspace.Id.Value).ToArray());
        CollectionAssert.IsSubsetOf(
            new[] { blazorFirstWorkspace.Value, blazorSecondWorkspace.Value },
            blazorAfterSwitchToFirst.Session.OpenWorkspaces.Select(workspace => workspace.Id.Value).ToArray());

        Assert.AreEqual(avaloniaFirstWorkspace.Value, avaloniaAfterSwitchToFirst.WorkspaceId?.Value);
        Assert.AreEqual(blazorFirstWorkspace.Value, blazorAfterSwitchToFirst.WorkspaceId?.Value);
        Assert.AreEqual("tab-skills", avaloniaAfterSwitchToFirst.ActiveTabId);
        Assert.AreEqual("tab-skills", blazorAfterSwitchToFirst.ActiveTabId);
        Assert.AreEqual("skills", avaloniaAfterSwitchToFirst.ActiveSectionId);
        Assert.AreEqual("skills", blazorAfterSwitchToFirst.ActiveSectionId);

        Assert.AreEqual(avaloniaSecondWorkspace.Value, avaloniaAfterSwitchToSecond.WorkspaceId?.Value);
        Assert.AreEqual(blazorSecondWorkspace.Value, blazorAfterSwitchToSecond.WorkspaceId?.Value);
        Assert.AreEqual("tab-info", avaloniaAfterSwitchToSecond.ActiveTabId);
        Assert.AreEqual("tab-info", blazorAfterSwitchToSecond.ActiveTabId);
        Assert.AreEqual("profile", avaloniaAfterSwitchToSecond.ActiveSectionId);
        Assert.AreEqual("profile", blazorAfterSwitchToSecond.ActiveSectionId);
        Assert.AreEqual("Avalonia Two", avaloniaAfterSwitchToSecond.Profile?.Name);
        Assert.AreEqual("Blazor Two", blazorAfterSwitchToSecond.Profile?.Name);
    }

    private static ShellRegionSnapshot BuildShellRegionSnapshot(CharacterOverviewState state, DefaultCommandAvailabilityEvaluator evaluator)
    {
        string[] enabledCommandIds = state.Commands
            .Where(command => evaluator.IsCommandEnabled(command, state))
            .Select(command => command.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        string[] enabledTabIds = state.NavigationTabs
            .Where(tab => evaluator.IsNavigationTabEnabled(tab, state))
            .Select(tab => tab.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        string[] dialogFieldIds = state.ActiveDialog?.Fields
            .Select(field => field.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray() ?? Array.Empty<string>();

        string[] dialogActionIds = state.ActiveDialog?.Actions
            .Select(action => action.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray() ?? Array.Empty<string>();

        return new ShellRegionSnapshot(
            HasActiveWorkspace: state.WorkspaceId is not null,
            OpenWorkspaceCount: state.Session.OpenWorkspaces.Count,
            ActiveTabId: state.ActiveTabId,
            Theme: state.Preferences.Theme,
            UiScalePercent: state.Preferences.UiScalePercent,
            EnabledCommandIds: enabledCommandIds,
            EnabledTabIds: enabledTabIds,
            DialogId: state.ActiveDialog?.Id,
            DialogTitle: state.ActiveDialog?.Title,
            DialogFieldIds: dialogFieldIds,
            DialogActionIds: dialogActionIds);
    }

    private static void AssertShellRegionsEqual(ShellRegionSnapshot avalonia, ShellRegionSnapshot blazor, string phase)
    {
        Assert.AreEqual(avalonia.HasActiveWorkspace, blazor.HasActiveWorkspace, $"Active workspace presence mismatch at phase '{phase}'.");
        Assert.AreEqual(avalonia.ActiveTabId, blazor.ActiveTabId, $"Active tab mismatch at phase '{phase}'.");
        Assert.AreEqual(avalonia.DialogId, blazor.DialogId, $"Dialog id mismatch at phase '{phase}'.");
        Assert.AreEqual(avalonia.DialogTitle, blazor.DialogTitle, $"Dialog title mismatch at phase '{phase}'.");

        CollectionAssert.AreEquivalent(
            avalonia.EnabledCommandIds,
            blazor.EnabledCommandIds,
            $"Enabled command ids mismatch at phase '{phase}'.");
        CollectionAssert.AreEquivalent(
            avalonia.EnabledTabIds,
            blazor.EnabledTabIds,
            $"Enabled tab ids mismatch at phase '{phase}'.");
        CollectionAssert.AreEquivalent(
            avalonia.DialogFieldIds,
            blazor.DialogFieldIds,
            $"Dialog field ids mismatch at phase '{phase}'.");
        CollectionAssert.AreEquivalent(
            avalonia.DialogActionIds,
            blazor.DialogActionIds,
            $"Dialog action ids mismatch at phase '{phase}'.");
    }

    private sealed record ShellRegionSnapshot(
        bool HasActiveWorkspace,
        int OpenWorkspaceCount,
        string? ActiveTabId,
        string? Theme,
        int UiScalePercent,
        string[] EnabledCommandIds,
        string[] EnabledTabIds,
        string? DialogId,
        string? DialogTitle,
        string[] DialogFieldIds,
        string[] DialogActionIds);

    private static string ResolveActiveRulesetId(CharacterOverviewState state)
    {
        CharacterWorkspaceId? activeWorkspaceId = state.Session.ActiveWorkspaceId ?? state.WorkspaceId;
        if (activeWorkspaceId is null)
            return RulesetDefaults.Sr5;

        OpenWorkspaceState? openWorkspace = state.Session.OpenWorkspaces
            .FirstOrDefault(workspace => string.Equals(workspace.Id.Value, activeWorkspaceId.Value.Value, StringComparison.Ordinal));
        return openWorkspace is null
            ? RulesetDefaults.Sr5
            : RulesetDefaults.Normalize(openWorkspace.RulesetId);
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            BaseAddress = BaseUri,
            Timeout = TimeSpan.FromSeconds(30)
        };

        if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            client.DefaultRequestHeaders.Remove("X-Api-Key");
            client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        }

        return client;
    }

    private static Uri ResolveBaseUri()
    {
        string? raw = Environment.GetEnvironmentVariable("CHUMMER_API_BASE_URL");
        if (string.IsNullOrWhiteSpace(raw))
            raw = Environment.GetEnvironmentVariable("CHUMMER_WEB_BASE_URL");
        if (string.IsNullOrWhiteSpace(raw))
            raw = "http://chummer-api:8080";

        if (!Uri.TryCreate(raw, UriKind.Absolute, out Uri? uri))
            throw new InvalidOperationException($"Invalid CHUMMER_API_BASE_URL/CHUMMER_WEB_BASE_URL: '{raw}'");

        return uri;
    }

    private static string? ResolveApiKey()
    {
        return Environment.GetEnvironmentVariable("CHUMMER_API_KEY");
    }

    private static string FindTestFilePath(string fileName)
    {
        string? root = Environment.GetEnvironmentVariable("CHUMMER_REPO_ROOT");
        string[] candidates =
        {
            Path.Combine(Directory.GetCurrentDirectory(), "Chummer.Tests", "TestFiles", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "TestFiles", fileName),
            Path.Combine(AppContext.BaseDirectory, "TestFiles", fileName),
            Path.Combine("/src", "Chummer.Tests", "TestFiles", fileName),
            string.IsNullOrWhiteSpace(root) ? string.Empty : Path.Combine(root, "Chummer.Tests", "TestFiles", fileName)
        };

        string? match = candidates.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));
        if (match is null)
            throw new FileNotFoundException("Could not locate test file.", fileName);

        return match;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return FindProperty(element, propertyName).GetString();
    }

    private static decimal GetDecimal(JsonElement element, string propertyName)
    {
        return FindProperty(element, propertyName).GetDecimal();
    }

    private static JsonElement FindProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement direct))
            return direct;

        if (element.TryGetProperty(char.ToLowerInvariant(propertyName[0]) + propertyName[1..], out JsonElement camel))
            return camel;

        throw new KeyNotFoundException($"Missing property '{propertyName}' in JSON payload.");
    }
}
