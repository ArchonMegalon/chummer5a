using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;

namespace Chummer.Rulesets.Sr4;

public class Sr4RulesetPlugin : IRulesetPlugin
{
    public RulesetId Id { get; } = new(RulesetDefaults.Sr4);

    public string DisplayName => "Shadowrun 4";

    public IRulesetSerializer Serializer { get; } = new Sr4RulesetSerializer();

    public IRulesetShellDefinitionProvider ShellDefinitions { get; } = new Sr4RulesetShellDefinitionProvider();

    public IRulesetCatalogProvider Catalogs { get; } = new Sr4RulesetCatalogProvider();

    public IRulesetRuleHost Rules { get; } = new Sr4NoOpRulesetRuleHost();

    public IRulesetScriptHost Scripts { get; } = new Sr4NoOpRulesetScriptHost();
}

public class Sr4RulesetSerializer : IRulesetSerializer
{
    public RulesetId RulesetId { get; } = new(RulesetDefaults.Sr4);

    public int SchemaVersion => Sr4WorkspaceCodec.SchemaVersion;

    public WorkspacePayloadEnvelope Wrap(string payloadKind, string payload)
    {
        if (string.IsNullOrWhiteSpace(payloadKind))
        {
            throw new ArgumentException("Payload kind is required.", nameof(payloadKind));
        }

        return new WorkspacePayloadEnvelope(
            RulesetId: RulesetDefaults.Sr4,
            SchemaVersion: SchemaVersion,
            PayloadKind: payloadKind.Trim(),
            Payload: payload ?? string.Empty);
    }
}

public class Sr4RulesetShellDefinitionProvider : IRulesetShellDefinitionProvider
{
    public IReadOnlyList<AppCommandDefinition> GetCommands()
    {
        return Sr4AppCommandCatalog.All;
    }

    public IReadOnlyList<NavigationTabDefinition> GetNavigationTabs()
    {
        return Sr4NavigationTabCatalog.All;
    }
}

public class Sr4RulesetCatalogProvider : IRulesetCatalogProvider
{
    public IReadOnlyList<WorkflowDefinition> GetWorkflowDefinitions()
    {
        return Sr4WorkflowCatalog.Definitions;
    }

    public IReadOnlyList<WorkflowSurfaceDefinition> GetWorkflowSurfaces()
    {
        return Sr4WorkflowCatalog.Surfaces;
    }

    public IReadOnlyList<WorkspaceSurfaceActionDefinition> GetWorkspaceActions()
    {
        return Sr4WorkspaceSurfaceActionCatalog.All;
    }

    public IReadOnlyList<DesktopUiControlDefinition> GetDesktopUiControls()
    {
        return Sr4DesktopUiControlCatalog.All;
    }
}

internal static class Sr4WorkflowCatalog
{
    public static readonly IReadOnlyList<WorkflowDefinition> Definitions =
    [
        new(WorkflowDefinitionIds.LibraryShell, "Library Shell", ["sr4.shell.menu", "sr4.shell.toolbar"], false),
        new(WorkflowDefinitionIds.CareerWorkbench, "Career Workbench", ["sr4.career.section"], true),
        new(WorkflowDefinitionIds.SelectionDialog, "Selection Dialog", ["sr4.selection.dialog"], false),
        new(WorkflowDefinitionIds.DiceTool, "Dice Tool", ["sr4.tool.dice"], false),
        new(WorkflowDefinitionIds.SessionDashboard, "Session Dashboard", ["sr4.session.summary"], true, true)
    ];

    public static readonly IReadOnlyList<WorkflowSurfaceDefinition> Surfaces =
    [
        new("sr4.shell.menu", WorkflowDefinitionIds.LibraryShell, WorkflowSurfaceKinds.ShellRegion, ShellRegionIds.MenuBar, WorkflowLayoutTokens.ShellFrame, ["file", "edit", "tools"]),
        new("sr4.shell.toolbar", WorkflowDefinitionIds.LibraryShell, WorkflowSurfaceKinds.ShellRegion, ShellRegionIds.ToolStrip, WorkflowLayoutTokens.ShellFrame, ["new_character", "open_character", "save_character"]),
        new("sr4.career.section", WorkflowDefinitionIds.CareerWorkbench, WorkflowSurfaceKinds.Workbench, ShellRegionIds.SectionPane, WorkflowLayoutTokens.CareerWorkbench, ["tab-info.summary", "tab-info.profile", "tab-skills.skills"]),
        new("sr4.selection.dialog", WorkflowDefinitionIds.SelectionDialog, WorkflowSurfaceKinds.Dialog, ShellRegionIds.DialogHost, WorkflowLayoutTokens.SelectionDialog, ["tab-gear.inventory"]),
        new("sr4.tool.dice", WorkflowDefinitionIds.DiceTool, WorkflowSurfaceKinds.Tool, ShellRegionIds.DialogHost, WorkflowLayoutTokens.ToolPanel, ["dice_roller"]),
        new("sr4.session.summary", WorkflowDefinitionIds.SessionDashboard, WorkflowSurfaceKinds.Dashboard, ShellRegionIds.SummaryHeader, WorkflowLayoutTokens.SessionDashboard, ["tab-info.summary", "tab-info.validate"])
    ];
}

public class Sr4NoOpRulesetRuleHost : IRulesetRuleHost
{
    private const string ErrorMessage = "SR4 rules engine is not implemented; this ruleset remains experimental.";

    public ValueTask<RulesetRuleEvaluationResult> EvaluateAsync(RulesetRuleEvaluationRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new RulesetRuleEvaluationResult(
            Success: false,
            Outputs: new Dictionary<string, object?>(StringComparer.Ordinal),
            Messages:
            [
                ErrorMessage,
                $"Rule '{request.RuleId}' cannot be evaluated until SR4 rule providers are implemented."
            ]));
    }
}

public class Sr4NoOpRulesetScriptHost : IRulesetScriptHost
{
    public ValueTask<RulesetScriptExecutionResult> ExecuteAsync(RulesetScriptExecutionRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        string error = $"SR4 script host is not implemented; script '{request.ScriptId}' cannot be executed because the ruleset remains experimental.";

        return ValueTask.FromResult(new RulesetScriptExecutionResult(
            Success: false,
            Error: error,
            Outputs: new Dictionary<string, object?>(StringComparer.Ordinal)));
    }
}
