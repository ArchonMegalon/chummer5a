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
    public IReadOnlyList<WorkspaceSurfaceActionDefinition> GetWorkspaceActions()
    {
        return Sr4WorkspaceSurfaceActionCatalog.All;
    }

    public IReadOnlyList<DesktopUiControlDefinition> GetDesktopUiControls()
    {
        return Sr4DesktopUiControlCatalog.All;
    }
}

public class Sr4NoOpRulesetRuleHost : IRulesetRuleHost
{
    private static readonly IReadOnlyList<string> Messages = ["SR4 rule host not configured; no-op evaluation applied."];

    public ValueTask<RulesetRuleEvaluationResult> EvaluateAsync(RulesetRuleEvaluationRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new RulesetRuleEvaluationResult(
            Success: true,
            Outputs: request.Inputs,
            Messages: Messages));
    }
}

public class Sr4NoOpRulesetScriptHost : IRulesetScriptHost
{
    public ValueTask<RulesetScriptExecutionResult> ExecuteAsync(RulesetScriptExecutionRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Dictionary<string, object?> outputs = new(StringComparer.Ordinal)
        {
            ["scriptId"] = request.ScriptId,
            ["mode"] = "noop",
            ["inputCount"] = request.Inputs.Count,
            ["rulesetId"] = RulesetDefaults.Sr4
        };

        return ValueTask.FromResult(new RulesetScriptExecutionResult(
            Success: true,
            Error: null,
            Outputs: outputs));
    }
}
