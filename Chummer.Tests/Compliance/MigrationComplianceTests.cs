using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Chummer.Contracts.Presentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Compliance;

[TestClass]
public class MigrationComplianceTests
{
    private static readonly Regex SectionMethodRegex = new(@"\bCharacter[A-Za-z0-9_]+\s+Parse([A-Za-z0-9_]+)\(string xml\)", RegexOptions.Compiled);
    private static readonly Regex SectionEndpointRegex = new(@"/api/characters/sections/([a-z0-9]+)", RegexOptions.Compiled);
    private static readonly Regex SectionMapCallRegex = new(@"MapSection\(app,\s*""([a-z0-9]+)""", RegexOptions.Compiled);
    private static readonly Regex UiActionRegex = new(@"data-action=""([a-z0-9]+)""", RegexOptions.Compiled);
    private static readonly Regex CommandRegex = new(@"data-command=""([a-z_]+)""", RegexOptions.Compiled);
    private static readonly Regex RunCommandRegex = new(@"data-run-command=""([a-z_]+)""", RegexOptions.Compiled);
    private static readonly Regex MenuCommandRegex = new(@"\[\s*""([a-z_]+)""\s*,", RegexOptions.Compiled);
    private static readonly Regex TabButtonRegex = new(@"<button class=""tab-btn""\s+data-tab=""([a-z0-9-]+)""", RegexOptions.Compiled);
    private static readonly Regex UiControlRegex = new(@"data-ui-control=""([a-z_]+)""", RegexOptions.Compiled);

    private static readonly HashSet<string> RequiredDesktopCommands = AppCommandCatalog.All
        .Select(command => command.Id)
        .ToHashSet(StringComparer.Ordinal);

