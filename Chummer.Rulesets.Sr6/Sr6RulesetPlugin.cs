using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;

namespace Chummer.Rulesets.Sr6;

public class Sr6RulesetPlugin : IRulesetPlugin
{
    public Sr6RulesetPlugin()
    {
        Capabilities = new Sr6NoOpRulesetCapabilityHost();
        Rules = new Sr6NoOpRulesetRuleHost(Capabilities);
        Scripts = new Sr6NoOpRulesetScriptHost(Capabilities);
    }

    public RulesetId Id { get; } = new(RulesetDefaults.Sr6);

    public string DisplayName => "Shadowrun 6";

    public IRulesetSerializer Serializer { get; } = new Sr6RulesetSerializer();

    public IRulesetShellDefinitionProvider ShellDefinitions { get; } = new Sr6RulesetShellDefinitionProvider();

    public IRulesetCatalogProvider Catalogs { get; } = new Sr6RulesetCatalogProvider();

    public IRulesetCapabilityHost Capabilities { get; }

    public IRulesetRuleHost Rules { get; }

    public IRulesetScriptHost Scripts { get; }
}

public class Sr6RulesetSerializer : IRulesetSerializer
{
    public RulesetId RulesetId { get; } = new(RulesetDefaults.Sr6);

    public int SchemaVersion => Sr6WorkspaceCodec.SchemaVersion;

    public WorkspacePayloadEnvelope Wrap(string payloadKind, string payload)
    {
        if (string.IsNullOrWhiteSpace(payloadKind))
        {
            throw new ArgumentException("Payload kind is required.", nameof(payloadKind));
        }

        return new WorkspacePayloadEnvelope(
            RulesetId: RulesetDefaults.Sr6,
            SchemaVersion: SchemaVersion,
            PayloadKind: payloadKind.Trim(),
            Payload: payload ?? string.Empty);
    }
}

public class Sr6RulesetShellDefinitionProvider : IRulesetShellDefinitionProvider
{
    public IReadOnlyList<AppCommandDefinition> GetCommands()
    {
        return Sr6AppCommandCatalog.All;
    }

    public IReadOnlyList<NavigationTabDefinition> GetNavigationTabs()
    {
        return Sr6NavigationTabCatalog.All;
    }
}

public class Sr6RulesetCatalogProvider : IRulesetCatalogProvider
{
    public IReadOnlyList<WorkflowDefinition> GetWorkflowDefinitions()
    {
        return Sr6WorkflowCatalog.Definitions;
    }

    public IReadOnlyList<WorkflowSurfaceDefinition> GetWorkflowSurfaces()
    {
        return Sr6WorkflowCatalog.Surfaces;
    }

    public IReadOnlyList<WorkspaceSurfaceActionDefinition> GetWorkspaceActions()
    {
        return Sr6WorkspaceSurfaceActionCatalog.All;
    }
}

internal static class Sr6WorkflowCatalog
{
    public static readonly IReadOnlyList<WorkflowDefinition> Definitions =
    [
        new(WorkflowDefinitionIds.LibraryShell, "Library Shell", ["sr6.shell.menu", "sr6.shell.toolbar"], false),
        new(WorkflowDefinitionIds.CareerWorkbench, "Career Workbench", ["sr6.career.section"], true),
        new(WorkflowDefinitionIds.SelectionDialog, "Selection Dialog", ["sr6.selection.dialog"], false),
        new(WorkflowDefinitionIds.DiceTool, "Dice Tool", ["sr6.tool.dice"], false),
        new(WorkflowDefinitionIds.SessionDashboard, "Session Dashboard", ["sr6.session.summary"], true, true)
    ];

