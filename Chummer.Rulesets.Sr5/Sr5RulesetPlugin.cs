using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;

namespace Chummer.Rulesets.Sr5;

public class Sr5RulesetPlugin : IRulesetPlugin
{
    public RulesetId Id { get; } = new(RulesetDefaults.Sr5);

    public string DisplayName => "Shadowrun 5";

    public IRulesetSerializer Serializer { get; } = new Sr5RulesetSerializer();

    public IRulesetShellDefinitionProvider ShellDefinitions { get; } = new Sr5RulesetShellDefinitionProvider();

    public IRulesetCatalogProvider Catalogs { get; } = new Sr5RulesetCatalogProvider();

    public IRulesetRuleHost Rules { get; } = new NoOpRulesetRuleHost();

    public IRulesetScriptHost Scripts { get; } = new NoOpRulesetScriptHost();
}

public class Sr5RulesetSerializer : IRulesetSerializer
{
    public RulesetId RulesetId { get; } = new(RulesetDefaults.Sr5);

    public int SchemaVersion => 1;

    public WorkspacePayloadEnvelope Wrap(string payloadKind, string payload)
    {
        if (string.IsNullOrWhiteSpace(payloadKind))
        {
            throw new ArgumentException("Payload kind is required.", nameof(payloadKind));
        }

        return new WorkspacePayloadEnvelope(
            RulesetId: RulesetDefaults.Sr5,
            SchemaVersion: SchemaVersion,
            PayloadKind: payloadKind.Trim(),
            Payload: payload ?? string.Empty);
    }
}

public class Sr5RulesetShellDefinitionProvider : IRulesetShellDefinitionProvider
{
    public IReadOnlyList<AppCommandDefinition> GetCommands()
    {
        return Sr5AppCommandCatalog.All;
    }

    public IReadOnlyList<NavigationTabDefinition> GetNavigationTabs()
    {
        return Sr5NavigationTabCatalog.All;
    }
}

public class Sr5RulesetCatalogProvider : IRulesetCatalogProvider
{
    public IReadOnlyList<WorkflowDefinition> GetWorkflowDefinitions()
    {
        return Sr5WorkflowCatalog.Definitions;
    }

    public IReadOnlyList<WorkflowSurfaceDefinition> GetWorkflowSurfaces()
    {
        return Sr5WorkflowCatalog.Surfaces;
    }

    public IReadOnlyList<WorkspaceSurfaceActionDefinition> GetWorkspaceActions()
    {
        return Sr5WorkspaceSurfaceActionCatalog.All;
    }

    public IReadOnlyList<DesktopUiControlDefinition> GetDesktopUiControls()
    {
        return Sr5DesktopUiControlCatalog.All;
    }
}

internal static class Sr5WorkflowCatalog
{
    public static readonly IReadOnlyList<WorkflowDefinition> Definitions =
    [
        new(WorkflowDefinitionIds.LibraryShell, "Library Shell", ["sr5.shell.menu", "sr5.shell.toolbar"], false),
        new(WorkflowDefinitionIds.CareerWorkbench, "Career Workbench", ["sr5.career.section"], true),
        new(WorkflowDefinitionIds.SelectionDialog, "Selection Dialog", ["sr5.selection.dialog"], false),
        new(WorkflowDefinitionIds.DiceTool, "Dice Tool", ["sr5.tool.dice"], false),
        new(WorkflowDefinitionIds.SessionDashboard, "Session Dashboard", ["sr5.session.summary"], true, true)
    ];

    public static readonly IReadOnlyList<WorkflowSurfaceDefinition> Surfaces =
    [
        new("sr5.shell.menu", WorkflowDefinitionIds.LibraryShell, WorkflowSurfaceKinds.ShellRegion, ShellRegionIds.MenuBar, WorkflowLayoutTokens.ShellFrame, ["file", "edit", "tools"]),
        new("sr5.shell.toolbar", WorkflowDefinitionIds.LibraryShell, WorkflowSurfaceKinds.ShellRegion, ShellRegionIds.ToolStrip, WorkflowLayoutTokens.ShellFrame, ["new_character", "open_character", "save_character"]),
        new("sr5.career.section", WorkflowDefinitionIds.CareerWorkbench, WorkflowSurfaceKinds.Workbench, ShellRegionIds.SectionPane, WorkflowLayoutTokens.CareerWorkbench, ["tab-info.summary", "tab-info.profile", "tab-skills.skills"]),
        new("sr5.selection.dialog", WorkflowDefinitionIds.SelectionDialog, WorkflowSurfaceKinds.Dialog, ShellRegionIds.DialogHost, WorkflowLayoutTokens.SelectionDialog, ["tab-gear.inventory"]),
        new("sr5.tool.dice", WorkflowDefinitionIds.DiceTool, WorkflowSurfaceKinds.Tool, ShellRegionIds.DialogHost, WorkflowLayoutTokens.ToolPanel, ["dice_roller"]),
        new("sr5.session.summary", WorkflowDefinitionIds.SessionDashboard, WorkflowSurfaceKinds.Dashboard, ShellRegionIds.SummaryHeader, WorkflowLayoutTokens.SessionDashboard, ["tab-info.summary", "tab-info.validate"])
    ];
}

public class NoOpRulesetRuleHost : IRulesetRuleHost
{
    private static readonly IReadOnlyList<string> Messages = ["Rule host not configured; no-op evaluation applied."];

    public ValueTask<RulesetRuleEvaluationResult> EvaluateAsync(RulesetRuleEvaluationRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new RulesetRuleEvaluationResult(
            Success: true,
            Outputs: request.Inputs,
            Messages: Messages));
    }
}

public class NoOpRulesetScriptHost : IRulesetScriptHost
{
    public ValueTask<RulesetScriptExecutionResult> ExecuteAsync(RulesetScriptExecutionRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Dictionary<string, object?> outputs = new(StringComparer.Ordinal)
        {
            ["scriptId"] = request.ScriptId,
            ["mode"] = "noop",
            ["inputCount"] = request.Inputs.Count
        };

        return ValueTask.FromResult(new RulesetScriptExecutionResult(
            Success: true,
            Error: null,
            Outputs: outputs));
    }
}
