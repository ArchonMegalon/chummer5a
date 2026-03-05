#nullable enable annotations

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
        Assert.IsEmpty(missingInUi, "Missing UI actions for sections: " + string.Join(", ", missingInUi));
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
        Assert.IsEmpty(missingCommands, "Missing desktop command buttons: " + string.Join(", ", missingCommands));

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
        string avaloniaDialogsCodePath = FindPath("Chummer.Avalonia", "MainWindow.Dialogs.cs");
        string avaloniaDialogsCodeText = File.ReadAllText(avaloniaDialogsCodePath);

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
        StringAssert.Contains(avaloniaAppCodeText, "AddChummerLocalRuntimeClient");
        StringAssert.Contains(avaloniaAppCodeText, "ICharacterOverviewPresenter");
        StringAssert.Contains(avaloniaMainWindowCodeText, "public MainWindow(");
        StringAssert.Contains(avaloniaDialogsCodeText, "new DesktopDialogWindow(");
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
            @"Chummer.Desktop.Runtime\Chummer.Desktop.Runtime.csproj",
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
    public void Api_startup_enforces_content_bundle_validation_contract()
    {
        string apiProgramPath = FindPath("Chummer.Api", "Program.cs");
        string apiProgramText = File.ReadAllText(apiProgramPath);
        string serviceRegistrationPath = FindPath("Chummer.Infrastructure", "DependencyInjection", "ServiceCollectionExtensions.cs");
        string serviceRegistrationText = File.ReadAllText(serviceRegistrationPath);
        string readmePath = FindPath("README.md");
        string readmeText = File.ReadAllText(readmePath);

        StringAssert.Contains(apiProgramText, "requireContentBundle: true");
        StringAssert.Contains(serviceRegistrationText, "CHUMMER_REQUIRE_CONTENT_BUNDLE");
        StringAssert.Contains(serviceRegistrationText, "ValidateContentBundle");
        StringAssert.Contains(serviceRegistrationText, "lifemodules.xml");
        StringAssert.Contains(serviceRegistrationText, "language XML files");
        string overlayServicePath = FindPath("Chummer.Infrastructure", "Files", "FileSystemContentOverlayCatalogService.cs");
        string overlayServiceText = File.ReadAllText(overlayServicePath);
        StringAssert.Contains(overlayServiceText, "ValidateManifestChecksums");
        StringAssert.Contains(overlayServiceText, "manifest.Checksums");
        StringAssert.Contains(readmeText, "\"checksums\"");
        StringAssert.Contains(readmeText, "CHUMMER_REQUIRE_CONTENT_BUNDLE");
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

        Assert.IsEmpty(duplicateIds, "Duplicate command ids in AppCommandCatalog: " + string.Join(", ", duplicateIds));
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

        Assert.IsEmpty(duplicateIds, "Duplicate tab ids in NavigationTabCatalog: " + string.Join(", ", duplicateIds));

        HashSet<string> catalogTabIds = NavigationTabCatalog.All
            .Select(tab => tab.Id)
            .ToHashSet(StringComparer.Ordinal);

        List<string> missingInCatalog = legacyTabIds
            .Where(tabId => !catalogTabIds.Contains(tabId))
            .OrderBy(x => x)
            .ToList();

        Assert.IsEmpty(missingInCatalog, "Legacy shell tabs missing from NavigationTabCatalog: " + string.Join(", ", missingInCatalog));

        List<string> tabsWithoutSection = NavigationTabCatalog.All
            .Where(tab => string.IsNullOrWhiteSpace(tab.SectionId))
            .Select(tab => tab.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.IsEmpty(tabsWithoutSection, "Navigation tabs without section bindings: " + string.Join(", ", tabsWithoutSection));
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
        Assert.IsEmpty(missingTargets, "Legacy data-action ids missing in WorkspaceSurfaceActionCatalog: " + string.Join(", ", missingTargets));

        List<string> duplicateActionIds = WorkspaceSurfaceActionCatalog.All
            .GroupBy(action => action.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(id => id)
            .ToList();
        Assert.IsEmpty(duplicateActionIds, "Duplicate workspace surface action ids: " + string.Join(", ", duplicateActionIds));
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
        Assert.IsEmpty(missingControls, "Legacy ui-control ids missing in DesktopUiControlCatalog: " + string.Join(", ", missingControls));

        List<string> controlsMissingPresenterTemplate = legacyControlIds
            .Where(controlId => !dialogTemplateText.Contains($"\"{controlId}\" =>", StringComparison.Ordinal))
            .OrderBy(x => x)
            .ToList();
        Assert.IsEmpty(controlsMissingPresenterTemplate, "Controls missing dialog templates: " + string.Join(", ", controlsMissingPresenterTemplate));
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

        Assert.IsGreaterThanOrEqualTo(16, tabIds.Count, $"Expected at least 16 desktop-style navigation tabs, got {tabIds.Count}.");
        CollectionAssert.Contains(tabIds.ToList(), "tab-info", "Missing Info navigation tab.");
        CollectionAssert.Contains(tabIds.ToList(), "tab-gear", "Missing Gear navigation tab.");
        CollectionAssert.Contains(tabIds.ToList(), "tab-magician", "Missing Magician navigation tab.");
        CollectionAssert.Contains(tabIds.ToList(), "tab-improvements", "Missing Improvements navigation tab.");
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
        Assert.IsEmpty(missingHandlers, "Missing ui control handler mappings: " + string.Join(", ", missingHandlers));
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
        StringAssert.Contains(testText, "Avalonia_and_Blazor_info_family_workspace_actions_render_matching_sections");
        StringAssert.Contains(testText, "Avalonia_and_Blazor_attributes_and_skills_workspace_actions_render_matching_sections");
        StringAssert.Contains(testText, "Avalonia_and_Blazor_gear_family_workspace_actions_render_matching_sections");
        StringAssert.Contains(testText, "Avalonia_and_Blazor_magic_family_workspace_actions_render_matching_sections");
        StringAssert.Contains(testText, "Avalonia_and_Blazor_support_family_workspace_actions_render_matching_sections");
        StringAssert.Contains(testText, "Avalonia_and_Blazor_combat_and_cyberware_workspace_actions_render_matching_sections");
        StringAssert.Contains(testText, "Avalonia_and_Blazor_dialog_workflow_keeps_shell_regions_in_parity");
    }

    [TestMethod]
    public void Blazor_shell_component_suite_is_present_for_phase4_gate()
    {
        string testPath = FindPath("Chummer.Tests", "Presentation", "BlazorShellComponentTests.cs");
        string testText = File.ReadAllText(testPath);
        string desktopShellRulesetPath = FindPath("Chummer.Tests", "Presentation", "DesktopShellRulesetCatalogTests.cs");
        string desktopShellRulesetText = File.ReadAllText(desktopShellRulesetPath);

        StringAssert.Contains(testText, "MenuBar_renders_open_menu_items_and_applies_enablement_state");
        StringAssert.Contains(testText, "MenuBar_invokes_toggle_and_execute_callbacks");
        StringAssert.Contains(testText, "ToolStrip_applies_selected_and_disabled_states");
        StringAssert.Contains(testText, "WorkspaceLeftPane_renders_shell_controls_and_invokes_callbacks");
        StringAssert.Contains(testText, "SectionPane_switches_between_placeholder_and_section_payload");
        StringAssert.Contains(testText, "DialogHost_renders_dialog_and_emits_events");
        StringAssert.Contains(desktopShellRulesetText, "DesktopShell_uses_active_ruleset_plugin_catalogs_for_actions_and_controls");
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
        StringAssert.Contains(uiE2eText, "CHUMMER_E2E_PLAYWRIGHT_SOFT_FAIL");
        StringAssert.Contains(uiE2eText, "CHUMMER_E2E_DOCKER_FALLBACK");
        StringAssert.Contains(uiE2eText, "CHUMMER_E2E_HOST_PROBE_ATTEMPTS");
        StringAssert.Contains(uiE2eText, "CHUMMER_E2E_DOCKER_PROBE_ATTEMPTS");
        StringAssert.Contains(uiE2eText, "docker_fetch_with_key");
        StringAssert.Contains(uiE2eText, "docker compose --profile test run --build --rm -T chummer-playwright");
        StringAssert.Contains(migrationLoopText, "bash scripts/e2e-ui.sh");

        StringAssert.Contains(playwrightScriptText, "input[type=\"file\"]");
        StringAssert.Contains(playwrightScriptText, "CHUMMER_UI_SAMPLE_FILE");
        StringAssert.Contains(playwrightScriptText, "BLUE.chum5");
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

        StringAssert.Contains(portalScriptText, "CHUMMER_E2E_PLAYWRIGHT_SOFT_FAIL");
        StringAssert.Contains(portalScriptText, "skipping portal e2e: docker daemon permission denied in this environment.");
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
        string runtimePath = FindPath("Chummer.Desktop.Runtime", "ServiceCollectionDesktopRuntimeExtensions.cs");
        string runtimeText = File.ReadAllText(runtimePath);
        string indexPath = FindPath("Chummer.Blazor.Desktop", "wwwroot", "index.html");
        string indexText = File.ReadAllText(indexPath);

        StringAssert.Contains(projectText, "Photino.Blazor");
        StringAssert.Contains(projectText, @"..\Chummer.Blazor\Chummer.Blazor.csproj");
        StringAssert.Contains(projectText, @"..\Chummer.Desktop.Runtime\Chummer.Desktop.Runtime.csproj");
        StringAssert.Contains(projectText, @"..\Chummer.Presentation\Chummer.Presentation.csproj");

        StringAssert.Contains(programText, "PhotinoBlazorAppBuilder.CreateDefault");
        StringAssert.Contains(programText, "RootComponents.Add<App>(\"app\")");
        StringAssert.Contains(programText, "AddChummerLocalRuntimeClient");

        StringAssert.Contains(runtimeText, "CHUMMER_CLIENT_MODE");
        StringAssert.Contains(runtimeText, "CHUMMER_DESKTOP_CLIENT_MODE");
        StringAssert.Contains(runtimeText, "CHUMMER_API_BASE_URL");
        StringAssert.Contains(runtimeText, "CHUMMER_API_KEY");
        StringAssert.Contains(runtimeText, "AddChummerHeadlessCore");
        StringAssert.Contains(runtimeText, "Set {ApiBaseUrlEnvironmentVariable} when {ClientModeEnvironmentVariable}=http (legacy: {LegacyDesktopClientModeEnvironmentVariable}=http).");
        Assert.IsFalse(runtimeText.Contains("http://127.0.0.1:8088", StringComparison.Ordinal));

        StringAssert.Contains(indexText, "<app>Loading...</app>");
        StringAssert.Contains(indexText, "_content/Chummer.Blazor/app.css");
        StringAssert.Contains(indexText, "_framework/blazor.webview.js");
    }

    [TestMethod]
    public void Blazor_shell_is_promoted_to_layout_layer()
    {
        string mainLayoutPath = FindPath("Chummer.Blazor", "Components", "Layout", "MainLayout.razor");
        string mainLayoutText = File.ReadAllText(mainLayoutPath);
        string desktopShellPath = FindPath("Chummer.Blazor", "Components", "Layout", "DesktopShell.razor");
        string desktopShellText = File.ReadAllText(desktopShellPath);
        string homePath = FindPath("Chummer.Blazor", "Components", "Pages", "Home.razor");
        string homeText = File.ReadAllText(homePath);
        string deepLinkPath = FindPath("Chummer.Blazor", "Components", "Pages", "DeepLinkCheck.razor");
        string deepLinkText = File.ReadAllText(deepLinkPath);

        StringAssert.Contains(mainLayoutText, "<DesktopShell />");
        Assert.IsFalse(mainLayoutText.Contains("IsHomeRoute()", StringComparison.Ordinal));
        Assert.IsFalse(mainLayoutText.Contains("@Body", StringComparison.Ordinal));
        StringAssert.Contains(desktopShellText, "class=\"desktop-shell\"");
        StringAssert.Contains(desktopShellText, "ImportedFileName=\"@ImportedFileName\"");
        StringAssert.Contains(desktopShellText, "ImportError=\"@ImportError\"");
        StringAssert.Contains(desktopShellText, "LastUiUtc=\"@_lastUiUtc\"");
        Assert.IsFalse(desktopShellText.Contains("ImportedFileName=\"ImportedFileName\"", StringComparison.Ordinal));
        Assert.IsFalse(desktopShellText.Contains("ImportError=\"ImportError\"", StringComparison.Ordinal));
        StringAssert.Contains(homeText, "@page \"/\"");
        StringAssert.Contains(deepLinkText, "@layout Chummer.Blazor.Components.Layout.NoLayout");
        Assert.IsFalse(homeText.Contains("desktop-shell", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Portal_docs_route_shares_api_cluster_contract()
    {
        string portalProgramPath = FindPath("Chummer.Portal", "Program.cs");
        string portalProgramText = File.ReadAllText(portalProgramPath);
        string portalSettingsPath = FindPath("Chummer.Portal", "appsettings.json");
        string portalSettingsText = File.ReadAllText(portalSettingsPath);

        StringAssert.Contains(portalProgramText, "RouteId = \"portal-docs\"");
        StringAssert.Contains(portalProgramText, "ClusterId = \"api-cluster\"");
        StringAssert.Contains(portalProgramText, "Path = \"/docs/{**catch-all}\"");
        Assert.IsFalse(portalProgramText.Contains("CHUMMER_PORTAL_DOCS_URL", StringComparison.Ordinal));
        Assert.IsFalse(portalProgramText.Contains("docs-cluster", StringComparison.Ordinal));
        Assert.IsFalse(portalProgramText.Contains("BuildRouteTransforms(apiRouteTransforms, \"/docs\")", StringComparison.Ordinal));
        Assert.IsFalse(portalSettingsText.Contains("DocsBaseUrl", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Portal_downloads_page_shows_explicit_unpublished_state()
    {
        string portalPageBuilderPath = FindPath("Chummer.Portal", "PortalPageBuilder.cs");
        string portalPageBuilderText = File.ReadAllText(portalPageBuilderPath);

        StringAssert.Contains(portalPageBuilderText, "version === 'unpublished'");
        StringAssert.Contains(portalPageBuilderText, "No published desktop builds yet");
        StringAssert.Contains(portalPageBuilderText, "Run desktop-downloads workflow and deploy the generated bundle.");
    }

    [TestMethod]
    public void Portal_download_manifest_discovers_local_artifacts_when_manifest_is_empty()
    {
        string portalProgramPath = FindPath("Chummer.Portal", "Program.cs");
        string portalProgramText = File.ReadAllText(portalProgramPath);
        string portalDownloadsServicePath = FindPath("Chummer.Portal", "PortalDownloadsService.cs");
        string portalDownloadsServiceText = File.ReadAllText(portalDownloadsServicePath);

        StringAssert.Contains(portalProgramText, "LoadReleaseManifest(resolvedManifestPath, resolvedReleaseFilesPath, downloadsBaseUrl)");
        StringAssert.Contains(portalDownloadsServiceText, "DiscoverLocalArtifacts");
        StringAssert.Contains(portalDownloadsServiceText, "LocalArtifactPattern");
        StringAssert.Contains(portalDownloadsServiceText, "chummer-(?<app>avalonia|blazor-desktop)-(?<rid>[^.]+)\\.(?<ext>zip|tar\\.gz)");
        StringAssert.Contains(portalDownloadsServiceText, "\"osx-x64\" => \"macOS x64\"");
        StringAssert.Contains(portalDownloadsServiceText, "if (parsedManifest is not null && parsedManifest.Downloads.Count > 0)");
        StringAssert.Contains(portalDownloadsServiceText, "return new DownloadReleaseManifest(");
        StringAssert.Contains(portalDownloadsServiceText, "Url: $\"/downloads/{relativePath}\"");
    }

    [TestMethod]
    public void Desktop_download_matrix_includes_avalonia_and_blazor_desktop_artifacts()
    {
        string workflowPath = FindPath(".github", "workflows", "desktop-downloads-matrix.yml");
        string workflowText = File.ReadAllText(workflowPath);
        string manifestScriptPath = FindPath("scripts", "generate-releases-manifest.sh");
        string manifestScriptText = File.ReadAllText(manifestScriptPath);
        string verifyScriptPath = FindPath("scripts", "verify-releases-manifest.sh");
        string verifyScriptText = File.ReadAllText(verifyScriptPath);

        StringAssert.Contains(workflowText, "project: Chummer.Avalonia/Chummer.Avalonia.csproj");
        StringAssert.Contains(workflowText, "project: Chummer.Blazor.Desktop/Chummer.Blazor.Desktop.csproj");
        StringAssert.Contains(
            workflowText,
            "app: avalonia\n            project: Chummer.Avalonia/Chummer.Avalonia.csproj\n            os: macos-latest\n            rid: osx-x64");
        StringAssert.Contains(
            workflowText,
            "app: blazor-desktop\n            project: Chummer.Blazor.Desktop/Chummer.Blazor.Desktop.csproj\n            os: macos-latest\n            rid: osx-x64");
        StringAssert.Contains(workflowText, "bash scripts/generate-releases-manifest.sh");
        StringAssert.Contains(workflowText, "Chummer.Application/**");
        StringAssert.Contains(workflowText, "Chummer.Core/**");
        StringAssert.Contains(workflowText, "Chummer.Desktop.Runtime/**");
        StringAssert.Contains(workflowText, "Chummer.Infrastructure/**");
        StringAssert.Contains(workflowText, "Chummer.Portal/**");
        StringAssert.Contains(workflowText, "scripts/generate-releases-manifest.sh");
        StringAssert.Contains(workflowText, "scripts/publish-download-bundle.sh");
        StringAssert.Contains(workflowText, "scripts/publish-download-bundle-s3.sh");
        StringAssert.Contains(workflowText, "deploy_portal_downloads");
        StringAssert.Contains(workflowText, "deploy-downloads");
        StringAssert.Contains(workflowText, "deploy-downloads-object-storage");
        StringAssert.Contains(workflowText, "CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL");
        StringAssert.Contains(workflowText, "CHUMMER_PORTAL_DOWNLOADS_DEPLOY_ENABLED");
        StringAssert.Contains(workflowText, "CHUMMER_PORTAL_DOWNLOADS_REQUIRE_PUBLISHED_VERSION");
        StringAssert.Contains(workflowText, "CHUMMER_PORTAL_DOWNLOADS_S3_URI");
        StringAssert.Contains(workflowText, "CHUMMER_PORTAL_DOWNLOADS_AWS_ACCESS_KEY_ID");
        StringAssert.Contains(workflowText, "Validate live verify URL");
        StringAssert.Contains(workflowText, "Set CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL to verify the live portal manifest after deployment.");
        StringAssert.Contains(workflowText, "Verify deployed manifest has artifacts");
        StringAssert.Contains(workflowText, "bash scripts/verify-releases-manifest.sh \"$CHUMMER_PORTAL_DOWNLOADS_DEPLOY_DIR\"");
        StringAssert.Contains(workflowText, "Verify deployed portal manifest has artifacts");
        Assert.IsFalse(
            workflowText.Contains("if: ${{ vars.CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL != '' }}", StringComparison.Ordinal),
            "Live portal manifest verification should be mandatory when deployment is enabled.");
        StringAssert.Contains(workflowText, "scripts/verify-releases-manifest.sh");

        StringAssert.Contains(manifestScriptText, "chummer-(?P<app>avalonia|blazor-desktop)-(?P<rid>[^.]+)\\.(?P<ext>zip|tar\\.gz)");
        StringAssert.Contains(manifestScriptText, "\"osx-x64\": \"macOS x64\"");
        StringAssert.Contains(manifestScriptText, "\"id\": f\"{app}-{rid}\"");
        StringAssert.Contains(manifestScriptText, "\"url\": f\"/downloads/files/{artifact.name}\"");
        StringAssert.Contains(verifyScriptText, "CHUMMER_PORTAL_DOWNLOADS_REQUIRE_PUBLISHED_VERSION");
        StringAssert.Contains(verifyScriptText, "version.lower() == \"unpublished\"");
    }

    [TestMethod]
    public void Readme_modern_stack_summary_tracks_current_gateway_and_runtime_contract()
    {
        string readmePath = FindPath("README.md");
        string readmeText = File.ReadAllText(readmePath);

        StringAssert.Contains(readmeText, "Modern migration path (Docker branch)");
        StringAssert.Contains(readmeText, "multi-head UI stack (`Chummer.Blazor`, `Chummer.Avalonia`, `Chummer.Blazor.Desktop`, `Chummer.Avalonia.Browser`, `Chummer.Portal`)");
        StringAssert.Contains(readmeText, "chummer-blazor-portal");
        StringAssert.Contains(readmeText, "chummer-avalonia-browser");
        StringAssert.Contains(readmeText, "chummer-portal");
        StringAssert.Contains(readmeText, "/api/*`, `/openapi/*`, and `/docs/*` share the same upstream contract through `CHUMMER_PORTAL_API_URL`.");
        StringAssert.Contains(readmeText, "CHUMMER_CLIENT_MODE");
        StringAssert.Contains(readmeText, "CHUMMER_DESKTOP_CLIENT_MODE");
        StringAssert.Contains(readmeText, "CHUMMER_PORTAL_DOWNLOADS_DEPLOY_DIR");
        StringAssert.Contains(readmeText, "CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL");
        StringAssert.Contains(readmeText, "CHUMMER_PORTAL_DOWNLOADS_S3_URI");
        StringAssert.Contains(readmeText, "CHUMMER_PORTAL_DOWNLOADS_AWS_ACCESS_KEY_ID");
        StringAssert.Contains(readmeText, "CHUMMER_PORTAL_DOWNLOADS_DEPLOY_ENABLED");
        StringAssert.Contains(readmeText, "CHUMMER_PORTAL_DOWNLOADS_REQUIRE_PUBLISHED_VERSION");
        StringAssert.Contains(readmeText, "scripts/publish-download-bundle-s3.sh");
        StringAssert.Contains(readmeText, "Live deployment verification is required");
        Assert.IsFalse(
            readmeText.Contains("two UI heads (`Chummer.Blazor`, `Chummer.Avalonia`)", StringComparison.Ordinal),
            "README summary regressed to outdated two-head architecture language.");
    }

    [TestMethod]
    public void Shell_and_overview_share_bootstrap_provider_for_startup_contract_data()
    {
        string providerContractPath = FindPath("Chummer.Presentation", "Shell", "IShellBootstrapDataProvider.cs");
        string providerContractText = File.ReadAllText(providerContractPath);
        string providerImplementationPath = FindPath("Chummer.Presentation", "Shell", "ShellBootstrapDataProvider.cs");
        string providerImplementationText = File.ReadAllText(providerImplementationPath);
        string shellPresenterPath = FindPath("Chummer.Presentation", "Shell", "ShellPresenter.cs");
        string shellPresenterText = File.ReadAllText(shellPresenterPath);
        string overviewPresenterPath = FindPath("Chummer.Presentation", "Overview", "CharacterOverviewPresenter.cs");
        string overviewPresenterText = File.ReadAllText(overviewPresenterPath);
        string blazorProgramPath = FindPath("Chummer.Blazor", "Program.cs");
        string blazorProgramText = File.ReadAllText(blazorProgramPath);
        string desktopProgramPath = FindPath("Chummer.Blazor.Desktop", "Program.cs");
        string desktopProgramText = File.ReadAllText(desktopProgramPath);
        string avaloniaAppPath = FindPath("Chummer.Avalonia", "App.axaml.cs");
        string avaloniaAppText = File.ReadAllText(avaloniaAppPath);

        StringAssert.Contains(providerContractText, "public interface IShellBootstrapDataProvider");
        StringAssert.Contains(providerContractText, "GetWorkspacesAsync");
        StringAssert.Contains(providerContractText, "ShellBootstrapData");
        StringAssert.Contains(providerImplementationText, "public sealed class ShellBootstrapDataProvider");
        StringAssert.Contains(providerImplementationText, "BootstrapCacheWindow");
        StringAssert.Contains(providerImplementationText, "GetWorkspacesAsync");
        StringAssert.Contains(providerImplementationText, "_client.GetShellBootstrapAsync");
        StringAssert.Contains(providerImplementationText, "_client.ListWorkspacesAsync");

        StringAssert.Contains(shellPresenterText, "_bootstrapDataProvider.GetAsync");
        StringAssert.Contains(overviewPresenterText, "_bootstrapDataProvider.GetAsync");
        Assert.IsFalse(shellPresenterText.Contains("_client.GetCommandsAsync", StringComparison.Ordinal));
        Assert.IsFalse(shellPresenterText.Contains("_runtimeClient.GetCommandsAsync", StringComparison.Ordinal));
        Assert.IsFalse(shellPresenterText.Contains("_runtimeClient.GetNavigationTabsAsync", StringComparison.Ordinal));
        Assert.IsFalse(overviewPresenterText.Contains("_client.GetCommandsAsync", StringComparison.Ordinal));

        StringAssert.Contains(blazorProgramText, "AddScoped<IShellBootstrapDataProvider, ShellBootstrapDataProvider>();");
        StringAssert.Contains(desktopProgramText, "AddSingleton<IShellBootstrapDataProvider, ShellBootstrapDataProvider>();");
        StringAssert.Contains(avaloniaAppText, "AddSingleton<IShellBootstrapDataProvider, ShellBootstrapDataProvider>();");
    }

    [TestMethod]
    public void Shell_preferences_and_session_are_persisted_through_separate_contracts()
    {
        string shellContractsPath = FindPath("Chummer.Contracts", "Presentation", "ShellBootstrapContracts.cs");
        string shellContractsText = File.ReadAllText(shellContractsPath);
        string clientContractPath = FindPath("Chummer.Presentation", "IChummerClient.cs");
        string clientContractText = File.ReadAllText(clientContractPath);
        string shellEndpointsPath = FindPath("Chummer.Api", "Endpoints", "ShellEndpoints.cs");
        string shellEndpointsText = File.ReadAllText(shellEndpointsPath);
        string shellPresenterPath = FindPath("Chummer.Presentation", "Shell", "ShellPresenter.cs");
        string shellPresenterText = File.ReadAllText(shellPresenterPath);
        string infrastructureDiPath = FindPath("Chummer.Infrastructure", "DependencyInjection", "ServiceCollectionExtensions.cs");
        string infrastructureDiText = File.ReadAllText(infrastructureDiPath);

        StringAssert.Contains(shellContractsText, "public sealed record ShellPreferences");
        StringAssert.Contains(shellContractsText, "public sealed record ShellSessionState");
        StringAssert.Contains(shellContractsText, "string? ActiveTabId");
        StringAssert.Contains(shellContractsText, "IReadOnlyDictionary<string, string>? ActiveTabsByWorkspace");
        Assert.IsFalse(shellContractsText.Contains("ShellUserPreferences", StringComparison.Ordinal));

        StringAssert.Contains(clientContractText, "GetShellPreferencesAsync");
        StringAssert.Contains(clientContractText, "SaveShellPreferencesAsync");
        StringAssert.Contains(clientContractText, "GetShellSessionAsync");
        StringAssert.Contains(clientContractText, "SaveShellSessionAsync");
        StringAssert.Contains(clientContractText, "Task<ShellBootstrapSnapshot> GetShellBootstrapAsync(string? rulesetId, CancellationToken ct);");
        Assert.IsFalse(
            clientContractText.Contains("GetShellBootstrapAsync(string? rulesetId, CancellationToken ct)\n    {", StringComparison.Ordinal),
            "IChummerClient should not include default interface method bodies for shell bootstrap composition.");
        Assert.IsFalse(
            clientContractText.Contains("GetShellBootstrapAsync(string? rulesetId, CancellationToken ct) =>", StringComparison.Ordinal),
            "IChummerClient should not include expression-bodied default interface methods for shell bootstrap composition.");

        StringAssert.Contains(shellEndpointsText, "/api/shell/preferences");
        StringAssert.Contains(shellEndpointsText, "/api/shell/session");
        StringAssert.Contains(shellEndpointsText, "IShellSessionService shellSessionService");
        StringAssert.Contains(shellEndpointsText, "shellSessionService.Load()");
        StringAssert.Contains(shellEndpointsText, "ActiveTabId: session.ActiveTabId");
        StringAssert.Contains(shellEndpointsText, "ActiveTabsByWorkspace: session.ActiveTabsByWorkspace");

        StringAssert.Contains(shellPresenterText, "SaveShellPreferencesAsync");
        StringAssert.Contains(shellPresenterText, "SaveShellSessionAsync");
        StringAssert.Contains(shellPresenterText, "ActiveTabId = resolvedActiveTabId");
        StringAssert.Contains(shellPresenterText, "_activeTabsByWorkspace");
        StringAssert.Contains(shellPresenterText, "BuildUpdatedWorkspaceTabMap");
        Assert.IsFalse(shellPresenterText.Contains("new ShellUserPreferences", StringComparison.Ordinal));

        StringAssert.Contains(infrastructureDiText, "AddSingleton<IShellPreferencesStore, SettingsShellPreferencesStore>();");
        StringAssert.Contains(infrastructureDiText, "AddSingleton<IShellSessionStore, SettingsShellSessionStore>();");
        StringAssert.Contains(infrastructureDiText, "AddSingleton<IShellPreferencesService, ShellPreferencesService>();");
        StringAssert.Contains(infrastructureDiText, "AddSingleton<IShellSessionService, ShellSessionService>();");
    }

    [TestMethod]
    public void Dual_head_shell_actions_and_controls_are_scoped_by_active_ruleset()
    {
        string blazorShellCodePath = FindPath("Chummer.Blazor", "Components", "Layout", "DesktopShell.razor.cs");
        string blazorShellCodeText = File.ReadAllText(blazorShellCodePath);
        string avaloniaStatePath = FindPath("Chummer.Avalonia", "MainWindow.StateRefresh.cs");
        string avaloniaStateText = File.ReadAllText(avaloniaStatePath);
        string avaloniaProjectorPath = FindPath("Chummer.Avalonia", "MainWindow.ShellFrameProjector.cs");
        string avaloniaProjectorText = File.ReadAllText(avaloniaProjectorPath);
        string dualHeadAcceptancePath = FindPath("Chummer.Tests", "Presentation", "DualHeadAcceptanceTests.cs");
        string dualHeadAcceptanceText = File.ReadAllText(dualHeadAcceptancePath);

        StringAssert.Contains(blazorShellCodeText, "public IShellSurfaceResolver ShellSurfaceResolver { get; set; } = default!;");
        StringAssert.Contains(blazorShellCodeText, "RefreshShellSurfaceState()");
        StringAssert.Contains(blazorShellCodeText, "ShellSurfaceResolver.Resolve(State, ShellState)");
        StringAssert.Contains(blazorShellCodeText, "_shellSurfaceState.Commands");
        StringAssert.Contains(blazorShellCodeText, "_shellSurfaceState.MenuRoots");
        StringAssert.Contains(blazorShellCodeText, "_shellSurfaceState.NavigationTabs");

        StringAssert.Contains(avaloniaStateText, "_shellSurfaceResolver.Resolve(state, _shellPresenter.State)");
        StringAssert.Contains(avaloniaStateText, "MainWindowShellFrameProjector.Project(");
        StringAssert.Contains(avaloniaStateText, "ApplyShellFrame(shellFrame);");
        Assert.IsFalse(avaloniaStateText.Contains("shellSurface.Commands", StringComparison.Ordinal));
        StringAssert.Contains(avaloniaProjectorText, "shellSurface.Commands");
        StringAssert.Contains(avaloniaProjectorText, "shellSurface.MenuRoots");
        StringAssert.Contains(avaloniaProjectorText, "shellSurface.NavigationTabs");
        StringAssert.Contains(avaloniaProjectorText, "shellSurface.WorkspaceActions");
        StringAssert.Contains(avaloniaProjectorText, "shellSurface.DesktopUiControls");

        StringAssert.Contains(dualHeadAcceptanceText, "RulesetShellCatalogResolver.ResolveWorkspaceActionsForTab(");
        StringAssert.Contains(dualHeadAcceptanceText, "RulesetShellCatalogResolver.ResolveDesktopUiControlsForTab(");
        Assert.IsFalse(dualHeadAcceptanceText.Contains("WorkspaceSurfaceActionCatalog.ForTab(avaloniaState.ActiveTabId", StringComparison.Ordinal));
        Assert.IsFalse(dualHeadAcceptanceText.Contains("DesktopUiControlCatalog.ForTab(avaloniaState.ActiveTabId", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Ruleset_shell_catalog_resolver_service_is_registered_and_consumed_without_raw_plugin_injection()
    {
        string rulesetServicesPath = FindPath("Chummer.Contracts", "Rulesets", "RulesetShellServices.cs");
        string rulesetServicesText = File.ReadAllText(rulesetServicesPath);
        string rulesetDiExtensionsPath = FindPath("Chummer.Rulesets.Sr5", "ServiceCollectionRulesetExtensions.cs");
        string rulesetDiExtensionsText = File.ReadAllText(rulesetDiExtensionsPath);
        string rulesetHostingDiExtensionsPath = FindPath("Chummer.Rulesets.Hosting", "ServiceCollectionRulesetHostingExtensions.cs");
        string rulesetHostingDiExtensionsText = File.ReadAllText(rulesetHostingDiExtensionsPath);
        string sr5RulesetPluginPath = FindPath("Chummer.Rulesets.Sr5", "Sr5RulesetPlugin.cs");
        string sr5RulesetPluginText = File.ReadAllText(sr5RulesetPluginPath);
        string infrastructureDiPath = FindPath("Chummer.Infrastructure", "DependencyInjection", "ServiceCollectionExtensions.cs");
        string infrastructureDiText = File.ReadAllText(infrastructureDiPath);
        string desktopRuntimeDiPath = FindPath("Chummer.Desktop.Runtime", "ServiceCollectionDesktopRuntimeExtensions.cs");
        string desktopRuntimeDiText = File.ReadAllText(desktopRuntimeDiPath);
        string blazorProgramPath = FindPath("Chummer.Blazor", "Program.cs");
        string blazorProgramText = File.ReadAllText(blazorProgramPath);
        string commandEndpointsPath = FindPath("Chummer.Api", "Endpoints", "CommandEndpoints.cs");
        string commandEndpointsText = File.ReadAllText(commandEndpointsPath);
        string navigationEndpointsPath = FindPath("Chummer.Api", "Endpoints", "NavigationEndpoints.cs");
        string navigationEndpointsText = File.ReadAllText(navigationEndpointsPath);
        string blazorShellPath = FindPath("Chummer.Blazor", "Components", "Layout", "DesktopShell.razor.cs");
        string blazorShellText = File.ReadAllText(blazorShellPath);
        string avaloniaMainWindowPath = FindPath("Chummer.Avalonia", "MainWindow.axaml.cs");
        string avaloniaMainWindowText = File.ReadAllText(avaloniaMainWindowPath);
        string shellStatePath = FindPath("Chummer.Presentation", "Shell", "ShellState.cs");
        string shellStateText = File.ReadAllText(shellStatePath);
        string shellPresenterPath = FindPath("Chummer.Presentation", "Shell", "ShellPresenter.cs");
        string shellPresenterText = File.ReadAllText(shellPresenterPath);
        string shellPresenterContractPath = FindPath("Chummer.Presentation", "Shell", "IShellPresenter.cs");
        string shellPresenterContractText = File.ReadAllText(shellPresenterContractPath);

        StringAssert.Contains(rulesetServicesText, "public interface IRulesetPluginRegistry");
        StringAssert.Contains(rulesetServicesText, "public interface IRulesetShellCatalogResolver");
        StringAssert.Contains(rulesetServicesText, "public sealed class RulesetShellCatalogResolverService");
        Assert.IsFalse(PathExistsInCandidateRoots("Chummer.Contracts", "Rulesets", "Sr5RulesetPlugin.cs"));

        StringAssert.Contains(rulesetDiExtensionsText, "AddSr5Ruleset(this IServiceCollection services)");
        StringAssert.Contains(rulesetDiExtensionsText, "Chummer.Rulesets.Sr5.Sr5RulesetPlugin");
        StringAssert.Contains(sr5RulesetPluginText, "public class Sr5RulesetPlugin");
        StringAssert.Contains(rulesetHostingDiExtensionsText, "AddRulesetInfrastructure(this IServiceCollection services)");
        StringAssert.Contains(rulesetHostingDiExtensionsText, "TryAddSingleton<IRulesetPluginRegistry, RulesetPluginRegistry>();");
        StringAssert.Contains(rulesetHostingDiExtensionsText, "TryAddSingleton<IRulesetShellCatalogResolver, RulesetShellCatalogResolverService>();");
        StringAssert.Contains(infrastructureDiText, "services.AddRulesetInfrastructure();");
        StringAssert.Contains(infrastructureDiText, "services.AddSr5Ruleset();");
        StringAssert.Contains(desktopRuntimeDiText, "services.AddRulesetInfrastructure();");
        StringAssert.Contains(desktopRuntimeDiText, "services.AddSr5Ruleset();");
        StringAssert.Contains(blazorProgramText, "builder.Services.AddRulesetInfrastructure();");
        StringAssert.Contains(blazorProgramText, "builder.Services.AddSr5Ruleset();");
        StringAssert.Contains(blazorProgramText, "AddSingleton<IShellSurfaceResolver, ShellSurfaceResolver>();");

        StringAssert.Contains(commandEndpointsText, "IRulesetShellCatalogResolver shellCatalogResolver");
        StringAssert.Contains(commandEndpointsText, "shellCatalogResolver.ResolveCommands(ruleset)");
        StringAssert.Contains(navigationEndpointsText, "IRulesetShellCatalogResolver shellCatalogResolver");
        StringAssert.Contains(navigationEndpointsText, "shellCatalogResolver.ResolveNavigationTabs(ruleset)");

        StringAssert.Contains(blazorShellText, "public IShellSurfaceResolver ShellSurfaceResolver { get; set; } = default!;");
        Assert.IsFalse(blazorShellText.Contains("IEnumerable<IRulesetPlugin> RulesetPlugins", StringComparison.Ordinal));
        StringAssert.Contains(avaloniaMainWindowText, "private readonly IShellSurfaceResolver _shellSurfaceResolver;");
        StringAssert.Contains(shellStateText, "string PreferredRulesetId");
        StringAssert.Contains(shellPresenterContractText, "Task SetPreferredRulesetAsync(string rulesetId, CancellationToken ct);");
        StringAssert.Contains(shellPresenterText, "State.PreferredRulesetId");
    }

    [TestMethod]
    public void Ruleset_seam_contracts_are_declared_without_changing_default_sr5_catalog_behavior()
    {
        string rulesetContractsPath = FindPath("Chummer.Contracts", "Rulesets", "RulesetContracts.cs");
        string rulesetContractsText = File.ReadAllText(rulesetContractsPath);
        string workspaceModelsPath = FindPath("Chummer.Contracts", "Workspaces", "CharacterWorkspaceModels.cs");
        string workspaceModelsText = File.ReadAllText(workspaceModelsPath);
        string workspaceApiModelsPath = FindPath("Chummer.Contracts", "Workspaces", "WorkspaceApiModels.cs");
        string workspaceApiModelsText = File.ReadAllText(workspaceApiModelsPath);
        string commandDefinitionPath = FindPath("Chummer.Contracts", "Presentation", "AppCommandDefinition.cs");
        string commandDefinitionText = File.ReadAllText(commandDefinitionPath);
        string tabDefinitionPath = FindPath("Chummer.Contracts", "Presentation", "NavigationTabDefinition.cs");
        string tabDefinitionText = File.ReadAllText(tabDefinitionPath);
        string actionDefinitionPath = FindPath("Chummer.Contracts", "Presentation", "WorkspaceSurfaceActionDefinition.cs");
        string actionDefinitionText = File.ReadAllText(actionDefinitionPath);
        string controlDefinitionPath = FindPath("Chummer.Contracts", "Presentation", "DesktopUiControlDefinition.cs");
        string controlDefinitionText = File.ReadAllText(controlDefinitionPath);
        string commandCatalogPath = FindPath("Chummer.Contracts", "Presentation", "AppCommandCatalog.cs");
        string commandCatalogText = File.ReadAllText(commandCatalogPath);
        string tabCatalogPath = FindPath("Chummer.Contracts", "Presentation", "NavigationTabCatalog.cs");
        string tabCatalogText = File.ReadAllText(tabCatalogPath);
        string actionCatalogPath = FindPath("Chummer.Contracts", "Presentation", "WorkspaceSurfaceActionCatalog.cs");
        string actionCatalogText = File.ReadAllText(actionCatalogPath);
        string controlCatalogPath = FindPath("Chummer.Contracts", "Presentation", "DesktopUiControlCatalog.cs");
        string controlCatalogText = File.ReadAllText(controlCatalogPath);
        string fileWorkspaceStorePath = FindPath("Chummer.Infrastructure", "Workspaces", "FileWorkspaceStore.cs");
        string fileWorkspaceStoreText = File.ReadAllText(fileWorkspaceStorePath);

        StringAssert.Contains(rulesetContractsText, "public static class RulesetDefaults");
        StringAssert.Contains(rulesetContractsText, "public readonly record struct RulesetId");
        StringAssert.Contains(rulesetContractsText, "public sealed record WorkspacePayloadEnvelope");
        StringAssert.Contains(rulesetContractsText, "public interface IRulesetPlugin");
        StringAssert.Contains(rulesetContractsText, "public interface IRulesetSerializer");
        StringAssert.Contains(rulesetContractsText, "public interface IRulesetShellDefinitionProvider");
        StringAssert.Contains(rulesetContractsText, "public interface IRulesetCatalogProvider");
        StringAssert.Contains(rulesetContractsText, "public interface IRulesetRuleHost");
        StringAssert.Contains(rulesetContractsText, "public interface IRulesetScriptHost");

        string rulesetServicesPath = FindPath("Chummer.Contracts", "Rulesets", "RulesetShellServices.cs");
        string rulesetServicesText = File.ReadAllText(rulesetServicesPath);
        StringAssert.Contains(rulesetServicesText, "public interface IRulesetPluginRegistry");
        StringAssert.Contains(rulesetServicesText, "public interface IRulesetShellCatalogResolver");

        StringAssert.Contains(workspaceModelsText, "string RulesetId = RulesetDefaults.Sr5");
        StringAssert.Contains(workspaceApiModelsText, "string RulesetId = RulesetDefaults.Sr5");
        StringAssert.Contains(commandDefinitionText, "string RulesetId = RulesetDefaults.Sr5");
        StringAssert.Contains(tabDefinitionText, "string RulesetId = RulesetDefaults.Sr5");
        StringAssert.Contains(actionDefinitionText, "string RulesetId = RulesetDefaults.Sr5");
        StringAssert.Contains(controlDefinitionText, "string RulesetId = RulesetDefaults.Sr5");

        StringAssert.Contains(commandCatalogText, "ForRuleset(string? rulesetId)");
        StringAssert.Contains(tabCatalogText, "ForRuleset(string? rulesetId)");
        StringAssert.Contains(actionCatalogText, "ForRuleset(string? rulesetId)");
        StringAssert.Contains(actionCatalogText, "ForTab(string? tabId, string? rulesetId)");
        StringAssert.Contains(controlCatalogText, "ForRuleset(string? rulesetId)");
        StringAssert.Contains(controlCatalogText, "ForTab(string? tabId, string? rulesetId)");
        StringAssert.Contains(fileWorkspaceStoreText, "WorkspacePayloadEnvelope");
        StringAssert.Contains(fileWorkspaceStoreText, "PayloadKind");
        StringAssert.Contains(fileWorkspaceStoreText, "Envelope");
    }

    [TestMethod]
    public void Workspace_service_routes_behavior_through_ruleset_codec_seam()
    {
        string workspaceServicePath = FindPath("Chummer.Infrastructure", "Workspaces", "WorkspaceService.cs");
        string workspaceServiceText = File.ReadAllText(workspaceServicePath);
        string codecContractPath = FindPath("Chummer.Application", "Workspaces", "IRulesetWorkspaceCodec.cs");
        string codecContractText = File.ReadAllText(codecContractPath);
        string codecResolverContractPath = FindPath("Chummer.Application", "Workspaces", "IRulesetWorkspaceCodecResolver.cs");
        string codecResolverContractText = File.ReadAllText(codecResolverContractPath);
        string sr5CodecPath = FindPath("Chummer.Infrastructure", "Workspaces", "Sr5WorkspaceCodec.cs");
        string sr5CodecText = File.ReadAllText(sr5CodecPath);
        string infrastructureDiPath = FindPath("Chummer.Infrastructure", "DependencyInjection", "ServiceCollectionExtensions.cs");
        string infrastructureDiText = File.ReadAllText(infrastructureDiPath);

        StringAssert.Contains(codecContractText, "public interface IRulesetWorkspaceCodec");
        StringAssert.Contains(codecContractText, "WrapImport");
        StringAssert.Contains(codecContractText, "ParseSummary");
        StringAssert.Contains(codecContractText, "ParseSection");
        StringAssert.Contains(codecContractText, "Validate");
        StringAssert.Contains(codecContractText, "UpdateMetadata");
        StringAssert.Contains(codecResolverContractText, "public interface IRulesetWorkspaceCodecResolver");

        StringAssert.Contains(workspaceServiceText, "IRulesetWorkspaceCodecResolver _workspaceCodecResolver");
        StringAssert.Contains(workspaceServiceText, "_workspaceCodecResolver.Resolve");
        Assert.IsFalse(workspaceServiceText.Contains("_characterFileQueries.ParseSummary", StringComparison.Ordinal));
        Assert.IsFalse(workspaceServiceText.Contains("_characterSectionQueries.ParseSection", StringComparison.Ordinal));
        Assert.IsFalse(workspaceServiceText.Contains("_characterMetadataCommands.UpdateMetadata", StringComparison.Ordinal));

        StringAssert.Contains(sr5CodecText, "public sealed class Sr5WorkspaceCodec");
        StringAssert.Contains(sr5CodecText, "public const string Sr5PayloadKind = \"sr5/chum5-xml\"");
        StringAssert.Contains(sr5CodecText, "UpdateMetadata");

        StringAssert.Contains(infrastructureDiText, "AddSingleton<IRulesetWorkspaceCodec, Sr5WorkspaceCodec>();");
        StringAssert.Contains(infrastructureDiText, "AddSingleton<IRulesetWorkspaceCodecResolver, RulesetWorkspaceCodecResolver>();");
    }

    [TestMethod]
    public void Architecture_guardrails_treat_portal_as_ui_head()
    {
        string guardrailPath = FindPath("Chummer.Tests", "Compliance", "ArchitectureGuardrailTests.cs");
        string guardrailText = File.ReadAllText(guardrailPath);

        StringAssert.Contains(guardrailText, "private static readonly string[] UiHeadProjects");
        StringAssert.Contains(guardrailText, "\"Chummer.Portal\"");
        StringAssert.Contains(guardrailText, "Ui_head_projects_do_not_import_non_presentation_layers");
    }

    [TestMethod]
    public void Runbook_supports_download_manifest_generation_mode()
    {
        string runbookPath = FindPath("scripts", "runbook.sh");
        string runbookText = File.ReadAllText(runbookPath);
        string generatorPath = FindPath("scripts", "generate-releases-manifest.sh");
        string generatorText = File.ReadAllText(generatorPath);
        string publisherPath = FindPath("scripts", "publish-download-bundle.sh");
        string publisherText = File.ReadAllText(publisherPath);
        string s3PublisherPath = FindPath("scripts", "publish-download-bundle-s3.sh");
        string s3PublisherText = File.ReadAllText(s3PublisherPath);
        string amendValidatorPath = FindPath("scripts", "validate-amend-manifests.sh");
        string amendValidatorText = File.ReadAllText(amendValidatorPath);

        StringAssert.Contains(runbookText, "RUNBOOK_MODE\" == \"downloads-manifest\"");
        StringAssert.Contains(runbookText, "RUNBOOK_MODE\" == \"downloads-sync\"");
        StringAssert.Contains(runbookText, "RUNBOOK_MODE\" == \"downloads-sync-s3\"");
        StringAssert.Contains(runbookText, "RUNBOOK_MODE\" == \"downloads-verify\"");
        StringAssert.Contains(runbookText, "RUNBOOK_MODE\" == \"amend-checksums\"");
        StringAssert.Contains(runbookText, "bash scripts/generate-releases-manifest.sh");
        StringAssert.Contains(runbookText, "bash scripts/publish-download-bundle.sh");
        StringAssert.Contains(runbookText, "bash scripts/publish-download-bundle-s3.sh");
        StringAssert.Contains(runbookText, "bash scripts/verify-releases-manifest.sh");
        StringAssert.Contains(runbookText, "bash scripts/validate-amend-manifests.sh");
        StringAssert.Contains(runbookText, "permission denied while trying to connect to the Docker daemon socket");
        StringAssert.Contains(runbookText, "DOWNLOADS_SYNC_DEPLOY_MODE");
        StringAssert.Contains(runbookText, "CHUMMER_PORTAL_DOWNLOADS_DEPLOY_ENABLED=true");
        StringAssert.Contains(runbookText, "DOCKER_TESTS_BUILD");
        StringAssert.Contains(runbookText, "docker compose run $build_arg --rm chummer-tests");
        StringAssert.Contains(runbookText, "TEST_DISABLE_BUILD_SERVERS");
        StringAssert.Contains(runbookText, "TEST_NO_RESTORE");
        StringAssert.Contains(runbookText, "DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER");
        StringAssert.Contains(runbookText, "--disable-build-servers");
        StringAssert.Contains(runbookText, "MSBUILDDISABLENODEREUSE");
        StringAssert.Contains(runbookText, "TEST_NUGET_PREFLIGHT");
        StringAssert.Contains(runbookText, "TEST_NUGET_ENDPOINT");
        StringAssert.Contains(runbookText, "NuGet preflight failed");

        StringAssert.Contains(generatorText, "Docker/Downloads/releases.json");
        StringAssert.Contains(generatorText, "Chummer.Portal/downloads/releases.json");
        StringAssert.Contains(generatorText, "PORTAL_DOWNLOADS_DIR");
        StringAssert.Contains(generatorText, "synced ${#portal_artifacts[@]} local portal artifact(s)");
        StringAssert.Contains(generatorText, "/downloads/files/");

        StringAssert.Contains(publisherText, "Expected desktop-download-bundle layout");
        StringAssert.Contains(publisherText, "generate-releases-manifest.sh");
        StringAssert.Contains(publisherText, "verify-releases-manifest.sh");
        StringAssert.Contains(publisherText, "PORTAL_DOWNLOADS_DIR");
        StringAssert.Contains(publisherText, "CHUMMER_PORTAL_DOWNLOADS_DEPLOY_ENABLED");
        StringAssert.Contains(publisherText, "Deployment mode requires CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL");
        StringAssert.Contains(publisherText, "Published ${#artifacts[@]} desktop artifact(s)");
        StringAssert.Contains(s3PublisherText, "CHUMMER_PORTAL_DOWNLOADS_S3_URI");
        StringAssert.Contains(s3PublisherText, "CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL");
        StringAssert.Contains(s3PublisherText, "aws s3 cp");
        StringAssert.Contains(s3PublisherText, "verify-releases-manifest.sh");
        StringAssert.Contains(s3PublisherText, "Published ${artifact_count} desktop artifact(s) to object storage target");

        string verifierPath = FindPath("scripts", "verify-releases-manifest.sh");
        string verifierText = File.ReadAllText(verifierPath);
        StringAssert.Contains(verifierText, "CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL");
        StringAssert.Contains(verifierText, "/downloads/releases.json");
        StringAssert.Contains(verifierText, "has no downloads");

        StringAssert.Contains(amendValidatorText, "checksums map is required");
        StringAssert.Contains(amendValidatorText, "missing checksum entry");
        StringAssert.Contains(amendValidatorText, "data");
        StringAssert.Contains(amendValidatorText, "lang");
    }

    [TestMethod]
    public void Amend_manifest_checksum_policy_is_enforced_in_ci()
    {
        string desktopWorkflowPath = FindPath(".github", "workflows", "desktop-downloads-matrix.yml");
        string desktopWorkflowText = File.ReadAllText(desktopWorkflowPath);
        string guardrailsWorkflowPath = FindPath(".github", "workflows", "docker-architecture-guardrails.yml");
        string guardrailsWorkflowText = File.ReadAllText(guardrailsWorkflowPath);
        string manifestPath = FindPath("Docker", "Amends", "manifest.json");
        string manifestText = File.ReadAllText(manifestPath);

        StringAssert.Contains(desktopWorkflowText, "scripts/validate-amend-manifests.sh");
        StringAssert.Contains(desktopWorkflowText, "Validate amend manifests checksums");
        StringAssert.Contains(guardrailsWorkflowText, "amend-manifest-checksums");
        StringAssert.Contains(guardrailsWorkflowText, "bash scripts/validate-amend-manifests.sh");

        StringAssert.Contains(manifestText, "\"checksums\"");
        StringAssert.Contains(manifestText, "\"data/qualities.test-amend.xml\"");
        StringAssert.Contains(manifestText, "\"lang/en-us.test-amend.xml\"");
    }

    [TestMethod]
    public void Dockerfile_tests_includes_blazor_desktop_project_for_container_build_checks()
    {
        string dockerfilePath = FindPath("Docker", "Dockerfile.tests");
        string dockerfileText = File.ReadAllText(dockerfilePath);

        StringAssert.Contains(dockerfileText, "COPY Chummer.Desktop.Runtime/Chummer.Desktop.Runtime.csproj Chummer.Desktop.Runtime/");
        StringAssert.Contains(dockerfileText, "COPY Chummer.Desktop.Runtime/ Chummer.Desktop.Runtime/");
        StringAssert.Contains(dockerfileText, "COPY Chummer.Blazor.Desktop/Chummer.Blazor.Desktop.csproj Chummer.Blazor.Desktop/");
        StringAssert.Contains(dockerfileText, "COPY Chummer.Blazor.Desktop/ Chummer.Blazor.Desktop/");
        StringAssert.Contains(dockerfileText, "COPY README.md ./");
        StringAssert.Contains(dockerfileText, "COPY Docker/Amends/ Docker/Amends/");
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
        StringAssert.Contains(uiE2eText, "docker compose --profile test run --build --rm -T chummer-playwright");
    }

    [TestMethod]
    public void Avalonia_mainwindow_uses_named_controls_over_findcontrol_orchestration()
    {
        string xamlPath = FindPath("Chummer.Avalonia", "MainWindow.axaml");
        string xamlText = File.ReadAllText(xamlPath);
        string menuControlPath = FindPath("Chummer.Avalonia", "Controls", "ShellMenuBarControl.axaml");
        string menuControlText = File.ReadAllText(menuControlPath);
        string codePath = FindPath("Chummer.Avalonia", "MainWindow.axaml.cs");
        string codeText = File.ReadAllText(codePath);
        string statePath = FindPath("Chummer.Avalonia", "MainWindow.StateRefresh.cs");
        string stateText = File.ReadAllText(statePath);
        string projectorPath = FindPath("Chummer.Avalonia", "MainWindow.ShellFrameProjector.cs");
        string projectorText = File.ReadAllText(projectorPath);

        Assert.IsFalse(codeText.Contains("FindControl<", StringComparison.Ordinal));
        StringAssert.Contains(codeText, "public MainWindow(");
        StringAssert.Contains(codeText, "_toolStrip = ToolStripControl;");
        StringAssert.Contains(codeText, "_workspaceStrip = WorkspaceStripControl;");
        StringAssert.Contains(codeText, "_menuBar = ShellMenuBarControl;");
        StringAssert.Contains(codeText, "_toolStrip.ImportFileRequested +=");
        StringAssert.Contains(codeText, "_toolStrip.ImportRawRequested +=");
        StringAssert.Contains(codeText, "_toolStrip.SaveRequested +=");
        StringAssert.Contains(codeText, "_toolStrip.CloseWorkspaceRequested +=");
        StringAssert.Contains(codeText, "_menuBar.MenuSelected +=");
        StringAssert.Contains(codeText, "_navigatorPane = NavigatorPaneControl;");
        StringAssert.Contains(codeText, "_sectionHost = SectionHostControl;");
        StringAssert.Contains(codeText, "_commandDialogPane = CommandDialogPaneControl;");
        StringAssert.Contains(codeText, "_summaryHeader = SummaryHeaderControl;");
        StringAssert.Contains(codeText, "_statusStrip = StatusStripControl;");
        StringAssert.Contains(codeText, "_navigatorPane.WorkspaceSelected +=");
        StringAssert.Contains(codeText, "_commandDialogPane.CommandSelected +=");
        StringAssert.Contains(stateText, "MainWindowShellFrameProjector.Project(");
        StringAssert.Contains(stateText, "ApplyShellFrame(shellFrame);");
        StringAssert.Contains(stateText, "_menuBar.SetMenuState(");
        StringAssert.Contains(stateText, "_navigatorPane.SetOpenWorkspaces(");
        StringAssert.Contains(stateText, "_navigatorPane.SetNavigationTabs(");
        StringAssert.Contains(stateText, "_navigatorPane.SetSectionActions(");
        StringAssert.Contains(stateText, "_navigatorPane.SetUiControls(");
        StringAssert.Contains(stateText, "_commandDialogPane.SetCommands(");
        StringAssert.Contains(stateText, "_commandDialogPane.SetDialog(");
        StringAssert.Contains(stateText, "_sectionHost.SetNotice(");
        StringAssert.Contains(stateText, "_sectionHost.SetSectionPreview(");
        StringAssert.Contains(stateText, "_toolStrip.SetStatusText(");
        StringAssert.Contains(stateText, "_workspaceStrip.SetWorkspaceText(");
        StringAssert.Contains(stateText, "_summaryHeader.SetValues(");
        StringAssert.Contains(stateText, "_statusStrip.SetValues(");
        StringAssert.Contains(projectorText, "BuildWorkspaceActionLookup");
        StringAssert.Contains(projectorText, "WorkspaceActionsById");
        StringAssert.Contains(projectorText, "shellSurface.Commands");
        StringAssert.Contains(projectorText, "shellSurface.NavigationTabs");

        StringAssert.Contains(xamlText, "x:Name=\"ToolStripControl\"");
        StringAssert.Contains(xamlText, "x:Name=\"WorkspaceStripControl\"");
        StringAssert.Contains(xamlText, "x:Name=\"ShellMenuBarControl\"");
        StringAssert.Contains(xamlText, "x:Name=\"NavigatorPaneControl\"");
        StringAssert.Contains(xamlText, "x:Name=\"SectionHostControl\"");
        StringAssert.Contains(xamlText, "x:Name=\"CommandDialogPaneControl\"");
        StringAssert.Contains(xamlText, "x:Name=\"SummaryHeaderControl\"");
        StringAssert.Contains(xamlText, "x:Name=\"StatusStripControl\"");
        StringAssert.Contains(xamlText, "<controls:ToolStripControl");
        StringAssert.Contains(xamlText, "<controls:WorkspaceStripControl");
        StringAssert.Contains(xamlText, "<controls:ShellMenuBarControl");
        StringAssert.Contains(xamlText, "<controls:NavigatorPaneControl");
        StringAssert.Contains(xamlText, "<controls:SectionHostControl");
        StringAssert.Contains(xamlText, "<controls:CommandDialogPaneControl");
        StringAssert.Contains(xamlText, "<controls:SummaryHeaderControl");
        StringAssert.Contains(xamlText, "<controls:StatusStripControl");
        StringAssert.Contains(menuControlText, "Classes=\"menu-button\"");
        StringAssert.Contains(xamlText, "Button.menu-button.active-menu");
    }

    [TestMethod]
    public void Avalonia_shell_layout_contains_core_desktop_regions()
    {
        string xamlPath = FindPath("Chummer.Avalonia", "MainWindow.axaml");
        string xamlText = File.ReadAllText(xamlPath);
        string navigatorControlPath = FindPath("Chummer.Avalonia", "Controls", "NavigatorPaneControl.axaml");
        string navigatorControlText = File.ReadAllText(navigatorControlPath);
        string commandPaneControlPath = FindPath("Chummer.Avalonia", "Controls", "CommandDialogPaneControl.axaml");
        string commandPaneControlText = File.ReadAllText(commandPaneControlPath);

        StringAssert.Contains(xamlText, "x:Name=\"MenuBarRegion\"");
        StringAssert.Contains(xamlText, "x:Name=\"ToolStripRegion\"");
        StringAssert.Contains(xamlText, "x:Name=\"WorkspaceStripRegion\"");
        StringAssert.Contains(xamlText, "x:Name=\"LeftNavigatorRegion\"");
        StringAssert.Contains(xamlText, "x:Name=\"SummaryHeaderRegion\"");
        StringAssert.Contains(xamlText, "x:Name=\"SectionRegion\"");
        StringAssert.Contains(xamlText, "x:Name=\"RightShellRegion\"");
        StringAssert.Contains(xamlText, "x:Name=\"StatusStripRegion\"");

        StringAssert.Contains(navigatorControlText, "Open Characters");
        StringAssert.Contains(navigatorControlText, "Navigation Tabs");
        StringAssert.Contains(navigatorControlText, "Section Actions");
        StringAssert.Contains(commandPaneControlText, "Command Palette");
    }

    [TestMethod]
    public void Dual_heads_wire_keyboard_shortcuts_for_core_commands()
    {
        string blazorShellPath = FindPath("Chummer.Blazor", "Components", "Layout", "DesktopShell.razor");
        string blazorShellText = File.ReadAllText(blazorShellPath);
        string avaloniaXamlPath = FindPath("Chummer.Avalonia", "MainWindow.axaml");
        string avaloniaXamlText = File.ReadAllText(avaloniaXamlPath);
        string avaloniaCodePath = FindPath("Chummer.Avalonia", "MainWindow.EventHandlers.cs");
        string avaloniaCodeText = File.ReadAllText(avaloniaCodePath);
        string shortcutCatalogPath = FindPath("Chummer.Presentation", "Shell", "DesktopShortcutCatalog.cs");
        string shortcutCatalogText = File.ReadAllText(shortcutCatalogPath);

        StringAssert.Contains(blazorShellText, "@onkeydown=\"OnShellKeyDown\"");
        string blazorShellCodePath = FindPath("Chummer.Blazor", "Components", "Layout", "DesktopShell.Commands.cs");
        string blazorShellCodeText = File.ReadAllText(blazorShellCodePath);
        StringAssert.Contains(blazorShellCodeText, "args.MetaKey");
        StringAssert.Contains(blazorShellCodeText, "DesktopShortcutCatalog.TryResolveCommandId");

        StringAssert.Contains(avaloniaXamlText, "KeyDown=\"Window_OnKeyDown\"");
        StringAssert.Contains(avaloniaCodeText, "Window_OnKeyDown");
        StringAssert.Contains(avaloniaCodeText, "DesktopShortcutCatalog.TryResolveCommandId");

        StringAssert.Contains(shortcutCatalogText, "\"save_character\"");
        StringAssert.Contains(shortcutCatalogText, "\"save_character_as\"");
        StringAssert.Contains(shortcutCatalogText, "\"close_window\"");
        StringAssert.Contains(shortcutCatalogText, "\"global_settings\"");
        StringAssert.Contains(shortcutCatalogText, "\"open_character\"");
        StringAssert.Contains(shortcutCatalogText, "\"new_character\"");
        StringAssert.Contains(shortcutCatalogText, "\"new_critter\"");
        StringAssert.Contains(shortcutCatalogText, "\"print_character\"");
        StringAssert.Contains(shortcutCatalogText, "\"refresh_character\"");
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

    private static bool PathExistsInCandidateRoots(params string[] parts)
    {
        foreach (string? root in CandidateRoots())
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            DirectoryInfo current = new(root);
            while (true)
            {
                string candidate = Path.Combine(new[] { current.FullName }.Concat(parts).ToArray());
                if (File.Exists(candidate))
                {
                    return true;
                }

                if (current.Parent is null)
                {
                    break;
                }

                current = current.Parent;
            }
        }

        return false;
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