    public static readonly IReadOnlyList<WorkflowSurfaceDefinition> Surfaces =
    [
        new("sr6.shell.menu", WorkflowDefinitionIds.LibraryShell, WorkflowSurfaceKinds.ShellRegion, ShellRegionIds.MenuBar, WorkflowLayoutTokens.ShellFrame, ["file", "edit", "tools"]),
        new("sr6.shell.toolbar", WorkflowDefinitionIds.LibraryShell, WorkflowSurfaceKinds.ShellRegion, ShellRegionIds.ToolStrip, WorkflowLayoutTokens.ShellFrame, ["new_character", "open_character", "save_character"]),
        new("sr6.career.section", WorkflowDefinitionIds.CareerWorkbench, WorkflowSurfaceKinds.Workbench, ShellRegionIds.SectionPane, WorkflowLayoutTokens.CareerWorkbench, ["tab-info.summary", "tab-info.profile", "tab-skills.skills"]),
        new("sr6.selection.dialog", WorkflowDefinitionIds.SelectionDialog, WorkflowSurfaceKinds.Dialog, ShellRegionIds.DialogHost, WorkflowLayoutTokens.SelectionDialog, ["tab-gear.inventory"]),
        new("sr6.tool.dice", WorkflowDefinitionIds.DiceTool, WorkflowSurfaceKinds.Tool, ShellRegionIds.DialogHost, WorkflowLayoutTokens.ToolPanel, ["dice_roller"]),
        new("sr6.session.summary", WorkflowDefinitionIds.SessionDashboard, WorkflowSurfaceKinds.Dashboard, ShellRegionIds.SummaryHeader, WorkflowLayoutTokens.SessionDashboard, ["tab-info.summary", "tab-info.validate"])
    ];
}

public class Sr6NoOpRulesetCapabilityHost : IRulesetCapabilityHost
{
    private const string RuleErrorMessage = "SR6 rules engine is not implemented; this ruleset remains experimental.";

    public ValueTask<RulesetCapabilityInvocationResult> InvokeAsync(RulesetCapabilityInvocationRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<RulesetCapabilityDiagnostic> diagnostics = string.Equals(request.InvocationKind, RulesetCapabilityInvocationKinds.Script, StringComparison.Ordinal)
            ?
            [
                new(
                    "sr6.script.experimental",
                    $"SR6 script host is not implemented; script '{request.CapabilityId}' cannot be executed because the ruleset remains experimental.",
                    RulesetCapabilityDiagnosticSeverities.Error)
            ]
            :
            [
                new("sr6.rule.experimental", RuleErrorMessage, RulesetCapabilityDiagnosticSeverities.Error),
                new(
                    "sr6.rule.unavailable",
                    $"Rule '{request.CapabilityId}' cannot be evaluated until SR6 rule providers are implemented.",
                    RulesetCapabilityDiagnosticSeverities.Error)
            ];

        return ValueTask.FromResult(new RulesetCapabilityInvocationResult(
            Success: false,
            Output: null,
            Diagnostics: diagnostics));
    }
}

public class Sr6NoOpRulesetRuleHost : IRulesetRuleHost
{
    private readonly RulesetRuleHostCapabilityAdapter _adapter;

    public Sr6NoOpRulesetRuleHost()
        : this(new Sr6NoOpRulesetCapabilityHost())
    {
    }

    public Sr6NoOpRulesetRuleHost(IRulesetCapabilityHost capabilityHost)
    {
        _adapter = new RulesetRuleHostCapabilityAdapter(capabilityHost);
    }

    public ValueTask<RulesetRuleEvaluationResult> EvaluateAsync(RulesetRuleEvaluationRequest request, CancellationToken ct)
    {
        return _adapter.EvaluateAsync(request, ct);
    }
}

public class Sr6NoOpRulesetScriptHost : IRulesetScriptHost
{
    private readonly RulesetScriptHostCapabilityAdapter _adapter;

    public Sr6NoOpRulesetScriptHost()
        : this(new Sr6NoOpRulesetCapabilityHost())
    {
    }

    public Sr6NoOpRulesetScriptHost(IRulesetCapabilityHost capabilityHost)
    {
        _adapter = new RulesetScriptHostCapabilityAdapter(capabilityHost);
    }

    public ValueTask<RulesetScriptExecutionResult> ExecuteAsync(RulesetScriptExecutionRequest request, CancellationToken ct)
    {
        return _adapter.ExecuteAsync(request, ct);
    }
}
