using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;

namespace Chummer.Rulesets.Sr6;

public class Sr6RulesetPlugin : IRulesetPlugin
{
    public RulesetId Id { get; } = new(RulesetDefaults.Sr6);

    public string DisplayName => "Shadowrun 6";

    public IRulesetSerializer Serializer { get; } = new Sr6RulesetSerializer();

    public IRulesetShellDefinitionProvider ShellDefinitions { get; } = new Sr6RulesetShellDefinitionProvider();

    public IRulesetCatalogProvider Catalogs { get; } = new Sr6RulesetCatalogProvider();

    public IRulesetRuleHost Rules { get; } = new Sr6NoOpRulesetRuleHost();

    public IRulesetScriptHost Scripts { get; } = new Sr6NoOpRulesetScriptHost();
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
    public IReadOnlyList<WorkspaceSurfaceActionDefinition> GetWorkspaceActions()
    {
        return Sr6WorkspaceSurfaceActionCatalog.All;
    }

    public IReadOnlyList<DesktopUiControlDefinition> GetDesktopUiControls()
    {
        return Sr6DesktopUiControlCatalog.All;
    }
}

public class Sr6NoOpRulesetRuleHost : IRulesetRuleHost
{
    private const string ErrorMessage = "SR6 rules engine is not implemented; this ruleset remains experimental.";

    public ValueTask<RulesetRuleEvaluationResult> EvaluateAsync(RulesetRuleEvaluationRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new RulesetRuleEvaluationResult(
            Success: false,
            Outputs: new Dictionary<string, object?>(StringComparer.Ordinal),
            Messages:
            [
                ErrorMessage,
                $"Rule '{request.RuleId}' cannot be evaluated until SR6 rule providers are implemented."
            ]));
    }
}

public class Sr6NoOpRulesetScriptHost : IRulesetScriptHost
{
    public ValueTask<RulesetScriptExecutionResult> ExecuteAsync(RulesetScriptExecutionRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        string error = $"SR6 script host is not implemented; script '{request.ScriptId}' cannot be executed because the ruleset remains experimental.";

        return ValueTask.FromResult(new RulesetScriptExecutionResult(
            Success: false,
            Error: error,
            Outputs: new Dictionary<string, object?>(StringComparer.Ordinal)));
    }
}
