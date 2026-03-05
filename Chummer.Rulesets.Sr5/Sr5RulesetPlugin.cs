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
        return AppCommandCatalog.ForRuleset(RulesetDefaults.Sr5);
    }

    public IReadOnlyList<NavigationTabDefinition> GetNavigationTabs()
    {
        return NavigationTabCatalog.ForRuleset(RulesetDefaults.Sr5);
    }
}

public class Sr5RulesetCatalogProvider : IRulesetCatalogProvider
{
    public IReadOnlyList<WorkspaceSurfaceActionDefinition> GetWorkspaceActions()
    {
        return WorkspaceSurfaceActionCatalog.ForRuleset(RulesetDefaults.Sr5);
    }

    public IReadOnlyList<DesktopUiControlDefinition> GetDesktopUiControls()
    {
        return DesktopUiControlCatalog.ForRuleset(RulesetDefaults.Sr5);
    }
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
