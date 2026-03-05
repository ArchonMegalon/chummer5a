using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Chummer.Blazor.Components.Layout;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Presentation;

[TestClass]
public sealed class DesktopShellRulesetCatalogTests
{
    [TestMethod]
    public void DesktopShell_uses_active_ruleset_plugin_catalogs_for_actions_and_controls()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        CharacterWorkspaceId workspaceId = new("ws-sr6");
        OpenWorkspaceState openWorkspace = new(
            Id: workspaceId,
            Name: "SR6 Runner",
            Alias: "SR6",
            LastOpenedUtc: DateTimeOffset.UtcNow,
            RulesetId: "sr6");

        CharacterOverviewState overviewState = CharacterOverviewState.Empty with
        {
            Session = new WorkspaceSessionState(workspaceId, [openWorkspace], [workspaceId]),
            OpenWorkspaces = [openWorkspace],
            WorkspaceId = workspaceId,
            ActiveTabId = "tab-info",
            IsBusy = false
        };

        AppCommandDefinition menuRoot = new("file", "menu.file", "menu", false, true, "sr6");
        NavigationTabDefinition infoTab = new("tab-info", "Info", "profile", "character", true, true, "sr6");
        ShellWorkspaceState shellWorkspace = new(
            Id: workspaceId,
            Name: openWorkspace.Name,
            Alias: openWorkspace.Alias,
            LastOpenedUtc: openWorkspace.LastOpenedUtc,
            RulesetId: "sr6");
        ShellState shellState = ShellState.Empty with
        {
            ActiveWorkspaceId = workspaceId,
            OpenWorkspaces = [shellWorkspace],
            ActiveRulesetId = "sr6",
            Commands = [menuRoot],
            MenuRoots = [menuRoot],
            NavigationTabs = [infoTab],
            ActiveTabId = infoTab.Id
        };

        context.Services.AddSingleton<ICharacterOverviewPresenter>(new StaticOverviewPresenter(overviewState));
        context.Services.AddSingleton<IShellPresenter>(new StaticShellPresenter(shellState));
        context.Services.AddSingleton<ICommandAvailabilityEvaluator, DefaultCommandAvailabilityEvaluator>();
        context.Services.AddSingleton<IRulesetPlugin, Sr5RulesetPlugin>();
        context.Services.AddSingleton<IRulesetPlugin, Sr6CatalogPlugin>();
        context.Services.AddSingleton<IRulesetPluginRegistry, RulesetPluginRegistry>();
        context.Services.AddSingleton<IRulesetShellCatalogResolver, RulesetShellCatalogResolverService>();
        context.Services.AddSingleton<IShellSurfaceResolver, ShellSurfaceResolver>();

        IRenderedComponent<DesktopShell> cut = context.Render<DesktopShell>();