    [TestMethod]
    [TestCategory("LegacyShellRegression")]
    public void Section_parsers_are_exposed_as_api_endpoints_and_ui_actions()
    {
        string interfacePath = FindPath("Chummer.Infrastructure", "Xml", "ICharacterSectionService.cs");
        string endpointDirectory = FindDirectory("Chummer.Api", "Endpoints");
        string indexPath = FindPath("Chummer.Web", "wwwroot", "index.html");

        string interfaceText = File.ReadAllText(interfacePath);
        string endpointText = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(endpointDirectory, "*.cs", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(File.ReadAllText));
        string indexText = File.ReadAllText(indexPath);

        HashSet<string> expectedSections = SectionMethodRegex.Matches(interfaceText)
            .Select(match => ToSectionName(match.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> endpointSections = SectionEndpointRegex.Matches(endpointText)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
        endpointSections.UnionWith(SectionMapCallRegex.Matches(endpointText)
            .Select(match => match.Groups[1].Value));

        HashSet<string> uiActions = UiActionRegex.Matches(indexText)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(expectedSections.OrderBy(x => x).ToList(), endpointSections.OrderBy(x => x).ToList(),
            "API endpoint set must match ICharacterSectionService parser set.");

        List<string> missingInUi = expectedSections.Where(section => !uiActions.Contains(section)).OrderBy(x => x).ToList();
        Assert.AreEqual(0, missingInUi.Count, "Missing UI actions for sections: " + string.Join(", ", missingInUi));
    }

    [TestMethod]
    [TestCategory("LegacyShellRegression")]
    public void Desktop_shell_commands_exist_and_have_handlers()
    {
        string indexPath = FindPath("Chummer.Web", "wwwroot", "index.html");
        string indexText = File.ReadAllText(indexPath);

        HashSet<string> commandIds = CommandRegex.Matches(indexText)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> runCommands = RunCommandRegex.Matches(indexText)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> menuCommands = MenuCommandRegex.Matches(indexText)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
        commandIds.UnionWith(runCommands);
        commandIds.UnionWith(menuCommands);

        List<string> missingCommands = RequiredDesktopCommands.Where(command => !commandIds.Contains(command)).OrderBy(x => x).ToList();
        Assert.AreEqual(0, missingCommands.Count, "Missing desktop command buttons: " + string.Join(", ", missingCommands));

        foreach (string command in RequiredDesktopCommands)
        {
            StringAssert.Contains(indexText, $"{command}:", $"Missing command handler for '{command}'.");
        }
    }

    [TestMethod]
    public void Dual_head_adapter_projects_reference_shared_presentation_layer()
    {
        string blazorProjectPath = FindPath("Chummer.Blazor", "Chummer.Blazor.csproj");
        string avaloniaProjectPath = FindPath("Chummer.Avalonia", "Chummer.Avalonia.csproj");
        string blazorProjectText = File.ReadAllText(blazorProjectPath);
        string avaloniaProjectText = File.ReadAllText(avaloniaProjectPath);
        string blazorProgramPath = FindPath("Chummer.Blazor", "Program.cs");
        string blazorProgramText = File.ReadAllText(blazorProgramPath);
        string avaloniaProgramPath = FindPath("Chummer.Avalonia", "Program.cs");
        string avaloniaProgramText = File.ReadAllText(avaloniaProgramPath);
        string avaloniaAppCodePath = FindPath("Chummer.Avalonia", "App.axaml.cs");
        string avaloniaAppCodeText = File.ReadAllText(avaloniaAppCodePath);
        string avaloniaMainWindowCodePath = FindPath("Chummer.Avalonia", "MainWindow.axaml.cs");
        string avaloniaMainWindowCodeText = File.ReadAllText(avaloniaMainWindowCodePath);

        StringAssert.Contains(blazorProjectText, @"..\Chummer.Presentation\Chummer.Presentation.csproj");
        StringAssert.Contains(blazorProjectText, @"..\Chummer.Contracts\Chummer.Contracts.csproj");
        StringAssert.Contains(avaloniaProjectText, @"..\Chummer.Presentation\Chummer.Presentation.csproj");
        StringAssert.Contains(avaloniaProjectText, @"..\Chummer.Contracts\Chummer.Contracts.csproj");
        StringAssert.Contains(avaloniaProjectText, "Avalonia.Desktop");
        StringAssert.Contains(avaloniaProjectText, "Avalonia.Themes.Fluent");

        Assert.IsTrue(File.Exists(FindPath("Chummer.Blazor", "CharacterOverviewStateBridge.cs")));
        Assert.IsTrue(File.Exists(FindPath("Chummer.Avalonia", "CharacterOverviewViewModelAdapter.cs")));
        Assert.IsTrue(File.Exists(FindPath("Chummer.Blazor", "Components", "App.razor")));
        Assert.IsTrue(File.Exists(FindPath("Chummer.Blazor", "Components", "Pages", "Home.razor")));
        Assert.IsTrue(File.Exists(FindPath("Chummer.Avalonia", "App.axaml")));
        Assert.IsTrue(File.Exists(FindPath("Chummer.Avalonia", "MainWindow.axaml")));
        Assert.IsTrue(File.Exists(FindPath("Chummer.Avalonia", "MainWindow.axaml.cs")));
        Assert.IsTrue(File.Exists(FindPath("Chummer.Avalonia", "DesktopDialogWindow.axaml")));
        Assert.IsTrue(File.Exists(FindPath("Chummer.Avalonia", "DesktopDialogWindow.axaml.cs")));
        StringAssert.Contains(blazorProgramText, "AddRazorComponents()");
        StringAssert.Contains(blazorProgramText, "MapRazorComponents<App>()");
        StringAssert.Contains(avaloniaProgramText, "BuildAvaloniaApp()");
        StringAssert.Contains(avaloniaProgramText, "UsePlatformDetect()");
        StringAssert.Contains(avaloniaAppCodeText, "ConfigureServices(");
        StringAssert.Contains(avaloniaAppCodeText, "GetRequiredService<MainWindow>()");
        StringAssert.Contains(avaloniaAppCodeText, "IChummerClient");
        StringAssert.Contains(avaloniaAppCodeText, "ICharacterOverviewPresenter");
        StringAssert.Contains(avaloniaMainWindowCodeText, "public MainWindow(");
        StringAssert.Contains(avaloniaMainWindowCodeText, "new DesktopDialogWindow(");
    }

    [TestMethod]
    public void Workspace_routes_include_section_projection_endpoint()
    {
        string workspaceEndpointsPath = FindPath("Chummer.Api", "Endpoints", "WorkspaceEndpoints.cs");
        string workspaceEndpointsText = File.ReadAllText(workspaceEndpointsPath);

        StringAssert.Contains(workspaceEndpointsText, "/api/workspaces/{id}/sections/{sectionId}");
        StringAssert.Contains(workspaceEndpointsText, "workspaceService.GetSection(workspaceId, sectionId)");
        StringAssert.Contains(workspaceEndpointsText, "/api/workspaces/{id}/summary");
        StringAssert.Contains(workspaceEndpointsText, "workspaceService.GetSummary(workspaceId)");
        StringAssert.Contains(workspaceEndpointsText, "/api/workspaces/{id}/validate");
        StringAssert.Contains(workspaceEndpointsText, "workspaceService.Validate(workspaceId)");
    }

    [TestMethod]
    public void Solution_includes_headless_and_dual_head_projects()
    {
        string solutionPath = FindPath("Chummer.sln");
        string solutionText = File.ReadAllText(solutionPath);

        string[] requiredProjects =
        {
            @"Chummer.Api\Chummer.Api.csproj",
            @"Chummer.Application\Chummer.Application.csproj",
            @"Chummer.Contracts\Chummer.Contracts.csproj",
            @"Chummer.Infrastructure\Chummer.Infrastructure.csproj",
            @"Chummer.Presentation\Chummer.Presentation.csproj",
            @"Chummer.Portal\Chummer.Portal.csproj",
            @"Chummer.Avalonia\Chummer.Avalonia.csproj",
            @"Chummer.Avalonia.Browser\Chummer.Avalonia.Browser.csproj",
            @"Chummer.Blazor\Chummer.Blazor.csproj",
            @"Chummer.Blazor.Desktop\Chummer.Blazor.Desktop.csproj"
        };

        foreach (string requiredProject in requiredProjects)
        {
            StringAssert.Contains(solutionText, requiredProject, "Missing solution entry: " + requiredProject);
        }
    }

    [TestMethod]
    public void Docker_compose_exposes_blazor_head_with_api_dependency()
    {
        string projectPath = FindPath("Chummer.Blazor", "Chummer.Blazor.csproj");
        string projectText = File.ReadAllText(projectPath);
        string programPath = FindPath("Chummer.Blazor", "Program.cs");
        string programText = File.ReadAllText(programPath);
        string migrationLoopPath = FindPath("scripts", "migration-loop.sh");
        string migrationLoopText = File.ReadAllText(migrationLoopPath);
        string apiIntegrationTestsPath = FindPath("Chummer.Tests", "ApiIntegrationTests.cs");
        string apiIntegrationTestsText = File.ReadAllText(apiIntegrationTestsPath);
        string dualHeadTestsPath = FindPath("Chummer.Tests", "Presentation", "DualHeadAcceptanceTests.cs");
        string dualHeadTestsText = File.ReadAllText(dualHeadTestsPath);

        StringAssert.Contains(projectText, "<Project Sdk=\"Microsoft.NET.Sdk.Web\">");
        StringAssert.Contains(programText, "AddRazorComponents()");
        StringAssert.Contains(programText, "CHUMMER_API_BASE_URL");
        StringAssert.Contains(programText, "http://chummer-api:8080");
        StringAssert.Contains(apiIntegrationTestsText, "http://chummer-api:8080");
        StringAssert.Contains(dualHeadTestsText, "http://chummer-api:8080");
        StringAssert.Contains(migrationLoopText, "docker compose up -d --build --remove-orphans chummer-api chummer-blazor");
        Assert.IsFalse(
            migrationLoopText.Contains("--profile ui up", StringComparison.Ordinal),
            "Migration loop must not require the ui profile to start chummer-blazor.");
    }

    [TestMethod]
    public void Default_state_persistence_is_file_backed_and_configurable()
    {
        string serviceRegistrationPath = FindPath("Chummer.Infrastructure", "DependencyInjection", "ServiceCollectionExtensions.cs");
        string serviceRegistrationText = File.ReadAllText(serviceRegistrationPath);

        StringAssert.Contains(serviceRegistrationText, "CHUMMER_STATE_PATH");
        StringAssert.Contains(serviceRegistrationText, "new FileSettingsStore(stateDirectory)");
        StringAssert.Contains(serviceRegistrationText, "new FileRosterStore(stateDirectory)");
        StringAssert.Contains(serviceRegistrationText, "new FileWorkspaceStore(stateDirectory)");
        StringAssert.Contains(serviceRegistrationText, "CHUMMER_WORKSPACE_STORE_PATH");
        StringAssert.Contains(serviceRegistrationText, "CHUMMER_AMENDS_PATH");
        StringAssert.Contains(serviceRegistrationText, "IContentOverlayCatalogService");
        Assert.IsFalse(serviceRegistrationText.Contains("new InMemoryWorkspaceStore()", StringComparison.Ordinal));
    }

    [TestMethod]
    public void App_command_catalog_ids_are_unique()
    {
        List<string> duplicateIds = AppCommandCatalog.All
            .GroupBy(command => command.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(id => id)
            .ToList();

        Assert.AreEqual(0, duplicateIds.Count, "Duplicate command ids in AppCommandCatalog: " + string.Join(", ", duplicateIds));
    }

    [TestMethod]
    public void Navigation_tab_catalog_ids_are_unique_and_cover_legacy_shell_tabs()
    {
        string indexPath = FindPath("Chummer.Web", "wwwroot", "index.html");
        string indexText = File.ReadAllText(indexPath);

        HashSet<string> legacyTabIds = TabButtonRegex.Matches(indexText)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        List<string> duplicateIds = NavigationTabCatalog.All
            .GroupBy(tab => tab.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(id => id)
            .ToList();

        Assert.AreEqual(0, duplicateIds.Count, "Duplicate tab ids in NavigationTabCatalog: " + string.Join(", ", duplicateIds));

        HashSet<string> catalogTabIds = NavigationTabCatalog.All
            .Select(tab => tab.Id)
            .ToHashSet(StringComparer.Ordinal);

        List<string> missingInCatalog = legacyTabIds
            .Where(tabId => !catalogTabIds.Contains(tabId))
            .OrderBy(x => x)
            .ToList();

        Assert.AreEqual(0, missingInCatalog.Count, "Legacy shell tabs missing from NavigationTabCatalog: " + string.Join(", ", missingInCatalog));

        List<string> tabsWithoutSection = NavigationTabCatalog.All
            .Where(tab => string.IsNullOrWhiteSpace(tab.SectionId))
            .Select(tab => tab.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.AreEqual(0, tabsWithoutSection.Count, "Navigation tabs without section bindings: " + string.Join(", ", tabsWithoutSection));
    }

    [TestMethod]
    public void Workspace_surface_action_catalog_covers_legacy_shell_actions()
    {
        string indexPath = FindPath("Chummer.Web", "wwwroot", "index.html");
        string indexText = File.ReadAllText(indexPath);

        HashSet<string> legacyActionIds = UiActionRegex.Matches(indexText)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> catalogTargets = WorkspaceSurfaceActionCatalog.All
            .Select(action => action.TargetId)
            .ToHashSet(StringComparer.Ordinal);

        List<string> missingTargets = legacyActionIds
            .Where(actionId => !catalogTargets.Contains(actionId))
            .OrderBy(x => x)
            .ToList();
        Assert.AreEqual(0, missingTargets.Count, "Legacy data-action ids missing in WorkspaceSurfaceActionCatalog: " + string.Join(", ", missingTargets));

        List<string> duplicateActionIds = WorkspaceSurfaceActionCatalog.All
            .GroupBy(action => action.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(id => id)
            .ToList();
        Assert.AreEqual(0, duplicateActionIds.Count, "Duplicate workspace surface action ids: " + string.Join(", ", duplicateActionIds));
    }

    [TestMethod]
    public void Desktop_ui_control_catalog_covers_legacy_shell_controls()
    {
        string indexPath = FindPath("Chummer.Web", "wwwroot", "index.html");
        string indexText = File.ReadAllText(indexPath);
        string presenterPath = FindPath("Chummer.Presentation", "Overview", "CharacterOverviewPresenter.cs");
        string dialogFactoryPath = FindPath("Chummer.Presentation", "Overview", "DesktopDialogFactory.cs");
        string dialogTemplateText = string.Join(
            Environment.NewLine,
            File.ReadAllText(presenterPath),
            File.ReadAllText(dialogFactoryPath));

        HashSet<string> legacyControlIds = UiControlRegex.Matches(indexText)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> catalogControlIds = DesktopUiControlCatalog.All
            .Select(control => control.Id)
            .ToHashSet(StringComparer.Ordinal);

        List<string> missingControls = legacyControlIds
            .Where(controlId => !catalogControlIds.Contains(controlId))
            .OrderBy(x => x)
            .ToList();
        Assert.AreEqual(0, missingControls.Count, "Legacy ui-control ids missing in DesktopUiControlCatalog: " + string.Join(", ", missingControls));

        List<string> controlsMissingPresenterTemplate = legacyControlIds
            .Where(controlId => !dialogTemplateText.Contains($"\"{controlId}\" =>", StringComparison.Ordinal))
            .OrderBy(x => x)
            .ToList();
        Assert.AreEqual(0, controlsMissingPresenterTemplate.Count, "Controls missing dialog templates: " + string.Join(", ", controlsMissingPresenterTemplate));
    }

    [TestMethod]
    [TestCategory("LegacyShellRegression")]
    public void Ui_exposes_summary_validate_and_metadata_actions()
    {
        string indexPath = FindPath("Chummer.Web", "wwwroot", "index.html");
        string indexText = File.ReadAllText(indexPath);

        StringAssert.Contains(indexText, "data-action=\"summary\"");
        StringAssert.Contains(indexText, "data-action=\"validate\"");
        StringAssert.Contains(indexText, "data-action=\"metadata\"");

        StringAssert.Contains(indexText, "action === \"summary\"");
        StringAssert.Contains(indexText, "action === \"validate\"");
        StringAssert.Contains(indexText, "action === \"metadata\"");
    }

    [TestMethod]
    [TestCategory("LegacyShellRegression")]
    public void Critical_commands_are_not_placeholder_stubs()
    {
        string indexPath = FindPath("Chummer.Web", "wwwroot", "index.html");
        string indexText = File.ReadAllText(indexPath);

        string[] disallowedPatterns =
        {
            "hero_lab_importer: () => showNote(",
            "print_setup: () => showNote(",
            "close_all: () => showNote(",
            "restart: () => location.reload()"
        };

        foreach (string pattern in disallowedPatterns)
        {
            Assert.IsFalse(indexText.Contains(pattern, StringComparison.Ordinal),
                $"Placeholder command implementation still present: {pattern}");
        }
    }

    [TestMethod]
    [TestCategory("LegacyShellRegression")]
    public void Desktop_shell_layout_contains_core_winforms_like_regions()
    {
        string indexPath = FindPath("Chummer.Web", "wwwroot", "index.html");
        string indexText = File.ReadAllText(indexPath);

        StringAssert.Contains(indexText, "class=\"menu-bar\"");
        StringAssert.Contains(indexText, "id=\"mdiStrip\"");
        StringAssert.Contains(indexText, "Character Navigator");
        StringAssert.Contains(indexText, "id=\"openCharactersTree\"");
        StringAssert.Contains(indexText, "id=\"charState\"");
        StringAssert.Contains(indexText, "id=\"serviceState\"");
        StringAssert.Contains(indexText, "id=\"timeState\"");
        StringAssert.Contains(indexText, "id=\"complianceState\"");
        StringAssert.Contains(indexText, "Desktop Summary Header");
        StringAssert.Contains(indexText, "data-ui-control=");
        StringAssert.Contains(indexText, "function handleUiControl(");

        HashSet<string> tabIds = TabButtonRegex.Matches(indexText)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        Assert.IsTrue(tabIds.Count >= 16, $"Expected at least 16 desktop-style navigation tabs, got {tabIds.Count}.");
        Assert.IsTrue(tabIds.Contains("tab-info"), "Missing Info navigation tab.");
        Assert.IsTrue(tabIds.Contains("tab-gear"), "Missing Gear navigation tab.");
        Assert.IsTrue(tabIds.Contains("tab-magician"), "Missing Magician navigation tab.");
        Assert.IsTrue(tabIds.Contains("tab-improvements"), "Missing Improvements navigation tab.");
    }

    [TestMethod]
    [TestCategory("LegacyShellRegression")]
    public void Workspace_uses_live_document_state_and_recent_file_hooks()
    {
        string indexPath = FindPath("Chummer.Web", "wwwroot", "index.html");
        string indexText = File.ReadAllText(indexPath);

        StringAssert.Contains(indexText, "const openDocs = [];");
        StringAssert.Contains(indexText, "const recentFiles = [];");
        StringAssert.Contains(indexText, "function syncCurrentDocumentFromForm()");
        StringAssert.Contains(indexText, "function openDocument(");
        StringAssert.Contains(indexText, "function addRecentFile(");
        StringAssert.Contains(indexText, "function executeCommand(");
        StringAssert.Contains(indexText, "open_recent_");
        StringAssert.Contains(indexText, "clear_unpinned_items");
        StringAssert.Contains(indexText, "openCharactersTreeEl.addEventListener(\"click\"");
        StringAssert.Contains(indexText, "mdiStripEl.addEventListener(\"click\"");
    }

    [TestMethod]
    [TestCategory("LegacyShellRegression")]
    public void Ui_click_paths_are_wired_for_commands_controls_and_dialogs()
    {
        string indexPath = FindPath("Chummer.Web", "wwwroot", "index.html");
        string indexText = File.ReadAllText(indexPath);

        StringAssert.Contains(indexText, "for (const button of document.querySelectorAll(\"[data-command]\"))");
        StringAssert.Contains(indexText, "await executeCommand(command)");
        StringAssert.Contains(indexText, "for (const button of document.querySelectorAll(\"[data-ui-control]\"))");
        StringAssert.Contains(indexText, "handleUiControl(button.dataset.uiControl)");
        StringAssert.Contains(indexText, "openDesktopDialog(");
        StringAssert.Contains(indexText, "dialogCloseEl.addEventListener(\"click\", closeDesktopDialog)");
        StringAssert.Contains(indexText, "dialogBackdropEl.addEventListener(\"click\"");
        StringAssert.Contains(indexText, "event.key === \"Escape\"");

        string[] dialogBackedCommands =
        {
            "print_setup: () => {",
            "dice_roller: async () => {",
            "global_settings: () => {",
            "character_settings: () => {",
            "translator: async () => {",
            "xml_editor: () => {",
            "master_index: async () => {",
            "character_roster: async () => {",
            "data_exporter: async () => {",
            "report_bug: () => {",
            "about: async () => {"
        };

        foreach (string command in dialogBackedCommands)
        {
            StringAssert.Contains(indexText, command, $"Expected dialog-backed command definition missing: {command}");
        }

        string[] dialogControlIds =
        {
            "globalUiScale",
            "globalTheme",
            "globalLanguage",
            "globalCompactMode",
            "characterPriority",
            "characterKarmaNuyen",
            "characterHouseRulesEnabled",
            "characterNotes",
            "diceExpression",
            "translatorSearch",
            "xmlEditorDialog",
            "dataExportPreview"
        };

        foreach (string controlId in dialogControlIds)
        {
            StringAssert.Contains(indexText, controlId, $"Expected dialog control id missing: {controlId}");
        }

        StringAssert.Contains(indexText, "const uiControlHandlers = {");
        HashSet<string> declaredControls = UiControlRegex.Matches(indexText)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
        List<string> missingHandlers = declaredControls
            .Where(controlId => !indexText.Contains($"{controlId}:", StringComparison.Ordinal))
            .OrderBy(x => x)
            .ToList();
        Assert.AreEqual(0, missingHandlers.Count, "Missing ui control handler mappings: " + string.Join(", ", missingHandlers));
        Assert.IsFalse(indexText.Contains("showNote(`Desktop control '${controlId}' invoked on", StringComparison.Ordinal),
            "Generic ui control placeholder behavior is still present.");
    }

    [TestMethod]
    public void Dual_head_acceptance_suite_is_present_for_primary_migration_gate()
    {
        string testPath = FindPath("Chummer.Tests", "Presentation", "DualHeadAcceptanceTests.cs");
        string testText = File.ReadAllText(testPath);

        StringAssert.Contains(testText, "Avalonia_and_Blazor_overview_flows_show_equivalent_state_after_import");
        StringAssert.Contains(testText, "Avalonia_and_Blazor_metadata_save_roundtrip_match");
        StringAssert.Contains(testText, "Avalonia_and_Blazor_tab_selection_loads_same_workspace_section");
        StringAssert.Contains(testText, "Avalonia_and_Blazor_command_dispatch_save_character_matches");
        StringAssert.Contains(testText, "Avalonia_and_Blazor_workspace_action_summary_matches");
        StringAssert.Contains(testText, "Avalonia_and_Blazor_dialog_workflow_keeps_shell_regions_in_parity");
    }

    [TestMethod]
    public void Blazor_shell_component_suite_is_present_for_phase4_gate()
    {
        string testPath = FindPath("Chummer.Tests", "Presentation", "BlazorShellComponentTests.cs");
        string testText = File.ReadAllText(testPath);

        StringAssert.Contains(testText, "MenuBar_renders_open_menu_items_and_applies_enablement_state");
        StringAssert.Contains(testText, "MenuBar_invokes_toggle_and_execute_callbacks");
        StringAssert.Contains(testText, "ToolStrip_applies_selected_and_disabled_states");
        StringAssert.Contains(testText, "WorkspaceLeftPane_renders_shell_controls_and_invokes_callbacks");
        StringAssert.Contains(testText, "SectionPane_switches_between_placeholder_and_section_payload");
        StringAssert.Contains(testText, "DialogHost_renders_dialog_and_emits_events");
    }

    [TestMethod]
    public void Playwright_ui_e2e_gate_is_present_for_phase4_gate()
    {
        string uiE2ePath = FindPath("scripts", "e2e-ui.sh");
        string uiE2eText = File.ReadAllText(uiE2ePath);
        string migrationLoopPath = FindPath("scripts", "migration-loop.sh");
        string migrationLoopText = File.ReadAllText(migrationLoopPath);
        string playwrightScriptPath = FindPath("scripts", "e2e-ui-playwright.cjs");
        string playwrightScriptText = File.ReadAllText(playwrightScriptPath);

        StringAssert.Contains(uiE2eText, "CHUMMER_UI_PLAYWRIGHT");
        StringAssert.Contains(uiE2eText, "docker compose --profile test run --rm -T chummer-playwright");
        StringAssert.Contains(migrationLoopText, "bash scripts/e2e-ui.sh");

        StringAssert.Contains(playwrightScriptText, "Import Raw XML");
        StringAssert.Contains(playwrightScriptText, "#tab-skills");
        StringAssert.Contains(playwrightScriptText, "global_settings");
        StringAssert.Contains(playwrightScriptText, "Save Workspace");
        StringAssert.Contains(playwrightScriptText, "playwright UI flow completed");
    }

    [TestMethod]
    public void Portal_playwright_e2e_uses_portal_stack_dependencies()
    {
        string portalScriptPath = FindPath("scripts", "e2e-portal.sh");
        string portalScriptText = File.ReadAllText(portalScriptPath);

        StringAssert.Contains(portalScriptText, "chummer-playwright-portal");
        StringAssert.Contains(portalScriptText, "docker compose --profile portal up -d --build chummer-api chummer-blazor-portal chummer-avalonia-browser chummer-portal");
    }

    [TestMethod]
    public void Blazor_desktop_host_project_is_present_and_photino_backed()
    {
        string projectPath = FindPath("Chummer.Blazor.Desktop", "Chummer.Blazor.Desktop.csproj");
        string projectText = File.ReadAllText(projectPath);
        string programPath = FindPath("Chummer.Blazor.Desktop", "Program.cs");
        string programText = File.ReadAllText(programPath);
        string indexPath = FindPath("Chummer.Blazor.Desktop", "wwwroot", "index.html");
        string indexText = File.ReadAllText(indexPath);

        StringAssert.Contains(projectText, "Photino.Blazor");
        StringAssert.Contains(projectText, @"..\Chummer.Blazor\Chummer.Blazor.csproj");
        StringAssert.Contains(projectText, @"..\Chummer.Presentation\Chummer.Presentation.csproj");

        StringAssert.Contains(programText, "PhotinoBlazorAppBuilder.CreateDefault");
        StringAssert.Contains(programText, "RootComponents.Add<App>(\"app\")");
        StringAssert.Contains(programText, "CHUMMER_API_BASE_URL");
        StringAssert.Contains(programText, "CHUMMER_API_KEY");

        StringAssert.Contains(indexText, "<app>Loading...</app>");
        StringAssert.Contains(indexText, "_content/Chummer.Blazor/app.css");
        StringAssert.Contains(indexText, "_framework/blazor.webview.js");
    }

    [TestMethod]
    public void Portal_docs_route_uses_dedicated_docs_cluster()
    {
        string portalProgramPath = FindPath("Chummer.Portal", "Program.cs");
        string portalProgramText = File.ReadAllText(portalProgramPath);

        StringAssert.Contains(portalProgramText, "CHUMMER_PORTAL_DOCS_URL");
        StringAssert.Contains(portalProgramText, "RouteId = \"portal-docs\"");
        StringAssert.Contains(portalProgramText, "ClusterId = \"docs-cluster\"");
        StringAssert.Contains(portalProgramText, "Path = \"/docs/{**catch-all}\"");
        StringAssert.Contains(portalProgramText, "BuildRouteTransforms(apiRouteTransforms, \"/docs\")");
    }

    [TestMethod]
    public void Desktop_download_matrix_includes_avalonia_and_blazor_desktop_artifacts()
    {
        string workflowPath = FindPath(".github", "workflows", "desktop-downloads-matrix.yml");
        string workflowText = File.ReadAllText(workflowPath);

        StringAssert.Contains(workflowText, "project: Chummer.Avalonia/Chummer.Avalonia.csproj");
        StringAssert.Contains(workflowText, "project: Chummer.Blazor.Desktop/Chummer.Blazor.Desktop.csproj");
        StringAssert.Contains(workflowText, "pattern = re.compile(r'^chummer-(?P<app>avalonia|blazor-desktop)-");
        StringAssert.Contains(workflowText, "'id': f'{app}-{rid}'");
    }

    [TestMethod]
    public void Runbook_supports_download_manifest_generation_mode()
    {
        string runbookPath = FindPath("scripts", "runbook.sh");
        string runbookText = File.ReadAllText(runbookPath);
        string generatorPath = FindPath("scripts", "generate-releases-manifest.sh");
        string generatorText = File.ReadAllText(generatorPath);

        StringAssert.Contains(runbookText, "RUNBOOK_MODE\" == \"downloads-manifest\"");
        StringAssert.Contains(runbookText, "bash scripts/generate-releases-manifest.sh");

        StringAssert.Contains(generatorText, "Docker/Downloads/releases.json");
        StringAssert.Contains(generatorText, "Chummer.Portal/downloads/releases.json");
        StringAssert.Contains(generatorText, "/downloads/files/");
    }

    [TestMethod]
    public void Dockerfile_tests_includes_blazor_desktop_project_for_container_build_checks()
    {
        string dockerfilePath = FindPath("Docker", "Dockerfile.tests");
        string dockerfileText = File.ReadAllText(dockerfilePath);

        StringAssert.Contains(dockerfileText, "COPY Chummer.Blazor.Desktop/Chummer.Blazor.Desktop.csproj Chummer.Blazor.Desktop/");
        StringAssert.Contains(dockerfileText, "COPY Chummer.Blazor.Desktop/ Chummer.Blazor.Desktop/");
    }

    [TestMethod]
    public void Ci_wires_blazor_component_and_playwright_jobs_for_phase4_gate()
    {
        string componentSuitePath = FindPath("scripts", "test-blazor-components.sh");
        string componentSuiteText = File.ReadAllText(componentSuitePath);
        string uiE2ePath = FindPath("scripts", "e2e-ui.sh");
        string uiE2eText = File.ReadAllText(uiE2ePath);

        StringAssert.Contains(componentSuiteText, "dotnet test Chummer.Tests/Chummer.Tests.csproj");
        StringAssert.Contains(componentSuiteText, "FullyQualifiedName~BlazorShellComponentTests");

        StringAssert.Contains(uiE2eText, "CHUMMER_UI_PLAYWRIGHT");
        StringAssert.Contains(uiE2eText, "docker compose --profile test run --rm -T chummer-playwright");
    }

    [TestMethod]
    public void Avalonia_mainwindow_uses_named_controls_over_findcontrol_orchestration()
    {
        string xamlPath = FindPath("Chummer.Avalonia", "MainWindow.axaml");
        string xamlText = File.ReadAllText(xamlPath);
        string codePath = FindPath("Chummer.Avalonia", "MainWindow.axaml.cs");
        string codeText = File.ReadAllText(codePath);

        Assert.IsFalse(codeText.Contains("FindControl<", StringComparison.Ordinal));
        StringAssert.Contains(codeText, "public MainWindow(");
        StringAssert.Contains(codeText, "_commandsList = CommandsList;");
        StringAssert.Contains(codeText, "_openWorkspacesList = OpenWorkspacesList;");
        StringAssert.Contains(codeText, "_navigationTabsList = NavigationTabsList;");
        StringAssert.Contains(codeText, "_dialogActionsList = DialogActionsList;");
        StringAssert.Contains(codeText, "UpdateMenuButtonStates");
        StringAssert.Contains(codeText, "menuButton.Classes.Set(\"active-menu\", active);");

        StringAssert.Contains(xamlText, "x:Name=\"CommandsList\"");
        StringAssert.Contains(xamlText, "x:Name=\"OpenWorkspacesList\"");
        StringAssert.Contains(xamlText, "x:Name=\"NavigationTabsList\"");
        StringAssert.Contains(xamlText, "x:Name=\"DialogActionsList\"");
        StringAssert.Contains(xamlText, "Classes=\"menu-button\"");
        StringAssert.Contains(xamlText, "Button.menu-button.active-menu");
    }

    [TestMethod]
    public void Dual_heads_wire_keyboard_shortcuts_for_core_commands()
    {
        string blazorHomePath = FindPath("Chummer.Blazor", "Components", "Pages", "Home.razor");
        string blazorHomeText = File.ReadAllText(blazorHomePath);
        string avaloniaXamlPath = FindPath("Chummer.Avalonia", "MainWindow.axaml");
        string avaloniaXamlText = File.ReadAllText(avaloniaXamlPath);
        string avaloniaCodePath = FindPath("Chummer.Avalonia", "MainWindow.axaml.cs");
        string avaloniaCodeText = File.ReadAllText(avaloniaCodePath);

        StringAssert.Contains(blazorHomeText, "@onkeydown=\"OnShellKeyDown\"");
        StringAssert.Contains(blazorHomeText, "args.CtrlKey");
        StringAssert.Contains(blazorHomeText, "\"save_character\"");
        StringAssert.Contains(blazorHomeText, "\"close_window\"");
        StringAssert.Contains(blazorHomeText, "\"global_settings\"");

        StringAssert.Contains(avaloniaXamlText, "KeyDown=\"Window_OnKeyDown\"");
        StringAssert.Contains(avaloniaCodeText, "Window_OnKeyDown");
        StringAssert.Contains(avaloniaCodeText, "Key.S => \"save_character\"");
        StringAssert.Contains(avaloniaCodeText, "Key.W => \"close_window\"");
        StringAssert.Contains(avaloniaCodeText, "Key.G => \"global_settings\"");
    }

    [TestMethod]
    public void Avalonia_headless_smoke_suite_is_present_for_phase5_gate()
    {
        string testPath = FindPath("Chummer.Tests", "Presentation", "AvaloniaHeadlessSmokeTests.cs");
        string testText = File.ReadAllText(testPath);

        StringAssert.Contains(testText, "Avalonia_headless_import_edit_switch_save_smoke");
        StringAssert.Contains(testText, "UseHeadless(");
        StringAssert.Contains(testText, "EnsureHeadlessPlatform();");
        StringAssert.Contains(testText, "adapter.ImportAsync(");
        StringAssert.Contains(testText, "UpdateMetadataAsync(");
        StringAssert.Contains(testText, "SaveAsync(");
    }

    private static string ToSectionName(string pascalName)
    {
        return pascalName.ToLowerInvariant();
    }

    private static string FindPath(params string[] parts)
    {
        foreach (string? root in CandidateRoots())
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;

            DirectoryInfo current = new(root);
            while (true)
            {
                string candidate = Path.Combine(new[] { current.FullName }.Concat(parts).ToArray());
                if (File.Exists(candidate))
                    return candidate;

                if (current.Parent == null)
                    break;

                current = current.Parent;
            }
        }

        throw new FileNotFoundException("Could not locate file.", Path.Combine(parts));
    }

    private static string FindDirectory(params string[] parts)
    {
        foreach (string? root in CandidateRoots())
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;

            DirectoryInfo current = new(root);
            while (true)
            {
                string candidate = Path.Combine(new[] { current.FullName }.Concat(parts).ToArray());
                if (Directory.Exists(candidate))
                    return candidate;

                if (current.Parent == null)
                    break;

                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate directory: " + Path.Combine(parts));
    }

    private static IEnumerable<string?> CandidateRoots()
    {
        yield return Environment.GetEnvironmentVariable("CHUMMER_REPO_ROOT");
        yield return Directory.GetCurrentDirectory();
        yield return AppContext.BaseDirectory;
        yield return "/src";
    }
}
