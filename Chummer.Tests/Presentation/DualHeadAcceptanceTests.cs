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
using Chummer.Contracts.Workspaces;
using Chummer.Presentation;
using Chummer.Presentation.Overview;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Presentation;

[TestClass]
public class DualHeadAcceptanceTests
{
    private static readonly Uri BaseUri = ResolveBaseUri();

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
    }

    private static HttpClient CreateClient()
    {
        return new HttpClient
        {
            BaseAddress = BaseUri,
            Timeout = TimeSpan.FromSeconds(10)
        };
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