        cut.WaitForAssertion(() =>
        {
            IReadOnlyList<AngleSharp.Dom.IElement> actionButtons = cut.FindAll(".section-actions .action-button");
            IReadOnlyList<AngleSharp.Dom.IElement> controlButtons = cut.FindAll(".controls .mini-btn");

            Assert.AreEqual(1, actionButtons.Count);
            Assert.AreEqual(1, controlButtons.Count);
            StringAssert.Contains(actionButtons[0].TextContent, "SR6 Matrix Action");
            Assert.AreEqual("ui-sr6-control", controlButtons[0].GetAttribute("data-ui-control"));
            StringAssert.Contains(controlButtons[0].TextContent, "SR6 Matrix Control");
        });
    }

    private sealed class StaticOverviewPresenter : ICharacterOverviewPresenter
    {
        public StaticOverviewPresenter(CharacterOverviewState state)
        {
            State = state;
        }

        public CharacterOverviewState State { get; private set; }

        public event EventHandler? StateChanged;

        public Task InitializeAsync(CancellationToken ct) => Task.CompletedTask;
        public Task ImportAsync(WorkspaceImportDocument document, CancellationToken ct) => Task.CompletedTask;
        public Task LoadAsync(CharacterWorkspaceId id, CancellationToken ct) => Task.CompletedTask;
        public Task SwitchWorkspaceAsync(CharacterWorkspaceId id, CancellationToken ct) => Task.CompletedTask;
        public Task CloseWorkspaceAsync(CharacterWorkspaceId id, CancellationToken ct) => Task.CompletedTask;
        public Task ExecuteCommandAsync(string commandId, CancellationToken ct) => Task.CompletedTask;
        public Task HandleUiControlAsync(string controlId, CancellationToken ct) => Task.CompletedTask;
        public Task ExecuteWorkspaceActionAsync(WorkspaceSurfaceActionDefinition action, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateDialogFieldAsync(string fieldId, string? value, CancellationToken ct) => Task.CompletedTask;
        public Task ExecuteDialogActionAsync(string actionId, CancellationToken ct) => Task.CompletedTask;
        public Task CloseDialogAsync(CancellationToken ct) => Task.CompletedTask;
        public Task SelectTabAsync(string tabId, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateMetadataAsync(UpdateWorkspaceMetadata command, CancellationToken ct) => Task.CompletedTask;
        public Task SaveAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class StaticShellPresenter : IShellPresenter
    {
        public StaticShellPresenter(ShellState state)
        {
            State = state;
        }

        public ShellState State { get; private set; }

        public event EventHandler? StateChanged;

        public Task InitializeAsync(CancellationToken ct) => Task.CompletedTask;
        public Task ExecuteCommandAsync(string commandId, CancellationToken ct) => Task.CompletedTask;
        public Task SelectTabAsync(string tabId, CancellationToken ct) => Task.CompletedTask;
        public Task ToggleMenuAsync(string menuId, CancellationToken ct) => Task.CompletedTask;
        public Task SetPreferredRulesetAsync(string rulesetId, CancellationToken ct) => Task.CompletedTask;

        public Task SyncWorkspaceContextAsync(CharacterWorkspaceId? activeWorkspaceId, CancellationToken ct)
        {
            State = State with { ActiveWorkspaceId = activeWorkspaceId };
            StateChanged?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }
    }

    private sealed class Sr6CatalogPlugin : IRulesetPlugin
    {
        public RulesetId Id { get; } = new("sr6");
        public string DisplayName => "SR6 test plugin";
        public IRulesetSerializer Serializer { get; } = new Sr6Serializer();
        public IRulesetShellDefinitionProvider ShellDefinitions { get; } = new Sr6ShellDefinitions();
        public IRulesetCatalogProvider Catalogs { get; } = new Sr6Catalogs();
        public IRulesetRuleHost Rules { get; } = new NoOpRulesetRuleHost();
        public IRulesetScriptHost Scripts { get; } = new NoOpRulesetScriptHost();
    }

    private sealed class Sr6Serializer : IRulesetSerializer
    {
        public RulesetId RulesetId { get; } = new("sr6");
        public int SchemaVersion => 1;

        public WorkspacePayloadEnvelope Wrap(string payloadKind, string payload)
        {
            return new WorkspacePayloadEnvelope("sr6", SchemaVersion, payloadKind, payload);
        }
    }

    private sealed class Sr6ShellDefinitions : IRulesetShellDefinitionProvider
    {
        public IReadOnlyList<AppCommandDefinition> GetCommands() =>
        [
            new AppCommandDefinition("file", "menu.file", "menu", false, true, "sr6")
        ];

        public IReadOnlyList<NavigationTabDefinition> GetNavigationTabs() =>
        [
            new NavigationTabDefinition("tab-info", "Info", "profile", "character", true, true, "sr6")
        ];
    }

    private sealed class Sr6Catalogs : IRulesetCatalogProvider
    {
        public IReadOnlyList<WorkspaceSurfaceActionDefinition> GetWorkspaceActions() =>
        [
            new WorkspaceSurfaceActionDefinition(
                Id: "sr6.action.matrix",
                Label: "SR6 Matrix Action",
                TabId: "tab-info",
                Kind: WorkspaceSurfaceActionKind.Section,
                TargetId: "profile",
                RequiresOpenCharacter: true,
                EnabledByDefault: true,
                RulesetId: "sr6")
        ];

        public IReadOnlyList<DesktopUiControlDefinition> GetDesktopUiControls() =>
        [
            new DesktopUiControlDefinition(
                Id: "ui-sr6-control",
                Label: "SR6 Matrix Control",
                TabId: "tab-info",
                RequiresOpenCharacter: true,
                EnabledByDefault: true,
                RulesetId: "sr6")
        ];
    }
}
